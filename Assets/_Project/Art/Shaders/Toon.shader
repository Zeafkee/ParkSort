Shader "ParkSort/Toon"
{
    Properties
    {
        _Tint ("Paint Color", Color) = (1, 1, 1, 1)
        _Shine ("Paint Shine", Range(0, 1)) = 0.35
        _Desaturate ("Desaturate", Range(0, 1)) = 0
        _LightDir ("Light Direction", Vector) = (0.4, 0.85, -0.35, 0)
        _LightColor ("Light Color", Color) = (1, 0.97, 0.9, 1)
        _Ambient ("Ambient Color", Color) = (0.5, 0.55, 0.68, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 _PS_LightColor;
            float4 _PS_Ambient;

            CBUFFER_START(UnityPerMaterial)
                float4 _Tint;
                float _Shine;
                float _Desaturate;
                float4 _LightDir;
                float4 _LightColor;
                float4 _Ambient;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half3 color       : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float3 lightDir = normalize(_LightDir.xyz);
                float3 viewDir = normalize(GetWorldSpaceViewDir(positionWS));

                float3 albedo = lerp(input.color.rgb, input.color.rgb * _Tint.rgb, input.color.a);
                float grey = dot(albedo, float3(0.3, 0.59, 0.11));
                albedo = lerp(albedo, float3(grey, grey, grey) * 0.9 + 0.08, _Desaturate);

                float halfLambert = dot(normalWS, lightDir) * 0.5 + 0.5;
                float themed = step(0.5, _PS_LightColor.a);
                float3 lightColor = lerp(_LightColor.rgb, _PS_LightColor.rgb, themed);
                float3 ambient = lerp(_Ambient.rgb, _PS_Ambient.rgb, themed);
                float3 lit = albedo * (ambient + lightColor * halfLambert * halfLambert);

                float3 halfVector = normalize(lightDir + viewDir);
                float highlight = pow(saturate(dot(normalWS, halfVector)), 24.0) * _Shine * input.color.a;
                float rim = pow(1.0 - saturate(dot(normalWS, viewDir)), 3.0) * 0.12;

                output.positionCS = TransformWorldToHClip(positionWS);
                output.color = lit + highlight + rim * albedo;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return half4(input.color, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
