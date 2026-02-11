Shader "Sugame/URP/WaterFillTextured"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}

        // Fill plane in WORLD SPACE
        _WaterLevelY ("Water Level Y (World)", Float) = 0

        _WaveAmp   ("Wave Amp (World Units)", Range(0,0.25)) = 0.035
        _WaveFreq  ("Wave Freq", Range(0,30)) = 10
        _WaveSpeed ("Wave Speed", Range(0,10)) = 1.2

        // SIDE WATER VEINS
        _VeinTex ("Side Vein Texture (R)", 2D) = "white" {}
        _VeinTiling ("Vein Tiling", Range(0.1, 20)) = 2.0
        _VeinStrength ("Vein Strength", Range(0, 1)) = 0.25
        _VeinSpeed ("Vein Scroll Speed", Range(0, 2)) = 0.35
        _VeinContrast ("Vein Contrast (Cheap)", Range(0.5, 6)) = 2.2
        _VeinDistort ("Vein Distort by Wave", Range(0, 0.2)) = 0.05

        // Top band thickness in world units
        _TopBand ("Top Band Width (World Units)", Range(0.001, 0.25)) = 0.06
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        LOD 200

        Pass
        {
            Name "WaterFill_Textured_Mobile"
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
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half3  positionOS : TEXCOORD0;
                half3  positionWS : TEXCOORD1;
                half2  uv         : TEXCOORD2;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;

            half _WaterLevelY;
            half _WaveAmp;
            half _WaveFreq;
            half _WaveSpeed;
            half _TopBand;

            TEXTURE2D(_VeinTex);
            SAMPLER(sampler_VeinTex);

            half _VeinTiling;
            half _VeinStrength;
            half _VeinSpeed;
            half _VeinContrast;
            half _VeinDistort;

            half Wave_World(half3 posWS)
            {
                half t = _Time.y * _WaveSpeed;
                half2 dir1 = half2(0.8575h, 0.5145h);
                half2 dir2 = half2(-0.3162h, 0.9487h);
                half2 xz = posWS.xz;
                half w1 = sin(dot(xz, dir1) * _WaveFreq + t) * _WaveAmp;
                half w2 = sin(dot(xz, dir2) * (_WaveFreq * 1.6h) + t * 1.6h) * (_WaveAmp * 0.6h);
                half w = w1 + w2;
                half aw = abs(w);
                w += (aw * w) * 0.6h;
                return w;
            }

            half CheapContrast(half v, half c)
            {
                return saturate((v - 0.5h) * c + 0.5h);
            }

            half SampleVeins_Object(half3 posOS, half wave)
            {
                half2 uv = half2(posOS.x, posOS.y);
                uv.x += wave * _VeinDistort;
                uv *= (_VeinTiling * 10.0h);
                uv.y += (_Time.y * _VeinSpeed) * 0.1h;
                half v = SAMPLE_TEXTURE2D(_VeinTex, sampler_VeinTex, uv).r;
                v = CheapContrast(v, _VeinContrast);
                return (v - 0.5h) * 2.0h;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionOS = (half3)IN.positionOS.xyz;
                float3 ws = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionWS = (half3)ws;
                OUT.positionCS = TransformWorldToHClip(ws);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half wave = Wave_World(IN.positionWS);
                half surfaceY = _WaterLevelY + wave;
                clip(surfaceY - IN.positionWS.y);

                half distToSurface = abs(surfaceY - IN.positionWS.y);
                half topBand = 1.0h - smoothstep(0.0h, _TopBand, distToSurface);
                half sideMask = 1.0h - topBand;

                // Wave distortion on texture UVs
                half2 waveUV = IN.uv;
                waveUV.x += wave * 0.5h;
                waveUV.y += wave * 0.3h;

                half4 texCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, waveUV);

                half3 col = texCol.rgb;

                // Top band: brighten texture slightly
                col += topBand * 0.15h;

                // Veins on side only
                half veins = SampleVeins_Object(IN.positionOS, wave);
                col += veins * _VeinStrength * sideMask;

                half alpha = texCol.a;

                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
}
