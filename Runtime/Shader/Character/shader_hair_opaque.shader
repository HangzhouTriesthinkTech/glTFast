Shader "Character/shader_hair_opaque"
{
    Properties
    {
        _MainTex ("Base Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Normal ("Normal Map", 2D) = "white" {}
        _AnisoMap ("Aniso Map", 2D) = "white" {}
    	_SpecularAdjust("Specular Adjust", Range(0,1)) = 0.5
        _Occlusion ("Ambient Occlusion", 2D) = "white" {}
        _OcclusionAdjust("Occlusion Adjust", Range(0,10.0)) = 1.0
    }
    SubShader
    {        
        LOD 100
        Pass
        {
        	Tags {
        		"RenderType"="Opaque" 
    			"Queue"="Geometry"
        		"LightMode"="ForwardBase"
        		}
        	Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
			#include "AutoLight.cginc"
			#include "Lighting.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            half3 _Color;
            sampler2D _Normal;
            sampler2D _Alpha;
            half _CutOff;
            half _SpecularAdjust;
            sampler2D _AnisoMap;
			float4 _AnisoMap_ST;
            sampler2D _Occlusion;
            half _OcclusionAdjust;

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
				float3 normal_dir : TEXCOORD2;
				float3 tangent_dir : TEXCOORD3;
				float3 binormal_dir : TEXCOORD4;
            	SHADOW_COORDS(5)
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.worldPos	= mul(unity_ObjectToWorld,v.vertex).xyz;
				o.normal_dir = normalize(UnityObjectToWorldNormal(v.normal));
				o.tangent_dir = normalize(UnityObjectToWorldDir(v.tangent.xyz));
				o.binormal_dir = normalize(cross(o.normal_dir, o.tangent_dir)) * v.tangent.w;
            	TRANSFER_SHADOW(o);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {                
                fixed4 base_color = tex2D(_MainTex, i.uv);
            	base_color.rgb = base_color.rgb * _Color;            	
                fixed3 normal = UnpackNormal(tex2D(_Normal, i.uv)); 

            	UNITY_LIGHT_ATTENUATION(atten, i, i.worldPos);
                half3 light_dir = normalize(UnityWorldSpaceLightDir(i.worldPos));
				half3 view_dir = normalize(UnityWorldSpaceViewDir(i.worldPos));
            	half3 normal_dir = normalize(i.normal_dir);
				half3 tangent_dir = normalize(i.tangent_dir);
				half3 binormal_dir = normalize(i.binormal_dir);           	
                fixed3x3 TBN = fixed3x3(tangent_dir, binormal_dir, normal_dir);
                normal_dir = normalize(mul(normal.xyz, TBN));

                half diff_term = dot(light_dir, normal_dir);
                half half_lambert = (diff_term + 1) * 0.5;
                half3 direct_diffuse = _LightColor0.xyz * base_color.rgb * _Color;

				half2 uv_aniso = i.uv * _AnisoMap_ST.xy + _AnisoMap_ST.zw;
            	half aniso_noise = tex2D(_AnisoMap,uv_aniso).r - 0.5;
				half3 half_dir = normalize(light_dir + view_dir);
				half NdotH = saturate(dot(normal_dir, half_dir));
				half TdotH = dot(tangent_dir, half_dir);
				half NdotV = saturate(dot(view_dir, normal_dir));
            	float aniso_atten = saturate(sqrt(saturate(half_lambert / NdotV)));
            	float3 aniso_offset1 = normal_dir * (aniso_noise + 0.05);
            	binormal_dir = normalize(binormal_dir + normal_dir * aniso_offset1);
            	float BdotH1 = dot(binormal_dir, half_dir) * 10;
				float spec_term = exp(-(TdotH * TdotH + BdotH1 * BdotH1)/ (1+ NdotH));
            	float3 final_spec = spec_term * aniso_atten * _LightColor0.xyz * _SpecularAdjust;
                fixed ao = tex2D(_Occlusion, i.uv).r * _OcclusionAdjust;
            	float3 final_color = (direct_diffuse + final_spec) * atten * ao;
                return fixed4(final_color, 1);
            }
            ENDCG
        }
    }
	Fallback "Diffuse"
}