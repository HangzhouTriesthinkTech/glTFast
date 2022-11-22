Shader "Character/shader_hair_transparent"
{
    Properties
    {
        _MainTex ("Base Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Normal ("Normal Map", 2D) = "white" {}
        _Alpha ("Alpha Mask", 2D) = "white" {}
        _CutOff ("Alpha Cut Off", Range(0,1)) = 1
        _AnisoMap ("Aniso Map", 2D) = "white" {}
    	_SpecularAdjust("Specular Adjust", Range(0,1)) = 0.5
    }
    SubShader
    {
    	Tags{
    		"RenderType"="TransparentCutout"
    		"Queue"="Transparent"
    		"LightMode"="ForwardBase"
    		}
    	Pass
    	{    		
    		ZWrite On
    		ColorMask RGB
    		
    		CGPROGRAM
    		#pragma vertex vert
    		#pragma fragment frag
    		#include "UnityCG.cginc"

    		sampler2D _MainTex;
    		float4 _MainTex_ST;
            half3 _Color;
            sampler2D _Alpha;
    		fixed _CutOff;
    		
    		struct a2v {
    			float4 vertex : POSITION;
    			float4 texcoord : TEXCOORD0;
    		};

    		struct v2f {
    			float4 pos : SV_POSITION;
    			float2 uv : TEXCOORD2;
    		};

    		v2f vert(a2v v)
    		{
    			v2f o;
    			o.pos = UnityObjectToClipPos(v.vertex);
    			o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
    			return o;
    		}

    		fixed4 frag(v2f i) : SV_Target{    			
                fixed4 base_color = tex2D(_MainTex, i.uv);
            	base_color.rgb = base_color.rgb * _Color; 
            	fixed alpha = tex2D(_Alpha, i.uv).r;            	
    			clip(alpha - _CutOff);
    			return fixed4(base_color.rgb, alpha);
    		}
    		ENDCG
    	}
    	Pass
    	{
    		Cull Off
    		ZWrite Off
//    		Blend SrcAlpha OneMinusSrcAlpha
    		Blend One OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            
            #include "UnityCG.cginc"
			#include "AutoLight.cginc"
			#include "Lighting.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            half3 _Color;
            sampler2D _Normal;
            sampler2D _Alpha;
            sampler2D _AnisoMap;
			float4 _AnisoMap_ST;
            half _SpecularAdjust;
            
            struct a2v
            {
                float4 vertex : POSITION;
				float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
				float4 tangent : TANGENT;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
				float3 worldPos : TEXCOORD1;
				float3 normal_dir : TEXCOORD2;
				float3 tangent_dir : TEXCOORD3;
				float3 binormal_dir : TEXCOORD4;
            	SHADOW_COORDS(5)
            	// LIGHTING_COORDS(5, 6)
            };

            v2f vert (a2v v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.worldPos	= mul(unity_ObjectToWorld,v.vertex).xyz;
				o.normal_dir = normalize(UnityObjectToWorldNormal(v.normal));
				o.tangent_dir = normalize(UnityObjectToWorldDir(v.tangent.xyz));
				o.binormal_dir = normalize(cross(o.normal_dir, o.tangent_dir)) * v.tangent.w;
            	TRANSFER_SHADOW(o);
            	// TRANSFER_VERTEX_TO_FRAGMENT(o);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 base_color = tex2D(_MainTex, i.uv);
            	base_color.rgb = base_color.rgb * _Color;            	
                fixed3 normal = UnpackNormal(tex2D(_Normal, i.uv));
            	fixed alpha = tex2D(_Alpha, i.uv).r;

            	fixed shadow = SHADOW_ATTENUATION(i);
            	// UNITY_LIGHT_ATTENUATION(attenuation, i, i.worldPos);
                half3 light_dir = normalize(UnityWorldSpaceLightDir(i.worldPos));
				half3 view_dir = normalize(UnityWorldSpaceViewDir(i.worldPos));
            	half3 normal_dir = normalize(i.normal_dir);
				half3 tangent_dir = normalize(i.tangent_dir);
				half3 binormal_dir = normalize(i.binormal_dir);       	
                fixed3x3 TBN = fixed3x3(tangent_dir, binormal_dir, normal_dir);
                normal_dir = normalize(mul(normal.xyz, TBN));

                half diff_term = dot(light_dir, normal_dir);
                half half_lambert = (diff_term + 1) * 0.5;
                half3 diffuse = _LightColor0.xyz * base_color.rgb * _Color;

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
            	float3 final_color = (diffuse + final_spec) * shadow;
            	// float3 final_color = (diffuse + final_spec) * attenuation;
                return fixed4(final_color * alpha, alpha);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"  
}