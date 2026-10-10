Shader "ParkSort/Particle"
{
    Properties
    {
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 1
        _Soft ("Soft Circle (0 = solid quad)", Range(0, 1)) = 1
        _Boost ("Brightness", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent+10" "RenderPipeline" = "UniversalPipeline" }

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

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _SrcBlend;
                float _DstBlend;
                float _Soft;
                float _Boost;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                half4  color      : COLOR;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4  color      : COLOR;
                float2 uv         : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.color = input.color;
                output.uv = input.uv * 2.0 - 1.0;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half falloff = saturate(1.0 - dot(input.uv, input.uv));
                half shape = lerp(1.0, falloff * falloff, _Soft);
                half alpha = input.color.a * shape;
                return half4(input.color.rgb * _Boost * alpha, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
