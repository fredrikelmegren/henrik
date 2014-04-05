Shader "FogOfWar/FoWPro" 
{
	Properties 
	{
		_MainTex ("Base (RGB)", 2D) = "black" {}
		_FogTex ("Fog (RGB)", 2D) = "white" {}
	}
 
	SubShader 
	{
		ZTest Always 
		Cull Off 
		ZWrite Off 
		Fog { Mode Off }
 
		Pass 
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc" 

			uniform sampler2D _MainTex;
			uniform sampler2D _FogTex;
			uniform sampler2D _CameraDepthTexture;

			uniform float4 _MainTex_TexelSize;
			uniform float4x4 _FrustumCornersWS;
			uniform float4 _CameraWS;

			float _minX = 0.0;
			float _minZ = 0.0;
			float _maxX = 256.0;
			float _maxZ = 256.0;

			struct v2f 
			{
				float4 pos : POSITION;
				half2 uv : TEXCOORD0;
				float2 uv_depth : TEXCOORD1;
				float4 interpolatedRay : TEXCOORD2;
			};

			//
			//
   
			v2f vert( appdata_img v )
			{
				v2f o;
				half index = v.vertex.z;
				v.vertex.z = 0.1;
				o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
				o.uv = v.texcoord.xy;
				o.uv_depth = v.texcoord.xy;
		
				#if UNITY_UV_STARTS_AT_TOP
				if (_MainTex_TexelSize.y < 0)
					o.uv.y = 1-o.uv.y;
				#endif				
		
				o.interpolatedRay = _FrustumCornersWS[(int)index];
				o.interpolatedRay.w = index;
		
				return o;
			}
    
			//
			//

			fixed4 frag (v2f i) : COLOR 
			{
				fixed4 orgCol = tex2D(_MainTex, i.uv);
     
				float avg = (orgCol.r + orgCol.g + orgCol.b) / 10.0;
				fixed4 bw = fixed4(avg, avg, avg, 1);

				float dpth = Linear01Depth(UNITY_SAMPLE_DEPTH(tex2D(_CameraDepthTexture,i.uv_depth)));
				float4 wsDir = dpth * i.interpolatedRay;
				float4 wsPos = _CameraWS + wsDir;

				float2 puv;

				fixed4 col = orgCol;
				if( ((wsPos.x < _minX)||(wsPos.x > _maxX)) || ((wsPos.z < _minZ)||(wsPos.z > _maxZ)) )
				{
					col = bw;
				}
				else
				{
					float4 px = (wsPos.x - _minX) / (_maxX - _minX);
					float4 pz = (wsPos.z - _minZ) / (_maxZ - _minZ);
					puv.x = px;
					puv.y = pz; 
					col = ( orgCol * tex2D(_FogTex, puv) );
				}
     
				return col;
			}
			ENDCG
		}
	} 

	FallBack off
}