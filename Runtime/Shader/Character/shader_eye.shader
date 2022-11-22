Shader "Character/shader_eye"
{
    Properties
    {
        _MainTex ("Base Texture", 2D) = "white" {}
        _Normal ("Normal Map", 2D) = "white" {}
    	_Specular ("Specular", 2D) = "black" {}
    }
    SubShader
    {
        LOD 100
        Pass
        {
        	Tags { "RenderType"="Opaque" }
        	Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"
			#include "AutoLight.cginc"
			#include "Lighting.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _Normal;
            sampler2D _Specular;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
				float4 tangent : TANGENT;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 pos : SV_POSITION;
				float3 worldPos : TEXCOORD1;
				float3 normal : TEXCOORD2;
				float3 tangent : TEXCOORD3;
				float3 bitangent : TEXCOORD4;
            	LIGHTING_COORDS(5,6)
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.worldPos	= mul(unity_ObjectToWorld,v.vertex).xyz;
				o.normal = normalize(UnityObjectToWorldNormal(v.normal));
				o.tangent = normalize(UnityObjectToWorldDir(v.tangent.xyz));
				o.bitangent = normalize(cross(o.normal, o.tangent)) * v.tangent.w;
            	TRANSFER_VERTEX_TO_FRAGMENT(o)
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed3x3 tangentTransform = fixed3x3( i.tangent, i.bitangent, i.normal);
                fixed3 normal = UnpackNormal(tex2D(_Normal, i.uv));
                fixed3 N = normalize(mul(normal.rgb, tangentTransform));
            	fixed3 V = normalize(_WorldSpaceCameraPos.xyz - i.worldPos.xyz);
                fixed3 L = normalize(_WorldSpaceLightPos0.xyz);
                fixed3 speL = normalize(V + L);
                fixed atten = LIGHT_ATTENUATION(i);
                fixed NdotSpeL = saturate(dot(speL, N));
                fixed specular = tex2D(_Specular, i.uv).r;
            	fixed NdotL = saturate(dot(N, L));
            	fixed4 base_color = tex2D(_MainTex, i.uv);
                fixed3 final_color = base_color.rgb * (NdotL * _LightColor0.xyz * atten + UNITY_LIGHTMODEL_AMBIENT.rgb);// * 0.5 + specular_color;
            	return fixed4(final_color, 1);
            }
            ENDCG
        }
    }
	FallBack "Diffuse"
}
