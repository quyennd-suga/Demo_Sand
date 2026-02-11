Shader "Sugame/URP/WaterFill"
{
    Properties
    {
        _SideColor ("Side Color (RGBA)", Color) = (0.2, 0.6, 1.0, 0.7)
        _TopColor  ("Top Color (RGBA)",  Color) = (0.6, 0.95, 1.0, 1.0)

        // NEW: Upper water color (just below the top band)
        _UpperWaterColor ("Upper Water Color (RGBA)", Color) = (0.35, 0.8, 1.0, 0.85)
        _UpperWaterDepth ("Upper Water Depth (World Units)", Range(0.001, 1.0)) = 0.25

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
                half3  positionOS : TEXCOORD0;
                half3  positionWS : TEXCOORD1;
            };

            half4 _SideColor;
            half4 _TopColor;

            // NEW
            half4 _UpperWaterColor;
            half  _UpperWaterDepth;

            half _WaterLevelY;
            half _WaveAmp;
            half _WaveFreq;
            half _WaveSpeed;
            half _TopBand;

            // Veins
            TEXTURE2D(_VeinTex);
            SAMPLER(sampler_VeinTex);

            half _VeinTiling;
            half _VeinStrength;
            half _VeinSpeed;
            half _VeinContrast;
            half _VeinDistort;

            // ----------------------------
            // Mobile-friendly wave (world)
            // ----------------------------
            half Wave_World(half3 posWS)
            {
                half t = _Time.y * _WaveSpeed;

                // Pre-normalized directions (avoid normalize per pixel)
                half2 dir1 = half2(0.8575h, 0.5145h);  // normalize(1,0.6)
                half2 dir2 = half2(-0.3162h, 0.9487h); // normalize(-0.4,1.2)

                half2 xz = posWS.xz;

                half w1 = sin(dot(xz, dir1) * _WaveFreq + t) * _WaveAmp;
                half w2 = sin(dot(xz, dir2) * (_WaveFreq * 1.6h) + t * 1.6h) * (_WaveAmp * 0.6h);

                half w = w1 + w2;

                // cheap chop
                half aw = abs(w);
                w += (aw * w) * 0.6h;

                return w;
            }

            // ----------------------------
            // Cheap contrast (no pow)
            // ----------------------------
            half CheapContrast(half v, half c)
            {
                // v: 0..1
                // c: 0.5..6
                // linear contrast around 0.5
                return saturate((v - 0.5h) * c + 0.5h);
            }

            half SampleVeins_Object(half3 posOS, half wave)
            {
                half2 uv = half2(posOS.x, posOS.y);

                // distortion by wave
                uv.x += wave * _VeinDistort;

                // tiling & scroll
                uv *= (_VeinTiling * 10.0h);
                uv.y += (_Time.y * _VeinSpeed) * 0.1h;

                half v = SAMPLE_TEXTURE2D(_VeinTex, sampler_VeinTex, uv).r;

                // cheap contrast instead of pow()
                v = CheapContrast(v, _VeinContrast);

                // remap to -1..1
                return (v - 0.5h) * 2.0h;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionOS = (half3)IN.positionOS.xyz;

                float3 ws = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionWS = (half3)ws;

                OUT.positionCS = TransformWorldToHClip(ws);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // wave in world space (rotation safe)
                half wave = Wave_World(IN.positionWS);

                // world-up surface
                half surfaceY = _WaterLevelY + wave;

                // clip by world y
                clip(surfaceY - IN.positionWS.y);

                // distance to surface
                half distToSurface = abs(surfaceY - IN.positionWS.y);

                // top band mask (distance to surface)
                half topBand = 1.0h - smoothstep(0.0h, _TopBand, distToSurface);

                half sideMask = 1.0h - topBand;

                // NEW: upper water mask (just below surface, does NOT affect top band)
                half upperMask = 1.0h - smoothstep(0.0h, _UpperWaterDepth, distToSurface);
                upperMask *= sideMask;

                // base color: side <-> top band
                half3 col = lerp(_SideColor.rgb, _TopColor.rgb, topBand);

                // NEW: tint upper water near surface
                col = lerp(col, _UpperWaterColor.rgb, upperMask);

                // veins on side only
                half veins = SampleVeins_Object(IN.positionOS, wave);
                col += veins * _VeinStrength * sideMask;

                // alpha
                half alpha = _SideColor.a;
                alpha = lerp(alpha, _UpperWaterColor.a, upperMask);
                alpha = max(alpha, _TopColor.a * topBand);

                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
}
