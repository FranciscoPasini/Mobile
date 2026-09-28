// Transparent purchase zone. Works from object-space position, so it fits Unity's Sphere or
// Cylinder, including squashed ones used as a flat circle. Radius 0.5 matches both meshes.
Shader "Custom/BuyZoneFill"
{
    Properties
    {
        _BaseColor ("Empty Color", Color) = (1, 1, 1, 0.2)
        _FillColor ("Fill Color", Color) = (0, 1, 0, 0.65)
        _BorderColor ("Border Color", Color) = (1, 1, 1, 0.85)
        _Fill ("Fill", Range(0, 1)) = 0
        _BorderWidth ("Border Width", Range(0, 0.5)) = 0.06
        _Radius ("Mesh Radius", Float) = 0.5
        [Enum(Grow From Center, 0, Clockwise Sweep, 1, Rise From Bottom, 2)] _FillMode ("Fill Mode", Float) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "BuyZone"

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _FillColor;
                half4 _BorderColor;
                float _Fill;
                float _BorderWidth;
                float _Radius;
                float _FillMode;
                float _Cull;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionOS = input.positionOS.xyz;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float radius = max(_Radius, 1e-4);
                // Horizontal position, -1..1 across the mesh as seen from above.
                float2 p = input.positionOS.xz / radius;
                float dist = saturate(length(p));
                float aa = max(fwidth(dist), 1e-4);

                float innerRadius = 1.0 - _BorderWidth;
                float border = smoothstep(innerRadius - aa, innerRadius, dist);

                float fillMask;
                if (_Fill >= 0.999)
                {
                    fillMask = 1.0;
                }
                else if (_FillMode < 0.5)
                {
                    float fillRadius = _Fill * innerRadius;
                    fillMask = 1.0 - smoothstep(fillRadius - aa, fillRadius, dist);
                }
                else if (_FillMode < 1.5)
                {
                    // 0 along local +Z, increasing clockwise when viewed from above.
                    float t = frac(atan2(p.x, p.y) / TWO_PI + 1.0);
                    // Angular width of one pixel, so the sweep edge stays smooth without a seam at t = 0.
                    float angleAA = aa / max(dist * TWO_PI, 1e-4);
                    fillMask = 1.0 - smoothstep(_Fill - angleAA, _Fill, t);
                }
                else
                {
                    float height = saturate(input.positionOS.y / radius * 0.5 + 0.5);
                    float heightAA = max(fwidth(height), 1e-4);
                    fillMask = 1.0 - smoothstep(_Fill - heightAA, _Fill, height);
                }
                fillMask *= step(0.0001, _Fill);

                half4 color = lerp(_BaseColor, _FillColor, fillMask);
                color = lerp(color, _BorderColor, border);
                return color;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
