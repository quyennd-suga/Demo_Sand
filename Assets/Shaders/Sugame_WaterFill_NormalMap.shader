Shader "Sugame/URP/WaterFill_NormalMap"
{
    Properties
    {
        _SideColor ("Side Color (RGBA)", Color) = (0.2, 0.6, 1.0, 0.7)
        _TopColor  ("Top Color (RGBA)",  Color) = (0.6, 0.95, 1.0, 1.0)

        _Fill ("Fill (0-1)", Range(0,1)) = 1
        _MinY ("Local Min Y", Float) = -0.5
        _MaxY ("Local Max Y", Float) = 0.5

        _WaveAmp   ("Wave Amp (Local Units)", Range(0,0.25)) = 0.035
        _WaveFreq  ("Wave Freq", Range(0,30)) = 10
        _WaveSpeed ("Wave Speed", Range(0,10)) = 1.2

        // ===========================
        // SIDE WATER VEINS
        // ===========================
        _VeinTex ("Side Vein Texture (R)", 2D) = "white" {}
        _VeinTiling ("Vein Tiling", Range(0.1, 10)) = 2.0
        _VeinStrength ("Vein Strength", Range(0, 1)) = 0.25
        _VeinSpeed ("Vein Scroll Speed", Range(0, 2)) = 0.35
        _VeinContrast ("Vein Contrast", Range(0.5, 6)) = 2.2
        _VeinDistort ("Vein Distort by Wave", Range(0, 0.2)) = 0.05

        // ===========================
        // SIDE NORMAL MAP
        // ===========================
        _SideNormalMap ("Side Normal Map", 2D) = "bump" {}
        _SideNormalStrength ("Side Normal Strength", Range(0,2)) = 1.0
        _SideNormalFadeToTop ("Side Normal Fade Near Top", Range(0,1)) = 0.7
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        LOD 200

        Pass
        {
            Name "MinimalLiquid3DWave"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS   : TEXCOORD2;
            };

            float4 _SideColor;
            float4 _TopColor;

            float _Fill;
            float _MinY;
            float _MaxY;

            float _WaveAmp;
            float _WaveFreq;
            float _WaveSpeed;

            // Veins
            TEXTURE2D(_VeinTex);
            SAMPLER(sampler_VeinTex);

            float _VeinTiling;
            float _VeinStrength;
            float _VeinSpeed;
            float _VeinContrast;
            float _VeinDistort;

            // Normal
            TEXTURE2D(_SideNormalMap);
            SAMPLER(sampler_SideNormalMap);

            float _SideNormalStrength;
            float _SideNormalFadeToTop;

            float Height01_Unclamped(float localY)
            {
                float denom = max(1e-5, (_MaxY - _MinY));
                return (localY - _MinY) / denom;
            }

            float Wave01(float3 posWS, float denom)
            {
                float t = _Time.y * _WaveSpeed;

                float2 dir1 = normalize(float2( 1.0,  0.6));
                float2 dir2 = normalize(float2(-0.4,  1.2));

                float2 xz = posWS.xz;

                float w1 = sin(dot(xz, dir1) * _WaveFreq + t) * _WaveAmp;
                float w2 = sin(dot(xz, dir2) * (_WaveFreq * 1.35) + t * 1.3) * (_WaveAmp * 0.6);

                float w = w1 + w2;
                w += (abs(w) * w) * 0.6;

                return w / denom;
            }

            // ✅ Generate shared UV for vein + normal
            float2 GetSideUV(float3 posWS, float wave)
            {
                float2 uv = float2(posWS.x, posWS.y);

                // ✅ same distortion as vein
                uv.x += wave * (1.0 + posWS.z * 0.2) * (1.0 + _VeinDistort);

                uv *= _VeinTiling;

                // ✅ same scroll as vein
                uv.y += _Time.y * _VeinSpeed;

                return uv;
            }

            float SampleVeins(float2 uv)
            {
                float v = SAMPLE_TEXTURE2D(_VeinTex, sampler_VeinTex, uv).r;
                v = pow(saturate(v), _VeinContrast);
                return (v - 0.5) * 2.0;
            }

            float3x3 BuildTBN(float3 N)
            {
                float3 up = abs(N.y) > 0.999 ? float3(1,0,0) : float3(0,1,0);
                float3 T = normalize(cross(up, N));
                float3 B = cross(N, T);
                return float3x3(T, B, N);
            }

            float3 SampleSideNormalWS(float2 uv, float3 N)
            {
                float4 nTex = SAMPLE_TEXTURE2D(_SideNormalMap, sampler_SideNormalMap, uv);
                float3 nTS = UnpackNormal(nTex);

                nTS.xy *= _SideNormalStrength;
                nTS = normalize(nTS);

                float3x3 TBN = BuildTBN(N);
                return normalize(mul(nTS, TBN));
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionOS = IN.positionOS.xyz;
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionCS = TransformWorldToHClip(OUT.positionWS);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float denom = max(1e-5, (_MaxY - _MinY));

                float h01_u = Height01_Unclamped(IN.positionOS.y);

                float wave = Wave01(IN.positionWS, denom);
                float fill01_u = _Fill + wave;

                if (h01_u > fill01_u) discard;

                float h01 = saturate(h01_u);
                float fill01 = saturate(fill01_u);

                // Surface band
                float topBandWidth = 0.08;
                float topBand = 1.0 - smoothstep(0.0, topBandWidth, abs(fill01 - h01));

                // Side only
                float sideMask = saturate(1.0 - topBand);

                // Base color
                float3 col = lerp(_SideColor.rgb, _TopColor.rgb, topBand);

                // ✅ shared UV
                float2 sideUV = GetSideUV(IN.positionWS, wave);

                // Veins
                float veins = SampleVeins(sideUV);
                col += veins * _VeinStrength * sideMask;

                // ✅ Normal uses same UV + same motion
                float3 N = normalize(IN.normalWS);

                float fadeToTop = saturate(1.0 - topBand * _SideNormalFadeToTop);
                float3 Nn = SampleSideNormalWS(sideUV, N);
                N = normalize(lerp(N, Nn, sideMask * fadeToTop));

                // Simple toon lighting
                float3 L = normalize(float3(0.35, 1.0, 0.25));
                float ndl = saturate(dot(N, L));
                ndl = smoothstep(0.25, 0.85, ndl);

                col *= lerp(0.75, 1.15, ndl);

                // Alpha
                float alpha = max(_SideColor.a, _TopColor.a * topBand);

                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
}
