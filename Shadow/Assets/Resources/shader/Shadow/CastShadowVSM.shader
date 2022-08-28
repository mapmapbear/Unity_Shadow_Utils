Shader "Shadow/CastShadowVSM"
{
	SubShader
	{
		Tags { "RenderType" = "Opaque" }
		
		Pass
		{
			Name "CastShadowWithoutAnimation"
			Tags{ "LightMode" = "ForwardBase" }
			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_shadowcaster
			#pragma multi_compile_instancing
			#include "UnityCG.cginc" 

			struct v2f
			{
				UNITY_VERTEX_INPUT_INSTANCE_ID
				V2F_SHADOW_CASTER;
			};

			v2f vert(appdata_base v)
			{
				UNITY_SETUP_INSTANCE_ID(v);
				v2f o;
				TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
				return o;
			}
			float4 frag(v2f i) : SV_Target
			{
				float depth = i.pos.z / i.pos.w;
				float depth2 = depth * depth;

				return float4(EncodeFloatRG(depth), EncodeFloatRG(depth2));
			}

			ENDCG
		}
	}
}
