#ifdef StaticShadow
	sampler2D _ShadowMapVariance;	//接收到了从ShadowManager传入的方差阴影纹理
	float4x4 _VarianceLightProjectionMatrix;		
#endif

UNITY_DECLARE_SHADOWMAP(_ShadowMap);
float _shadowMapSize;
float4x4 _LightProjectionMatrix;	

//返回是否在阴影中
inline float GetShadowBase(float3 shadowCoord, float bias)
{
	shadowCoord.z += bias;
	return UNITY_SAMPLE_SHADOW(_ShadowMap, shadowCoord.xyz);
}

//获得阴影衰减因子
inline float GetShadowPCF(float3 ShadowCoord)
{
	float atten = 0;
	int _PCFBlur = 1;
	#if !defined(UNITY_REVERSED_Z) 
		float bias = -0.001;
	#else
		float bias = 0.001;
	#endif
	
	#ifdef NO_PCF
			return GetShadowBase(ShadowCoord, bias);
	#endif

	#ifdef PCF_2X2
			_PCFBlur = 1;
	#endif
	
	#ifdef PCF_4X4
			_PCFBlur = 2;
	#endif
	
	#ifdef PCF_8X8
			_PCFBlur = 3;
	#endif
	
	#ifdef PCF_16X16
			_PCFBlur = 2;
	#endif

	int PCF_Num = _PCFBlur;

	for(int i = -PCF_Num; i <= PCF_Num; ++i)
	{
		for(int j = -PCF_Num; j <= PCF_Num; ++j)
		{
			atten += GetShadowBase(float3(ShadowCoord.x + i * _shadowMapSize, ShadowCoord.y + j * _shadowMapSize, ShadowCoord.z), bias);
	    }
	}
	atten /= ((PCF_Num + PCF_Num + 1) * (PCF_Num + PCF_Num + 1));
	return atten;
}

inline float4 EncodeVSM(float2 v)
{
	float4 enc = float4(EncodeFloatRG(v.x), EncodeFloatRG(v.y));
	return enc;
}

inline float2 DecodeVSM(float4 enc)
{
	float2 v = float2(DecodeFloatRG(enc.xy), DecodeFloatRG(enc.zw));
	return v;
}

#ifdef StaticShadow
	inline float GetShadowVSM(float3 shadowCoord)
	{
		float2 moments = DecodeVSM(tex2D(_ShadowMapVariance, shadowCoord.xy));	// 对ShadowMap进行采样
#if !defined(UNITY_REVERSED_Z) 
		float p = step(shadowCoord.z, moments.x);
#else
		float p = step(moments.x, shadowCoord.z);
#endif
		float variance = moments.y - moments.x * moments.x;
		variance = max(variance, 0.0001);
		float d = shadowCoord.z - moments.x;
		float pMax = variance / (variance + d * d);
		float amount = 0.2;												// 数量
		pMax = clamp((pMax - amount) / (1.0 - amount), 0.0, 1.0);		// clamp --> if((pMax - amount) / (1.0 - amount) < 0) return 0; if((pMax - amount) / (1.0 - amount) > 1) return 1; else return 0;
		float shadow = max(p, pMax);

		return shadow;
	}
#endif

inline half ShadowAttenuate(float3 shadowCoord, half shadow)
{
	half3 shadowPos = (shadowCoord.xyz - 0.5) * 2;
	float x1 = 1 - saturate((1 - abs(shadowPos.y)) * 3);
	float x2 = 1 - saturate((1 - abs(shadowPos.x)) * 3);
	float x3 = 1 - saturate((1 - abs(shadowPos.z)) * 3);//step(shadowCoord.z, 0);
	float trans = max(max(x1, x2), x3);
	
	return lerp(shadow, 1, trans);
}

