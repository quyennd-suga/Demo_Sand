Shader "Custom/Mask"
{
    Properties
    {
        _StencilRef("Stencil Ref", Int) = 1
    }

    SubShader
    {
        // QUAN TRỌNG: Queue phải TRƯỚC Transparent để render trước Water Flow
        Tags { "Queue" = "Geometry+500" "RenderType" = "Opaque" }
        
        // Không vẽ màu, chỉ ghi stencil
        ColorMask 0
        ZWrite Off
        
        Pass
        {
            Stencil
            {
                Ref [_StencilRef]
                Comp Always
                Pass Replace
            }
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Không vẽ gì (ColorMask 0), chỉ ghi stencil
                return fixed4(0, 0, 0, 0);
            }
            ENDCG
        }
    }
}
