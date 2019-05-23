Shader "DeedPlanner/SimpleLineShader" {
	Properties
	{
		[PerRendererData] _Color("Color", Color) = (1, 1, 1, 1)
	}
	SubShader{
		Pass {
			Blend SrcAlpha OneMinusSrcAlpha
			Cull Off

			BindChannels {
				Bind "Vertex", vertex
				Bind "Color", color
			}

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			struct data {
				float4 vertex : POSITION;
				float4 color : COLOR;
			};

			float4 _Color;

			data vert(data d)
			{
				data newData;
				newData.vertex = UnityObjectToClipPos(d.vertex);
				newData.color = d.color;
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