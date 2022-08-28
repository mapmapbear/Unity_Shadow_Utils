Shader "Shadow/Blur"
{
	Properties
	{
		_MainTex("MainTex", 2D) = "white"{}
		_TextureSize("TextureSize", float) = 512
	}

	SubShader
	{
		pass
		{
			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			#pragma multi_compile_instancing
			#include "Assets/Resources/Shader/Shadow/ShadowLibrary.cginc"

			sampler2D _MainTex;
			float _TextureSize;
			int _Blur;

			struct v2f
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			v2f vert(appdata_img v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = v.texcoord.xy;
				return o;
			}

			fixed4 frag(v2f i):SV_TARGET
			{
				//int Blur = 1;
				//float4 color = GetGaussianBlurColor(i.uv, _TextureSize, Blur, _MainTex);

				float2 color = 0;
				float totel = 4;
				float pixSize = 1/_TextureSize;
				color += DecodeVSM(tex2D(_MainTex, i.uv));
				color += 0.5*DecodeVSM(tex2D(_MainTex, i.uv + float2(0, -pixSize)));
				color += 0.5*DecodeVSM(tex2D(_MainTex, i.uv + float2(0, pixSize)));
				color += 0.5*DecodeVSM(tex2D(_MainTex, i.uv + float2(-pixSize, 0)));
				color += 0.5*DecodeVSM(tex2D(_MainTex, i.uv + float2(pixSize, 0)));
				color += 0.25*DecodeVSM(tex2D(_MainTex, i.uv + float2(pixSize, -pixSize)));
				color += 0.25*DecodeVSM(tex2D(_MainTex, i.uv + float2(-pixSize, -pixSize)));
				color += 0.25*DecodeVSM(tex2D(_MainTex, i.uv + float2(-pixSize, pixSize)));
				color += 0.25*DecodeVSM(tex2D(_MainTex, i.uv + float2(pixSize, pixSize)));
				
				color /= totel;

				
				return EncodeVSM(color);
			}

			ENDCG
		}
	}
}