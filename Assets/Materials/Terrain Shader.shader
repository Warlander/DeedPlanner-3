Shader "Custom/Terrain Shader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex("Main Texture Array", 2DArray) = "" {}
        _FillerTex("Filler Texture, don't use", 2D) = "" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        #pragma require 2darray
		#include "UnityCG.cginc"

        UNITY_DECLARE_TEX2DARRAY(_MainTex);

        struct Input
        {
            float2 uv_MainTex;
            float2 uv2_FillerTex;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float3 uv = float3(IN.uv_MainTex.x, IN.uv_MainTex.y, IN.uv2_FillerTex.x);
            fixed4 c = UNITY_SAMPLE_TEX2DARRAY(_MainTex, uv) * _Color;
            o.Albedo = c.rgb;
            o.Metallic = 0.0;
            o.Smoothness = 0.0;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
