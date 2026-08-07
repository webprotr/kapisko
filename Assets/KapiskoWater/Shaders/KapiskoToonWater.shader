Shader "Kapisko/ToonWaterURP"
{
    Properties
    {
        [Header(Water)]
        _BaseColor ("Water Color", Color) = (0.1, 0.65, 1.0, 0.8)
        _MainTex ("Water Texture", 2D) = "white" {}

        [Header(Texture Movement)]
        _ScrollSpeedX ("Scroll Speed X", Float) = 0.03
        _ScrollSpeedY ("Scroll Speed Y", Float) = 0.02
        _Distortion ("Texture Distortion", Range(0, 0.2)) = 0.025

        [Header(Waves)]
        _WaveSpeed ("Wave Speed", Float) = 0.8
        _WaveStrength ("Wave Strength", Range(0, 0.5)) = 0.04
        _WaveFrequency ("Wave Frequency", Range(0.1, 10)) = 1.5

        [Header(Toon)]
        _LightColor ("Light Water Color", Color) = (0.35, 0.9, 1.0, 1)
        _DarkColor ("Dark Water Color", Color) = (0.02, 0.3, 0.65, 1)
        _ColorSteps ("Color Steps", Range(2, 8)) = 4
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "RenderPipeline"="UniversalPipeline"
        }

        Pass
        {
            Name "ForwardLit"

            Tags
            {
                "LightMode"="UniversalForward"
            }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)

                float4 _BaseColor;
                float4 _LightColor;
                float4 _DarkColor;
                float4 _MainTex_ST;

                float _ScrollSpeedX;
                float _ScrollSpeedY;
                float _Distortion;

                float _WaveSpeed;
                float _WaveStrength;
                float _WaveFrequency;

                float _ColorSteps;

            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;

                float3 positionOS = input.positionOS.xyz;

                float time = _Time.y * _WaveSpeed;

                float wave1 =
                    sin(positionOS.x * _WaveFrequency + time);

                float wave2 =
                    cos(positionOS.z * (_WaveFrequency * 0.75)
                    + time * 1.25);

                positionOS.y +=
                    (wave1 + wave2) * _WaveStrength;

                VertexPositionInputs positionInputs =
                    GetVertexPositionInputs(positionOS);

                VertexNormalInputs normalInputs =
                    GetVertexNormalInputs(input.normalOS);

                output.positionHCS =
                    positionInputs.positionCS;

                output.normalWS =
                    normalInputs.normalWS;

                output.uv =
                    TRANSFORM_TEX(input.uv, _MainTex);

                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float time = _Time.y;

                float2 uv = input.uv;

                uv.x += time * _ScrollSpeedX;
                uv.y += time * _ScrollSpeedY;

                float distortionX =
                    sin(uv.y * 10.0 + time) * _Distortion;

                float distortionY =
                    cos(uv.x * 10.0 + time) * _Distortion;

                uv += float2(
                    distortionX,
                    distortionY
                );

                half4 tex =
                    SAMPLE_TEXTURE2D(
                        _MainTex,
                        sampler_MainTex,
                        uv
                    );

                Light mainLight = GetMainLight();

                float3 normal =
                    normalize(input.normalWS);

                float lightAmount =
                    saturate(
                        dot(normal, mainLight.direction)
                    );

                float steps =
                    max(_ColorSteps, 2.0);

                float toon =
                    floor(lightAmount * steps) /
                    max(steps - 1.0, 1.0);

                toon = saturate(toon);

                float3 toonColor =
                    lerp(
                        _DarkColor.rgb,
                        _LightColor.rgb,
                        toon
                    );

                float3 color =
                    tex.rgb *
                    _BaseColor.rgb *
                    toonColor;

                float alpha =
                    tex.a *
                    _BaseColor.a;

                return half4(color, alpha);
            }

            ENDHLSL
        }
    }

    FallBack Off
}