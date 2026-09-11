Shader "Custom/HologramDepthGrid"
{
    Properties
    {
        [HDR] _GridColor ("Grid Color", Color) = (0.1, 0.65, 1.0, 1.0)
        _EmissionMultiplier ("Emission Multiplier", Range(0.0, 8.0)) = 0.0
        _GridDensity ("Grid Density (Cols, Rows)", Vector) = (24.0, 13.5, 0.0, 0.0)
        _LineWidth ("Line Width", Range(0.001, 0.05)) = 0.015
        _GridLinesIntensity ("Gridlines Intensity", Range(0.0, 1.0)) = 0.0
        _CrosshairSize ("Crosshair Size", Range(0.0, 0.3)) = 0.0
        _CrosshairIntensity ("Crosshair Intensity", Range(0.0, 1.0)) = 0.0
        _GimbalRadius ("Gimbal Radius", Range(0.05, 0.5)) = 0.28
        _GimbalSpeed ("Gimbal Rotation Speed", Float) = 0.35
        _CircleIntensity ("Circle / Gimbal Intensity", Range(0.0, 1.0)) = 0.0
        _EdgeFadeDist ("Edge Fade Distance", Range(0.01, 0.3)) = 0.1
        _PulseFrequency ("Pulse Frequency", Float) = 1.5
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent+105" 
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            Name "DepthGridPass"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _GridColor;
                float4 _GridDensity;
                float _EmissionMultiplier;
                float _LineWidth;
                float _GridLinesIntensity;
                float _CrosshairSize;
                float _CrosshairIntensity;
                float _GimbalRadius;
                float _GimbalSpeed;
                float _CircleIntensity;
                float _EdgeFadeDist;
                float _PulseFrequency;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positionInputs.positionCS;
                output.uv = input.uv;
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;

                // 1. Soft Edge Feathering toward quad borders
                float2 edgeDist = min(uv, 1.0 - uv);
                float edgeFade = smoothstep(0.0, _EdgeFadeDist, min(edgeDist.x, edgeDist.y));
                if (edgeFade <= 0.0001)
                {
                    return float4(0.0, 0.0, 0.0, 0.0);
                }

                // 2. Coordinate Grid Lines & Glowing Node Intersections
                float2 gUV = uv * _GridDensity.xy;
                float2 cellFract = abs(frac(gUV) - 0.5);

                float lineX = smoothstep(_LineWidth * 0.5, 0.0, cellFract.x * (1.0 / _GridDensity.x));
                float lineY = smoothstep(_LineWidth * 0.5, 0.0, cellFract.y * (1.0 / _GridDensity.y));

                float nodeDots = lineX * lineY * 2.2;
                float gridLines = (max(lineX, lineY) * 0.5 + nodeDots);

                // 3. Central HUD Crosshair Lines with Measurement Notches
                float2 centerOffset = abs(uv - 0.5);
                float chX = smoothstep(_LineWidth * 0.6, 0.0, centerOffset.y) * step(centerOffset.x, _CrosshairSize);
                float chY = smoothstep(_LineWidth * 0.6, 0.0, centerOffset.x) * step(centerOffset.y, _CrosshairSize);

                // Crosshair tick notches
                float tickX = smoothstep(0.003, 0.0, abs(frac(centerOffset.x * 40.0) - 0.5)) * step(centerOffset.x, _CrosshairSize) * smoothstep(0.008, 0.0, centerOffset.y);
                float tickY = smoothstep(0.003, 0.0, abs(frac(centerOffset.y * 40.0) - 0.5)) * step(centerOffset.y, _CrosshairSize) * smoothstep(0.008, 0.0, centerOffset.x);
                float crosshair = (max(chX, chY) * 1.5 + (tickX + tickY) * 0.8);

                // 4. Rotating Gimbal Ring (Aspect-corrected for 16:9 widescreen)
                float2 p = (uv - 0.5) * float2(16.0 / 9.0, 1.0);
                float r = length(p);
                float theta = atan2(p.y, p.x);

                // Outer and inner concentric gimbal circles
                float ringCore = smoothstep(_LineWidth * 0.6, 0.0, abs(r - _GimbalRadius));
                float ringBloom = smoothstep(_LineWidth * 2.5, 0.0, abs(r - _GimbalRadius)) * 0.45;
                float innerGimbal = smoothstep(_LineWidth * 0.4, 0.0, abs(r - (_GimbalRadius * 0.72))) * 0.6;

                // Rotating degree telemetry notches
                float rotAngle1 = theta + (_Time.y * _GimbalSpeed);
                float rotAngle2 = theta - (_Time.y * _GimbalSpeed * 1.35);

                float ticks1 = pow(abs(cos(rotAngle1 * 16.0)), 20.0) * smoothstep(0.016, 0.0, abs(r - _GimbalRadius));
                float ticks2 = pow(abs(cos(rotAngle2 * 32.0)), 24.0) * smoothstep(0.012, 0.0, abs(r - (_GimbalRadius * 0.72)));

                // 4 Cardinal telemetry markers
                float cardinalMarks = pow(abs(cos(rotAngle1 * 2.0)), 32.0) * smoothstep(0.025, 0.0, abs(r - _GimbalRadius)) * 1.8;

                float gimbal = (ringCore * 1.4 + ringBloom + innerGimbal + ticks1 * 1.6 + ticks2 * 1.2 + cardinalMarks);

                // 5. Subtle Breathing Pulse
                float pulse = 0.88 + 0.12 * sin(_Time.y * _PulseFrequency);

                // 6. Holographic Composite
                float totalIntensity = (gridLines * _GridLinesIntensity + crosshair * _CrosshairIntensity + gimbal * _CircleIntensity) * pulse * edgeFade;
                float3 finalColor = _GridColor.rgb * totalIntensity * _EmissionMultiplier;

                // 100% Additive: Blends over scene with no dark backing box
                return float4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
