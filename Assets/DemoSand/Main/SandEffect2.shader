Shader "Custom/SandEffect2"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _SandColor ("Sand Color 1", Color) = (0.9, 0.8, 0.4, 1)
        _SandColor2 ("Sand Color 2", Color) = (0.85, 0.75, 0.35, 1)
        _SandColor3 ("Sand Color 3", Color) = (0.95, 0.85, 0.45, 1)
        _SandColor4 ("Sand Color 4", Color) = (0.88, 0.78, 0.38, 1)
        _WaterLevelY ("Water Level Y (Local From Pivot)", Float) = 0
        _NoiseScale ("Noise Scale", Float) = 50.0
        _NoiseStrength ("Noise Edge Strength", Range(0, 0.2)) = 0.05
        _GrainStrength ("Grain Strength", Range(0, 0.5)) = 0.1
        _WaveSpeed ("Wave Speed", Float) = 2.0
        _WaveFrequency ("Wave Frequency", Float) = 10.0
        _Turbulence ("Sand Turbulence", Float) = 1.0
        _FlowSpread ("Flow Spread", Range(0, 1)) = 0.3
        _FillVelocity ("Fill Velocity", Range(0, 1)) = 0.0
        _FillDirX ("Fill Direction X", Float) = 0.0
        _FillDirY ("Fill Direction Y", Float) = 1.0
        _FillPosX ("Fill Position X (anchored)", Float) = 0.0
        _FillPosY ("Fill Position Y (anchored)", Float) = 0.5
        _PeakX ("Peak X Position", Range(0, 1)) = 0.5
        _PeakHeight ("Peak Height", Range(0, 0.5)) = 0.2
        _BoundsMinX ("Bounds Min X", Float) = -0.5
        _BoundsMaxX ("Bounds Max X", Float) = 0.5

        // ==================== Surface Drifting Grains ====================
        _SurfaceBandHeight ("Surface Band Height", Range(0.01, 0.5)) = 0.15
        _SurfaceGrainScale ("Surface Grain Scale", Range(5, 100)) = 40.0
        _SurfaceGrainRadius ("Surface Grain Radius", Range(0.05, 0.5)) = 0.3
        _SurfaceGrainSoftness ("Surface Grain Softness", Range(0.01, 0.2)) = 0.06
        _SurfaceGrainSpeed ("Surface Grain Speed", Range(0, 5)) = 1.2
        _SurfaceGrainOpacity ("Surface Grain Opacity", Range(0, 1)) = 0.9
        _SurfaceGrainBright ("Surface Grain Brightness", Range(0.5, 2.0)) = 1.3

        // ==================== Depth Darkening ====================
        _BottomDarkness ("Bottom Darkness", Range(0, 1)) = 0.4
        _DarknessDepth  ("Darkness Depth Range", Float) = 0.5
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD1;
                float normalizedWorldX : TEXCOORD2;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _SandColor;
            fixed4 _SandColor2;
            fixed4 _SandColor3;
            fixed4 _SandColor4;
            float _WaterLevelY;
            float _NoiseScale;
            float _NoiseStrength;
            float _GrainStrength;
            float _WaveSpeed;
            float _WaveFrequency;
            float _Turbulence;
            float _FlowSpread;
            float _FillVelocity;
            float _FillDirX;
            float _FillDirY;
            float _FillPosX;
            float _FillPosY;
            float _PeakX;
            float _PeakHeight;
            float _BoundsMinX;
            float _BoundsMaxX;

            float _SurfaceBandHeight;
            float _SurfaceGrainScale;
            float _SurfaceGrainRadius;
            float _SurfaceGrainSoftness;
            float _SurfaceGrainSpeed;
            float _SurfaceGrainOpacity;
            float _SurfaceGrainBright;

            float _BottomDarkness;
            float _DarknessDepth;

            float random(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453123);
            }

            float signedRandom(float2 uv)
            {
                return random(uv) * 2.0 - 1.0;
            }

            // ---- Hash functions for Voronoi ----
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

            // ---- Voronoi with free random 360-degree drift per grain ----
            float2 FreeDriftVoronoi(float2 uv, float scale, float speed)
            {
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

                        // Primary direction: strongly horizontal, random left or right
                        float hSign = sign(rnd.x - 0.5); // -1 or +1
                        float hSpeed = 0.7 + rnd.y * 1.3; // speed variation per grain

                        // Base horizontal drift
                        float2 flow = frac(rnd + _Time.y * speed * hSpeed * float2(hSign * 0.18, 0.0));

                        // Chaotic turbulence: scales with speed so speed=0 means fully static
                        float timeOff = rnd.x * 6.2831;
                        float chaos1 = sin(_Time.y * speed * (2.1 + rnd.y * 3.5) + timeOff);
                        float chaos2 = sin(_Time.y * speed * (4.3 + rnd.x * 2.8) + timeOff * 1.9);
                        float chaos3 = cos(_Time.y * speed * (1.7 + rnd.y * 4.1) + timeOff * 0.7);
                        flow.x += 0.10 * chaos1 + 0.06 * chaos2; // strong horizontal chaos
                        flow.y += 0.02 * chaos3;                  // minimal vertical wobble
                        flow = frac(flow);

                        float2 pt = neighbor + flow - fg;
                        // Square grains (Chebyshev distance)
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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

                o.normalizedWorldX = (o.worldPos.x - _BoundsMinX) / max(_BoundsMaxX - _BoundsMinX, 0.0001);

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float uvX = i.normalizedWorldX;
                
                // Get object pivot Y
                float objY = unity_ObjectToWorld._m13;
                
                // ── SÓNG TĨNH (khi đứng yên) ──────────────────────────────
                float staticPhase = _Time.y * 0.3;
                float wave1 = sin(uvX * _WaveFrequency + staticPhase);
                float wave2 = sin(uvX * _WaveFrequency * 1.5 + staticPhase * 1.3 + 1.5);
                float staticWave = (wave1 + wave2 * 0.5) * _NoiseStrength;


                // ── TAM GIÁC ĐỈNH DI CHUYỂN (chỉ đỉnh di chuyển, hai cạnh cố định) ─────────────
                // peakX: [0,1] là vị trí đỉnh, uvX: [0,1] là vị trí ngang
                float width = 1.0; // chiều rộng tam giác, luôn phủ kín block
                float t = 1.0 - abs(uvX - _PeakX) / width;
                t = saturate(t);
                float mountainWave = t * _PeakHeight;

                // ── BLEND theo velocity (phản ứng nhanh) ──────────────────
                float blendT = smoothstep(0.0, 0.05, _FillVelocity);
                float surfaceOffset = lerp(staticWave, mountainWave, blendT);

                float surfaceY = objY + _WaterLevelY + surfaceOffset;
                //float surfaceY = objY + surfaceOffset;
                float d = surfaceY - i.worldPos.y;
                clip(d);

                // ── GRAIN ──────────────────────────────────────────────────
                float3 objWS = float3(unity_ObjectToWorld._m03, unity_ObjectToWorld._m13, unity_ObjectToWorld._m23);
                float3 anchoredWS = i.worldPos - objWS;
                float2 grainUV = anchoredWS.xy;

                // ── FILL ENTRY POINT ──
                float2 fillPos = float2(_FillPosX, _FillPosY);

                // ── BASE CELL trước — mọi tính toán dùng cell center ──
                float2 baseCell = floor(grainUV * _NoiseScale);
                float2 cellCenter = (baseCell + 0.5) / _NoiseScale;

                // Vector từ entry point đến CELL CENTER (không phải pixel)
                // → tất cả pixel trong cùng cell có cùng offset → giữ cấu trúc hạt
                float2 toCell = cellCenter - fillPos;
                float cellDist = length(toCell);
                float2 pushDir = cellDist > 0.001 ? toCell / cellDist : float2(0.0, 1.0);

                // Proximity: mạnh gần entry point, yếu dần khi xa
                float proximity = 1.0 - smoothstep(0.0, 1.5, cellDist);
                float impactStrength = _Turbulence * proximity;

                // ── SÓNG dùng per-cell seed (ổn định, không phụ thuộc fillPos) ──
                // cellDist chỉ dùng cho proximity + pushDir, KHÔNG dùng cho wave phase
                float cellSeed = random(baseCell * 0.17 + float2(0.13, 0.79)) * 6.28;
                float gWave1 = sin(cellSeed * 5.0 + _Time.y * 1.8);
                float gWave2 = sin(cellSeed * 3.0 - _Time.y * 2.8 + 2.1);
                float gWave3 = cos(cellSeed * 7.0 + _Time.y * 1.3 + 4.7);
                float pushWave = gWave1 * 0.5 + gWave2 * 0.3 + gWave3 * 0.2;

                // Xoáy vuông góc
                float2 swirlDir = float2(-pushDir.y, pushDir.x);
                float swirlWave = sin(cellSeed * 4.0 + _Time.y * 2.0);

                // ── CELL OFFSET ──
                // Offset nhỏ (±1..3 cells) để giữ cấu trúc pixel đồng bộ
                float pushMag = pushWave * impactStrength * _FlowSpread * 1.5;
                float swirlMag = swirlWave * impactStrength * _FlowSpread * 1.0;

                float offsetX = round(pushDir.x * pushMag + swirlDir.x * swirlMag);
                float offsetY = round(pushDir.y * pushMag + swirlDir.y * swirlMag);

                // Per-cell jitter nhẹ
                float cellRand1 = signedRandom(baseCell + float2(0.1, 0.2));
                float cellRand2 = signedRandom(baseCell + float2(0.3, 0.7));
                float timeJitter = sin(_Time.y * 2.8 + cellRand1 * 6.28);
                float jitterX = round(cellRand1 * timeJitter * impactStrength * 0.2);
                float jitterY = round(cellRand2 * timeJitter * impactStrength * 0.15);

                float2 grainCell = baseCell + float2(offsetX + jitterX, offsetY + jitterY);
                float grain = random(grainCell);

                // ── CHỌN 1 TRONG 4 MÀU dựa trên grain random ──
                float colorIdx = floor(grain * 4.0);
                colorIdx = min(colorIdx, 3.0);
                fixed4 col = colorIdx < 1.0 ? _SandColor :
                             colorIdx < 2.0 ? _SandColor2 :
                             colorIdx < 3.0 ? _SandColor3 : _SandColor4;

                // ── ĐỘ TỐI ĐÁY ─────────────────────────────────────
                // d = khoảng cách xuống dưới bề mặt, càng sâu càng tối
                float depthT = saturate(d / max(_DarknessDepth, 0.001));
                float darkFactor = 1.0 - depthT * _BottomDarkness;
                col.rgb *= darkFactor;

                // ── SURFACE DRIFTING GRAINS ───────────────────────────────
                // d = distance below surface (0 = at surface, grows downward)
                float surfaceBand = 1.0 - smoothstep(0.0, _SurfaceBandHeight, d);

                if (surfaceBand > 0.001)
                {
                    float2 surfUV = anchoredWS.xy;
                    float2 sv = FreeDriftVoronoi(surfUV, _SurfaceGrainScale, _SurfaceGrainSpeed);
                    float surfGrain = 1.0 - smoothstep(
                        _SurfaceGrainRadius - _SurfaceGrainSoftness,
                        _SurfaceGrainRadius, sv.x);

                    // Brighter color variation per grain
                    // Surface grain cũng dùng 4-color picking
                    float surfColorIdx = floor(sv.y * 4.0);
                    surfColorIdx = min(surfColorIdx, 3.0);
                    fixed3 surfBaseColor = surfColorIdx < 1.0 ? _SandColor.rgb :
                                          surfColorIdx < 2.0 ? _SandColor2.rgb :
                                          surfColorIdx < 3.0 ? _SandColor3.rgb : _SandColor4.rgb;
                    float brightness = lerp(0.75, _SurfaceGrainBright, frac(sv.y * 4.0));
                    float3 surfGrainColor = surfBaseColor * brightness;

                    // Sparkle on brightest grains
                    float sparkle = step(0.88, sv.y) * surfGrain * 0.35;
                    surfGrainColor += sparkle;

                    float surfAlpha = surfGrain * surfaceBand * _SurfaceGrainOpacity;
                    col.rgb = lerp(col.rgb, surfGrainColor, surfAlpha);
                }

                return col;
            }
            ENDCG
        }
    }
}