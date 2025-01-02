Shader "Universal Render Pipeline/OutlinedLit"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _OutlineColor ("Outline Color", Color) = (0, 0, 0, 1)
        _OutlineThickness ("Outline Thickness", Float) = 0.03
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalRenderPipeline"
            "RenderType" = "Opaque"
            "UniversalMaterialType" = "Lit"
        }

        LOD 300

        // Outline Pass
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "Always" }

            Cull Front // Render back faces
            ZWrite On
            ZTest LEqual
            ColorMask RGB

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };
            

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 color : COLOR;
            };

            float _OutlineThickness;
            float4 _OutlineColor;

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 normalWS = normalize(TransformObjectToWorldNormal(input.normalOS));
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                positionWS += normalWS * _OutlineThickness;
                output.positionHCS = TransformWorldToHClip(positionWS);
                output.color = _OutlineColor;
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                return input.color;
            }
            ENDHLSL
        }

        // Main Lit Pass
        Pass
        {
            Name "Lit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

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

            float4 _BaseColor;
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv;
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                return texColor * _BaseColor;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
