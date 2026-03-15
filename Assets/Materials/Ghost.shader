Shader "DeedPlanner/Ghost"
{
    Properties
    {
        [MainTexture]
        _BaseMap   ("Texture",  2D)    = "white" {}
        [MainColor][HDR]
        _BaseColor ("Color",   Color) = (0, 1, 0, 0.3)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType"     = "Transparent"
            "Queue"          = "Transparent"
        }

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }

            Blend  SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull   Back

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4  _BaseColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv         = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 texSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);

                half4 color;
                color.rgb = texSample.rgb * _BaseColor.rgb;
                // Multiply texture alpha (silhouette cutout) with the ghost alpha.
                // If the texture has no alpha channel (DXT1), texSample.a == 1 and
                // the entire mesh renders at the ghost's configured opacity.
                color.a = texSample.a * _BaseColor.a;
                return color;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
