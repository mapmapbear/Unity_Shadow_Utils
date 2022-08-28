Shader "Shadow/CastShadow"
{
	SubShader
	{
		Tags { "RenderType" = "Opaque" }
		
		Pass
		{
			Name "CastShadow"
			Tags{ "LightMode" = "ForwardBase" }
			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_shadowcaster
			#pragma multi_compile_instancing
			#include "UnityCG.cginc" 
			#include "UnityInstancing.cginc"
			//#include "Assets/Resources/shader/GPUSkinning/GPUSkinningLibrary.cginc"
			#pragma shader_feature _GPUAnimation

			struct a2v
			{
				UNITY_VERTEX_INPUT_INSTANCE_ID
				float4 vertex : POSITION;
				float4 tangent : TANGENT;
			};
			struct v2f
			{
				UNITY_VERTEX_INPUT_INSTANCE_ID
				//V2F_SHADOW_CASTER;
				float4 pos : SV_POSITION;
			};

			v2f vert(a2v v)
			{
				UNITY_SETUP_INSTANCE_ID(v);
				v2f o;
				
				//#ifdef _GPUAnimation	//有动画 -- 小兵
				//	o.pos = UnityObjectToClipPos(anima(v.tangent, v.vertex));
				//#else		//无动画 -- 英雄等
					o.pos = UnityObjectToClipPos(v.vertex);
				//#endif
				//TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
					//o.pos = UnityApplyLinearShadowBias(o.pos);
				return o;
			}
			float4 frag(v2f i) : SV_Target
			{
				return 0.5;
			}

			ENDCG
		}
	}
}