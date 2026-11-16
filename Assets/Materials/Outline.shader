// Pulsing outline drawn as a slightly bigger, inside-out copy of the mesh.
// Add it as an extra material on a renderer to highlight it.
Shader "Custom/Outline"
{
    Properties
    {
        [HDR] _Color ("Color", Color) = (1, 0.8, 0.25, 1)
        _Width ("Width (meters)", Range(0, 0.5)) = 0.08
        _PulseSpeed ("Pulse Speed", Range(0, 10)) = 5
        _PulseAmount ("Pulse Amount", Range(0, 1)) = 0.4
        // 0 follows normals. 1 pushes out from the pivot, which closes gaps on hard edged meshes like cubes.
        _CornerFix ("Corner Fix", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Cull Front
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _Width;
                float _PulseSpeed;
                float _PulseAmount;
                float _CornerFix;
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
                half pulse : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);

                half pulse = 0.5 + 0.5 * sin(_Time.y * _PulseSpeed);

                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float3 fromPivotWS = SafeNormalize(TransformObjectToWorldDir(input.positionOS.xyz, false));
                float3 dir = SafeNormalize(lerp(normalWS, fromPivotWS, _CornerFix));

                float width = _Width * (1.0 + pulse * _PulseAmount);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz) + dir * width;

                output.positionCS = TransformWorldToHClip(positionWS);
                output.pulse = pulse;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 color = _Color;
                color.a *= lerp(1.0 - _PulseAmount, 1.0, input.pulse);
                return color;
            }
            ENDHLSL
        }
    }
}
