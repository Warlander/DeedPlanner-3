Shader "DeedPlanner/SimpleSubtleLineShader" {
	Properties
	{
		[PerRendererData] _Color("Color", Color) = (1, 1, 1, 1)
	}
	SubShader{
		Pass {
			Blend SrcAlpha OneMinusSrcAlpha

			BindChannels {
				Bind "Vertex", vertex
				Bind "Color", color
			}

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#include "UnityCG.cginc"

			struct data {
			    UNITY_VERTEX_INPUT_INSTANCE_ID
				float4 vertex : POSITION;
				float4 color : COLOR;
			};

			float4 _Color;

			data vert(data d)
			{
			    UNITY_SETUP_INSTANCE_ID(d);
				data newData;
				float3 cameraPosition = _WorldSpaceCameraPos;
				float4 vertex = UnityObjectToClipPos(d.vertex);
				float4 worldVertex = mul(unity_ObjectToWorld, d.vertex);
				float dist = distance(cameraPosition, worldVertex.xyz);
				float distReversed = (1 / dist);
				float alphaRatio = min(distReversed * 20, 1);
				float4 color = d.color;
				color.a = color.a * alphaRatio;
				newData.vertex = vertex;
				newData.color = color;
				return newData;
			}

			float4 frag(data d) : COLOR
			{
				float4 col = _Color * d.color;
				return col;
			}
			ENDCG
		}
	}
}