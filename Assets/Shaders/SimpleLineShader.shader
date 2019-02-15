Shader "DeedPlanner/SimpleLineShader" {
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

			data vert(data d)
			{
				data newData;
				newData.vertex = UnityObjectToClipPos(d.vertex);
				newData.color = d.color;
				return newData;
			}

			float4 frag(data d) : COLOR
			{
				return d.color;
			}
			ENDCG
		}
	}
}