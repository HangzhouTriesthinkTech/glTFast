Shader "Character/shader_empty" {
    Properties
    {
    }
    SubShader
    {
    	Tags { "RenderType"="Transparent" }
        LOD 100

        Pass
        {
            ColorMask 0
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
			#include "AutoLight.cginc"

            half _SpecularPower;
            half _SpecularScale;

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

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
            	clip(-1);
                return 1;
            }
            ENDCG
        }
    }
}
