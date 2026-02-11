Shader "Game/WaterStream_URP_StaticMesh"
{
    Properties
    {
        _Color("Color", Color) = (0.2,0.7,1,0.8)
        _MainTex("MainTex (Detail/Mask)", 2D) = "white" {}

        _TexTiling("Tex Tiling (X=along, Y=across)", Vector) = (1, 1, 0, 0)
        _TexContrast("Tex Contrast", Range(0.5, 3.0)) = 1.4

        _FlowSpeed("Flow Speed", Float) = 2.0
        _Alpha("Alpha", Range(0,1)) = 0.85

        // Length window
        _HeadCut01("Head Cut 0..1 (reveal)", Range(0,1)) = 1.0
        _TailCut01("Tail Cut 0..1 (retract)", Range(0,1)) = 0.0

        _SoftAdd("Soft Add (0=alpha,1=softadd)", Range(0,1)) = 0.0

        // Detail (not hue)
        _DetailStrength("Detail Strength", Range(0, 1)) = 0.25
        _DetailAffect("Detail Affect (0=Brightness, 1=Alpha)", Range(0,1)) = 0.0
        _DetailAlphaMin("Detail Alpha Min", Range(0,1)) = 0.7

        // ====== NEW: Edge fringe & distort (Head) ======
        _HeadFringeWidth("Head Fringe Width (UV.x)", Range(0.0, 0.35)) = 0.14
        _HeadFeather("Head Feather (soft alpha)", Range(0.0, 0.25)) = 0.06
        _HeadFringeAmp("Head Fringe Amp", Range(0,0.35)) = 0.18
        _HeadFringeFreq("Head Fringe Freq", Range(1,60)) = 28
        _HeadFringeSpeed("Head Fringe Speed", Range(0,20)) = 10

        // ====== NEW: Edge fringe & distort (Tail / retract) ======
        _TailFringeWidth("Tail Fringe Width (UV.x)", Range(0.0, 0.35)) = 0.16
        _TailFeather("Tail Feather (soft alpha)", Range(0.0, 0.25)) = 0.08
        _TailFringeAmp("Tail Fringe Amp", Range(0,0.35)) = 0.16
        _TailFringeFreq("Tail Fringe Freq", Range(1,60)) = 22
        _TailFringeSpeed("Tail Fringe Speed", Range(0,20)) = 9

        // Strand breakup along width (more realistic tassels)
        _StrandCount("Strand Count", Range(4, 40)) = 16
        _StrandSharpness("Strand Sharpness", Range(0.5, 6.0)) = 2.4
        _StrandExtend("Strand Extend", Range(0, 0.20)) = 0.06

        // Distort UV for detail texture near edges
        _EdgeUVWarp("Edge UV Warp Strength", Range(0, 0.2)) = 0.08
        _EdgeUVWarpWidth("Edge UV Warp Width", Range(0, 0.35)) = 0.18
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

                half _HeadCut01;
                half _TailCut01;
                half _SoftAdd;

                half _DetailStrength;
                half _DetailAffect;
                half _DetailAlphaMin;

                half _HeadFringeWidth;
                half _HeadFeather;
                half _HeadFringeAmp;
                half _HeadFringeFreq;
                half _HeadFringeSpeed;

                half _TailFringeWidth;
                half _TailFeather;
                half _TailFringeAmp;
                half _TailFringeFreq;
                half _TailFringeSpeed;

                half _StrandCount;
                half _StrandSharpness;
                half _StrandExtend;

                half _EdgeUVWarp;
                half _EdgeUVWarpWidth;
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

            // stable hash + value noise-ish
            half Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 34.345);
                return frac(p.x * p.y);
            }

            // Smooth hash: interpolate between cell values for no hard boundaries
            half SmoothHash(float x, float seed)
            {
                float i = floor(x);
                float f = frac(x);
                f = f * f * (3.0 - 2.0 * f);
                half a = Hash21(float2(i, seed));
                half b = Hash21(float2(i + 1.0, seed));
                return lerp(a, b, f);
            }

            half CheapContrast(half v, half c)
            {
                return saturate((v - 0.5h) * c + 0.5h);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;
                float t = _Time.y;

                float head = saturate(_HeadCut01);
                float tail = saturate(_TailCut01);

                // -------- Regions near edges (0..1)
                // headRegion: 1 near head, 0 away
                float headRegion = saturate((uv.x - (head - _HeadFringeWidth)) / max(1e-5, _HeadFringeWidth));
                // tailRegion: 1 near tail, 0 away
                float tailRegion = saturate(((tail + _TailFringeWidth) - uv.x) / max(1e-5, _TailFringeWidth));

                // -------- Strand breakup along width (smooth – no banding)
                float count = max(4.0, _StrandCount);
                float y = saturate(uv.y);

                // Smooth random offsets (interpolated, no floor discontinuity)
                half r0 = SmoothHash(y * count, 1.31);
                half r1 = SmoothHash(y * count * 2.17, 0.73);

                // Smooth strand pattern using sine instead of floor/frac
                float strandWave = sin(y * count * 3.14159);
                float strandCore = strandWave * strandWave;
                strandCore = pow(strandCore, max(0.5, _StrandSharpness * 0.4));

                // -------- Head wiggle (tua rua)
                float headWave =
                    sin(y * _HeadFringeFreq + t * _HeadFringeSpeed + r0 * 6.2831)
                    * _HeadFringeAmp;

                float headJitter =
                    (Hash21(float2(y * 21.0, t)) - 0.5)
                    * 0.06;

                float headLenVar = (r1 - 0.35) * 0.10;

                float headEdgeWiggle = head
                    + (headWave + headJitter + headLenVar)
                    + strandCore * _StrandExtend;

                // -------- Tail wiggle (distort khi retract)
                // dùng phase khác để tail “rút” nhìn turbulent hơn
                float tailWave =
                    sin(y * _TailFringeFreq + t * _TailFringeSpeed + r1 * 6.2831)
                    * _TailFringeAmp;

                float tailJitter =
                    (Hash21(float2(y * 17.0, t * 1.13)) - 0.5)
                    * 0.06;

                float tailLenVar = (r0 - 0.35) * 0.10;

                float tailEdgeWiggle = tail
                    + (tailWave + tailJitter + tailLenVar)
                    - strandCore * (_StrandExtend * 0.85); // ngược dấu để nhìn như bị kéo rút

                // -------- Base window masks (smooth – no step jaggies)
                float featherPx = fwidth(uv.x) * 2.0; // 2-pixel smooth edge

                float headMaskRigid  = smoothstep(0.0, featherPx, head - uv.x);
                float tailMaskRigid  = smoothstep(0.0, featherPx, uv.x - tail);

                // -------- Wiggly edge masks (smooth)
                float headMaskWiggle = smoothstep(0.0, featherPx, headEdgeWiggle - uv.x);
                float tailMaskWiggle = smoothstep(0.0, featherPx, uv.x - tailEdgeWiggle);

                // Blend rigid<->wiggle only near regions
                float headMask = lerp(headMaskRigid, headMaskWiggle, headRegion);
                float tailMask = lerp(tailMaskRigid, tailMaskWiggle, tailRegion);

                // -------- Feather alpha at both ends (soft edge)
                // head feather: fades out near head boundary
                float headFeather = 1.0;
                if (_HeadFeather > 1e-5)
                {
                    // distance from head boundary (rigid head)
                    float dH = (head - uv.x);
                    headFeather = saturate(dH / _HeadFeather);
                    // make it smoother
                    headFeather = headFeather * headFeather * (3.0 - 2.0 * headFeather);
                    // only apply in head region
                    headFeather = lerp(1.0, headFeather, headRegion);
                }

                // tail feather: fades out near tail boundary
                float tailFeather = 1.0;
                if (_TailFeather > 1e-5)
                {
                    float dT = (uv.x - tail);
                    tailFeather = saturate(dT / _TailFeather);
                    tailFeather = tailFeather * tailFeather * (3.0 - 2.0 * tailFeather);
                    tailFeather = lerp(1.0, tailFeather, tailRegion);
                }

                float segMask = headMask * tailMask;

                // -------- UV warp near edges to make texture turbulent at head/tail
                float edgeRegion = saturate(max(headRegion, tailRegion));
                float edgeWarpRegion = saturate(edgeRegion * ( _EdgeUVWarpWidth > 1e-5 ? 1.0 : 0.0 ));

                // a cheap warp signal (per y + time + per strand randomness)
                float warpSignal =
                    sin((y * 12.0 + r0 * 3.0) + t * 6.0)
                    + (Hash21(float2(y * 9.0, t * 0.8)) - 0.5) * 0.8;

                float2 flowUV = uv * _TexTiling.xy;
                flowUV.x -= t * _FlowSpeed;

                // warp mainly in V (across) and a bit in U (along)
                float warp = warpSignal * _EdgeUVWarp * edgeWarpRegion;
                flowUV.y += warp;
                flowUV.x += warp * 0.35;

                // -------- Sample detail texture (no hue)
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, flowUV);

                half texGray = dot(tex.rgb, half3(0.299h, 0.587h, 0.114h));
                texGray = CheapContrast(texGray, _TexContrast);

                half3 baseCol = _Color.rgb;

                // detail -1..1
                half detail = (texGray - 0.5h) * 2.0h;

                // alpha base
                half a = _Alpha * _Color.a;
                a *= segMask;
                a *= headFeather * tailFeather;

                // brightness mode
                half3 colBrightness = saturate(baseCol + detail * _DetailStrength);

                // alpha mode
                half alphaMask = lerp(1.0h, lerp(_DetailAlphaMin, 1.0h, texGray), _DetailStrength);
                half aAlpha = a * alphaMask;

                // select mode: 0=brightness, 1=alpha
                half3 col = lerp(colBrightness, baseCol, _DetailAffect);
                a = lerp(a, aAlpha, _DetailAffect);

                half4 outCol = half4(col, a);

                // softadd option
                outCol.rgb = lerp(outCol.rgb, outCol.rgb * outCol.a + outCol.rgb, _SoftAdd);

                return outCol;
            }
            ENDHLSL
        }
    }
}
