Shader "Custom/Water_Flow"
{
    Properties
    {
        _WaterColor("Water Color", Color) = (1,0.2,0.2,1)
        _FillAmount("Fill Amount", Range(0,1)) = 1
        
        [KeywordEnum(Y, X, Combined)] _FillMode("Fill Mode", Float) = 0

        _FillDir("Fill Direction", Vector) = (0,0,1,0)
        _MinValue("Min", Float) = -1
        _MaxValue("Max", Float) = 1

        _BorderColor("Border Color", Color) = (1,0.2,0.2,1)
        _BorderIntensity("Border Tint Intensity", Range(0,1)) = 0.45

        _HighlightColor("Highlight Color", Color) = (1,1,1,1)
        _HighlightIntensity("Highlight Intensity", Float) = 0.8
        _HighlightWidth("Highlight Width", Float) = 0.1

        _FresnelStrength("Fresnel Strength", Float) = 0.5
        _FresnelPower("Fresnel Power", Float) = 3.0

        _DepthStrength("Depth Darkening", Float) = 0.35

        _WaveScale("Wave Scale", Float) = 0.01
        _WaveSpeed("Wave Speed", Float) = 1.0

        _SpecularIntensity("Specular Intensity", Float) = 0
        _SpecularSize("Specular Size", Float) = 0
        
        // Blend control
        _BlendStrength("Blend Strength with WaterFill", Range(0,1)) = 1.0
    }

    SubShader
    {
        // Render TRƯỚC WaterFill để không bị mờ
        Tags { "Queue" = "Transparent-5" "RenderType" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            // Stencil test: chỉ render ở vùng có stencil = 1 (được mask ghi)
            Stencil
            {
                Ref 1
                Comp Equal
            }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _WaterColor;
            float _FillAmount;
            float _FillMode;

            float3 _FillDir;
            float _MinValue;
            float _MaxValue;

            float4 _BorderColor;
            float _BorderIntensity;

            float4 _HighlightColor;
            float _HighlightIntensity;
            float _HighlightWidth;

            float _FresnelStrength;
            float _FresnelPower;

            float _DepthStrength;

            float _WaveScale;
            float _WaveSpeed;

            float _SpecularIntensity;
            float _SpecularSize;
            
            float _BlendStrength;

            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float3 posOS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 viewDirWS : TEXCOORD2;
                float2 uv : TEXCOORD3;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);

                o.posOS = v.vertex.xyz;
                o.normalWS = UnityObjectToWorldNormal(v.normal);

                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.viewDirWS = normalize(_WorldSpaceCameraPos - worldPos);
                
                o.uv = v.uv;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Sử dụng local position Y để fill từ trên xuống
                float localY = i.posOS.y;
                
                // Normalize Y position về range 0-1
                float normalizedY = (localY - _MinValue) / (_MaxValue - _MinValue);
                
                // Đảo ngược để fill từ trên xuống: 1 = trên (miệng ống), 0 = dưới
                float uvProgress = 1.0 - normalizedY;
                
                // Map _FillAmount (0->1) sang position space
                float fillLine = _FillAmount;

                // Waves (tiny surface motion)
                float wave =
                    sin(i.posOS.x * 6 + _Time.y * _WaveSpeed) * _WaveScale * 0.6 +
                    cos(i.posOS.z * 7 + _Time.y * _WaveSpeed * 0.8) * _WaveScale * 0.4;

                // Tính khoảng cách từ fill line đến vị trí hiện tại
                float diff = fillLine - uvProgress + wave;

                // Soft edge với gradient dài hơn để blend mượt với WaterFill
                float alpha = smoothstep(0.0, 0.15, diff);
                if (alpha <= 0.01) discard;

                float surfaceDist = fillLine - uvProgress;

                // ============ 1. BASE COLOR ============
                float3 col = _WaterColor.rgb;

                // ============ 2. DEPTH GRADIENT ============
                float d = saturate(surfaceDist * 1.5);
                float depthDark = lerp(1.0, 1.0 - _DepthStrength, d);
                col *= depthDark;

                // ============ 3. SURFACE HIGHLIGHT ============
                float surfaceHighlight = exp(-pow(surfaceDist / _HighlightWidth, 2.0));
                col += _HighlightColor.rgb * surfaceHighlight * _HighlightIntensity;

                // ============ 4. SPECULAR ============
                float spec = exp(-pow(length(i.posOS.xz) / _SpecularSize, 2.0));
                col += spec * _SpecularIntensity;

                // ============ 5. FRESNEL RIM LIGHT ============
                float fres = pow(1.0 - saturate(dot(i.normalWS, i.viewDirWS)), _FresnelPower);
                col = lerp(col, _BorderColor.rgb * 1.2, fres * _FresnelStrength);

                // ============ 6. BORDER TINT ============
                col = lerp(col, _BorderColor.rgb, _BorderIntensity);

                // ============ 7. BRIGHTNESS ============
                col = pow(col, 0.75);

                float waveScale = _WaveScale;
                float highlightWidth = _HighlightWidth;

                if (_FillAmount <= 0 || _FillAmount >= 1)
                {
                    highlightWidth = 0;
                    waveScale = 0;
                }
                else
                {
                    highlightWidth = 0.1;
                    waveScale = 0.05;
                }
                
                // Alpha đầy đủ để không bị mờ hơn WaterFill
                // Chỉ giảm nhẹ ở edge để blend mượt
                float blendAlpha = _WaterColor.a * alpha * _BlendStrength;

                return float4(col, blendAlpha);
            }
            ENDCG
        }
    }
}