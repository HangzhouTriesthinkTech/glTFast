Shader "Character/shader_cornea"
{
    Properties
    {
    	_MainTex ("Main Tex", 2D) = "white" {}
        _SpecularPower ("Specular Power", Range(0, 256)) = 30
        _SpecularScale ("Specular Scale", Range(0, 50)) = 1
    }
    SubShader
    {
    	Pass
        {
        	Tags { 
        		"RenderType"="Transparent+100"
        		"Queue" = "Transparent"
        		"LightMode"="ForwardBase"
        		}
        	Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
			#include "AutoLight.cginc"
            #include "Lighting.cginc" 

            sampler2D _MainTex;
            float4 _MainTex_ST;
            half _SpecularPower;
            half _SpecularScale;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
				float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 pos : SV_POSITION;
				float3 worldPos : TEXCOORD1;
				float3 normal : TEXCOORD2;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.worldPos	= mul(unity_ObjectToWorld,v.vertex).xyz;
				o.normal = normalize(UnityObjectToWorldNormal(v.normal));
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
            	fixed3 ambient = UNITY_LIGHTMODEL_AMBIENT.xyz;
            	fixed3 worldnormal = normalize(UnityObjectToWorldNormal(i.normal));
            	fixed3 worldlight = normalize(UnityWorldSpaceLightDir(i.worldPos));
            	fixed3 diffuse = _LightColor0.rgb * saturate(dot(worldnormal, worldlight));
            	fixed3 viewDir = normalize(UnityWorldSpaceViewDir(i.worldPos));
            	fixed3 halfDir = normalize(worldlight + viewDir);
            	fixed specular = pow(saturate(dot(worldnormal, halfDir)), _SpecularPower) * _SpecularScale;
            	return fixed4(diffuse + ambient, specular);
            }
            ENDCG
        }
    }
}
