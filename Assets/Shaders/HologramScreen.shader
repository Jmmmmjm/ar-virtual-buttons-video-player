Shader "Custom/HologramScreen"
{
    Properties
    {
        _BaseMap ("Video / Screen Texture", 2D) = "black" {}
        [HDR] _HoloColor ("Hologram Tint Color", Color) = (0.2, 0.85, 1.0, 1.0)
        _Brightness ("Brightness Multiplier", Range(1.0, 6.0)) = 3.2
        _EmissionMultiplier ("HDR Emission Multiplier", Range(1.0, 8.0)) = 2.5
        _Saturation ("Saturation Boost", Range(0.5, 3.0)) = 1.4
        _Alpha ("Base Transparency", Range(0.1, 1.0)) = 0.95
        _ScanlineFrequency ("Scanline Frequency", Float) = 220.0
        _ScanlineSpeed ("Scanline Scroll Speed", Float) = 2.5
        _ScanlineIntensity ("Scanline Depth", Range(0.0, 1.0)) = 0.22
        _GlitchIntensity ("Glitch Intensity", Range(0.0, 1.0)) = 0.0
        _EdgeFadeDist ("Edge Fade Distance", Range(0.001, 0.1)) = 0.025
        _SweepSpeed ("Laser Sweep Speed", Float) = 1.2
        _SweepWidth ("Laser Sweep Width", Range(0.01, 0.3)) = 0.08
        _SweepIntensity ("Laser Sweep Intensity", Range(0.0, 2.0)) = 0.45
        _PhosphorGlow ("Phosphor Glow", Range(0.0, 1.0)) = 0.35
        _ChromaticJitter ("Chromatic Jitter Base", Range(0.0, 0.02)) = 0.002

        // Upgraded Holographic Features
        _HexGridIntensity ("Hex Grid Intensity", Range(0.0, 1.0)) = 0.35
        _HexGridScale ("Hex Grid Density", Float) = 45.0
        _MacroblockGlitch ("Macroblock Glitch Intensity", Range(0.0, 1.0)) = 0.0
        _FresnelIridescence ("Grazing Iridescence", Range(0.0, 2.0)) = 0.85
        _SecondarySweepSpeed ("Secondary Sweep Speed", Float) = -2.6
        _SecondarySweepWidth ("Secondary Sweep Width", Range(0.01, 0.2)) = 0.04
        _SecondarySweepIntensity ("Secondary Sweep Intensity", Range(0.0, 2.0)) = 0.35
        _AnamorphicStreak ("Anamorphic Streak", Range(0.0, 1.0)) = 0.25
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent+100" 
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "HologramScreenPass"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 worldNormal: TEXCOORD1;
                float3 viewDirWS  : TEXCOORD3;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _HoloColor;
                float _Brightness;
                float _EmissionMultiplier;
                float _Saturation;
                float _Alpha;
                float _ScanlineFrequency;
                float _ScanlineSpeed;
                float _ScanlineIntensity;
                float _GlitchIntensity;
                float _EdgeFadeDist;
                float _SweepSpeed;
                float _SweepWidth;
                float _SweepIntensity;
                float _PhosphorGlow;
                float _ChromaticJitter;

                float _HexGridIntensity;
                float _HexGridScale;
                float _MacroblockGlitch;
                float _FresnelIridescence;
                float _SecondarySweepSpeed;
                float _SecondarySweepWidth;
                float _SecondarySweepIntensity;
                float _AnamorphicStreak;
            CBUFFER_END

            float PseudoRandom(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            // Regular hexagon perimeter distance function
            float HexDist(float2 p)
            {
                const float2 s = float2(1.0, 1.7320508);
                const float2 h = s * 0.5;
                float2 a = (frac(p / s) - 0.5) * s;
                float2 b = (frac((p - h) / s) - 0.5) * s;
                float2 gv = dot(a, a) < dot(b, b) ? a : b;
                
                // Distance to hexagon border (edge radius = 0.5)
                return max(abs(gv.x), dot(abs(gv), float2(0.5, 0.8660254)));
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                float3 posOS = input.positionOS.xyz;
                float totalGlitch = max(_GlitchIntensity, _MacroblockGlitch);
                if (totalGlitch > 0.001)
                {
                    float noise = (PseudoRandom(float2(_Time.y * 18.0, input.positionOS.y * 12.0)) - 0.5) * 0.006 * totalGlitch;
                    posOS.x += noise;
                }

                VertexPositionInputs positionInputs = GetVertexPositionInputs(posOS);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = positionInputs.positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.worldNormal = normalInputs.normalWS;
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(positionInputs.positionWS);

                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float totalGlitch = max(_GlitchIntensity, _MacroblockGlitch);

                // 1. Soft borderless edge fade: seamlessly dissolves outer quad boundary
                float2 edgeDist = min(uv, 1.0 - uv);
                float edgeFade = smoothstep(0.0, _EdgeFadeDist, min(edgeDist.x, edgeDist.y));

                // 2. 2D Macroblock Glitch & Matrix Corruption
                float2 glitchedUV = uv;
                float blockInvert = 0.0;

                if (totalGlitch > 0.001)
                {
                    float2 blockGrid = float2(32.0, 18.0);
                    float2 blockID = floor(uv * blockGrid);
                    float blockTime = floor(_Time.y * 16.0);
                    float blockNoise = PseudoRandom(blockID + float2(blockTime * 0.13, blockTime * 0.27));
                    
                    if (blockNoise > (1.0 - totalGlitch * 0.55))
                    {
                        float2 blockDisp = float2(
                            PseudoRandom(blockID + float2(1.17, blockTime)) - 0.5,
                            PseudoRandom(blockID + float2(blockTime, 3.41)) - 0.5
                        );
                        glitchedUV += blockDisp * float2(0.08, 0.03) * totalGlitch;
                        blockInvert = step(0.78, blockNoise);
                    }
                }

                // 3. Chromatic Aberration & High-Frequency Jitter
                float micro1 = sin(_Time.y * 56.0) * cos(_Time.y * 37.0);
                float micro2 = PseudoRandom(glitchedUV + _Time.y * 10.0) - 0.5;
                float idleJitter = (micro1 * 0.7 + micro2 * 0.3) * _ChromaticJitter;

                float sliceBlock = floor(glitchedUV.y * 28.0);
                float sliceNoise = (PseudoRandom(float2(sliceBlock, floor(_Time.y * 24.0))) - 0.5) * 0.04 * totalGlitch;

                float totalChromOffset = idleJitter + totalGlitch * 0.028;
                float2 uvR = glitchedUV + float2(totalChromOffset + sliceNoise, 0.0);
                float2 uvG = glitchedUV + float2(sliceNoise * 0.25, 0.0);
                float2 uvB = glitchedUV - float2(totalChromOffset - sliceNoise * 0.5, 0.0);

                // 4. Center Video Sample & Matrix Inversion
                float rCenter = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvR).r;
                float gCenter = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvG).g;
                float bCenter = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvB).b;
                float3 centerRGB = float3(rCenter, gCenter, bCenter);

                if (blockInvert > 0.5)
                {
                    centerRGB = lerp(centerRGB, float3(1.0 - centerRGB.g, 1.0 - centerRGB.b, 1.0 - centerRGB.r), totalGlitch);
                }

                // 5. Multi-tap Phosphor Glow Sampling
                float glowOffset = 0.0035 * (1.0 + totalGlitch * 2.0);
                float3 tapL1 = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, glitchedUV + float2(-glowOffset, 0.0)).rgb;
                float3 tapR1 = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, glitchedUV + float2( glowOffset, 0.0)).rgb;
                float3 tapL2 = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, glitchedUV + float2(-glowOffset * 2.2, 0.0)).rgb;
                float3 tapR2 = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, glitchedUV + float2( glowOffset * 2.2, 0.0)).rgb;

                float3 phosphorBloom = (tapL1 + tapR1) * 0.32 + (tapL2 + tapR2) * 0.18;
                float3 rawVideoRGB = centerRGB + phosphorBloom * _PhosphorGlow;

                // 6. Horizontal Anamorphic Bloom Streaks
                float streakOffset1 = 0.035;
                float streakOffset2 = 0.085;
                float3 streakL1 = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, glitchedUV + float2(-streakOffset1, 0.0)).rgb;
                float3 streakR1 = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, glitchedUV + float2( streakOffset1, 0.0)).rgb;
                float3 streakL2 = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, glitchedUV + float2(-streakOffset2, 0.0)).rgb;
                float3 streakR2 = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, glitchedUV + float2( streakOffset2, 0.0)).rgb;

                float3 brightL1 = max(0.0, streakL1 - 0.40);
                float3 brightR1 = max(0.0, streakR1 - 0.40);
                float3 brightL2 = max(0.0, streakL2 - 0.55);
                float3 brightR2 = max(0.0, streakR2 - 0.55);
                float3 anamorphicBloom = ((brightL1 + brightR1) * 0.6 + (brightL2 + brightR2) * 0.4) * _AnamorphicStreak * 2.5;

                // 7. Color Saturation Boost
                float rawLuma = dot(rawVideoRGB, float3(0.299, 0.587, 0.114));
                float3 saturatedVideo = lerp(float3(rawLuma, rawLuma, rawLuma), rawVideoRGB, _Saturation);

                // 8. Procedural Hexagonal Grid (Light-field projector nano-honeycomb)
                float2 hexUV = float2(uv.x * 1.7777778, uv.y) * _HexGridScale;
                float hexD = HexDist(hexUV);
                float hexBorder = smoothstep(0.41, 0.49, hexD);
                float hexCenter = smoothstep(0.09, 0.0, hexD) * 0.4;
                float hexPattern = hexBorder + hexCenter;
                float hexGlow = hexPattern * _HexGridIntensity * smoothstep(0.04, 0.5, rawLuma);

                // 9. Dynamic Hologram Scanlines with Subtle Interference Bands
                float scanPhase = (uv.y * _ScanlineFrequency) + (_Time.y * _ScanlineSpeed);
                float scanMicro = 0.5 + 0.5 * cos(scanPhase);
                scanMicro = pow(scanMicro, 1.35); // Sharp high-density raster ridges

                float band1 = sin((uv.y * 14.0) - (_Time.y * 1.1));
                float band2 = sin((uv.y * 31.0) + (_Time.y * 2.3));
                float interference = 0.86 + 0.14 * (band1 * 0.6 + band2 * 0.4);
                float scanFactor = 1.0 - (_ScanlineIntensity * (1.0 - scanMicro * interference));

                // 10. Dual Counter-Sweeping Laser Beams with Dynamic Interference Fringes
                // Primary sweep: sweeps downward
                float sweepProgress1 = frac(_Time.y * _SweepSpeed);
                float sweepDist1 = abs(uv.y - (1.0 - sweepProgress1));
                float laserCore1 = smoothstep(_SweepWidth * 0.25, 0.0, sweepDist1);
                float laserBloom1 = smoothstep(_SweepWidth, 0.0, sweepDist1);
                float sweep1 = (laserCore1 * 1.8 + laserBloom1 * 0.7) * _SweepIntensity;

                // Secondary sweep: high-speed telemetry pulse sweeping upward
                float sweepProgress2 = frac(_Time.y * abs(_SecondarySweepSpeed));
                float sweepDist2 = abs(uv.y - sweepProgress2);
                float laserCore2 = smoothstep(_SecondarySweepWidth * 0.25, 0.0, sweepDist2);
                float laserBloom2 = smoothstep(_SecondarySweepWidth, 0.0, sweepDist2);
                float sweep2 = (laserCore2 * 2.2 + laserBloom2 * 0.8) * _SecondarySweepIntensity;

                // Interference fringes where sweeps cross
                float crossOverlap = sweep1 * sweep2;
                float crossFringe = sin(uv.y * 320.0 + _Time.y * 45.0) * 0.5 + 0.5;
                float crossInterference = crossOverlap * (2.0 + 3.5 * crossFringe);
                float totalLaserSweep = sweep1 + sweep2 + crossInterference;

                // 11. Iridescent Grazing Diffraction (Fresnel Spectral Fringe)
                float3 normalWS = normalize(input.worldNormal);
                float3 viewDirWS = normalize(input.viewDirWS);
                float NdotV = abs(dot(normalWS, viewDirWS));
                float fresnelGrazing = pow(1.0 - saturate(NdotV), 2.5);
                float3 spectralCos = 0.5 + 0.5 * cos(6.28318 * (fresnelGrazing + float3(0.0, 0.33, 0.67)));
                float3 iridescentFringe = spectralCos * fresnelGrazing * _FresnelIridescence;

                // 12. Subtle Hologram Noise & Flicker
                float flicker = 1.0 - (PseudoRandom(float2(_Time.y * 2.5, 0.5)) * 0.035 * (1.0 + totalGlitch * 3.0));

                // 13. HDR Holographic Composite
                float3 holoTint = _HoloColor.rgb;
                float3 tintedVideo = saturatedVideo * holoTint;

                // Crisp HDR core punch to bright graphics
                tintedVideo += saturatedVideo * max(0.0, rawLuma - 0.35) * 0.45;

                // Anamorphic horizontal streak
                tintedVideo += anamorphicBloom * holoTint;

                // Base holographic color
                float3 finalRGB = tintedVideo * scanFactor * flicker;

                // Hex grid optical light-field nano-structure
                finalRGB += (holoTint * 1.5 + float3(0.2, 0.4, 0.6)) * hexGlow;

                // Traveling laser sweep energy (illuminates active pixels and cross-interference)
                finalRGB += (finalRGB * 1.3 + holoTint * (rawLuma + 0.1) * 0.6) * totalLaserSweep;

                // Grazing Iridescence dispersion
                finalRGB += iridescentFringe * (rawLuma * 1.5 + 0.08);

                // Brightness & HDR emission multiplier & edge fade
                finalRGB *= (_Brightness * _EmissionMultiplier * edgeFade);

                // 14. Active Pixel Alpha Curve: pow(luma, 0.65) * 2.2
                // Solid, vivid HUD graphics and video content with zero dark background box
                float activeAlpha = saturate(pow(max(0.0, rawLuma), 0.65) * 2.2);
                activeAlpha = saturate(activeAlpha * (1.0 + totalLaserSweep * 0.25) + fresnelGrazing * 0.35 * _FresnelIridescence);
                float finalAlpha = activeAlpha * _Alpha * edgeFade;

                return float4(finalRGB, finalAlpha);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
