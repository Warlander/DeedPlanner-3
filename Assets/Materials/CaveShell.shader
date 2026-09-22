Shader "DeedPlanner/Cave Shell"
{
    Properties
    {
        [NoScaleOffset] _MainTex("Cave Textures", 2DArray) = "white" {}
        [NoScaleOffset] _NormalArray("Cave Normals", 2DArray) = "" {}
        _NormalStrength("Normal Strength", Range(0, 1)) = 1
        _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 2
        [HideInInspector] _CaveOverview("Cave Overview", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "CaveShell"
            Tags { "LightMode" = "UniversalForward" }
            Cull [_Cull]
            ZWrite On

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma target 3.5
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D_ARRAY(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D_ARRAY(_NormalArray);
            SAMPLER(sampler_NormalArray);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _CaveOverview;
                half _NormalStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float2 textureIndex : TEXCOORD1;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                nointerpolation float2 textureData : TEXCOORD1;
                float fogFactor : TEXCOORD2;
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.textureData = input.textureIndex;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                half physicalVisibility = abs(input.textureData.y - 1.0h);
                half overviewVisibility = 1.0h - step(1.5h, input.textureData.y);
                clip(lerp(physicalVisibility, overviewVisibility, _CaveOverview) - 0.5h);
                half4 color = SAMPLE_TEXTURE2D_ARRAY(_MainTex, sampler_MainTex, input.uv, input.textureData.x);
                color.rgb *= _BaseColor.rgb;
                color.rgb *= lerp(0.55h, 1.0h, abs(input.textureData.y - 1.0h));
                half4 packedNormal = SAMPLE_TEXTURE2D_ARRAY(_NormalArray, sampler_NormalArray, input.uv, input.textureData.x);
                half3 normalTS = normalize(packedNormal.rgb * 2.0h - 1.0h);
                // Fixed texture-space light adds relief without darkening the unlit cave interior.
                half3 detailLight = normalize(half3(-0.4h, 0.6h, 0.7h));
                half detail = clamp(1.0h + 0.3h * (dot(normalTS, detailLight) - detailLight.z), 0.85h, 1.15h);
                color.rgb *= lerp(1.0h, detail, packedNormal.a * _NormalStrength);
                color.rgb = MixFog(color.rgb, input.fogFactor);
                return color;
            }
            ENDHLSL
        }
    }
}
