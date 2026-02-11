Shader "Game/SandStream_URP_StaticMesh"
{
    Properties
    {
        _Color("Sand Color", Color) = (0.92, 0.78, 0.55, 1.0)
        _ColorDark("Sand Color Dark", Color) = (0.72, 0.55, 0.35, 1.0)

        // Texture 1: Grain / particle dots (white dots on black)
        _GrainTex("Grain Particle Tex", 2D) = "black" {}
        _GrainTiling("Grain Tiling (XY)", Vector) = (3, 4, 0, 0)
        _GrainBrightness("Grain Brightness", Range(0.5, 6.0)) = 3.0
        _GrainThreshold("Grain Threshold (cutoff)", Range(0.0, 0.8)) = 0.15

        // Texture 2: Noise / distortion map (colorful noise)
        _NoiseTex("Noise Distort Tex", 2D) = "gray" {}
        _NoiseTiling("Noise Tiling (XY)", Vector) = (1.5, 1.5, 0, 0)
        _DistortStrength("Distort Strength", Range(0, 0.15)) = 0.04

        _FlowSpeed("Fall Speed", Float) = 3.0
        _FlowSpeed2("Fall Speed Layer2", Float) = 2.2
        _Alpha("Alpha", Range(0,1)) = 1.0

        // Length window
        _HeadCut01("Head Cut 0..1 (reveal)", Range(0,1)) = 1.0
        _TailCut01("Tail Cut 0..1 (retract)", Range(0,1)) = 0.0

        // ====== Edge fringe & distort (Head) ======
        _HeadFringeWidth("Head Fringe Width", Range(0.0, 0.35)) = 0.16
        _HeadFeather("Head Feather", Range(0.0, 0.25)) = 0.08
        _HeadFringeAmp("Head Fringe Amp", Range(0,0.35)) = 0.20
        _HeadFringeFreq("Head Fringe Freq", Range(1,60)) = 32
        _HeadFringeSpeed("Head Fringe Speed", Range(0,20)) = 11

        // ====== Edge fringe & distort (Tail) ======
        _TailFringeWidth("Tail Fringe Width", Range(0.0, 0.35)) = 0.18
        _TailFeather("Tail Feather", Range(0.0, 0.25)) = 0.10
        _TailFringeAmp("Tail Fringe Amp", Range(0,0.35)) = 0.18
        _TailFringeFreq("Tail Fringe Freq", Range(1,60)) = 26
        _TailFringeSpeed("Tail Fringe Speed", Range(0,20)) = 9

        // Strand breakup
        _StrandCount("Strand Count", Range(4, 40)) = 20
        _StrandSharpness("Strand Sharpness", Range(0.5, 6.0)) = 2.8
        _StrandExtend("Strand Extend", Range(0, 0.20)) = 0.07

        // Edge UV warp
        _EdgeUVWarp("Edge UV Warp", Range(0, 0.2)) = 0.10
        _EdgeUVWarpWidth("Edge UV Warp Width", Range(0, 0.35)) = 0.20

        // Sand-specific
        _CenterDensity("Center Density Boost", Range(1.0, 3.0)) = 1.5
        _Scatter("Tail Scatter", Range(0, 0.15)) = 0.04
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

            TEXTURE2D(_GrainTex);  SAMPLER(sampler_GrainTex);
            TEXTURE2D(_NoiseTex);  SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
                half4  _Color;
                half4  _ColorDark;

                float4 _GrainTiling;
                half   _GrainBrightness;
                half   _GrainThreshold;

                float4 _NoiseTiling;
                half   _DistortStrength;

                half   _FlowSpeed;
                half   _FlowSpeed2;
                half   _Alpha;

                half   _HeadCut01;
                half   _TailCut01;

                half   _HeadFringeWidth;
                half   _HeadFeather;
                half   _HeadFringeAmp;
                half   _HeadFringeFreq;
                half   _HeadFringeSpeed;

                half   _TailFringeWidth;
                half   _TailFeather;
                half   _TailFringeAmp;
                half   _TailFringeFreq;
                half   _TailFringeSpeed;

                half   _StrandCount;
                half   _StrandSharpness;
                half   _StrandExtend;

                half   _EdgeUVWarp;
                half   _EdgeUVWarpWidth;

                half   _CenterDensity;
                half   _Scatter;
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

            // Smooth hash: interpolate between cell values for no hard boundaries
            half SmoothHash(float x, float seed)
            {
                float i = floor(x);
                float f = frac(x);
                f = f * f * (3.0 - 2.0 * f); // hermite smoothstep
                half a = Hash21(float2(i, seed));
                half b = Hash21(float2(i + 1.0, seed));
                return lerp(a, b, f);
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
                float  t  = _Time.y;

                float head = saturate(_HeadCut01);
                float tail = saturate(_TailCut01);

                // ======== Head / Tail regions (0..1) ========
                float headRegion = saturate((uv.x - (head - _HeadFringeWidth)) / max(1e-5, _HeadFringeWidth));
                float tailRegion = saturate(((tail + _TailFringeWidth) - uv.x) / max(1e-5, _TailFringeWidth));

                // ======== Strand breakup (smooth – no hard banding) ========
                float count = max(4.0, _StrandCount);
                float y     = saturate(uv.y);

                // Smooth random offsets (interpolated, no floor discontinuity)
                half r0 = SmoothHash(y * count, 1.31);
                half r1 = SmoothHash(y * count * 2.17, 0.73);

                // Smooth strand pattern using sine instead of floor/frac
                float strandWave = sin(y * count * 3.14159);
                float strandCore = strandWave * strandWave; // smooth 0..1
                strandCore = pow(strandCore, max(0.5, _StrandSharpness * 0.4));
                strandCore *= lerp(0.7, 1.0, r0);

                // ======== Head wiggle (smooth, no discontinuity) ========
                float headWave = sin(y * _HeadFringeFreq + t * _HeadFringeSpeed + r0 * 6.2831) * _HeadFringeAmp;
                // Smooth jitter: use sine combo instead of hash
                float headJitter = sin(y * 53.0 + t * 3.7) * cos(y * 37.0 + t * 2.3) * 0.04;
                float headLenVar = (r1 - 0.5) * 0.08;
                float headEdgeWiggle = head + (headWave + headJitter + headLenVar) + strandCore * _StrandExtend;

                // ======== Tail wiggle (smooth) ========
                float tailWave = sin(y * _TailFringeFreq + t * _TailFringeSpeed + r1 * 6.2831) * _TailFringeAmp;
                float tailJitter = sin(y * 47.0 + t * 4.1) * cos(y * 31.0 + t * 2.7) * 0.04;
                float tailLenVar = (r0 - 0.5) * 0.08;
                float tailEdgeWiggle = tail + (tailWave + tailJitter + tailLenVar) - strandCore * (_StrandExtend * 0.85);

                // ======== Masks (smooth – no step jaggies) ========
                // Use smoothstep with a small pixel-width feather to avoid aliasing
                float featherPx = fwidth(uv.x) * 2.0; // 2-pixel smooth edge

                float headMaskRigid  = smoothstep(0.0, featherPx, head - uv.x);
                float tailMaskRigid  = smoothstep(0.0, featherPx, uv.x - tail);
                float headMaskWiggle = smoothstep(0.0, featherPx, headEdgeWiggle - uv.x);
                float tailMaskWiggle = smoothstep(0.0, featherPx, uv.x - tailEdgeWiggle);

                float headMask = lerp(headMaskRigid, headMaskWiggle, headRegion);
                float tailMask = lerp(tailMaskRigid, tailMaskWiggle, tailRegion);

                // ======== Feather (soft edges) ========
                float headFeather = 1.0;
                if (_HeadFeather > 1e-5)
                {
                    float dH = (head - uv.x);
                    headFeather = saturate(dH / _HeadFeather);
                    headFeather = headFeather * headFeather * (3.0 - 2.0 * headFeather);
                    headFeather = lerp(1.0, headFeather, headRegion);
                }

                float tailFeather = 1.0;
                if (_TailFeather > 1e-5)
                {
                    float dT = (uv.x - tail);
                    tailFeather = saturate(dT / _TailFeather);
                    tailFeather = tailFeather * tailFeather * (3.0 - 2.0 * tailFeather);
                    tailFeather = lerp(1.0, tailFeather, tailRegion);
                }

                float segMask = headMask * tailMask;

                // ======== Edge UV warp ========
                float edgeRegion     = saturate(max(headRegion, tailRegion));
                float edgeWarpRegion = saturate(edgeRegion * (_EdgeUVWarpWidth > 1e-5 ? 1.0 : 0.0));

                // ================================================================
                //  NOISE TEXTURE  – sample the colorful noise map
                //  Use RG channels as 2D distortion vector
                // ================================================================
                float2 noiseUV = uv * _NoiseTiling.xy;
                noiseUV.x -= t * 0.3;  // slow drift
                noiseUV.y -= t * 0.6;
                half4 noiseSample = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUV);

                // Distortion vector from RG channels (remap 0..1 -> -1..+1)
                float2 distort = (noiseSample.rg - 0.5) * 2.0 * _DistortStrength;

                // Extra warp near edges
                float warpSignal = sin((y * 14.0 + r0 * 3.5) + t * 6.0)
                                 + (Hash21(float2(y * 9.0, t * 0.8)) - 0.5) * 0.8;
                float edgeWarp = warpSignal * _EdgeUVWarp * edgeWarpRegion;

                // ================================================================
                //  GRAIN TEXTURE  – Layer 1 (main falling grain particles)
                // ================================================================
                float2 grainUV1 = uv * _GrainTiling.xy;
                grainUV1.x -= t * _FlowSpeed;        // falling motion
                grainUV1   += distort;                // noise-based distortion
                grainUV1.y += edgeWarp;
                grainUV1.x += edgeWarp * 0.35;

                // Add scatter near tail (sand spreads as it falls further)
                float tailDist  = saturate(1.0 - (uv.x - tail) / max(1e-5, head - tail));
                float scatter   = tailDist * _Scatter;
                grainUV1.y     += sin(y * 40.0 + t * 5.0) * scatter;

                half grain1 = SAMPLE_TEXTURE2D(_GrainTex, sampler_GrainTex, grainUV1).r;

                // ================================================================
                //  GRAIN TEXTURE  – Layer 2 (offset, different speed for density)
                // ================================================================
                float2 grainUV2 = uv * _GrainTiling.xy * 1.3;  // slightly different scale
                grainUV2.x -= t * _FlowSpeed2;
                grainUV2.y += 0.37;                    // offset to avoid alignment
                grainUV2   += distort * 0.8;
                grainUV2.y += edgeWarp * 0.7;
                grainUV2.x += edgeWarp * 0.25;
                grainUV2.y += sin(y * 35.0 + t * 4.0) * scatter * 0.8;

                half grain2 = SAMPLE_TEXTURE2D(_GrainTex, sampler_GrainTex, grainUV2).r;

                // ================================================================
                //  Combine grain layers
                //  Screen blend: 1 - (1-a)*(1-b) keeps bright dots additive
                // ================================================================
                half grainCombined = 1.0 - (1.0 - grain1) * (1.0 - grain2);

                // Boost brightness
                grainCombined = saturate(grainCombined * _GrainBrightness);

                // Threshold to make individual particles pop
                half grainMask = smoothstep(_GrainThreshold, _GrainThreshold + 0.25, grainCombined);

                // ================================================================
                //  Center density boost (more particles in center of stream)
                // ================================================================
                float centerY = abs(uv.y - 0.5) * 2.0;   // 0 at center, 1 at edges
                float densityFalloff = 1.0 - centerY * centerY;
                densityFalloff = lerp(1.0, densityFalloff, _CenterDensity - 1.0);

                // ================================================================
                //  Color: lerp between light and dark sand based on grain
                // ================================================================
                half3 sandLight = _Color.rgb;
                half3 sandDark  = _ColorDark.rgb;

                // Use noise B channel for subtle color variation
                half colorNoise = noiseSample.b;
                half3 baseCol   = lerp(sandDark, sandLight, saturate(grainCombined * 0.6 + colorNoise * 0.4));

                // Slight warm highlight on bright grains
                baseCol += grainMask * 0.08;

                // ================================================================
                //  Alpha: particle-based
                //  grainMask drives the visibility of individual particles
                // ================================================================
                half a = grainMask * densityFalloff;
                a *= _Alpha * _Color.a;
                a *= segMask;
                a *= headFeather * tailFeather;

                // Ensure some base opacity in the dense center
                half baseFill = densityFalloff * 0.35 * segMask * headFeather * tailFeather * _Alpha;
                a = max(a, baseFill);

                half4 outCol = half4(baseCol, saturate(a));
                return outCol;
            }
            ENDHLSL
        }
    }
}
