Shader "Custom/HologramEmitterRing"
{
    Properties
    {
        [HDR] _BaseColor ("Base Color", Color) = (0.1, 0.85, 1.0, 1.0)
        _EmissionMultiplier ("Emission Multiplier", Range(1.0, 10.0)) = 3.0
        _InnerRadius ("Inner Radius", Range(0.05, 0.5)) = 0.15
        _OuterRadius ("Outer Radius", Range(0.2, 0.5)) = 0.48
        _TickCount ("Tick Count", Float) = 48.0
        _PulseSpeed ("Pulse Speed", Float) = 2.0
        _RotationSpeed ("Rotation Speed", Float) = 0.5
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent+110" 
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            Name "EmitterRingPass"
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
                float4 _BaseColor;
                float _EmissionMultiplier;
                float _InnerRadius;
                float _OuterRadius;
                float _TickCount;
                float _PulseSpeed;
                float _RotationSpeed;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positionInputs.positionCS;
                output.uv = input.uv;
                return output;
            }

            float DrawRing(float r, float targetR, float width)
            {
                return smoothstep(width, 0.0, abs(r - targetR));
            }

            float4 frag(Varyings input) : SV_Target
            {
                // Centered coordinates (-0.5 to 0.5)
                float2 p = input.uv - float2(0.5, 0.5);
                float r = length(p);
                float theta = atan2(p.y, p.x);

                // Early discard / mask outside mandala radius to keep dark regions pure black (0 additive)
                float overallMask = smoothstep(_InnerRadius - 0.015, _InnerRadius, r) * smoothstep(_OuterRadius + 0.015, _OuterRadius, r);
                if (overallMask <= 0.0001)
                {
                    return float4(0.0, 0.0, 0.0, 0.0);
                }

                // 1. Concentric Glowing Boundary & Harmonic Rings
                float ringInnerCore = DrawRing(r, _InnerRadius, 0.005) * 2.0;
                float ringInnerGlow = DrawRing(r, _InnerRadius, 0.018) * 0.5;
                float ringOuterCore = DrawRing(r, _OuterRadius, 0.006) * 2.2;
                float ringOuterGlow = DrawRing(r, _OuterRadius, 0.022) * 0.6;

                float rHarmonic1 = lerp(_InnerRadius, _OuterRadius, 0.32);
                float rHarmonic2 = lerp(_InnerRadius, _OuterRadius, 0.65);
                float rHarmonic3 = lerp(_InnerRadius, _OuterRadius, 0.82);

                float ringMid1 = DrawRing(r, rHarmonic1, 0.0035) * 1.2;
                float ringMid2 = DrawRing(r, rHarmonic2, 0.003) * 0.9;
                float ringMid3 = DrawRing(r, rHarmonic3, 0.0025) * 0.7;

                float concentricRings = ringInnerCore + ringInnerGlow + ringOuterCore + ringOuterGlow + ringMid1 + ringMid2 + ringMid3;

                // 2. Rotating Degree Notches & Telemetry Ticks
                // Primary clockwise rotating outer dial
                float thetaRot1 = theta + (_Time.y * _RotationSpeed);
                float ticksCos1 = cos(thetaRot1 * _TickCount);
                float tickPattern1 = smoothstep(0.70, 0.96, ticksCos1);
                float tickMask1 = step(rHarmonic3, r) * step(r, _OuterRadius - 0.005);
                float dialTicks = tickPattern1 * tickMask1 * 1.5;

                // Secondary counter-clockwise inner telemetry notches (compass subdivision)
                float thetaRot2 = theta - (_Time.y * _RotationSpeed * 0.75);
                float ticksCos2 = cos(thetaRot2 * (_TickCount * 0.5));
                float tickPattern2 = smoothstep(0.82, 0.98, ticksCos2);
                float tickMask2 = step(_InnerRadius + 0.005, r) * step(r, rHarmonic1);
                float innerTicks = tickPattern2 * tickMask2 * 1.3;

                // 4 Cardinal Axis Pointer Notches
                float cardinalCos = pow(abs(cos(thetaRot1 * 2.0)), 32.0);
                float cardinalTicks = cardinalCos * step(_InnerRadius - 0.008, r) * step(r, _OuterRadius + 0.008) * 2.0;

                // 3. Pulsing Radial Energy Waves
                // Shockwaves propagating smoothly from inner ring to outer ring
                float wavePhase = frac(r * 14.0 - (_Time.y * _PulseSpeed));
                float waveBloom = pow(wavePhase, 2.8) * smoothstep(_InnerRadius, rHarmonic2, r) * smoothstep(_OuterRadius, rHarmonic2, r);
                float radialWaves = waveBloom * 1.8;

                // Micro radar sweep needle
                float radarAngle = frac((thetaRot1 / 6.2831853) + 0.5);
                float radarSweep = pow(radarAngle, 12.0) * step(_InnerRadius, r) * step(r, _OuterRadius) * 0.6;

                // Breathing brightness modulation
                float breath = 0.88 + 0.12 * sin(_Time.y * 3.5);

                // 4. Composition & HDR Emission
                float totalIntensity = (concentricRings + dialTicks + innerTicks + cardinalTicks + radialWaves + radarSweep) * breath * overallMask;
                float3 finalColor = _BaseColor.rgb * totalIntensity * _EmissionMultiplier;

                // 100% Additive: Black adds 0, preserving postcard artwork underneath completely
                return float4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
