Shader "Unlit/Trees"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
	_Color("Color", Color) = (1, 1, 1)
		_Cutoff("Cutoff", float) = 0.5
	}
		SubShader
	{
		Tags{ "RenderType" = "Opaque" }
		LOD 100

		Pass
	{
		Tags{ "LightMode" = "ForwardBase" }
		//Blend SrcAlpha OneMinusSrcAlpha
		Cull Off

		CGPROGRAM
#pragma vertex vert
#pragma fragment frag
		// make fog work
#pragma multi_compile_fog
#pragma multi_compile_instancing

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
		UNITY_VERTEX_INPUT_INSTANCE_ID
	};

	struct v2f
	{
		float2 uv : TEXCOORD0;
		UNITY_FOG_COORDS(1)
			float4 vertex : SV_POSITION;
		float ao : TEXCOORD2;
		float3 normal : NORMAL;
#if defined(ShadowEnable)
		float4 worldPos : TEXCOORD3;
#endif
		UNITY_VERTEX_INPUT_INSTANCE_ID
	};

	sampler2D _MainTex;
	sampler2D _ShadowMapTexture;
	float4 _MainTex_ST;
	float4 _Color;
	float _Cutoff;

	v2f vert(appdata v)
	{
		v2f o;

		UNITY_SETUP_INSTANCE_ID(v);
		UNITY_TRANSFER_INSTANCE_ID(v, o);

		o.vertex = UnityObjectToClipPos(v.vertex);
		o.uv = TRANSFORM_TEX(v.uv, _MainTex);
		o.ao = length(v.vertex.xz) * 0.4;
		o.ao = min(max(min(abs(v.vertex.y - 10) / 7, 2) * 1, o.ao), 2);
		float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
		o.normal = (float3(v.vertex.x, v.vertex.y / 10, v.vertex.z));

		float3x3 worldRotationMat = float3x3(unity_ObjectToWorld[0][0], unity_ObjectToWorld[0][1], unity_ObjectToWorld[0][2],
			unity_ObjectToWorld[1][0], unity_ObjectToWorld[1][1], unity_ObjectToWorld[1][2],
			unity_ObjectToWorld[2][0], unity_ObjectToWorld[2][1], unity_ObjectToWorld[2][2]);
		o.normal = mul(worldRotationMat, o.normal);
#if defined(ShadowEnable)
		o.worldPos = worldPos;
#endif

		UNITY_TRANSFER_FOG(o,o.vertex);
		return o;
	}

	fixed4 frag(v2f i) : SV_Target
	{
		UNITY_SETUP_INSTANCE_ID(i);

	// sample the texture
	fixed4 col = tex2D(_MainTex, i.uv);
	col.xyz *= _Color;

	//return shadow;

	if (col.a < _Cutoff)
	{
		discard;
	}

	float3 normal = normalize(i.normal.xyz);
	//float3 normal = float3(0, 1, 0);
	//return i.normal.xyzz;// dot(normal, _WorldSpaceLightPos0.xyz);

	half shadow = 1;
#if defined(ShadowEnable) && defined(ReceiveLayer)
	shadow = GetShadow(i.worldPos);
#endif

	float ao = 1.7;// pow(i.ao, 1)  * 1.1;
				   //return ao;
	float3 ambient = unity_AmbientEquator * col.xyz * ao * 0.5;
	float3 diffuse = col.xyz * _LightColor0.xyz * ao * 2 * (max(dot(normal, normalize(_WorldSpaceLightPos0.xyz)), 0) * 0.7 + max(dot(float3(0, 1, 0), normalize(_WorldSpaceLightPos0.xyz)), 0) * 0.3);

	//return (max(dot(normal, normalize(_WorldSpaceLightPos0.xyz)), 0) + max(dot(float3(0, 1, 0), normalize(_WorldSpaceLightPos0.xyz)), 0)) * 0.5;
	float3 finialColor = ambient + diffuse *shadow;
	finialColor *= 0.5;

	//return float4(ambient.xyzz);
	// apply fog
	UNITY_APPLY_FOG(i.fogCoord, finialColor);
	return float4(finialColor, 1);
	//return float4(finialColor, col.a);
	}
		ENDCG
	}
	}
}
