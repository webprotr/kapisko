Shader "Kapisko/WaterfallURP"
{
    Properties
    {
        _BaseColor ("Water Color", Color) = (0.25, 0.8, 1.0, 0.75)
        _MainTex ("Water Texture", 2D) = "white" {}

        _FlowSpeed ("Flow Speed", Float) = 1.2
        _Distortion ("Distortion", Range(0, 0.2)) = 0.04

        _Brightness ("Brightness", Range(0.2, 3)) = 1.4
        _Alpha ("Alpha", Range(0, 1)) = 0.8

        _WaveAmount ("Side Wave", Range(0, 0.2)) = 0.03
        _WaveFrequency ("Wave Frequency", Range(0.1, 20)) = 6
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
            Name "Waterfall"

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

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)

            float4 _BaseColor;
            float4 _MainTex_ST;

            float _FlowSpeed;
            float _Distortion;
            float _Brightness;
            float _Alpha;

            float _WaveAmount;
            float _WaveFrequency;

            CBUFFER_END

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

            Varyings Vert(Attributes input)
            {
                Varyings output;

                float3 pos = input.positionOS.xyz;

                float time = _Time.y;

                pos.x +=
                    sin(
                        pos.y * _WaveFrequency +
                        time * 2.0
                    ) * _WaveAmount;

                VertexPositionInputs posInputs =
                    GetVertexPositionInputs(pos);

                output.positionHCS =
                    posInputs.positionCS;

                output.uv =
                    TRANSFORM_TEX(input.uv, _MainTex);

                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;

                float time = _Time.y;

                // Aşağı doğru akan su
                uv.y -= time * _FlowSpeed;

                // Hafif sağ-sol kırılma
                uv.x +=
                    sin(
                        uv.y * 15.0 +
                        time * 3.0
                    ) * _Distortion;

                half4 tex =
                    SAMPLE_TEXTURE2D(
                        _MainTex,
                        sampler_MainTex,
                        uv
                    );

                float3 color =
                    tex.rgb *
                    _BaseColor.rgb *
                    _Brightness;

                float alpha =
                    tex.a *
                    _BaseColor.a *
                    _Alpha;

                return half4(
                    color,
                    alpha
                );
            }

            ENDHLSL
        }
    }

    FallBack Off
}