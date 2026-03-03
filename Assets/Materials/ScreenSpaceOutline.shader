Shader "DeedPlanner/ScreenSpaceOutline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (1, 1, 1, 1)
        _OutlineWidth ("Outline Width", Float) = 5.0
        // _BlitTexture and _MaskTex are bound globally by C# at runtime.
        // NOT declared here: per-material Property slots would shadow the global bindings.
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        // ------------------------------------------------------------------
        // Pass 0 – SolidColor
        // Renders geometry as a flat colour into the mask render texture.
        // Used with cmd.DrawRenderer to build the per-frame outline mask.
        // ------------------------------------------------------------------
        Pass
        {
            Name "SolidColor"
            ZTest Always
            ZWrite Off
            Cull Off
            ColorMask RGBA

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }

        // ------------------------------------------------------------------
        // Pass 1 – DilateComposite
        // Fullscreen triangle pass: reads _MaskTex + _BlitTexture (scene colour),
        // dilates the mask by _OutlineWidth pixels using an 8-direction max filter,
        // and composites the outline border over the scene.
        //
        // Uses Core.hlsl only (not Blit.hlsl) to avoid TEXTURE2D_X / XR macro
        // requirements. _BlitTexture is bound globally via cmd.SetGlobalTexture
        // before DrawProcedural; _MaskTex is likewise bound globally.
        // ------------------------------------------------------------------
        Pass
        {
            Name "DilateComposite"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex dilateVert
            #pragma fragment dilateFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Both textures are set as global shader properties from C# before the draw.
            TEXTURE2D(_BlitTexture); SAMPLER(sampler_BlitTexture);
            TEXTURE2D(_MaskTex);     SAMPLER(sampler_MaskTex);

            // Set manually via cmd.SetGlobalVector — SetGlobalTexture does NOT
            // auto-populate _TexelSize for globally-bound textures.
            float4 _MaskTex_TexelSize; // (.xy = 1/wh, .zw = wh)
            float  _OutlineWidth;

            struct FSAttribs  { uint vertexID : SV_VertexID; };
            struct FSVaryings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            // Fullscreen triangle: three vertices generated from vertex ID, no mesh required.
            FSVaryings dilateVert(FSAttribs input)
            {
                FSVaryings output;
                float2 uv = float2((input.vertexID << 1) & 2, input.vertexID & 2);
                output.positionCS = float4(uv * 2.0 - 1.0, 0.0, 1.0);
                output.uv = uv;
                // Flip Y on APIs where UV origin is at bottom (OpenGL).
                #if UNITY_UV_STARTS_AT_TOP
                output.uv.y = 1.0 - output.uv.y;
                #endif
                return output;
            }

            half4 dilateFrag(FSVaryings input) : SV_Target
            {
                float2 uv   = input.uv;
                float2 step = _MaskTex_TexelSize.xy * _OutlineWidth;

                half4 center = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, uv);
                half4 maxS   = center;
                half4 s;

                // Axis-aligned neighbours
                s = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, uv + float2(-1,  0) * step); maxS = max(maxS, s);
                s = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, uv + float2( 1,  0) * step); maxS = max(maxS, s);
                s = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, uv + float2( 0, -1) * step); maxS = max(maxS, s);
                s = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, uv + float2( 0,  1) * step); maxS = max(maxS, s);
                // Diagonal neighbours (normalised to ~1 px)
                s = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, uv + float2(-0.70711, -0.70711) * step); maxS = max(maxS, s);
                s = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, uv + float2( 0.70711, -0.70711) * step); maxS = max(maxS, s);
                s = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, uv + float2(-0.70711,  0.70711) * step); maxS = max(maxS, s);
                s = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, uv + float2( 0.70711,  0.70711) * step); maxS = max(maxS, s);

                // Outline = dilated mask minus the original silhouette.
                float outlineAlpha = maxS.a * (1.0 - ceil(center.a));

                // Composite: blend outline colour over the scene.
                half4 scene = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, uv);
                return lerp(scene, half4(maxS.rgb, 1.0), outlineAlpha);
            }
            ENDHLSL
        }
    }
}
