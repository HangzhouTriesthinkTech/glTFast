Shader "Character/shader_eyelash"
{
    Properties
    {
        _MainTex ("Base Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Normal ("Normal Map", 2D) = "white" {}
        _Alpha ("Alpha Mask", 2D) = "white" {}
    }
    SubShader
    {
        Tags{
    		"RenderType"="Transparent"
    		"Queue"="Transparent-500"
    		"LightMode"="ForwardBase"
    		}
    	Pass
    	{    		
    		ZWrite On
    		ColorMask RGBA
    		CGPROGRAM
    		#pragma vertex vert
    		#pragma fragment frag
    		#include "Lighting.cginc"

    		sampler2D _MainTex;
    		float4 _MainTex_ST;
            half3 _Color;
            sampler2D _Alpha;
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
    			clip(alpha - 1);
    			return fixed4(base_color.rgb, alpha);
    		}
    		ENDCG
    	}
    	Pass
    	{
    		Cull Off
    		ZWrite Off
    		Blend One OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
			#include "AutoLight.cginc"
			#include "Lighting.cginc"
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 pos : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            half3 _Color;
            sampler2D _Normal;
            sampler2D _Alpha;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed alpha = tex2D(_Alpha, i.uv).r;
            	fixed4 base_color = tex2D(_MainTex, i.uv);
                float3 final_color = base_color.rgb * _Color.rgb * alpha;
                return fixed4(final_color, alpha);
            }
            ENDCG
    	}
    }
}
