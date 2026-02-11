Shader "Custom/Water_Flow_NoStencil_WaterFillStyle"
{
    Properties
    {
        // WaterFill style colors (default values giống WaterFill)
        _SideColor ("Side Color (RGBA)", Color) = (0.2, 0.6, 1.0, 0.7)
        _TopColor  ("Top Color (RGBA)",  Color) = (0.6, 0.95, 1.0, 1.0)
        
        // Fill control (giữ logic cũ)
        _FillAmount("Fill Amount", Range(0,1)) = 1
        _MinValue("Min Y", Float) = -1
        _MaxValue("Max Y", Float) = 1
        
        // Wave settings (default values giống WaterFill)
        _WaveAmp   ("Wave Amp (World Units)", Range(0,0.25)) = 0.028
        _WaveFreq  ("Wave Freq", Range(0,30)) = 5.6
        _WaveSpeed ("Wave Speed", Range(0,10)) = 3.04
        
        // Vein texture (default values giống WaterFill)
        _VeinTex ("Side Vein Texture (R)", 2D) = "white" {}
        _VeinTiling ("Vein Tiling", Range(0.1, 20)) = 7.5
        _VeinStrength ("Vein Strength", Range(0, 1)) = 0.14
        _VeinSpeed ("Vein Scroll Speed", Range(0, 2)) = 0.46
        _VeinContrast ("Vein Contrast (Cheap)", Range(0.5, 6)) = 0.76
        _VeinDistort ("Vein Distort by Wave", Range(0, 1)) = 0.602
        
        // Top band (default values giống WaterFill)
        _TopBand ("Top Band Width (World Units)", Range(0.001, 0.25)) = 0.08
        _EdgeAA ("Surface Edge AA (World Units)", Range(0.0, 0.05)) = 0.01
        
        // Blend control
        _BlendStrength("Blend Strength", Range(0,1)) = 1.0
    }

    SubShader
    {
        // Render trước WaterFill một chút để không đè lên
        // WaterFill ở 3000, Water_Flow ở 2999
        Tags { "Queue" = "Transparent-1" "RenderType" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            // WaterFill style properties
            float4 _SideColor;
            float4 _TopColor;
            
            // Fill control
            float _FillAmount;
            float _MinValue;
            float _MaxValue;
            
            // Wave
            float _WaveAmp;
            float _WaveFreq;
            float _WaveSpeed;
            
            // Vein
            sampler2D _VeinTex;
            float _VeinTiling;
            float _VeinStrength;
            float _VeinSpeed;
            float _VeinContrast;
            float _VeinDistort;
            
            // Top band
            float _TopBand;
            float _EdgeAA;
            
            float _BlendStrength;

            struct appdata {
                float4 vertex : POSITION;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float3 posOS : TEXCOORD0;
                float3 posWS : TEXCOORD1;
            };

            // Wave function (giống WaterFill)
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

            // Vein sampling (giống WaterFill)
            float SampleVeins_WorldAnchored(float3 posWS, float wave)
            {
                float3 objWS = float3(unity_ObjectToWorld._m03, unity_ObjectToWorld._m13, unity_ObjectToWorld._m23);
                float3 anchoredWS = posWS - objWS;

                float2 uv = anchoredWS.xy;
                uv.y += wave * _VeinDistort;
                uv *= (_VeinTiling * 0.1);
                uv.x -= (_Time.y * _VeinSpeed) * 0.1;

                float v = tex2D(_VeinTex, uv).r;
                v = CheapContrast(v, _VeinContrast);

                return (v - 0.5) * 2.0;
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.posOS = v.vertex.xyz;
                o.posWS = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // ========== FILL LOGIC (giữ nguyên) ==========
                float localY = i.posOS.y;
                float normalizedY = (localY - _MinValue) / (_MaxValue - _MinValue);
                float uvProgress = 1.0 - normalizedY;
                
                // Wave cho fill line
                float wave = Wave_World(i.posWS);
                float fillLine = _FillAmount;
                
                // Tính khoảng cách từ fill line
                float diff = fillLine - uvProgress + wave * 0.5;
                
                // Soft edge
                float alpha = smoothstep(0.0, 0.15, diff);
                if (alpha <= 0.01) discard;
                
                // Distance từ surface (để tính top band)
                float surfaceDist = fillLine - uvProgress;
                
                // ========== WATERFILL STYLE VISUAL ==========
                
                // Top band: sáng hơn ở gần mặt nước
                float topBand = 1.0 - smoothstep(0.0, _TopBand, abs(surfaceDist));
                float sideMask = 1.0 - topBand;
                
                // Blend SideColor và TopColor
                float3 col = lerp(_SideColor.rgb, _TopColor.rgb, topBand);
                
                // Vein texture (chỉ ở side, không ở top)
                float veins = SampleVeins_WorldAnchored(i.posWS, wave);
                col += veins * _VeinStrength * sideMask;
                
                // Alpha blend giữa side và top
                float finalAlpha = lerp(_SideColor.a, _TopColor.a, topBand);
                finalAlpha *= alpha * _BlendStrength;
                
                return float4(col, finalAlpha);
            }
            ENDCG
        }
    }
}
