Shader "Custom/Sand_Flow_SandFillStyle"
{
    Properties
    {
        _SideColor ("Side Color (RGBA)", Color) = (0.92, 0.78, 0.55, 0.85)
        _TopColor  ("Top Color (RGBA)",  Color) = (1.0, 0.92, 0.72, 1.0)

        // ==================== Fill Control ====================
        _FillAmount("Fill Amount", Range(0,1)) = 1
        _MinValue("Min Y", Float) = -1
        _MaxValue("Max Y", Float) = 1

        // ==================== Grain Settings ====================
        _GrainScale ("Grain Scale", Range(5, 80)) = 35.0
        _GrainRadius ("Grain Radius", Range(0.05, 0.5)) = 0.28
        _GrainSoftness ("Grain Edge Softness", Range(0.01, 0.3)) = 0.06

        // ==================== Flow Settings ====================
        _FlowSpeed ("Flow Speed", Range(0, 10)) = 1.5
        _FlowDirection ("Flow Dir (XY)", Vector) = (0.15, -1.0, 0, 0)

        // ==================== Turbulence ====================
        _Turbulence ("Turbulence Strength", Range(0, 2.0)) = 0.6
        _TurbulenceScale ("Turbulence Scale", Range(0.5, 15)) = 3.0
        _TurbulenceSpeed ("Turbulence Speed", Range(0, 5)) = 1.8

        // ==================== Layer 2 ====================
        _Grain2Scale ("Layer2 Scale", Range(5, 80)) = 22.0
        _Grain2Speed ("Layer2 Speed", Range(0, 5)) = 1.0
        _Grain2Opacity ("Layer2 Opacity", Range(0, 1)) = 0.5

        // ==================== Surface ====================
        _TopBand ("Top Band Width", Range(0.001, 0.3)) = 0.07
        _DepthDarken ("Depth Darken", Range(0, 0.6)) = 0.25
        _BlendStrength ("Blend Strength", Range(0,1)) = 1.0
    }

    SubShader
    {
        Tags { "Queue" = "Transparent-1" "RenderType" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Stencil
            {
                Ref 1
                Comp Equal
            }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _SideColor;
            float4 _TopColor;
            float _FillAmount;
            float _MinValue;
            float _MaxValue;

            float _GrainScale;
            float _GrainRadius;
            float _GrainSoftness;
            float _FlowSpeed;
            float4 _FlowDirection;
            float _Turbulence;
            float _TurbulenceScale;
            float _TurbulenceSpeed;
            float _Grain2Scale;
            float _Grain2Speed;
            float _Grain2Opacity;

            float _TopBand;
            float _DepthDarken;
            float _BlendStrength;

            struct appdata {
                float4 vertex : POSITION;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float3 posOS : TEXCOORD0;
                float3 posWS : TEXCOORD1;
            };

            // ---- Hash functions ----
            float2 Hash22(float2 p)
            {
                float3 q = float3(dot(p, float2(127.1, 311.7)),
                                  dot(p, float2(269.5, 183.3)),
                                  dot(p, float2(419.2, 371.9)));
                return frac(sin(q.xy) * 43758.5453);
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            // ---- Simple 2D value noise ----
            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f); // smoothstep

                float a = Hash21(i);
                float b = Hash21(i + float2(1, 0));
                float c = Hash21(i + float2(0, 1));
                float d = Hash21(i + float2(1, 1));

                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            // ---- FBM turbulence (3 octaves) → returns float2 distortion ----
            float2 TurbulenceDistort(float2 uv)
            {
                float t = _Time.y * _TurbulenceSpeed;
                float2 p = uv * _TurbulenceScale;

                // Channel X distortion
                float nx = 0.0;
                nx += ValueNoise(p + float2(t * 0.7, t * 0.3)) * 0.5;
                nx += ValueNoise(p * 2.1 + float2(-t * 0.4, t * 0.9)) * 0.25;
                nx += ValueNoise(p * 4.3 + float2(t * 1.1, -t * 0.6)) * 0.125;

                // Channel Y distortion (different seed offset)
                float ny = 0.0;
                ny += ValueNoise(p + float2(t * 0.5, -t * 0.8) + 50.0) * 0.5;
                ny += ValueNoise(p * 2.1 + float2(t * 0.6, t * 0.2) + 50.0) * 0.25;
                ny += ValueNoise(p * 4.3 + float2(-t * 0.3, t * 1.3) + 50.0) * 0.125;

                // Remap 0..~0.875 → -1..+1
                float2 d = float2(nx, ny) - 0.4375;
                return d * _Turbulence * 2.0;
            }

            // ---- Animated Voronoi with turbulence + chaotic flow ----
            float2 FlowingVoronoi(float2 uv, float scale, float speed, float2 turbOffset)
            {
                // Apply turbulence distortion BEFORE grid
                uv += turbOffset / scale;

                uv *= scale;
                float2 ig = floor(uv);
                float2 fg = frac(uv);

                float minDist = 1.0;
                float cellBright = 0.5;

                for (int vy = -1; vy <= 1; vy++)
                {
                    for (int vx = -1; vx <= 1; vx++)
                    {
                        float2 neighbor = float2(float(vx), float(vy));
                        float2 cellID = ig + neighbor;

                        float2 rnd = Hash22(cellID);

                        // Always flow strictly downward in world space
                        float2 grainFlowDir = float2(0.0, -1.0);

                        // Per-grain speed variation: wide spread (0.3x ~ 1.7x) for realistic particle feel
                        float speedVar = 0.3 + rnd.y * 1.4;

                        // High multiplier (0.55) for fast visible falling, like free-fall particles
                        float2 flow = frac(rnd + _Time.y * speed * speedVar * grainFlowDir * 0.55);

                        // Minimal horizontal sway — keeps focus on downward fall
                        float timeOffset = rnd.x * 6.2831;
                        flow.x += 0.02 * sin(_Time.y * (1.0 + rnd.y * 1.5) + timeOffset);
                        flow = frac(flow);

                        float2 pt = neighbor + flow - fg;
                        float dist = max(abs(pt.x), abs(pt.y));

                        if (dist < minDist)
                        {
                            minDist = dist;
                            cellBright = Hash21(cellID);
                        }
                    }
                }

                return float2(minDist, cellBright);
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
                // ===== Fill logic =====
                float localY = i.posOS.y;
                float normalizedY = (localY - _MinValue) / (_MaxValue - _MinValue);
                float uvProgress = 1.0 - normalizedY;

                float diff = _FillAmount - uvProgress;
                float fillAlpha = smoothstep(0.0, 0.08, diff);
                if (fillAlpha <= 0.01) discard;

                float surfaceDist = diff;

                // ===== Coordinate anchored to object =====
                float3 objOrigin = float3(unity_ObjectToWorld._m03,
                                          unity_ObjectToWorld._m13,
                                          unity_ObjectToWorld._m23);
                float2 baseUV = (i.posWS.xy - objOrigin.xy);

                // ===== Turbulence distortion =====
                float2 turb = TurbulenceDistort(baseUV);

                // ===== Layer 1: main grains =====
                float2 v1 = FlowingVoronoi(baseUV, _GrainScale, _FlowSpeed, turb);
                float grain1 = 1.0 - smoothstep(_GrainRadius - _GrainSoftness,
                                                 _GrainRadius, v1.x);
                float bright1 = v1.y;

                // ===== Layer 2: secondary grains =====
                float2 turb2 = TurbulenceDistort(baseUV + float2(7.3, -3.1));
                float2 v2 = FlowingVoronoi(baseUV + float2(3.7, 1.3),
                                            _Grain2Scale, _Grain2Speed, turb2 * 0.8);
                float grain2 = 1.0 - smoothstep(_GrainRadius * 0.7 - _GrainSoftness,
                                                 _GrainRadius * 0.7, v2.x);
                float bright2 = v2.y;

                // ===== Combine grain layers =====
                float grainMask = saturate(grain1 + grain2 * _Grain2Opacity);
                float grainBright = lerp(bright1, bright2, grain2 * _Grain2Opacity * 0.5);

                // ===== Color =====
                float topBand = 1.0 - smoothstep(0.0, _TopBand, surfaceDist);
                float depth01 = saturate(uvProgress / max(_FillAmount, 0.001));
                float depthFactor = lerp(1.0, 1.0 - _DepthDarken, depth01);

                float3 grainColor = lerp(_SideColor.rgb * 0.65,
                                         _SideColor.rgb * 1.2,
                                         grainBright);
                grainColor *= depthFactor;

                float3 bgColor = _SideColor.rgb * 0.35 * depthFactor;
                float3 col = lerp(bgColor, grainColor, grainMask);

                // Top band blend
                col = lerp(col, _TopColor.rgb, topBand * 0.8);

                // Sparkle
                float sparkle = step(0.92, grainBright) * grain1 * 0.4;
                col += sparkle * (1.0 - topBand);

                // ===== Alpha =====
                float grainAlpha = lerp(0.12, _SideColor.a, grainMask);
                float finalAlpha = lerp(grainAlpha, _TopColor.a, topBand);
                finalAlpha *= fillAlpha * _BlendStrength;

                return float4(col, saturate(finalAlpha));
            }
            ENDCG
        }
    }
}
