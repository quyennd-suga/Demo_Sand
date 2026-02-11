Shader "Sugame/URP/TriplanarMobileLit"
{
    Properties
    {
        _BaseMap("Base Map", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1,1,1,1)

        _Tiling("World Tiling (units)", Float) = 1.0
        _BlendSharpness("Blend Sharpness", Range(1,8)) = 4

        [Toggle(_USE_NORMALMAP)] _UseNormalMap("Use Normal Map", Float) = 0
        _NormalMap("Normal Map", 2D) = "bump" {}
        _NormalScale("Normal Scale", Range(0,2)) = 1

        _Metallic("Metallic", Range(0,1)) = 0
        _Smoothness("Smoothness", Range(0,1)) = 0.5
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma target 2.0

            // URP keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _SCREEN_SPACE_OCCLUSION

            #pragma shader_feature_local _USE_NORMALMAP

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);       SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NormalMap);     SAMPLER(sampler_NormalMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _Tiling;
                half _BlendSharpness;
                half _NormalScale;
                half _Metallic;
                half _Smoothness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3  normalWS   : TEXCOORD1;
            };

            Varyings vert (Attributes v)
            {
                Varyings o;
                VertexPositionInputs posInputs = GetVertexPositionInputs(v.positionOS.xyz);
                VertexNormalInputs nrmInputs    = GetVertexNormalInputs(v.normalOS);

                o.positionCS = posInputs.positionCS;
                o.positionWS = posInputs.positionWS;
                o.normalWS   = half3(nrmInputs.normalWS);
                return o;
            }

            // --- Helpers: very cheap pow approximation for mobile
            inline half3 BlendWeights(half3 n, half sharpness)
            {
                // n = abs(normal)
                // sharpness 1..8 : higher = sharper blend edges
                // Approx: w = n^sharpness, normalized
                // Use repeated multiply (cheaper than pow)
                half3 w = n;
                // clamp sharpness to int-ish behavior for cheapness
                // We'll approximate by multiplying a few times based on sharpness range.
                if (sharpness > 1.5h) w *= n;
                if (sharpness > 2.5h) w *= n;
                if (sharpness > 3.5h) w *= n;
                if (sharpness > 4.5h) w *= n;
                if (sharpness > 5.5h) w *= n;
                if (sharpness > 6.5h) w *= n;
                if (sharpness > 7.5h) w *= n;

                half sum = w.x + w.y + w.z + 1e-5h;
                return w / sum;
            }

            inline half4 SampleTriplanarBase(float3 posWS, half3 nWS, half tiling, half sharpness)
            {
                half3 an = abs(nWS);
                half3 w = BlendWeights(an, sharpness);

                // World to UV per axis
                // X projection uses YZ, Y uses XZ, Z uses XY
                float2 uvX = posWS.zy * rcp(max(tiling, 1e-5));
                float2 uvY = posWS.xz * rcp(max(tiling, 1e-5));
                float2 uvZ = posWS.xy * rcp(max(tiling, 1e-5));

                half4 x = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvX);
                half4 y = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvY);
                half4 z = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvZ);

                return x * w.x + y * w.y + z * w.z;
            }

            #if defined(_USE_NORMALMAP)
            inline half3 UnpackNormalAG(half4 nrm)
            {
                // Standard normal map unpack (DXT5nm style not assumed; Unity normal map is usually BC5/ARGB)
                half3 n;
                n.xy = nrm.xy * 2 - 1;
                n.z  = sqrt(saturate(1 - dot(n.xy, n.xy)));
                return n;
            }

            inline half3 SampleTriplanarNormal(float3 posWS, half3 nWS, half tiling, half sharpness, half normalScale)
            {
                half3 an = abs(nWS);
                half3 w = BlendWeights(an, sharpness);

                float2 uvX = posWS.zy * rcp(max(tiling, 1e-5));
                float2 uvY = posWS.xz * rcp(max(tiling, 1e-5));
                float2 uvZ = posWS.xy * rcp(max(tiling, 1e-5));

                // Sample tangent-space normals for each projection
                half3 nx = UnpackNormalAG(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uvX));
                half3 ny = UnpackNormalAG(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uvY));
                half3 nz = UnpackNormalAG(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uvZ));

                // Scale strength (xy only)
                nx.xy *= normalScale; ny.xy *= normalScale; nz.xy *= normalScale;

                // Re-normalize each
                nx.z = sqrt(saturate(1 - dot(nx.xy, nx.xy)));
                ny.z = sqrt(saturate(1 - dot(ny.xy, ny.xy)));
                nz.z = sqrt(saturate(1 - dot(nz.xy, nz.xy)));

                // Convert each projection normal into WORLD space oriented per axis projection
                // For X projection (onto YZ plane): normal basis = ( +X, +Y, +Z ) but mapped as:
                // tangent=Z, bitangent=Y, normal=X
                half3 nX_ws = half3(nx.z, nx.y, nx.x); // cheap swizzle orientation
                // For Y projection (onto XZ plane): normal points Y
                half3 nY_ws = half3(ny.x, ny.z, ny.y);
                // For Z projection (onto XY plane): normal points Z
                half3 nZ_ws = half3(nz.x, nz.y, nz.z);

                // Blend and normalize
                half3 nBlend = nX_ws * w.x + nY_ws * w.y + nZ_ws * w.z;
                return normalize(nBlend);
            }
            #endif

            half4 frag (Varyings i) : SV_Target
            {
                half3 nWS = normalize(i.normalWS);
                half4 baseSample = SampleTriplanarBase(i.positionWS, nWS, _Tiling, _BlendSharpness);
                half3 albedo = baseSample.rgb * _BaseColor.rgb;
                half alpha = baseSample.a * _BaseColor.a;

                // Build InputData for URP lighting
                InputData inputData;
                ZERO_INITIALIZE(InputData, inputData);
                inputData.positionWS = i.positionWS;

                #if defined(_USE_NORMALMAP)
                    half3 nTriplanar = SampleTriplanarNormal(i.positionWS, nWS, _Tiling, _BlendSharpness, _NormalScale);
                    inputData.normalWS = nTriplanar;
                #else
                    inputData.normalWS = nWS;
                #endif

                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(i.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(i.positionWS);

                // Simple Lit-like surface
                SurfaceData surfaceData;
                ZERO_INITIALIZE(SurfaceData, surfaceData);
                surfaceData.albedo = albedo;
                surfaceData.alpha = alpha;
                surfaceData.metallic = _Metallic;
                surfaceData.smoothness = _Smoothness;
                surfaceData.normalTS = half3(0,0,1);
                surfaceData.occlusion = 1;
                surfaceData.emission = 0;

                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                return color;
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
