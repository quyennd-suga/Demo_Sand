Shader "Shader Graphs/WaterGraphTube_Restored_URP"
{
    Properties
    {
        Vector1_ab5bbc78aace46cca47c976624c57654 ("Distortion", Float) = 0.01
        Vector1_f64f7fc51f514d1fa0c302cd1c54ace5 ("Smoothness", Float) = 0
        Vector1_7f26fb54a3f1446cae706364d1d4c87b ("DistortionVertex", Float) = 0.1

        [NoScaleOffset][Normal] Texture2D_df814fb1718447f4b430f7939cffe52f ("NormalMap", 2D) = "bump" {}
        Vector1_ddb6293edce643b0ad001ef8e4bfe970 ("NormalStrength", Float) = 1
        Vector1_ca96e47f336c4f3e94f6e18dd5768814 ("NormalTiling", Float) = 1

        Vector1_30c2129fc1b14cacae4c5402852fa917 ("SpecularFloat", Float) = 0

        _Disolve ("Disolve (HeadCut 0..1)", Range(0, 1)) = 1
        _ReverseDisolve ("ReverseDisolve (TailCut 0..1)", Range(0, 1)) = 0

        Vector1_a34d470e309a4e6196963d7f1b300204 ("CenterConnect", Float) = 0.08

        _BaseColor ("BaseColor", Color) = (0.2,0.7,1,0.85)
        _Metalic ("Metalic", Range(0,1)) = 0
        _AmbientOcclusion ("AmbientOcclusion", Range(0,1)) = 1

        _WaterSpeed ("WaterSpeed (xy)", Vector) = (0,0.2,0,0)
        _NormalSpeed ("NormalSpeed (xy)", Vector) = (0,-0.2,0,0)

        _RandomDissolveNoiseScale ("RandomDissolveNoiseScale", Float) = 10
        _RandomDissolve ("RandomDissolve (Edge Roughness)", Range(0,1)) = 0.5
        _GradientNoiseScale ("GradientNoiseScale", Float) = 10

        [HideInInspector] _BUILTIN_QueueOffset ("Float", Float) = 0
        [HideInInspector] _BUILTIN_QueueControl ("Float", Float) = -1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }

        LOD 200

        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            // URP core + lighting
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // ===== Textures =====
            TEXTURE2D(Texture2D_df814fb1718447f4b430f7939cffe52f);
            SAMPLER(sampler_Texture2D_df814fb1718447f4b430f7939cffe52f);

            CBUFFER_START(UnityPerMaterial)
                half  Vector1_ab5bbc78aace46cca47c976624c57654; // Distortion (UV)
                half  Vector1_f64f7fc51f514d1fa0c302cd1c54ace5; // Smoothness
                half  Vector1_7f26fb54a3f1446cae706364d1d4c87b; // DistortionVertex

                half  Vector1_ddb6293edce643b0ad001ef8e4bfe970; // NormalStrength
                half  Vector1_ca96e47f336c4f3e94f6e18dd5768814; // NormalTiling
                half  Vector1_30c2129fc1b14cacae4c5402852fa917; // SpecularFloat

                half  _Disolve;         // head cut 0..1
                half  _ReverseDisolve;  // tail cut 0..1

                half  Vector1_a34d470e309a4e6196963d7f1b300204; // CenterConnect

                half4 _BaseColor;
                half  _Metalic;
                half  _AmbientOcclusion;

                half4 _WaterSpeed;
                half4 _NormalSpeed;

                half  _RandomDissolveNoiseScale;
                half  _RandomDissolve;
                half  _GradientNoiseScale;
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
                float4 positionHCS : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                half3  normalWS    : TEXCOORD1;
                half3  tangentWS   : TEXCOORD2;
                half3  bitangentWS : TEXCOORD3;
                float2 uv          : TEXCOORD4;
            };

            // ---- hash / noise (cheap) ----
            half Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 34.345);
                return frac(p.x * p.y);
            }

            // smooth value noise
            half Noise2D(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);

                half a = Hash21(i);
                half b = Hash21(i + float2(1, 0));
                half c = Hash21(i + float2(0, 1));
                half d = Hash21(i + float2(1, 1));

                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            half CheapContrast(half v, half c)
            {
                return saturate((v - 0.5h) * c + 0.5h);
            }

            // Unpack normal, scale XY, rebuild Z
            half3 UnpackNormalScaleRGorAG(half4 n, half strength)
            {
                // supports normal maps in RG (DXT5nm also ok-ish)
                half2 xy = (n.ag * 2.0h - 1.0h); // common packed in AG in Unity
                // If your normal is standard RG, change to: half2 xy = (n.rg * 2 - 1);
                xy *= strength;
                half z = sqrt(saturate(1.0h - dot(xy, xy)));
                return half3(xy.x, xy.y, z);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);

                // Vertex distortion along normal (subtle wobble)
                // Use UV.x (length) + time for stable wave
                float t = _Time.y;
                float wobble =
                    (Noise2D(IN.uv * _GradientNoiseScale + t * 0.6) - 0.5) * 2.0;
                wobble += sin((IN.uv.x * 6.2831) + t * 2.0) * 0.35;

                float3 nWS = TransformObjectToWorldNormal(IN.normalOS);
                posWS += nWS * (wobble * Vector1_7f26fb54a3f1446cae706364d1d4c87b * 0.02);

                OUT.positionWS = posWS;
                OUT.positionHCS = TransformWorldToHClip(posWS);

                // Build TBN
                half3 normalWS = normalize(nWS);
                half3 tangentWS = normalize(TransformObjectToWorldDir(IN.tangentOS.xyz));
                half tangentSign = IN.tangentOS.w * GetOddNegativeScale();
                half3 bitangentWS = normalize(cross(normalWS, tangentWS) * tangentSign);

                OUT.normalWS = normalWS;
                OUT.tangentWS = tangentWS;
                OUT.bitangentWS = bitangentWS;

                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;
                float t = _Time.y;

                // ===== Dissolve window by UV.x (0=from, 1=to) =====
                half head = saturate(_Disolve);        // reveal head
                half tail = saturate(_ReverseDisolve); // retract tail

                // Random edge roughness (tua rua) near both ends
                half edgeNoise = Noise2D(uv * _RandomDissolveNoiseScale + t * 0.35);
                edgeNoise = (edgeNoise - 0.5h) * 2.0h; // -1..1

                // Apply roughness stronger near ends
                half nearHead = saturate((uv.x - (head - 0.15h)) / 0.15h);
                half nearTail = saturate(((tail + 0.15h) - uv.x) / 0.15h);
                half edgeRegion = saturate(max(nearHead, nearTail));
                half rough = edgeNoise * _RandomDissolve * edgeRegion * 0.12h;

                half headEdge = head + rough;
                half tailEdge = tail - rough;

                // Visible if tailEdge <= uv.x <= headEdge
                half segMask = step(tailEdge, uv.x) * step(uv.x, headEdge);

                // soft fade (feather) to avoid too “hard cut”
                half feather = 0.06h + Vector1_a34d470e309a4e6196963d7f1b300204; // reuse CenterConnect as extra softness
                half headFade = saturate((headEdge - uv.x) / max(1e-4h, feather));
                half tailFade = saturate((uv.x - tailEdge) / max(1e-4h, feather));
                segMask *= headFade * tailFade;

                // ===== UV distortion for water movement =====
                float2 flow = uv + _WaterSpeed.xy * t;

                // Add distortion (stronger near edges for turbulent start/end)
                half warpNoise = Noise2D(flow * _GradientNoiseScale + t * 1.2);
                warpNoise = (warpNoise - 0.5h) * 2.0h;

                half edgeBoost = lerp(0.35h, 1.0h, edgeRegion);
                float2 warp = float2(warpNoise, -warpNoise) * (Vector1_ab5bbc78aace46cca47c976624c57654 * edgeBoost);

                float2 nUV = uv * max(0.0001, Vector1_ca96e47f336c4f3e94f6e18dd5768814) + _NormalSpeed.xy * t;
                nUV += warp;

                // ===== Normal map =====
                half4 nTex = SAMPLE_TEXTURE2D(Texture2D_df814fb1718447f4b430f7939cffe52f, sampler_Texture2D_df814fb1718447f4b430f7939cffe52f, nUV);
                half3 nTS = UnpackNormalScaleRGorAG(nTex, Vector1_ddb6293edce643b0ad001ef8e4bfe970);

                // Transform normal to world
                half3 nWS = normalize(
                    IN.tangentWS * nTS.x +
                    IN.bitangentWS * nTS.y +
                    IN.normalWS * nTS.z
                );

                // ===== Simple URP lighting (PBR-ish) =====
                InputData inputData;
                inputData.positionWS = IN.positionWS;
                inputData.normalWS = nWS;
                inputData.viewDirectionWS = SafeNormalize(GetWorldSpaceViewDir(IN.positionWS));
                inputData.shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                inputData.fogCoord = 0;
                inputData.vertexLighting = half3(0,0,0);
                inputData.bakedGI = SampleSH(nWS);

                // Build surface
                SurfaceData surface;
                surface.albedo = _BaseColor.rgb;
                surface.metallic = _Metalic;
                surface.specular = half3(Vector1_30c2129fc1b14cacae4c5402852fa917,
                                         Vector1_30c2129fc1b14cacae4c5402852fa917,
                                         Vector1_30c2129fc1b14cacae4c5402852fa917);
                surface.smoothness = Vector1_f64f7fc51f514d1fa0c302cd1c54ace5;
                surface.normalTS = half3(0,0,1);
                surface.emission = half3(0,0,0);
                surface.occlusion = _AmbientOcclusion;
                surface.alpha = _BaseColor.a * segMask; // alpha by mask
                surface.clearCoatMask = 0;
                surface.clearCoatSmoothness = 0;

                half4 col = UniversalFragmentPBR(inputData, surface);

                // Optional: make it a bit “watery” by boosting fresnel-like highlight
                half ndv = saturate(dot(inputData.normalWS, inputData.viewDirectionWS));
                half fres = pow(1.0h - ndv, 4.0h);
                col.rgb += fres * 0.15h;

                // keep alpha from mask
                col.a = surface.alpha;

                return col;
            }
            ENDHLSL
        }
    }

    Fallback "Hidden/Shader Graph/FallbackError"
}
