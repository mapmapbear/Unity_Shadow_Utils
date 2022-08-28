Shader "Unlit/ShadwoRecvTest"
{
	Properties
	{
	}
		SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog

			#pragma multi_compile NO_PCF PCF_2X2 PCF_4X4 PCF_8X8 PCF_16X16

			#pragma multi_compile _ ShadowEnable
			#pragma multi_compile _ StaticShadow 
			#pragma multi_compile _ DynamicShadow 
			#pragma multi_compile _ SpotShadow
			#pragma multi_compile _ SpotLight
			
			#pragma multi_compile _ ReceiveLayer
			
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "Assets/Resources/Shader/Shadow/ShadowLibrary.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				half3 normal    : NORMAL;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
				half3 normal    : NORMAL;

#if defined(ShadowEnable) || defined(SpotLight)
			float4 worldPos : TEXCOORD3;
#endif
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.normal = UnityObjectToWorldNormal(v.normal);
				UNITY_TRANSFER_FOG(o,o.vertex);
				float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
#if defined(ShadowEnable) || defined(SpotLight)
			o.worldPos = worldPos;
#endif

				return o;

			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = 1;// tex2D(_MainTex, i.uv);

				half3 normal = normalize(i.normal);

				half shadow = 1;
				#if defined(ReceiveLayer) && defined(ShadowEnable)
						shadow = GetShadow(i.worldPos);
				#endif
				half3 diffuse = max(dot(normal, _WorldSpaceLightPos0.xyz), 0) * _LightColor0.xyz * shadow;

				float3 spotLight = 0;

				#ifdef SpotLight
					spotLight = GetSpotLight(i.worldPos, normal);
					#if defined(ReceiveLayer) && defined(ShadowEnable)
							float spotShadow = GetSpotShadow(i.worldPos);
							spotLight *= spotShadow;
					#endif
				#endif



				col.xyz = col.xyz * (diffuse + unity_AmbientEquator + spotLight)
				// apply fog
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			ENDCG
		}
	}
}
