Shader "Character/shader_skin_sss" 
{
    Properties 
    {         
        _MainTex ("MainTexture", 2D) = "white" {}
		_Color ("Color", Color) = (1,1,1,1)
        _MainNormalTex ("NormalTex", 2D) = "white" {}
        _SmoothTex ("SmoothTex", 2D) = "white" {}
        _CurveTex ("CurveTex", 2D) = "white" {}
        _SSSLUT ("SSSLUT", 2D) = "white" {}
        _KelemenLUT ("KelemenLUT", 2D) = "white" {}
		[KeywordEnum(OFF,ON)]SSS("SSS Mode",Float) = 1	    
		_CurveFactor("CurveFactor",Range(0,5)) = 1		
		_SpX("SpecularGGX", Range(0,50)) = 1
		_SP("Specularr", Range(0,50)) = 1
		_SC("SpecularrColor", Color) = (1,1,1,1)
        _Mask ("Mask", 2D) = "white" {}
    }

    SubShader 
    {
        Tags{"RenderType"="Opaque"}
		Pass 
		{
			Tags { "LightMode"="ForwardBase"}
			CGPROGRAM
			#pragma vertex vert_new
			#pragma fragment frag_new
            #pragma multi_compile_fwdbase
			#pragma multi_compile SSS_OFF SSS_ON SSS_BLENDNORMAL
			#include "UnityCG.cginc"
            #include "AutoLight.cginc"		
			
			#define PIE 3.1415926535
			#define E 2.71828
			
			sampler2D _MainTex;
			float4 _MainTex_ST;
			sampler2D _MainNormalTex;
			float4 _MainNormalTex_ST;
			sampler2D _SmoothTex;
			sampler2D _CurveTex;
			sampler2D _SSSLUT;
			sampler2D _KelemenLUT;
			float _SpX;
			float _CurveFactor;
			float _SP;
			float4 _SC;
			fixed4 _Color;
			sampler2D _Mask;

            float4 _LightColor0;
			
			struct v2f_new
			{
                float4 pos : SV_POSITION;
				float4 uv : TEXCOORD0;	
                float4 T2W0 :TEXCOORD1;
                float4 T2W1 :TEXCOORD2;
                float4 T2W2 :TEXCOORD3;	
				float3 worldNormal:TEXCOORD4;
                LIGHTING_COORDS(4, 5)	
			};
			
			v2f_new vert_new(appdata_full v)
			{
				v2f_new o;				
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv.xy = TRANSFORM_TEX(v.texcoord, _MainTex);
				o.uv.zw = TRANSFORM_TEX(v.texcoord, _MainNormalTex);

                float3 worldPos = mul(unity_ObjectToWorld, v.vertex);
                float3 worldNormal = UnityObjectToWorldNormal(v.normal);
				o.worldNormal = UnityObjectToWorldNormal(v.normal);
                float3 worldTangent = UnityObjectToWorldDir(v.tangent);
                float3 worldBitangent = cross(worldNormal, worldTangent) * v.tangent.w;

                o.T2W0 = float4 (worldTangent.x, worldBitangent.x, worldNormal.x, worldPos .x);
                o.T2W1 = float4 (worldTangent.y, worldBitangent.y, worldNormal.y, worldPos .y);
                o.T2W2 = float4 (worldTangent.z, worldBitangent.z, worldNormal.z, worldPos .z);
				
				TRANSFER_VERTEX_TO_FRAGMENT(o);

				return o;
			}

			inline float fresnelReflectance(float3 H, float3 V, float F0)
			{
				float base = 1.0 - dot(V, H);
				float exponential = pow(base, 5.0);
				return exponential + F0 * (1.0 - exponential);
			}

			half4 frag_new(v2f_new i) : SV_Target
			{
				fixed4 mask = tex2D(_Mask, i.uv);
				clip(mask - 0.5);
				fixed4 ambient = UNITY_LIGHTMODEL_AMBIENT;//环境光
                fixed attenuation = LIGHT_ATTENUATION(i);//投影
				float3 albedo = tex2D(_MainTex, i.uv.xy).rgb;

                float3 worldPos  = float3(i.T2W0.w, i.T2W1.w, i.T2W2.w);
                fixed3 lightDir = normalize(UnityWorldSpaceLightDir(worldPos));
                fixed3 viewDir = normalize(UnityWorldSpaceViewDir(worldPos));
				
				fixed4 tangentNormal = tex2D(_MainNormalTex, i.uv.zw);
                fixed3 bump = UnpackNormal(tangentNormal);
                fixed3 worldBump = normalize(half3( dot(i.T2W0.xyz,bump),
                                                    dot(i.T2W1.xyz,bump),
                                                    dot(i.T2W2.xyz,bump)));
				fixed cuv = tex2D(_CurveTex,i.uv.zw) * _CurveFactor;
				fixed NoL = dot(worldBump,lightDir);
				fixed3 diffuse = tex2D(_SSSLUT,float2(NoL * 0.5 + 0.5, cuv));				

				half3 h = lightDir + viewDir;
                half3 H = normalize(h);
				fixed NoH = dot(worldBump,H);	
                fixed smooth = tex2D(_SmoothTex, i.uv.zw).r;
				half3 N = normalize(i.worldNormal);

				float PH = pow(2.0 * tex2D(_KelemenLUT, float2(NoH, smooth)), 10.0);
				fixed F =  fresnelReflectance( H, viewDir, 0.028);  //0.028
				half3 specular1 = saturate( PH * F / dot( h, h )) * _SP;

				float cosT = dot(N, H);
				float d =  pow(_SpX, 2) / (PIE * pow((1 + (-1 + pow(_SpX, 2))*pow(cosT, 2)), 2));
				float f = _SC + (1 - _SC)*pow((1 - dot(H, lightDir)), 5);
				float k = 2 / sqrt(PIE * (_SpX + 2));
				float v = 1 / ((dot(N, lightDir) * (1 - k) + k) * (dot(N, viewDir) * (1 - k) + k));
				float all =saturate(d * f * v);

				fixed3 finalColor = (ambient + (diffuse + specular1 + all) * _LightColor0.rgb * attenuation) * albedo;
				return fixed4(finalColor,1);
			}
			ENDCG
		}
    }
    FallBack "Diffuse"    
}