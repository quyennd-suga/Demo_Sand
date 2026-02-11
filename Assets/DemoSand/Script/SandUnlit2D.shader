Shader "Custom/SandUnlit2D"
{
    Properties
    {
        [Header(Main Texture)]
        _MainTex ("Sand Texture (RGB)", 2D) = "white" {}
        [HDR] _Color ("Color Tint", Color) = (1,1,1,1)
        
        [Header(Optional Height Gradient)]
        [Toggle] _UseHeightGradient ("Use Height Gradient", Float) = 0
        _GradientPower ("Gradient Power", Range(0,2)) = 1.0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Geometry"
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        LOD 100
        Cull Off
        ZWrite On

        Pass
        {
            Name "SandUnlit"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma shader_feature _USEHEIGHTGRADIENT_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float  _GradientPower;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Sample texture có màu sẵn
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                
                // Nhân với color tint (dùng để đổi màu runtime)
                half4 col = texColor * _Color;

                #ifdef _USEHEIGHTGRADIENT_ON
                    // Optional: làm tối/sáng theo chiều cao
                    float heightFactor = IN.uv.y;
                    float gradient = lerp(0.85, 1.15, pow(heightFactor, _GradientPower));
                    col.rgb *= gradient;
                #endif

                col.a = 1.0;
                return col;
            }
            ENDHLSL
        }
    }

    // Fallback cho Built-in render pipeline
    SubShader
    {
        Tags
        {
            "Queue" = "Geometry"
            "RenderType" = "Opaque"
        }

        LOD 100
        Cull Off
        ZWrite On

        Pass
        {
            Name "SandUnlitBuiltin"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma shader_feature _USEHEIGHTGRADIENT_ON

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                float4 color  : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos  : SV_POSITION;
                float2 uv   : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _GradientPower;

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 texColor = tex2D(_MainTex, i.uv);
                fixed4 col = texColor * _Color;

                #ifdef _USEHEIGHTGRADIENT_ON
                    float heightFactor = i.uv.y;
                    float gradient = lerp(0.85, 1.15, pow(heightFactor, _GradientPower));
                    col.rgb *= gradient;
                #endif

                col.a = 1.0;
                return col;
            }
            ENDCG
        }
    }

    Fallback "Sprites/Default"
}
