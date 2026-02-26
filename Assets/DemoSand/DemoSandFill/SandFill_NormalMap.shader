Shader "Sugame/URP/SandFill"
{
    Properties
    {
        [Header(Main)]
        _MainTex ("Sand Texture (RGB)", 2D) = "white" {}
        [HDR] _Color ("Color Tint", Color) = (0.92, 0.68, 0.38, 1.0)

        [Header(Fill Level)]
        _WaterLevelY ("Water Level Y (Local From Pivot)", Float) = 0

        [Header(Settle Animation)]
        _SettleAmp   ("Settle Amplitude", Range(0,0.05)) = 0.008
        _SettleFreq  ("Settle Frequency", Range(0,15)) = 3
        _SettleSpeed ("Settle Speed", Range(0,5)) = 0.25

        [Header(Texture Mapping)]
        _TexTiling ("Texture Tiling (World Space)", Range(0.1, 20)) = 2.0
        _SandScrollSpeed ("Sand Scroll Speed (Top-Down)", Range(0, 2)) = 0.15

        [Header(Visual)]
        _TopBand   ("Top Band Width (World Units)", Range(0.001, 0.25)) = 0.05
        _TopBright ("Top Brightness Boost", Range(1.0, 1.5)) = 1.1
        _EdgeAA    ("Surface Edge AA (World Units)", Range(0.0, 0.05)) = 0.008

        [Header(Optional Height Gradient)]
        [Toggle] _UseHeightGradient ("Use Height Gradient", Float) = 0
        _GradientPower ("Gradient Power", Range(0,2)) = 1.0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        Pass
        {
            Name "SandFill_Simple"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma shader_feature _USEHEIGHTGRADIENT_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4  _Color;
                float  _WaterLevelY;
                float  _SettleAmp;
                float  _SettleFreq;
                float  _SettleSpeed;
                float  _TexTiling;
                float  _SandScrollSpeed;
                float  _TopBand;
                float  _TopBright;
                float  _EdgeAA;
                float  _GradientPower;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float Settle_World(float3 posWS)
            {
                float t = _Time.y * _SettleSpeed;
                float2 dir1 = float2(0.8575, 0.5145);
                float2 dir2 = float2(-0.3162, 0.9487);
                float2 xz = posWS.xz;
                float s1 = sin(dot(xz, dir1) * _SettleFreq + t) * _SettleAmp;
                float s2 = sin(dot(xz, dir2) * (_SettleFreq * 1.2) + t * 0.8) * (_SettleAmp * 0.5);
                return s1 + s2;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                float3 ws = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionWS = ws;
                OUT.positionCS = TransformWorldToHClip(ws);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float settle = Settle_World(IN.positionWS);
                float objY = unity_ObjectToWorld._m13;
                float surfaceY = objY + _WaterLevelY + settle;
                float d = surfaceY - IN.positionWS.y;
                clip(d);
                float aa = max(fwidth(d), 0.0) + _EdgeAA;
                float topBand = 1.0 - smoothstep(0.0, _TopBand + aa, d);

                float3 objWS = (float3)unity_ObjectToWorld._m03_m13_m23;
                float3 anchoredWS = IN.positionWS - objWS;
                float2 uv = anchoredWS.xy;
                uv *= (_TexTiling * 0.1);
                uv.y -= _Time.y * _SandScrollSpeed;

                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                half3 col = texColor.rgb * _Color.rgb;
                col = lerp(col, col * _TopBright, topBand);

                #ifdef _USEHEIGHTGRADIENT_ON
                    float fillH = max(_WaterLevelY * 2.0, 0.01);
                    float depthNorm = saturate(d / fillH);
                    float heightFactor = 1.0 - depthNorm;
                    float gradient = lerp(0.85, 1.15, pow(max(heightFactor, 0.0001), max(_GradientPower, 0.0001)));
                    col *= gradient;
                #endif

                float alpha = lerp(_Color.a, min(1.0, _Color.a + 0.1), topBand);
                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
}
