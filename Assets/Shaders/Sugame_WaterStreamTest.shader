Shader "Game/WaterStream_URP_StaticMesh_NoUV"
{
    Properties
    {
        _Color("Color", Color) = (0.2,0.7,1,0.8)
        _MainTex("MainTex (Detail/Mask)", 2D) = "white" {}

        _TexTiling("Tex Tiling (X=along, Y=across)", Vector) = (1, 1, 0, 0)
        _TexContrast("Tex Contrast", Range(0.5, 3.0)) = 1.4

        _FlowSpeed("Flow Speed (units/sec)", Float) = 2.0
        _Alpha("Alpha", Range(0,1)) = 0.85

        _HeadCut01("Head Cut 0..1", Range(0,1)) = 1.0
        _TailCut01("Tail Cut 0..1", Range(0,1)) = 0.0

        _SoftAdd("Soft Add", Range(0,1)) = 0.0

        _DetailStrength("Detail Strength", Range(0, 1)) = 0.25
        _DetailAffect("Detail Affect", Range(0,1)) = 0.0
        _DetailAlphaMin("Detail Alpha Min", Range(0,1)) = 0.7

        _HeadFringeWidth("Head Fringe Width", Range(0, 0.35)) = 0.14
        _HeadFeather("Head Feather", Range(0, 0.25)) = 0.06
        _HeadFringeAmp("Head Fringe Amp", Range(0,0.35)) = 0.18
        _HeadFringeFreq("Head Fringe Freq", Range(1,60)) = 28
        _HeadFringeSpeed("Head Fringe Speed", Range(0,20)) = 10

        _TailFringeWidth("Tail Fringe Width", Range(0, 0.35)) = 0.16
        _TailFeather("Tail Feather", Range(0, 0.25)) = 0.08
        _TailFringeAmp("Tail Fringe Amp", Range(0,0.35)) = 0.16
        _TailFringeFreq("Tail Fringe Freq", Range(1,60)) = 22
        _TailFringeSpeed("Tail Fringe Speed", Range(0,20)) = 9

        _StrandCount("Strand Count", Range(4, 40)) = 16
        _StrandSharpness("Strand Sharpness", Range(0.5, 6)) = 2.4
        _StrandExtend("Strand Extend", Range(0, 0.2)) = 0.06

        _EdgeUVWarp("Edge UV Warp", Range(0, 0.2)) = 0.08
        _EdgeUVWarpWidth("Edge UV Warp Width", Range(0, 0.35)) = 0.18

        // ===== NO UV FLOW =====
        _FlowTotalLength("Flow Total Length", Float) = 1.0

        _StraightDirOS("Straight Dir OS", Vector) = (0,1,0,0)
        _StraightOriginOS("Straight Origin OS", Vector) = (0,0,0,0)
        _StraightLen("Straight Length", Float) = 0.5

        _BendCenterOS("Bend Center OS", Vector) = (0,0,0,0)
        _BendAxisOS("Bend Axis OS", Vector) = (0,0,1,0)
        _BendUOS("Bend U OS", Vector) = (1,0,0,0)
        _BendVOS("Bend V OS", Vector) = (0,1,0,0)
        _BendRadius("Bend Radius", Float) = 0.25
        _BendAngleRad("Bend Angle Rad", Range(0,6.28318)) = 1.570796
        _BendDetectWidth("Bend Detect Width", Float) = 0.35

        _AcrossDirOS("Across Dir OS", Vector) = (1,0,0,0)
        _AcrossScale("Across Scale", Float) = 1.0
        _AcrossBias("Across Bias", Float) = 0.5
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float4 _TexTiling;
                half _TexContrast, _FlowSpeed, _Alpha;
                half _HeadCut01, _TailCut01, _SoftAdd;
                half _DetailStrength, _DetailAffect, _DetailAlphaMin;

                half _HeadFringeWidth, _HeadFeather, _HeadFringeAmp, _HeadFringeFreq, _HeadFringeSpeed;
                half _TailFringeWidth, _TailFeather, _TailFringeAmp, _TailFringeFreq, _TailFringeSpeed;

                half _StrandCount, _StrandSharpness, _StrandExtend;
                half _EdgeUVWarp, _EdgeUVWarpWidth;

                float _FlowTotalLength;
                float4 _StraightDirOS, _StraightOriginOS;
                float _StraightLen;

                float4 _BendCenterOS, _BendAxisOS, _BendUOS, _BendVOS;
                float _BendRadius, _BendAngleRad, _BendDetectWidth;

                float4 _AcrossDirOS;
                float _AcrossScale, _AcrossBias;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 posOS : TEXCOORD0;
            };

            // ---------- helpers ----------
            float SafeLenOS(float3 v) { return max(1e-6, length(v)); }
            float3 SafeNormalizeOS(float3 v) { return v / SafeLenOS(v); }

            half Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 34.345);
                return frac(p.x * p.y);
            }

            half CheapContrast(half v, half c)
            {
                return saturate((v - 0.5h) * c + 0.5h);
            }

            Varyings vert(Attributes IN)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                o.posOS = IN.positionOS.xyz;
                return o;
            }

            float ComputeFlow01(float3 posOS)
            {
                float3 dir = SafeNormalizeOS(_StraightDirOS.xyz);
                float s = dot(posOS - _StraightOriginOS.xyz, dir);

                float3 toC = posOS - _BendCenterOS.xyz;
                float3 axis = SafeNormalizeOS(_BendAxisOS.xyz);
                float3 inPlane = toC - dot(toC, axis) * axis;

                float r = length(inPlane);
                float u = dot(inPlane, SafeNormalizeOS(_BendUOS.xyz));
                float v = dot(inPlane, SafeNormalizeOS(_BendVOS.xyz));
                float ang = atan2(v, u);
                if (ang < 0) ang += 6.2831853;

                float arc = _BendRadius * clamp(ang, 0, _BendAngleRad);
                float bendW = saturate(1 - abs(r - _BendRadius) / _BendDetectWidth);

                float dist = lerp(s, _StraightLen + arc, bendW);
                return saturate(dist / _FlowTotalLength);
            }

            float ComputeAcross01(float3 posOS)
            {
                float3 d = SafeNormalizeOS(_AcrossDirOS.xyz);
                return saturate(dot(posOS, d) * _AcrossScale + _AcrossBias);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float t = _Time.y;

                float flow01 = ComputeFlow01(IN.posOS);
                float across01 = ComputeAcross01(IN.posOS);

                float head = _HeadCut01;
                float tail = _TailCut01;

                float headMask = step(flow01, head);
                float tailMask = step(tail, flow01);

                float segMask = headMask * tailMask;

                float speedUV = _FlowSpeed / max(0.001, _TexTiling.x);

                float2 flowUV;
                flowUV.x = flow01 * _TexTiling.x - t * speedUV;
                flowUV.y = across01 * _TexTiling.y;

                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, flowUV);
                half texGray = CheapContrast(dot(tex.rgb, half3(0.299,0.587,0.114)), _TexContrast);

                half3 col = saturate(_Color.rgb + (texGray - 0.5) * 2 * _DetailStrength);

                half a = _Alpha * _Color.a * segMask;
                return half4(col, a);
            }
            ENDHLSL
        }
    }
}
