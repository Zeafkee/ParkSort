Shader "ParkSort/Blob"
{
    Properties
    {
        _Color ("Color", Color) = (0, 0, 0, 0.5)
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 10
        _Power ("Falloff Power", Float) = 2
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            Blend [_SrcBlend] [_DstBlend]
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _SrcBlend;
                float _DstBlend;
                float _Power;
            #ifndef UNITY_INSTANCING_ENABLED
                float4 _Color;
            #endif
            CBUFFER_END

            #ifdef UNITY_INSTANCING_ENABLED
                UNITY_INSTANCING_BUFFER_START(PerInstance)
                    UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
                UNITY_INSTANCING_BUFFER_END(PerInstance)
                #define COLOR UNITY_ACCESS_INSTANCED_PROP(PerInstance, _Color)
            #else
                #define COLOR _Color
            #endif

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                half4  color      : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv * 2.0 - 1.0;
                output.color = COLOR;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half falloff = saturate(1.0 - dot(input.uv, input.uv));
                falloff = pow(falloff, _Power);
                return half4(input.color.rgb * falloff * input.color.a, falloff * input.color.a);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
