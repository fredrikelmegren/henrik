
Shader "FogOfWar/FoWOverlay" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
	}
	SubShader {
		Tags { "Queue"="Transparent" "RenderType"="Transparent" }
		LOD 200
		ZTest Always 
		Cull Off 
		ZWrite Off 
		Fog { Mode Off }
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc" 

			uniform sampler2D _MainTex;
			uniform float4 _MainTex_TexelSize;

			struct v2f 
			{
				float4 pos : POSITION;
				half2 uv : TEXCOORD0;
			};

			//
			//
   
			v2f vert( appdata_img v )
			{
				v2f o;
				o.pos = mul (UNITY_MATRIX_MVP, v.vertex);
				o.uv = v.texcoord;
		
				#if UNITY_UV_STARTS_AT_TOP
				if (_MainTex_TexelSize.y < 0)
					o.uv.y = 1 - o.uv.y;
				#endif				
		
				return o;
			}
    
			//
			//

			fixed4 frag (v2f i) : COLOR 
			{
				fixed4 col = tex2D(_MainTex, i.uv);

				col.a = 1 - col.rgb.r;

				if(col.a < 0.001)
					discard;

				return col;
			}
			ENDCG
		}
	} 
	FallBack off
}
