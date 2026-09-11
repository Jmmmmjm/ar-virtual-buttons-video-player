Shader "Custom/HologramProjectorBeam"
{
    Properties
    {
        [HDR] _BeamColor ("Beam Color", Color) = (0.1, 0.7, 1.0, 0.4)
        _Intensity ("Intensity", Float) = 2.0
        _ScrollSpeed ("Scroll Speed", Float) = 1.2
        _FalloffPower ("Vertical Falloff Power", Float) = 1.8
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

        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            Name "ProjectorBeamPass"
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

            CBUFFER_START(UnityPerMaterial)
                float4 _BeamColor;
                float _Intensity;
                float _ScrollSpeed;
                float _FalloffPower;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = positionInputs.positionCS;
                output.uv = input.uv;
                output.worldNormal = normalInputs.normalWS;
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(positionInputs.positionWS);

                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                // Vertical gradient fading smoothly from base (y=0) to screen bottom (y=1)
                float verticalGradient = pow(saturate(1.0 - input.uv.y), _FalloffPower);

                // Animated light ray waves
                float wave = sin((input.uv.y * 14.0) - (_Time.y * _ScrollSpeed * 4.0)) * 0.5 + 0.5;
                float beamPattern = 0.75 + 0.25 * wave;

                // Edge glancing intensity (Fresnel-like)
                float3 normal = normalize(input.worldNormal);
                float3 viewDir = normalize(input.viewDirWS);
                float glancing = pow(1.0 - abs(dot(normal, viewDir)), 1.5);

                // Soft horizontal edge fade to ensure beam dissipates seamlessly into air
                float hFade = smoothstep(0.0, 0.18, min(input.uv.x, 1.0 - input.uv.x));

                float3 rgb = _BeamColor.rgb * _Intensity * verticalGradient * beamPattern * (glancing * 1.2 + 0.3) * hFade;
                return float4(rgb, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
