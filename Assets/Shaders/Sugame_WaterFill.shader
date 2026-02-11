Shader "Sugame/URP/WaterFill"
{
    Properties
    {
        _SideColor ("Side Color (RGBA)", Color) = (0.2, 0.6, 1.0, 0.7)
        _TopColor  ("Top Color (RGBA)",  Color) = (0.6, 0.95, 1.0, 1.0)

        _WaterLevelY ("Water Level Y (Local From Pivot)", Float) = 0

        _WaveAmp   ("Wave Amp (World Units)", Range(0,0.25)) = 0.035
        _WaveFreq  ("Wave Freq", Range(0,30)) = 10
        _WaveSpeed ("Wave Speed", Range(0,10)) = 1.2

        _VeinTex ("Side Vein Texture (R)", 2D) = "white" {}
        _VeinTiling ("Vein Tiling", Range(0.1, 20)) = 2.0
        _VeinStrength ("Vein Strength", Range(0, 1)) = 0.25
        _VeinSpeed ("Vein Scroll Speed", Range(0, 2)) = 0.35
        _VeinContrast ("Vein Contrast (Cheap)", Range(0.5, 6)) = 2.2
        _VeinDistort ("Vein Distort by Wave", Range(0, 1)) = 0.05

        _TopBand ("Top Band Width (World Units)", Range(0.001, 0.25)) = 0.06

        // NEW: Anti-alias strength for the water surface edge
        _EdgeAA ("Surface Edge AA (World Units)", Range(0.0, 0.05)) = 0.01
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        LOD 200

        Pass
        {
            Name "WaterFill_WorldUp_Mobile"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0; // IMPORTANT: float, not half
            };

            half4 _SideColor;
            half4 _TopColor;

            float _WaterLevelY;
            float _WaveAmp;
            float _WaveFreq;
            float _WaveSpeed;
            float _TopBand;
            float _EdgeAA;

            TEXTURE2D(_VeinTex);
            SAMPLER(sampler_VeinTex);

            float _VeinTiling;
            float _VeinStrength;
            float _VeinSpeed;
            float _VeinContrast;
            float _VeinDistort;

            float Wave_World(float3 posWS)
            {
                float t = _Time.y * _WaveSpeed;

                float2 dir1 = float2(0.8575, 0.5145);
                float2 dir2 = float2(-0.3162, 0.9487);

                float2 xz = posWS.xz;

                float w1 = sin(dot(xz, dir1) * _WaveFreq + t) * _WaveAmp;
                float w2 = sin(dot(xz, dir2) * (_WaveFreq * 1.6) + t * 1.6) * (_WaveAmp * 0.6);

                float w = w1 + w2;

                float aw = abs(w);
                w += (aw * w) * 0.6;

                return w;
            }

            float CheapContrast(float v, float c)
            {
                return saturate((v - 0.5) * c + 0.5);
            }

            float SampleVeins_WorldAnchored(float3 posWS, float wave)
            {
                float3 objWS = (float3)unity_ObjectToWorld._m03_m13_m23;
                float3 anchoredWS = posWS - objWS;

                float2 uv = anchoredWS.xy;

                uv.y += wave * _VeinDistort;

                uv *= (_VeinTiling * 0.1);
                uv.x -= (_Time.y * _VeinSpeed) * 0.1;

                float v = SAMPLE_TEXTURE2D(_VeinTex, sampler_VeinTex, uv).r;
                v = CheapContrast(v, _VeinContrast);

                return (v - 0.5) * 2.0;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 ws = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionWS = ws;
                OUT.positionCS = TransformWorldToHClip(ws);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float wave = Wave_World(IN.positionWS);

                float objY = unity_ObjectToWorld._m13;
                float surfaceY = objY + _WaterLevelY + wave;

                // signed distance: >0 means below surface
                float d = surfaceY - IN.positionWS.y;

                // hard clip (still ok)
                clip(d);

                // Anti-alias width in world units:
                // fwidth(d) gives pixel footprint in world space, add user control _EdgeAA
                float aa = max(fwidth(d), 0.0) + _EdgeAA;

                // Top band only below surface (no abs)
                float topBand = 1.0 - smoothstep(0.0, _TopBand + aa, d);

                float sideMask = 1.0 - topBand;

                float3 col = lerp(_SideColor.rgb, _TopColor.rgb, topBand);

                float veins = SampleVeins_WorldAnchored(IN.positionWS, wave);
                col += veins * _VeinStrength * sideMask;

                // alpha: blend theo band thay vì max (giảm “gãy” ở mép)
                float alpha = lerp(_SideColor.a, _TopColor.a, topBand);

                return half4((half3)col, (half)alpha);
            }
            ENDHLSL
        }
    }
}