//获得平行光阴影衰减因子
inline half GetShadow(float4 worldPos)
{
	half shadow = 1;
#if !defined(SpotShadow) && defined(DynamicShadow)
		float4 shadowCoord = mul(_LightProjectionMatrix, worldPos);      //光源相机渲染场景的深度
	    shadowCoord.xyz = shadowCoord.xyz / shadowCoord.w;
#if !defined(UNITY_REVERSED_Z) 
	    shadowCoord.xyz = shadowCoord.xyz * 0.5 + 0.5;
#else 
		shadowCoord.xy = shadowCoord.xy * 0.5 + 0.5;
#endif
	
		shadow = GetShadowPCF(shadowCoord.xyz);		//阴影衰减因子
		shadow = ShadowAttenuate(shadowCoord, shadow);
#endif
	#ifdef StaticShadow
		float4 vsmShadowCoord = mul(_VarianceLightProjectionMatrix, worldPos);      //光源相机渲染场景的深度
		vsmShadowCoord.xyz = vsmShadowCoord.xyz / vsmShadowCoord.w;
#if !defined(UNITY_REVERSED_Z) 
		vsmShadowCoord.xyz = vsmShadowCoord.xyz * 0.5 + 0.5;
#else 
		vsmShadowCoord.xy = vsmShadowCoord.xy * 0.5 + 0.5;
#endif
		half vsmShadow = GetShadowVSM(vsmShadowCoord.xyz);
		vsmShadow = ShadowAttenuate(vsmShadowCoord.xyz, vsmShadow);

		shadow = min(shadow, vsmShadow);
	#endif
	return shadow;
}

inline half GetShadowStatic(float4 worldPos)
{
	half shadow = 1;		//阴影衰减因子

#ifdef StaticShadow
	float4 vsmShadowCoord = mul(_VarianceLightProjectionMatrix, worldPos);      //光源相机渲染场景的深度
	vsmShadowCoord.xyz = vsmShadowCoord.xyz / vsmShadowCoord.w;
#if !defined(UNITY_REVERSED_Z) 
	vsmShadowCoord.xyz = vsmShadowCoord.xyz * 0.5 + 0.5;
#else 
	vsmShadowCoord.xy = vsmShadowCoord.xy * 0.5 + 0.5;
#endif
	half vsmShadow = GetShadowVSM(vsmShadowCoord.xyz);
	vsmShadow = ShadowAttenuate(vsmShadowCoord.xyz, vsmShadow);

	shadow = min(shadow, vsmShadow);
#endif
	return shadow;
}

inline float GetSpotShadow(float4 worldPos)
{
	float shadow = 1;
#if defined(SpotShadow) && defined(DynamicShadow)
	float4 shadowCoord = mul(_LightProjectionMatrix, worldPos);      //光源相机渲染场景的深度
    shadowCoord.xyz = shadowCoord.xyz / shadowCoord.w;
#if !defined(UNITY_REVERSED_Z) 
	shadowCoord.xyz = shadowCoord.xyz * 0.5 + 0.5;
#else 
	shadowCoord.xy = shadowCoord.xy * 0.5 + 0.5;
#endif

	shadow = GetShadowPCF(shadowCoord.xyz);		//阴影衰减因子
#endif
	return shadow;
}

float4 _SpotLightPos;									//聚光灯位置
float4 _SpotLightRot;									//聚光灯方向
float _SpotRange;											//聚光灯光照范围
float _SpotAngle;										//聚光灯光照角度
fixed3 _SpotColor;										//聚光灯光照颜色
float _SpotIntensity;										//聚光灯光照强度
float _Atten;

//获取聚光灯光照
inline fixed3 GetSpotLight(half3 worldPos, half3 worldNormal)
{	
	float3 displacement = _SpotLightPos.xyz - worldPos;
	float distance = length(displacement);
	float3 spotLightDir = displacement / distance;
	fixed3 spotDiffuse = _SpotColor.xyz * max(0, dot(worldNormal, spotLightDir));
	fixed3 spotFinalLight;

	float attenDistance = pow(1-clamp(distance / _SpotRange,0,1), _Atten);//(1 / (_Atten + _Atten * distance + _Atten * pow(distance, 2))) * (_Range/2);		//聚光灯的距离衰减
	float attenAngle = (dot(spotLightDir, _SpotLightRot) - cos(radians(_SpotAngle / 2)))     /     (cos(radians((_SpotAngle * 0.75) / 2)) - cos(radians(_SpotAngle / 2)));	//聚光灯张角衰减
	float allAtten = attenDistance * attenAngle;					
	spotFinalLight = max(0, spotDiffuse * _SpotIntensity * allAtten);						//聚光灯衰减过后的最终光

	return spotFinalLight;
}


