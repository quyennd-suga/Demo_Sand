Shader "URP/Water/TubeFlowWater_Mobile"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (0.15, 0.6, 0.75, 0.7)
        _Emission("Emission", Range(0, 5)) = 0.5

        _NormalMap("Normal Map", 2D) = "bump" {}
        _NormalStrength("Normal Strength", Range(0, 2)) = 0.7
        _NormalTiling("Normal Tiling", Range(0.1, 10)) = 2
        _FlowSpeed("Flow Speed", Range(-5, 5)) = 1.0
        _Distortion("Distortion (UV)", Range(0, 0.2)) = 0.03

        _Disolve("Disolve", Range(0,1)) = 0.0
        _ReverseDisolve("ReverseDisolve", Range(0,1)) = 0.0  // 0 = disable reverse (SAFE)
        _NoiseScale("Noise Scale", Range(0.1, 30)) = 10
        _NoiseStrength("Noise Strength", Range(0, 1)) = 0.25

        [Toggle(_ALPHACLIP_ON)] _AlphaClipOn("Alpha Clip (On/Off)", Float) = 0
        _AlphaClip("AlphaClip Threshold", Range(0,1)) = 0.1

        _SpecColor("Spec Color", Color) = (0.7,0.9,1,1)
        _SpecPower("Spec Power", Range(8, 128)) = 32
        _SpecIntensity("Spec Intensity", Range(0, 2)) = 0.3
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma shader_feature_local _ALPHACLIP_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half  _Emission;

                half  _NormalStrength;
                half  _NormalTiling;
                half  _FlowSpeed;
                half  _Distortion;

                half  _Disolve;
                half  _ReverseDisolve;
                half  _NoiseScale;
                half  _NoiseStrength;

                half  _AlphaClipOn;
                half  _AlphaClip;

                half4 _SpecColor;
                half  _SpecPower;
                half  _SpecIntensity;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3  normalWS   : TEXCOORD1;
                half4  tangentWS  : TEXCOORD2;
                float2 uv         : TEXCOORD3;
                half   fogFactor  : TEXCOORD4;
            };

            inline half Hash21(half2 p)
            {
                p = frac(p * half2(123.34h, 456.21h));
                p += dot(p, p + 45.32h);
                return frac(p.x * p.y);
            }

            inline half ValueNoise(half2 uv)
            {
                half2 i = floor(uv);
                half2 f = frac(uv);
                half2 u = f * f * (3.0h - 2.0h * f);

                half a = Hash21(i + half2(0,0));
                half b = Hash21(i + half2(1,0));
                half c = Hash21(i + half2(0,1));
                half d = Hash21(i + half2(1,1));

                return lerp(lerp(a,b,u.x), lerp(c,d,u.x), u.y);
            }

            inline half3 NormalStrength(half3 n, half strength)
            {
                n.xy *= strength;
                n.z = lerp(1.0h, n.z, saturate(strength));
                return normalize(n);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs nrm   = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

                OUT.positionCS = pos.positionCS;
                OUT.positionWS = pos.positionWS;
                OUT.normalWS   = nrm.normalWS;
                OUT.tangentWS  = half4(nrm.tangentWS, IN.tangentOS.w);
                OUT.uv         = IN.uv;
                OUT.fogFactor  = ComputeFogFactor(OUT.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // TBN
                half3 N = normalize(IN.normalWS);
                half3 T = normalize(IN.tangentWS.xyz);
                half sign = IN.tangentWS.w * GetOddNegativeScale();
                half3 B = sign * normalize(cross(N, T));
                half3x3 TBN = half3x3(T, B, N);

                half t = (half)_Time.y;

                // Flow along tube (assume V is along length)
                float2 uvFlow = IN.uv;
                uvFlow.y += t * _FlowSpeed;

                // Normal + cheap distortion
                float2 nuv = uvFlow * _NormalTiling;
                half3 nTS = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, nuv));
                nTS = NormalStrength(nTS, _NormalStrength);
                float2 uvD = uvFlow + (float2)nTS.xy * (float)_Distortion;

                // === Dissolve/ReverseDissolve SAFE ===
                // match graph shape but avoid "reverse=0 => kill all"
                half oneMinus = 1.0h - (half)IN.uv.y;

                half noise = ValueNoise((half2)(IN.uv * _NoiseScale + t * half2(0.0h, 0.5h)));
                noise = (noise * 2.0h - 1.0h) * _NoiseStrength;

                half inVal = oneMinus * (oneMinus + noise);

                half openMask = step(_Disolve * 2.0h, inVal);

                // reverseMask:
                // - if ReverseDisolve ~ 0 => disable reverse => reverseMask = 1
                // - else => (1 - step(reverse*2, inVal))
                half rev = saturate(_ReverseDisolve);
                half reverseMask = (rev < 0.0001h) ? 1.0h : (1.0h - step(rev * 2.0h, inVal));

                half alpha = openMask * reverseMask;

                #if defined(_ALPHACLIP_ON)
                    clip(alpha - _AlphaClip);
                #endif

                // Color
                half3 baseCol = _BaseColor.rgb;

                // ultra-cheap spec highlight (fake light up)
                half3 V = SafeNormalize(GetWorldSpaceViewDir(IN.positionWS));
                half3 Nw = normalize(mul(nTS, TBN));
                half3 H = SafeNormalize(V + half3(0,1,0));
                half spec = pow(saturate(dot(Nw, H)), _SpecPower) * _SpecIntensity;

                half3 col = baseCol + _SpecColor.rgb * spec;
                col += baseCol * _Emission;

                col = MixFog(col, IN.fogFactor);

                return half4(col, alpha * _BaseColor.a);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
