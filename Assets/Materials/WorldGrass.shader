// Grass aligned to the world. Top, sides, and bottom each get their own projection
// so a cube face is not stretched. Tile size ignores the mesh UVs and scale.
Shader "Custom/WorldGrass"
{
    Properties
    {
        [MainTexture][NoScaleOffset] _BaseMap ("Grass Texture", 2D) = "white" {}
        [MainColor] _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _Tiling ("Tiling Per Meter", Vector) = (0.5, 0.5, 0, 0)
        _Offset ("Offset", Vector) = (0, 0, 0, 0)
        _Rotation ("Rotation", Range(-180, 180)) = 0
        _BlendSharpness ("Side Blend", Range(1, 8)) = 4
        [Normal][NoScaleOffset] _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Strength", Range(0, 2)) = 1
        _Smoothness ("Smoothness", Range(0, 1)) = 0.05
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float4 _Tiling;
                float4 _Offset;
                float _Rotation;
                float _BlendSharpness;
                float _BumpScale;
                float _Smoothness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float2 ProjectUV(float2 plane)
            {
                float2 uv = plane * _Tiling.xy + _Offset.xy;
                float rad = radians(_Rotation);
                float s = sin(rad);
                float c = cos(rad);
                return float2(c * uv.x - s * uv.y, s * uv.x + c * uv.y);
            }

            // How much of the top, side, and front projections to use. A flat face picks one.
            float3 ProjectionWeights(float3 normalWS)
            {
                float3 blend = pow(abs(normalWS), _BlendSharpness);
                return blend / max(dot(blend, float3(1, 1, 1)), 1e-4);
            }

            half3 TriplanarColor(float2 uvX, float2 uvY, float2 uvZ, float3 blend)
            {
                half3 cx = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvX).rgb;
                half3 cy = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvY).rgb;
                half3 cz = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvZ).rgb;
                return cx * blend.x + cy * blend.y + cz * blend.z;
            }

            float3 TriplanarNormal(float2 uvX, float2 uvY, float2 uvZ, float3 normalWS, float3 blend)
            {
                half3 nx = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uvX), _BumpScale);
                half3 ny = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uvY), _BumpScale);
                half3 nz = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uvZ), _BumpScale);

                nx = half3(nx.xy + normalWS.zy, abs(nx.z) * normalWS.x);
                ny = half3(ny.xy + normalWS.xz, abs(ny.z) * normalWS.y);
                nz = half3(nz.xy + normalWS.xy, abs(nz.z) * normalWS.z);

                return normalize(nx.zyx * blend.x + ny.xzy * blend.y + nz.xyz * blend.z);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = pos.positionCS;
                output.positionWS = pos.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float3 surfaceNormal = normalize(input.normalWS);
                float3 blend = ProjectionWeights(surfaceNormal);
                float2 uvX = ProjectUV(input.positionWS.zy * float2(sign(surfaceNormal.x), 1));
                float2 uvY = ProjectUV(input.positionWS.xz * float2(sign(surfaceNormal.y), 1));
                float2 uvZ = ProjectUV(input.positionWS.xy * float2(sign(surfaceNormal.z), 1));

                half3 albedo = TriplanarColor(uvX, uvY, uvZ, blend) * _BaseColor.rgb;
                float3 normalWS = TriplanarNormal(uvX, uvY, uvZ, surfaceNormal, blend);

                #if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                    float4 shadowCoord = ComputeScreenPos(input.positionCS);
                #else
                    float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                #endif

                Light mainLight = GetMainLight(shadowCoord);
                half3 radiance = mainLight.color * (mainLight.distanceAttenuation * mainLight.shadowAttenuation);
                half3 lighting = SampleSH(normalWS) + LightingLambert(radiance, mainLight.direction, normalWS);

                half3 viewDir = GetWorldSpaceNormalizeViewDir(input.positionWS);
                half3 halfDir = SafeNormalize(mainLight.direction + viewDir);
                half shininess = exp2(10 * _Smoothness + 1);
                half spec = pow(saturate(dot(normalWS, halfDir)), shininess) * _Smoothness;
                half3 color = albedo * lighting + radiance * spec;

                #if defined(_ADDITIONAL_LIGHTS)
                    uint count = GetAdditionalLightsCount();
                    LIGHT_LOOP_BEGIN(count)
                        Light light = GetAdditionalLight(lightIndex, input.positionWS, half4(1, 1, 1, 1));
                        half3 add = light.color * (light.distanceAttenuation * light.shadowAttenuation);
                        color += albedo * LightingLambert(add, light.direction, normalWS);
                    LIGHT_LOOP_END
                #endif

                color = MixFog(color, ComputeFogFactor(input.positionCS.z));
                return half4(color, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif

                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
                #if UNITY_REVERSED_Z
                    output.positionCS.z = min(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    output.positionCS.z = max(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
