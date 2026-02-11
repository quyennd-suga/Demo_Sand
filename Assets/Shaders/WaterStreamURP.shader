Shader "Game/WaterStream_URP"
{
    Properties
    {
        _Color("Color", Color) = (0.2,0.7,1,0.8)
        _MainTex("MainTex (Detail/Mask)", 2D) = "white" {}

        _TexTiling("Tex Tiling (X=along, Y=across)", Vector) = (1, 1, 0, 0)
        _TexContrast("Tex Contrast", Range(0.5, 3.0)) = 1.4

        _FlowSpeed("Flow Speed", Float) = 2.0
        _Alpha("Alpha", Range(0,1)) = 0.85

        // Head / Fringe
        _FringeAmp("Fringe Amp", Range(0,0.5)) = 0.22
        _FringeFreq("Fringe Freq", Float) = 28.0
        _FringeSpeed("Fringe Speed", Float) = 10.0

        _TasselCount("Tassel Count", Range(4, 30)) = 14

        // Length window
        _Cutoff01("Head Cut 0..1 (to reveal)", Range(0,1)) = 1.0
        _TailCut01("Tail Cut 0..1 (from retract)", Range(0,1)) = 0.0

        _SoftAdd("Soft Add (0=alpha,1=softadd)", Range(0,1)) = 0.0

        // Detail (not hue)
        _DetailStrength("Detail Strength", Range(0, 1)) = 0.25
        _DetailAffect("Detail Affect (0=Brightness, 1=Alpha)", Range(0,1)) = 0.0
        _DetailAlphaMin("Detail Alpha Min", Range(0,1)) = 0.7
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "RenderPipeline"="UniversalPipeline"
        }

        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }

            Cull Off
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;

                float4 _TexTiling;
                half _TexContrast;

                half _FlowSpeed;
                half _Alpha;

                half _FringeAmp;
                half _FringeFreq;
                half _FringeSpeed;

                half _TasselCount;

                half _Cutoff01;
                half _TailCut01;
                half _SoftAdd;

                half _DetailStrength;
                half _DetailAffect;
                half _DetailAlphaMin;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            half Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 34.345);
                return frac(p.x * p.y);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half CheapContrast(half v, half c)
            {
                return saturate((v - 0.5h) * c + 0.5h);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;
                float t = _Time.y;

                // hard silhouette across width
                float widthMask = step(0.01, uv.y) * step(uv.y, 0.99);

                // tassels/fringe at head
                float count = max(4.0, _TasselCount);
                float y = saturate(uv.y);

                float tid = floor(y * count);
                float fy  = frac(y * count);

                half r0 = Hash21(float2(tid, tid * 1.31));
                half r1 = Hash21(float2(tid * 2.17, tid * 0.73));

                float strandCore = smoothstep(0.48, 0.42, abs(fy - 0.5));
                strandCore = strandCore * strandCore;

                float wave =
                    sin(y * _FringeFreq + t * _FringeSpeed + r0 * 6.2831)
                    * _FringeAmp;

                float jitter =
                    (Hash21(float2(y * 21.0, t)) - 0.5)
                    * 0.06;

                float lenVar = (r1 - 0.35) * 0.10;

                // head is allowed to be "wiggly" but still clamped by 0..1-ish usage
                float headEdge =
                    _Cutoff01
                    + (wave + jitter + lenVar)
                    + strandCore * 0.05;

                // reveal (head)
                float headMask = step(uv.x, headEdge);
                // retract from start (tail)
                float tailMask = step(_TailCut01, uv.x);
                // final visible segment
                float segMask = headMask * tailMask;

                // flow uv
                float2 flowUV = uv * _TexTiling.xy;
                flowUV.x -= t * _FlowSpeed;

                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, flowUV);

                // grayscale only (avoid hue shift)
                half texGray = dot(tex.rgb, half3(0.299h, 0.587h, 0.114h));
                texGray = CheapContrast(texGray, _TexContrast);

                half3 baseCol = _Color.rgb;

                // detail in -1..1
                half detail = (texGray - 0.5h) * 2.0h;

                // alpha base
                half a = _Alpha * _Color.a;
                a *= widthMask * segMask;

                // brightness mode
                half3 colBrightness = saturate(baseCol + detail * _DetailStrength);

                // alpha mode
                half alphaMask = lerp(1.0h, lerp(_DetailAlphaMin, 1.0h, texGray), _DetailStrength);
                half aAlpha = a * alphaMask;

                // select mode: 0=brightness, 1=alpha
                half3 col = lerp(colBrightness, baseCol, _DetailAffect);
                a = lerp(a, aAlpha, _DetailAffect);

                half4 outCol = half4(col, a);

                // keep your softadd option
                outCol.rgb = lerp(outCol.rgb, outCol.rgb * outCol.a + outCol.rgb, _SoftAdd);

                return outCol;
            }
            ENDHLSL
        }
    }
}
