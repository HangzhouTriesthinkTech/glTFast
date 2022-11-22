Shader "Cloth/shader_cloth_common"
{
    Properties
    {
        _AOTex("Occlusion", 2D) = "white" {}
		_Color("Color", Color) = (1,1,1,1)
        _FabricTex ("Fabric Tex", 2D) = "white" {}
        _FabricNormal ("Fabric Normal", 2D) = "bump" {}
        _FabricRoughness ("Fabric Roughness", 2D) = "white" {}

        _NormalIntensity("NormalIntensity", Range (0.1,3)) = 1.0
        _RoughnessAdjust("RoughnessAdjust", Range(0,2.0)) = 0.75
        _OcclusionAdjust("OcclusionAdjust", Range(0,10.0)) = 1.0
		
//        _DyeingHue("Dyeing Hue", Range (0,10)) = 1.0
//        _DyeingSaturation("Dyeing Saturation", Range (0,10)) = 1.2
//        _DyeingValue("Dyeing Value", Range (0,10)) = 1.1


    }
    SubShader
    {
        Tags { 
            "RenderType"="Opaque"
            "Queue"="Geometry"
            "LightMode"="ForwardBase" 
            }
        LOD 100
        Cull Off
        CGPROGRAM

        #pragma surface surf Standard fullforwardshadows
        
        sampler2D _AOTex;
		float4 _Color;

        sampler2D _FabricTex;
        sampler2D _FabricNormal;
        sampler2D _FabricRoughness;

        float _NormalIntensity;
        float _RoughnessAdjust;
        float _OcclusionAdjust;

        // float _DyeingHue;
        // float _DyeingSaturation;
        // float _DyeingValue;

        struct Input
        {
            float2 uv_FabricTex;
            float2 uv_AOTex;
        };

		float Epsilon = 1e-10;
        
        float3 RGBtoHCV(in float3 RGB)
        {
            // Based on work by Sam Hocevar and Emil Persson
            float4 P = (RGB.g < RGB.b) ? float4(RGB.bg, -1.0, 2.0/3.0) : float4(RGB.gb, 0.0, -1.0/3.0);
            float4 Q = (RGB.r < P.x) ? float4(P.xyw, RGB.r) : float4(RGB.r, P.yzx);
            float C = Q.x - min(Q.w, Q.y);
            float H = abs((Q.w - Q.y) / (6 * C + Epsilon) + Q.z);
            return float3(H, C, Q.x);
        }

        float3 RGBtoHSV(in float3 RGB)
        {
            float3 HCV = RGBtoHCV(RGB);
            float S = HCV.y / (HCV.z + Epsilon);
            return float3(HCV.x, S, HCV.z);
        }


        float3 HUEtoRGB(in float H)
        {
            float R = abs(H * 6 - 3) - 1;
            float G = 2 - abs(H * 6 - 2);
            float B = 2 - abs(H * 6 - 4);
            return saturate(float3(R,G,B));
        }
        
        float3 HSVtoRGB(in float3 HSV)
        {
            float3 RGB = HUEtoRGB(HSV.x);
            return ((RGB - 1) * HSV.y + 1) * HSV.z;
        }

        float3 NormalBlend_Unity(float3 n1, float3 n2)
        {
            float3x3 nBasis = float3x3(float3(n1.z, n1.y, -n1.x), // +90 degree rotation around y axis
                            float3(n1.x, n1.z, -n1.y), // -90 degree rotation around x axis
                            float3(n1.x, n1.y,  n1.z));
            
            return n2.x*nBasis[0] + n2.y*nBasis[1] + n2.z*nBasis[2];
        }

        float3 NormalBlend_UDN(float3 n1, float3 n2)
        {
            // Unpack
            return normalize(float3(n1.xy + n2.xy, n1.z));
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float4 BaseColor = tex2D (_FabricTex, IN.uv_FabricTex);
            // float4 Metallic = tex2D (_MetallicTex, IN.uv_MetallicTex);
            float4 Roughness = tex2D ( _FabricRoughness, IN.uv_FabricTex);
            float4 Occlusion = tex2D (_AOTex, IN.uv_AOTex);
            float3 Normal = UnpackNormal(tex2D(_FabricNormal, IN.uv_FabricTex));
            
            // float3 BaseColorHSV = RGBtoHSV(BaseColor.rgb);
            // float3 DyeingColor = saturate(BaseColorHSV * float3(_DyeingHue, _DyeingSaturation, _DyeingValue));
            
            o.Albedo = BaseColor.rgb * _Color.rgb;
            o.Metallic = 0;
            o.Smoothness = 1.0 - saturate(Roughness.r * _RoughnessAdjust);
            o.Alpha = BaseColor.a;
            o.Normal = Normal;
            o.Occlusion = 1.0 - saturate((1.0 - Occlusion.x) * _OcclusionAdjust);
        }
        ENDCG
    }
    FallBack "Diffuse"
}
