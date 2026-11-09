#ifndef DEEDPLANNER_SURFACE_HEIGHT_INCLUDED
#define DEEDPLANNER_SURFACE_HEIGHT_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"

float _SurfaceHeightDetailEnabled;

float SurfaceHeightScale(float scale, float3 view, float3 positionWS)
{
    float angleFade = smoothstep(0.2, 0.5, view.z);
    float distanceFade = 1.0 - smoothstep(8.0, 24.0, distance(GetCameraPositionWS(), positionWS));
    return scale * _SurfaceHeightDetailEnabled * angleFade * distanceFade;
}

void SurfaceHeight_float(float2 UV, float3 View, float3 PositionWS, UnityTexture2D HeightTex,
    float Scale, out float2 Out, out float2 Ddx, out float2 Ddy)
{
    Out = UV;
    Ddx = ddx(UV);
    Ddy = ddy(UV);
    if (Scale <= 0.0 || _SurfaceHeightDetailEnabled < 0.5) return;
    float3 view = normalize(View);
    float scale = SurfaceHeightScale(Scale, view, PositionWS);
    if (scale <= 0.00001 || dot(view.xy, view.xy) < 0.000001) return;

    float layers = ceil(lerp(48.0, 24.0, saturate(view.z)));
    float layerStep = rcp(layers);
    float2 uvStep = view.xy / max(view.z, 0.2) * scale * layerStep;
    float2 nearUV = UV;
    float2 farUV = UV;
    float nearDepth = 0.0;
    float farDepth = 0.0;

    [loop] for (int step = 0; step < 48; step++)
    {
        if (farDepth >= SAMPLE_TEXTURE2D_GRAD(HeightTex.tex, HeightTex.samplerstate, farUV, Ddx, Ddy).r) break;
        nearUV = farUV;
        nearDepth = farDepth;
        farUV -= uvStep;
        farDepth += layerStep;
    }

    [unroll] for (int refinement = 0; refinement < 4; refinement++)
    {
        float2 middleUV = (nearUV + farUV) * 0.5;
        float middleDepth = (nearDepth + farDepth) * 0.5;
        if (middleDepth < SAMPLE_TEXTURE2D_GRAD(HeightTex.tex, HeightTex.samplerstate, middleUV, Ddx, Ddy).r)
        {
            nearUV = middleUV;
            nearDepth = middleDepth;
        }
        else
        {
            farUV = middleUV;
            farDepth = middleDepth;
        }
    }
    Out = (nearUV + farUV) * 0.5;
}

void SurfaceHeightArray_float(float2 UV, float3 View, float3 PositionWS, float Index, UnityTexture2DArray HeightTex,
    float Scale, out float2 Out, out float2 Ddx, out float2 Ddy)
{
    Out = UV;
    Ddx = ddx(UV);
    Ddy = ddy(UV);
    if (Scale <= 0.0 || _SurfaceHeightDetailEnabled < 0.5) return;
    float3 view = normalize(View);
    float scale = SurfaceHeightScale(Scale, view, PositionWS);
    if (scale <= 0.00001 || dot(view.xy, view.xy) < 0.000001) return;

    float layers = ceil(lerp(48.0, 24.0, saturate(view.z)));
    float layerStep = rcp(layers);
    float2 uvStep = view.xy / max(view.z, 0.2) * scale * layerStep;
    float2 nearUV = UV;
    float2 farUV = UV;
    float nearDepth = 0.0;
    float farDepth = 0.0;

    [loop] for (int step = 0; step < 48; step++)
    {
        if (farDepth >= SAMPLE_TEXTURE2D_ARRAY_GRAD(HeightTex.tex, HeightTex.samplerstate, farUV, Index, Ddx, Ddy).r) break;
        nearUV = farUV;
        nearDepth = farDepth;
        farUV -= uvStep;
        farDepth += layerStep;
    }

    [unroll] for (int refinement = 0; refinement < 4; refinement++)
    {
        float2 middleUV = (nearUV + farUV) * 0.5;
        float middleDepth = (nearDepth + farDepth) * 0.5;
        if (middleDepth < SAMPLE_TEXTURE2D_ARRAY_GRAD(HeightTex.tex, HeightTex.samplerstate, middleUV, Index, Ddx, Ddy).r)
        {
            nearUV = middleUV;
            nearDepth = middleDepth;
        }
        else
        {
            farUV = middleUV;
            farDepth = middleDepth;
        }
    }
    Out = (nearUV + farUV) * 0.5;
}

#endif
