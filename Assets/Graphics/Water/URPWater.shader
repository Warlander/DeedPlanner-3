Shader "DeedPlanner/URPWater"
{
    Properties
    {
        // --- Color / transparency ---
        _BaseColor          ("Base Color",              Color)          = (0.15, 0.45, 0.70, 0.65)
        _DeepColor          ("Deep Water Color",        Color)          = (0.02, 0.10, 0.28, 0.90)
        _DepthFade          ("Depth Fade Distance",     Float)          = 2.0

        // --- Normal maps (waves) ---
        _NormalMap          ("Normal Map",              2D)             = "bump" {}
        _NormalMap2         ("Normal Map 2",            2D)             = "bump" {}
        _NormalScale        ("Normal Scale",            Float)          = 0.5
        _ScrollSpeed1       ("Scroll Speed Layer 1",    Vector)         = (0.03, 0.02, 0, 0)
        _ScrollSpeed2       ("Scroll Speed Layer 2",    Vector)         = (-0.02, 0.03, 0, 0)
        _NormalTiling       ("Normal Tiling",           Float)          = 0.15

        // --- Fresnel ---
        _FresnelPower       ("Fresnel Power",           Float)          = 4.0
        _FresnelBias        ("Fresnel Bias",            Range(0,1))     = 0.02

        // --- Specular ---
        _SpecColor          ("Specular Color",          Color)          = (0.9, 0.9, 0.9, 1.0)
        _Shininess          ("Shininess",               Float)          = 128.0

        // --- Planar reflections (Ultra / PLANAR_REFLECTIONS keyword) ---
        _ReflectionTex      ("Reflection Texture",      2D)             = "black" {}
        _ReflectionStrength ("Reflection Strength",     Range(0,1))     = 0.85

        // --- Shore highlight (readability in map editor) ---
        _ShoreColor         ("Shore Highlight Color",   Color)          = (0.50, 0.80, 0.92, 1.0)
        _ShoreFadeDistance  ("Shore Fade Distance",     Float)          = 0.5
        _ShoreStrength      ("Shore Strength",          Range(0,1))     = 0.65
        _MinWaterAlpha      ("Min Water Alpha",         Range(0,1))     = 0.50
    }

    SubShader
    {
        Tags
        {
            "RenderType"        = "Transparent"
            "Queue"             = "Transparent"
            "RenderPipeline"    = "UniversalPipeline"
            "IgnoreProjector"   = "True"
        }

        Pass
        {
            Name "WaterForward"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex WaterVert
            #pragma fragment WaterFrag
            #pragma multi_compile_instancing
            #pragma multi_compile _ PLANAR_REFLECTIONS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            // -------------------------------------------------------------------
            // Textures (must be outside CBUFFER for SRP Batcher compatibility)
            // -------------------------------------------------------------------
            TEXTURE2D(_NormalMap);      SAMPLER(sampler_NormalMap);
            TEXTURE2D(_NormalMap2);     SAMPLER(sampler_NormalMap2);
            TEXTURE2D(_ReflectionTex);  SAMPLER(sampler_ReflectionTex);

            // -------------------------------------------------------------------
            // Constant buffer (per-material — SRP Batcher compatible)
            // -------------------------------------------------------------------
            CBUFFER_START(UnityPerMaterial)
                half4   _BaseColor;
                half4   _DeepColor;
                float   _DepthFade;

                float4  _NormalMap_ST;
                float4  _NormalMap2_ST;
                float   _NormalScale;
                float4  _ScrollSpeed1;
                float4  _ScrollSpeed2;
                float   _NormalTiling;

                float   _FresnelPower;
                float   _FresnelBias;

                half4   _SpecColor;
                float   _Shininess;

                float4  _ReflectionTex_ST;
                float   _ReflectionStrength;

                half4   _ShoreColor;
                float   _ShoreFadeDistance;
                float   _ShoreStrength;
                float   _MinWaterAlpha;
            CBUFFER_END

            // -------------------------------------------------------------------
            // Vertex / Fragment structs
            // -------------------------------------------------------------------
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float2 uv1         : TEXCOORD2;
                float2 uv2         : TEXCOORD3;
                float4 screenPos   : TEXCOORD4;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // -------------------------------------------------------------------
            // Vertex shader
            // -------------------------------------------------------------------
            Varyings WaterVert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs   nrmInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS   = nrmInputs.normalWS;

                // Use world-space XZ for UV so tiling stays stable as the water
                // plane follows the camera (PrepareWater repositions it each frame).
                float2 worldXZ = posInputs.positionWS.xz * _NormalTiling;
                output.uv1 = worldXZ + _Time.y * _ScrollSpeed1.xy;
                output.uv2 = worldXZ + _Time.y * _ScrollSpeed2.xy;

                output.screenPos = ComputeScreenPos(posInputs.positionCS);
                return output;
            }

            // -------------------------------------------------------------------
            // Fragment shader
            // -------------------------------------------------------------------
            half4 WaterFrag(Varyings input) : SV_Target
            {
                // --- Dual-layer normal maps ---
                half3 n1 = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap,  sampler_NormalMap,  input.uv1));
                half3 n2 = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap2, sampler_NormalMap2, input.uv2));
                // Blend normals: weight each by _NormalScale, bias toward surface-up
                half2 blendedXY = (n1.xy + n2.xy) * _NormalScale;
                half3 worldNormal = normalize(half3(
                    input.normalWS.x + blendedXY.x,
                    input.normalWS.y,
                    input.normalWS.z + blendedXY.y));

                // --- Depth-based transparency ---
                // screenPos.w is the clip-space W (= eye-space depth for perspective cameras)
                float2 screenUV  = input.screenPos.xy / input.screenPos.w;
                float rawDepth   = SampleSceneDepth(screenUV);
                float sceneDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
                float surfDepth  = input.screenPos.w;
                float depthDiff  = sceneDepth - surfDepth;
                half  depthAlpha = saturate(depthDiff / max(_DepthFade, 0.001));
                // Shore factor: 1.0 right at the shoreline edge, 0.0 in open water
                float shoreFactor = 1.0 - saturate(depthDiff / max(_ShoreFadeDistance, 0.001));

                // --- Fresnel ---
                float3 viewDirWS = normalize(GetWorldSpaceViewDir(input.positionWS));
                float  ndotv     = saturate(dot(worldNormal, viewDirWS));
                float  fresnel   = _FresnelBias + (1.0 - _FresnelBias) * pow(1.0 - ndotv, _FresnelPower);

                // --- Main light specular ---
                Light mainLight = GetMainLight();
                float3 halfDir  = normalize(mainLight.direction + viewDirWS);
                float  nh       = max(0.0, dot(worldNormal, halfDir));
                half   spec     = pow(nh, max(_Shininess, 1.0));

                // --- Base water color (depth-blended) ---
                half4 waterColor = lerp(_BaseColor, _DeepColor, depthAlpha);
                // Blend toward lighter shore color near the waterline for editor readability
                waterColor.rgb = lerp(waterColor.rgb, _ShoreColor.rgb, shoreFactor * _ShoreStrength);

                // --- Reflections (Ultra quality: PLANAR_REFLECTIONS keyword) ---
                #ifdef PLANAR_REFLECTIONS
                    half3 reflColor = SAMPLE_TEXTURE2D(_ReflectionTex, sampler_ReflectionTex, screenUV).rgb;
                    waterColor.rgb  = lerp(waterColor.rgb, reflColor * _ReflectionStrength, fresnel);
                #else
                    // High quality: subtle Fresnel-based sky-colour tint instead of reflection
                    waterColor.rgb = lerp(waterColor.rgb, _SpecColor.rgb * 0.25, fresnel * 0.4);
                #endif

                // Add specular highlight
                waterColor.rgb += spec * _SpecColor.rgb * mainLight.color;

                // Final alpha: minimum opacity so shallow/shore water is always visible;
                // linearly grows from _MinWaterAlpha (shore) to full material alpha (deep).
                float baseAlpha = lerp(_MinWaterAlpha, waterColor.a, depthAlpha);
                waterColor.a = saturate(baseAlpha + fresnel * 0.25);

                return waterColor;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
