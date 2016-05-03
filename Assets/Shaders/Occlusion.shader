///
/// Basic occlusion shader that can be used with spatial mapping meshes.
/// No pixels will be rendered at the object's location.
///
Shader "HoloToolkit/Occlusion"
{
	Properties
	{
	}
	SubShader
	{
		Tags
		{
			"RenderType"="Opaque"
			"Queue"="Geometry-1"
		}

		Pass
		{
			ColorMask 0 // Color will not be rendered.
			Offset 50, 100

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma exclude_renderers d3d11_9x

			#include "UnityCG.cginc"

			struct v2f {
				float4 pos : SV_POSITION;
			};

			v2f vert (appdata_base v)
			{
				v2f o;
				o.pos = mul (UNITY_MATRIX_MVP, v.vertex);
				return o;
			}

			half4 frag (v2f i) : COLOR
			{
				return float4(1,1,1,1);
			}
			ENDCG
		}
	}
}