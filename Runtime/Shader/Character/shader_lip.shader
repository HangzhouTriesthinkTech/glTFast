Shader "Character/shader_lip"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
		_Color ("Color", Color) = (1,1,1,1)
        _Normal ("Normal Map", 2D) = "white" {}
    	_Specular ("Specular", 2D) = "gray" {}
        _Roughness ("Roughness", 2D) = "black" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _Normal;
            sampler2D _Specular;
            sampler2D _Roughness;
			fixed4 _Color;
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                float3 normal = UnpackNormal(tex2D(_Normal, i.uv));
                fixed specular = tex2D(_Specular, i.uv).r;
                fixed roughness = tex2D(_Roughness, i.uv).r;
                fixed3 finalColor = col * _Color.rgb;
                return fixed4(finalColor, 1);
            }
            ENDCG
        }
    }
}
