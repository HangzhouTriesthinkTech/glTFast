using GLTFast.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace GLTFast.Materials
{

    public class CustomMaterialGenerator : BuiltInMaterialGenerator
    {
        protected static readonly int k_Color = Shader.PropertyToID("_Color");
        protected static readonly int k_MainTex = Shader.PropertyToID("_MainTex");
        protected static readonly int k_NormalTex = Shader.PropertyToID("_MainNormalTex");
        protected static readonly int k_SmoothTex = Shader.PropertyToID("_SmoothTex");
        protected static readonly int k_CurveTex = Shader.PropertyToID("_CurveTex");
        protected static readonly int k_SSSLUT = Shader.PropertyToID("_SSSLUT");
        protected static readonly int k_KelemenLUT = Shader.PropertyToID("_KelemenLUT");
        protected static readonly int k_SSS = Shader.PropertyToID("SSS");
        protected static readonly int k_CurveFactor = Shader.PropertyToID("_CurveFactor");
        protected static readonly int k_SpX = Shader.PropertyToID("_SpX");
        protected static readonly int k_SP = Shader.PropertyToID("_SP");
        protected static readonly int k_SC = Shader.PropertyToID("_SC");
        protected static readonly int k_Mask = Shader.PropertyToID("_Mask");

        protected static readonly int k_Specular = Shader.PropertyToID("_Specular");
        protected static readonly int k_Alpha = Shader.PropertyToID("_Alpha");
        protected static readonly int k_Normal = Shader.PropertyToID("_Normal");

        protected static readonly int k_SpecularPower = Shader.PropertyToID("_SpecularPower");
        protected static readonly int k_SpecularScale = Shader.PropertyToID("_SpecularScale");

        protected static readonly int k_CutOff = Shader.PropertyToID("_CutOff");
        protected static readonly int k_AnisoMap = Shader.PropertyToID("_AnisoMap");
        protected static readonly int k_SpecularAdjust = Shader.PropertyToID("_SpecularAdjust");


        protected static readonly int k_AOTex = Shader.PropertyToID("_AOTex");
        protected static readonly int k_FabricTex = Shader.PropertyToID("_FabricTex");
        protected static readonly int k_FabricNormal = Shader.PropertyToID("_FabricNormal");
        protected static readonly int k_FabricRoughness = Shader.PropertyToID("_FabricRoughness");
        protected static readonly int k_NormalIntensity = Shader.PropertyToID("_NormalIntensity");
        protected static readonly int k_RoughnessAdjust = Shader.PropertyToID("_RoughnessAdjust");
        protected static readonly int k_OcclusionAdjust = Shader.PropertyToID("_OcclusionAdjust");

#if UNITY_EDITOR
        protected const string CUSTOM_SHADER_PATH_PREFIX = "Assets/Scripts/Render/Shader/";
#endif

        public override Material GenerateMaterial(
            Schema.Material gltfMaterial,
            IGltfReadable gltf,
            bool pointsSupport = false
        )
        {
            if (gltfMaterial.extensions != null) {
                if (gltfMaterial.extensions.VENDOR_materials_characterEmpty != null)
                {
                   return this.GenerateCharacterEmptyMaterial(gltfMaterial, gltf, pointsSupport);
                } 
                if (gltfMaterial.extensions.VENDOR_materials_characterSkinSSS != null)
                {
                    return this.GenerateCharacterSkinSSSMaterial(gltfMaterial, gltf, pointsSupport);
                }
                if (gltfMaterial.extensions.VENDOR_materials_characterEye != null)
                {
                    return this.GenerateCharacterEyeMaterial(gltfMaterial, gltf, pointsSupport);
                }
                if (gltfMaterial.extensions.VENDOR_materials_characterEyelash != null)
                {
                    return this.GenerateCharacterEyelashMaterial(gltfMaterial, gltf, pointsSupport);
                }
                if (gltfMaterial.extensions.VENDOR_materials_characterCornea != null)
                {
                    return this.GenerateCharacterCorneaMaterial(gltfMaterial, gltf, pointsSupport);
                }
                if (gltfMaterial.extensions.VENDOR_materials_characterHairTransparent != null)
                {
                    return this.GenerateCharacterHairTransparentMaterial(gltfMaterial, gltf, pointsSupport);
                }
                if (gltfMaterial.extensions.VENDOR_materials_clothCommon != null)
                {
                    return this.GenerateClothCommonMaterial(gltfMaterial, gltf, pointsSupport);
                }
            }
            return base.GenerateMaterial(gltfMaterial, gltf, pointsSupport);
        }

        void CustomTrySetTextureTransform(
            Schema.TextureInfo textureInfo,
            UnityEngine.Material material,
            int texturePropertyId,
            bool flipY = false
            )
        {
            var hasTransform = false;
            // Scale (x,y) and Transform (z,w)
            float4 textureST = new float4(
                1, 1,// scale
                0, 0 // transform
                );

            var texCoord = textureInfo.texCoord;

            if (textureInfo.extensions?.KHR_texture_transform != null)
            {
                hasTransform = true;
                var tt = textureInfo.extensions.KHR_texture_transform;
                if (tt.texCoord >= 0)
                {
                    texCoord = tt.texCoord;
                }

                float cos = 1;
                float sin = 0;

                if (tt.offset != null)
                {
                    textureST.z = tt.offset[0];
                    textureST.w = 1 - tt.offset[1];
                }
                if (tt.scale != null)
                {
                    textureST.x = tt.scale[0];
                    textureST.y = tt.scale[1];
                }
                if (tt.rotation != 0)
                {
                    cos = math.cos(tt.rotation);
                    sin = math.sin(tt.rotation);

                    var newRot = new Vector2(textureST.x * sin, textureST.y * -sin);

                    textureST.x *= cos;
                    textureST.y *= cos;

                    textureST.z -= newRot.y; // move offset to move rotation point (horizontally) 
                }

                textureST.w -= textureST.y; // move offset to move flip axis point (vertically)
            }


            if (flipY)
            {
                hasTransform = true;
                textureST.w = 1 - textureST.w; // flip offset in Y
                textureST.y = -textureST.y; // flip scale in Y
            }

            if (hasTransform)
            {
                material.EnableKeyword(KW_TEXTURE_TRANSFORM);
            }

            material.SetTextureOffset(texturePropertyId, textureST.zw);
            material.SetTextureScale(texturePropertyId, textureST.xy);
        }

        protected bool CustomTrySetTexture(
            Schema.TextureInfo textureInfo,
            UnityEngine.Material material,
            IGltfReadable gltf,
            int texturePropertyId
            )
        {
            if (textureInfo != null && textureInfo.index >= 0)
            {
                int textureIndex = textureInfo.index;
                var srcTexture = gltf.GetSourceTexture(textureIndex);
                if (srcTexture != null)
                {
                    var texture = gltf.GetTexture(textureIndex);
                    if (texture != null)
                    {
                        material.SetTexture(texturePropertyId, texture);
                        var isKtx = srcTexture.isKtx;
                        CustomTrySetTextureTransform(
                            textureInfo,
                            material,
                            texturePropertyId,
                            isKtx
                            );
                        return true;
                    }
#if UNITY_IMAGECONVERSION
                    logger?.Error(LogCode.TextureLoadFailed, textureIndex.ToString());
#endif
                }
                else
                {
                    logger?.Error(LogCode.TextureNotFound, textureIndex.ToString());
                }
            }
            return false;
        }


        private Material GenerateCharacterEmptyMaterial(Schema.Material gltfMaterial, IGltfReadable gltf, bool pointsSupport)
        {
#if UNITY_EDITOR
            var shader = AssetDatabase.LoadAssetAtPath<Shader>($"{CUSTOM_SHADER_PATH_PREFIX}Character/shader_empty.shader");
#else
            var shader = FindShader("Character/shader_empty");
#endif
            var mat = new Material(shader);
            this.GenerateGeneralMaterial(gltfMaterial, gltf, mat);
            return mat;
        }

        private Material GenerateCharacterSkinSSSMaterial(Schema.Material gltfMaterial, IGltfReadable gltf, bool pointsSupport)
        {
#if UNITY_EDITOR
            var shader = AssetDatabase.LoadAssetAtPath<Shader>($"{CUSTOM_SHADER_PATH_PREFIX}Character/shader_skin_sss.shader");
#else
            var shader = FindShader("Character/shader_skin_sss");
#endif
            var mat = new Material(shader);
            this.GenerateGeneralMaterial(gltfMaterial, gltf, mat);

            CustomTrySetTexture(gltfMaterial.normalTexture, mat, gltf, k_NormalTex);
            if (gltfMaterial.pbrMetallicRoughness != null)
            {
                mat.SetColor(k_Color, gltfMaterial.pbrMetallicRoughness.baseColor);
                CustomTrySetTexture(
                            gltfMaterial.pbrMetallicRoughness.baseColorTexture,
                            mat,
                            gltf,
                            k_MainTex
                            );
            }
            var sss = gltfMaterial.extensions.VENDOR_materials_characterSkinSSS;
            CustomTrySetTexture(
                    sss.smoothTex,
                    mat,
                    gltf,
                    k_SmoothTex
                );
            CustomTrySetTexture(
                   sss.curveTex,
                   mat,
                   gltf,
                   k_CurveTex
               );
            CustomTrySetTexture(
                   sss.ssslut,
                   mat,
                   gltf,
                   k_SSSLUT
               );
            CustomTrySetTexture(
                   sss.kelemenLUT,
                   mat,
                   gltf,
                   k_KelemenLUT
               );
            mat.SetFloat(k_SSS, sss.sss);
            mat.SetFloat(k_CurveFactor, sss.curveFactor);
            mat.SetFloat(k_SpX, sss.spx);
            mat.SetFloat(k_SP, sss.sp);
            mat.SetColor(k_SC, sss.sc);

            CustomTrySetTexture(
                   sss.mask,
                   mat,
                   gltf,
                   k_Mask
               );
            return mat;
        }


        private Material GenerateCharacterEyeMaterial(Schema.Material gltfMaterial, IGltfReadable gltf, bool pointsSupport)
        {
#if UNITY_EDITOR
            var shader = AssetDatabase.LoadAssetAtPath<Shader>($"{CUSTOM_SHADER_PATH_PREFIX}Character/shader_eye.shader");
#else
            var shader = FindShader("Character/shader_eye");
#endif
            var mat = new Material(shader);
            this.GenerateGeneralMaterial(gltfMaterial, gltf, mat);
            CustomTrySetTexture(gltfMaterial.normalTexture, mat, gltf, k_Normal);

            if (gltfMaterial.pbrMetallicRoughness != null)
            {
                CustomTrySetTexture(
                    gltfMaterial.pbrMetallicRoughness.baseColorTexture,
                    mat,
                    gltf,
                    k_MainTex
                );
            }
            var eye = gltfMaterial.extensions.VENDOR_materials_characterEye;
            CustomTrySetTexture(
                eye.specular,
                mat,
                gltf,
                k_Specular
            );
            return mat;
        }

        private Material GenerateCharacterEyelashMaterial(Schema.Material gltfMaterial, IGltfReadable gltf, bool pointsSupport)
        {
#if UNITY_EDITOR
            var shader = AssetDatabase.LoadAssetAtPath<Shader>($"{CUSTOM_SHADER_PATH_PREFIX}Character/shader_eyelash.shader");
#else
            var shader = FindShader("Character/shader_eyelash");
#endif
            var mat = new Material(shader);
            this.GenerateGeneralMaterial(gltfMaterial, gltf, mat);
            CustomTrySetTexture(gltfMaterial.normalTexture, mat, gltf, k_Normal);
            if (gltfMaterial.pbrMetallicRoughness != null)
            {
                CustomTrySetTexture(
                    gltfMaterial.pbrMetallicRoughness.baseColorTexture,
                    mat,
                    gltf,
                    k_MainTex
                );
            }
            var eyelash = gltfMaterial.extensions.VENDOR_materials_characterEyelash;
            CustomTrySetTexture(
                eyelash.alpha,
                mat,
                gltf,
                k_Alpha
            );
            if (gltfMaterial.pbrMetallicRoughness != null)
            {
                mat.SetColor(k_Color, gltfMaterial.pbrMetallicRoughness.baseColor);
            }
            return mat;
        }

        private void GenerateGeneralMaterial(Schema.Material gltfMaterial, IGltfReadable gltf, Material mat)
        {
            mat.name = gltfMaterial.name;
            if (gltfMaterial.doubleSided)
            {
                mat.SetFloat(cullModePropId, 0);
#if UNITY_EDITOR
                mat.doubleSidedGI = true;
#endif
            }

        }

        private Material GenerateCharacterCorneaMaterial(Schema.Material gltfMaterial, IGltfReadable gltf, bool pointsSupport)
        {
#if UNITY_EDITOR
            var shader = AssetDatabase.LoadAssetAtPath<Shader>($"{CUSTOM_SHADER_PATH_PREFIX}Character/shader_cornea.shader");
#else
            var shader = FindShader("Character/shader_cornea");
#endif
            var mat = new Material(shader);
            this.GenerateGeneralMaterial(gltfMaterial, gltf, mat);
            CustomTrySetTexture(
                gltfMaterial.pbrMetallicRoughness.baseColorTexture,
                mat,
                gltf,
                k_MainTex
            );
            var cornea = gltfMaterial.extensions.VENDOR_materials_characterCornea;
            mat.SetFloat(k_SpecularPower, cornea.specularPower);
            mat.SetFloat(k_SpecularScale, cornea.specularScale);
            return mat;
        }


        private Material GenerateCharacterHairTransparentMaterial(Schema.Material gltfMaterial, IGltfReadable gltf, bool pointsSupport)
        {
#if UNITY_EDITOR
            var shader = AssetDatabase.LoadAssetAtPath<Shader>($"{CUSTOM_SHADER_PATH_PREFIX}Character/shader_hair_transparent.shader");
#else
            var shader = FindShader("Character/shader_hair_transparent");
#endif
            var mat = new Material(shader);
            this.GenerateGeneralMaterial(gltfMaterial, gltf, mat);
            if (gltfMaterial.pbrMetallicRoughness != null)
            {
                mat.SetColor(k_Color, gltfMaterial.pbrMetallicRoughness.baseColor);
                CustomTrySetTexture(
                    gltfMaterial.pbrMetallicRoughness.baseColorTexture,
                    mat,
                    gltf,
                    k_MainTex
                );
            }
            CustomTrySetTexture(gltfMaterial.normalTexture, mat, gltf, k_Normal);
            var ext = gltfMaterial.extensions.VENDOR_materials_characterHairTransparent;

            CustomTrySetTexture(
                ext.alpha,
                mat,
                gltf,
                k_Alpha
            );
            CustomTrySetTexture(
               ext.anisoMap,
               mat,
               gltf,
               k_AnisoMap
            );
            mat.SetFloat(k_CutOff, ext.cutOff);
            mat.SetFloat(k_SpecularAdjust, ext.specularAdjust);
            return mat;
        }

        private Material GenerateClothCommonMaterial(Schema.Material gltfMaterial, IGltfReadable gltf, bool pointsSupport)
        {
#if UNITY_EDITOR
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(
                gltfMaterial.doubleSided ? 
                $"{CUSTOM_SHADER_PATH_PREFIX}Cloth/shader_cloth_common.shader" :
                $"{CUSTOM_SHADER_PATH_PREFIX}Cloth/shader_cloth_common_single_side.shader"
                );
#else
            var shader = FindShader(gltfMaterial.doubleSided ?  "Cloth/shader_cloth_common": "Cloth/shader_cloth_common_single_side.shader");
#endif
            var mat = new Material(shader);
            mat.name = gltfMaterial.name;
            CustomTrySetTexture(
                gltfMaterial.occlusionTexture,
                mat,
                gltf,
                k_AOTex
            );
            if (gltfMaterial.occlusionTexture != null)
            {
                mat.SetFloat(k_OcclusionAdjust, gltfMaterial.occlusionTexture.strength);
            }
            if (gltfMaterial.pbrMetallicRoughness != null)
            {
                mat.SetColor(k_Color, gltfMaterial.pbrMetallicRoughness.baseColor);
                CustomTrySetTexture(
                    gltfMaterial.pbrMetallicRoughness.baseColorTexture,
                    mat,
                    gltf,
                    k_FabricTex
                );
                CustomTrySetTexture(
                    gltfMaterial.pbrMetallicRoughness.metallicRoughnessTexture,
                    mat,
                    gltf, k_FabricRoughness);
                mat.SetFloat(k_RoughnessAdjust, gltfMaterial.pbrMetallicRoughness.roughnessFactor);
            }
            CustomTrySetTexture(gltfMaterial.normalTexture, mat, gltf, k_FabricNormal);
            if (gltfMaterial.normalTexture != null)
            {
                mat.SetFloat(k_NormalIntensity, gltfMaterial.normalTexture.scale);
            }
            mat.doubleSidedGI = gltfMaterial.doubleSided;
            return mat;
        }
    }

}