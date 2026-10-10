Shader "ParkSort/Ground"
{
    Properties
    {
        _LotRect ("Lot Rect (minX, minZ, maxX, maxZ)", Vector) = (-3, 0, 3, 6)
        _RoadWidth ("Road Width", Float) = 1.3
        _LotColor ("Lot Color", Color) = (0.36, 0.39, 0.47, 1)
        _RoadColor ("Road Color", Color) = (0.44, 0.47, 0.55, 1)
        _LineColor ("Line Color", Color) = (0.95, 0.96, 1, 1)
        _GrassColor ("Grass Color", Color) = (0.56, 0.8, 0.45, 1)
        _GrassColor2 ("Grass Color 2", Color) = (0.51, 0.76, 0.42, 1)
        _CurbColor ("Curb Color", Color) = (0.86, 0.87, 0.92, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry-10" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _LotRect;
                float _RoadWidth;
                float4 _LotColor;
                float4 _RoadColor;
                float4 _LineColor;
                float4 _GrassColor;
                float4 _GrassColor2;
                float4 _CurbColor;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 world      : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.world = positionWS.xz;
                return output;
            }

            float BoxDistance(float2 p, float2 lo, float2 hi)
            {
                float2 center = (lo + hi) * 0.5;
                float2 d = abs(p - center) - (hi - lo) * 0.5;
                return length(max(d, 0.0)) + min(max(d.x, d.y), 0.0);
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 p = input.world;
                float aa = max(fwidth(p.x), fwidth(p.y)) * 1.5;

                float lot = BoxDistance(p, _LotRect.xy, _LotRect.zw);
                float road = BoxDistance(p, _LotRect.xy - _RoadWidth, _LotRect.zw + _RoadWidth) - 0.35;

                float2 checker = floor(p * 0.5);
                float pattern = fmod(abs(checker.x + checker.y), 2.0);
                half3 color = lerp(_GrassColor.rgb, _GrassColor2.rgb, pattern);

                float curb = 1.0 - smoothstep(0.0, aa, abs(road) - 0.07);
                float inRoad = 1.0 - smoothstep(-aa, aa, road);
                color = lerp(color, _RoadColor.rgb, inRoad);
                color = lerp(color, _CurbColor.rgb, curb);

                float inLot = 1.0 - smoothstep(-aa, aa, lot);
                float2 cell = p - _LotRect.xy;
                float2 grid = abs(frac(cell + 0.5) - 0.5);
                float lines = 1.0 - smoothstep(0.02, 0.02 + aa, min(grid.x, grid.y));
                float border = 1.0 - smoothstep(0.0, aa, abs(lot) - 0.035);
                half3 lotColor = lerp(_LotColor.rgb, _LineColor.rgb, lines * 0.16);
                color = lerp(color, lotColor, inLot);
                color = lerp(color, _LineColor.rgb, border * 0.85);

                float2 dash = p - _LotRect.xy;
                float ringDistance = abs(lot - _RoadWidth * 0.5);
                float along = (abs(p.x - (_LotRect.x + _LotRect.z) * 0.5) > (_LotRect.z - _LotRect.x) * 0.5) ? dash.y : dash.x;
                float dashes = step(0.5, frac(along * 0.5));
                float centre = (1.0 - smoothstep(0.03, 0.03 + aa, ringDistance)) * dashes * inRoad * (1.0 - inLot);
                color = lerp(color, _LineColor.rgb, centre * 0.55);

                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
