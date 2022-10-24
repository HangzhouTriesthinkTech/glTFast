using GLTFast.Logging;
using UnityEngine;
using Character = GLTFast.Schema.CustomMaterials.Character;
using Cloth = GLTFast.Schema.CustomMaterials.Cloth;

namespace GLTFast.Export
{
    using Schema;
    using System;

    // TODO: define a unique vendor name.
    // TODO: define a extension name.

    public class CustomMaterialExporter : StandardMaterialExport
    {
        static readonly int k_NormalTex = Shader.PropertyToID("_MainNormalTex");
        static readonly int k_SmoothTex = Shader.PropertyToID("_SmoothTex");
        static readonly int k_CurveTex = Shader.PropertyToID("_CurveTex");
        static readonly int k_SSSLUT = Shader.PropertyToID("_SSSLUT");
        static readonly int k_KelemenLUT = Shader.PropertyToID("_KelemenLUT");
        static readonly int k_SSS = Shader.PropertyToID("SSS");
        static readonly int k_CurveFactor = Shader.PropertyToID("_CurveFactor");
        static readonly int k_SpX = Shader.PropertyToID("_SpX");
        static readonly int k_SP = Shader.PropertyToID("_SP");
        static readonly int k_SC = Shader.PropertyToID("_SC");
        static readonly int k_Mask = Shader.PropertyToID("_Mask");

        static readonly int k_Normal = Shader.PropertyToID("_Normal");
        static readonly int k_Specular = Shader.PropertyToID("_Specular");
        static readonly int k_Alpha = Shader.PropertyToID("_Alpha");

        static readonly int k_SpecularPower = Shader.PropertyToID("_SpecularPower");
        static readonly int k_SpecularScale = Shader.PropertyToID("_SpecularScale");

        static readonly int k_CutOff = Shader.PropertyToID("_CutOff");
        static readonly int k_AnisoMap = Shader.PropertyToID("_AnisoMap");
        static readonly int k_SpecularAdjust = Shader.PropertyToID("_SpecularAdjust");

        static readonly int k_AOTex = Shader.PropertyToID("_AOTex");
        static readonly int k_FabricTex = Shader.PropertyToID("_FabricTex");
        static readonly int k_FabricNormal = Shader.PropertyToID("_FabricNormal");
        static readonly int k_FabricRoughness = Shader.PropertyToID("_FabricRoughness");
        static readonly int k_NormalIntensity = Shader.PropertyToID("_NormalIntensity");
        static readonly int k_RoughnessAdjust = Shader.PropertyToID("_RoughnessAdjust");
        static readonly int k_OcclusionAdjust = Shader.PropertyToID("_OcclusionAdjust");

        public override bool ConvertMaterial(UnityEngine.Material uMaterial, out Schema.Material material, IGltfWritable gltf, ICodeLogger logger)
        {
            material = new Material
            {
                name = uMaterial.name,
                extensions = new MaterialExtension(),
                pbrMetallicRoughness = new PbrMetallicRoughness
                {
                    metallicFactor = 0,
                    roughnessFactor = 1.0f
                }
            };

            switch (uMaterial.shader.name)
            {
                case "Character/shader_empty":
                    {
                        return ConvertCharacterEmptyMaterial(uMaterial, material, gltf, logger);
                    }
                case "Character/shader_skin_sss":
                    {
                        return ConvertCharacterSkinSSSMaterial(uMaterial, material, gltf, logger);
                    }
                case "Character/shader_eye":
                    {
                        return ConvertCharacterEyeMaterial(uMaterial, material, gltf, logger);
                    }
                case "Character/shader_eyelash":
                    {
                        return ConvertCharacterEyelashMaterial(uMaterial, material, gltf, logger);
                    }
                case "Character/shader_cornea":
                    {
                        return ConvertCharacterCorneaMaterial(uMaterial, material, gltf, logger);
                    }
                case "Character/shader_hair_transparent":
                    {
                        return ConvertCharacterHairTransparentMaterial(uMaterial, material, gltf, logger);
                    }
                case "Cloth/shader_cloth_common":
                    {
                        return ConvertClothCommonMaterial(uMaterial, material, gltf, logger, true);
                    }
                case "Cloth/shader_cloth_common_single_side":
                    {
                        return ConvertClothCommonMaterial(uMaterial, material, gltf, logger, false);
                    }
                default:
                    {
                        // fallback to default;
                        return base.ConvertMaterial(uMaterial, out material, gltf, logger);
                    }
            }
        }

        private bool ConvertGeneralMaterial(UnityEngine.Material uMaterial, Material material, IGltfWritable gltf, ICodeLogger logger)
        {
            material.doubleSided = IsDoubleSided(uMaterial);
            SetAlphaModeAndCutoff(uMaterial, material);

            if (
                uMaterial.HasProperty(k_NormalTex)
            )
            {
                var normalTex = uMaterial.GetTexture(k_NormalTex);

                if (normalTex != null)
                {
                    if (normalTex is Texture2D)
                    {
                        material.normalTexture = ExportNormalTextureInfo(normalTex, uMaterial, gltf);
                        if (material.normalTexture != null)
                        {
                            ExportTextureTransform(material.normalTexture, uMaterial, k_NormalTex, gltf);
                        }
                    }
                    else
                    {
                        logger?.Error(LogCode.TextureInvalidType, "normal", uMaterial.name);
                        return false;
                    }
                }
            } else if (uMaterial.HasProperty(k_Normal))
            {
                var normalTex = uMaterial.GetTexture(k_Normal);

                if (normalTex != null)
                {
                    if (normalTex is Texture2D)
                    {
                        material.normalTexture = ExportNormalTextureInfo(normalTex, uMaterial, gltf);
                        if (material.normalTexture != null)
                        {
                            ExportTextureTransform(material.normalTexture, uMaterial, k_Normal, gltf);
                        }
                    }
                    else
                    {
                        logger?.Error(LogCode.TextureInvalidType, "normal", uMaterial.name);
                        return false;
                    }
                }
            }

            return true;
        }

        static TextureInfo ExportAnyTexture(UnityEngine.Material uMaterial, int kId, IGltfWritable gltf)
        {
            var texture = uMaterial.GetTexture(kId);
            if (texture == null)
            {
                return null;
            }
            TextureInfo ret = ExportTextureInfo(texture, gltf);
            if (ret != null)
            {
                ExportTextureTransform(ret, uMaterial, kId, gltf);
            }

            return ret;
        }

        static OcclusionTextureInfo ExportOcclusionTextureInfo(
            UnityEngine.Material material,
            int kId,
            IGltfWritable gltf
        )
        {
            var texture2d = material.GetTexture(kId) as Texture2D;
            if (texture2d == null)
            {
                return null;
            }
            var imageExport = new ImageExport(texture2d);
            if (AddImageExport(gltf, imageExport, out var textureId))
            {
                return new OcclusionTextureInfo
                {
                    index = textureId,
                    strength = material.GetFloat(k_OcclusionAdjust)
                };
            }
            return null;
        }

        private bool ConvertPbrMaterial(UnityEngine.Material uMaterial, Material material, IGltfWritable gltf, ICodeLogger logger)
        {
            var pbr = new PbrMetallicRoughness { metallicFactor = 0, roughnessFactor = 1.0f };
            material.pbrMetallicRoughness = pbr;

            if (uMaterial.HasProperty(k_Color))
            {
                pbr.baseColor = uMaterial.GetColor(k_Color);
            }
            material.pbrMetallicRoughness.baseColorTexture = ExportAnyTexture(uMaterial, k_MainTex, gltf);

            return true;
        }


        private bool ConvertCharacterSkinSSSMaterial(UnityEngine.Material uMaterial, Schema.Material material, IGltfWritable gltf, ICodeLogger logger)
        {
            if (!ConvertGeneralMaterial(uMaterial, material, gltf, logger))
            {
                return false;
            }
            if (!ConvertPbrMaterial(uMaterial, material, gltf, logger))
            {
                return false;
            }
            var sss = new Character.MaterialSkinSSS();
            material.extensions.VENDOR_materials_characterSkinSSS = sss;
            sss.smoothTex = ExportAnyTexture(uMaterial, k_SmoothTex, gltf);
            sss.curveTex = ExportAnyTexture(uMaterial, k_CurveTex, gltf);
            sss.ssslut = ExportAnyTexture(uMaterial, k_SSSLUT, gltf);
            sss.kelemenLUT = ExportAnyTexture(uMaterial, k_KelemenLUT, gltf);
            sss.sss = uMaterial.GetFloat(k_SSS);
            sss.curveFactor = uMaterial.GetFloat(k_CurveFactor);
            sss.spx = uMaterial.GetFloat(k_SpX);
            sss.sp = uMaterial.GetFloat(k_SP);
            sss.sc = uMaterial.GetColor(k_SC);
            sss.mask = ExportAnyTexture(uMaterial, k_Mask, gltf);

            return true;
        }

        private bool ConvertCharacterEmptyMaterial(UnityEngine.Material uMaterial, Schema.Material material, IGltfWritable gltf, ICodeLogger logger)
        {
            if (!ConvertGeneralMaterial(uMaterial, material, gltf, logger))
            {
                return false;
            }
            material.extensions.VENDOR_materials_characterEmpty = new Character.MaterialEmpty();
            return true;
        }

        private bool ConvertCharacterEyeMaterial(UnityEngine.Material uMaterial, Material material, IGltfWritable gltf, ICodeLogger logger)
        {
            if (!ConvertGeneralMaterial(uMaterial, material, gltf, logger))
            {
                return false;
            }
            if (!ConvertPbrMaterial(uMaterial, material, gltf, logger))
            {
                return false;
            }
            var eye = new Character.MaterialEye();
            material.extensions.VENDOR_materials_characterEye = eye;
            eye.specular = ExportAnyTexture(uMaterial, k_Specular, gltf);
            return true;
        }

        private bool ConvertCharacterEyelashMaterial(UnityEngine.Material uMaterial, Material material, IGltfWritable gltf, ICodeLogger logger)
        {
            if (!ConvertGeneralMaterial(uMaterial, material, gltf, logger))
            {
                return false;
            }
            if (!ConvertPbrMaterial(uMaterial, material, gltf, logger))
            {
                return false;
            }
            var eye = new Character.MaterialEyelash();
            material.extensions.VENDOR_materials_characterEyelash = eye;
            eye.alpha = ExportAnyTexture(uMaterial, k_Alpha, gltf);
            return true;
        }

        private bool ConvertCharacterCorneaMaterial(UnityEngine.Material uMaterial, Material material, IGltfWritable gltf, ICodeLogger logger)
        {
            if (!ConvertGeneralMaterial(uMaterial, material, gltf, logger))
            {
                return false;
            }
            if (!ConvertPbrMaterial(uMaterial, material, gltf, logger))
            {
                return false;
            }
            var ext = new Character.MaterialCornea();
            material.extensions.VENDOR_materials_characterCornea = ext;
            ext.specularPower = uMaterial.GetFloat(k_SpecularPower);
            ext.specularScale = uMaterial.GetFloat(k_SpecularScale);
            return true;
        }

        private bool ConvertCharacterHairTransparentMaterial(UnityEngine.Material uMaterial, Material material, IGltfWritable gltf, ICodeLogger logger)
        {
            if (!ConvertGeneralMaterial(uMaterial, material, gltf, logger))
            {
                return false;
            }
            if (!ConvertPbrMaterial(uMaterial, material, gltf, logger))
            {
                return false;
            }
            var ext = new Character.MaterialHairTransparent();
            material.extensions.VENDOR_materials_characterHairTransparent = ext;
            ext.alpha = ExportAnyTexture(uMaterial, k_Alpha, gltf);
            ext.cutOff = uMaterial.GetFloat(k_CutOff);
            ext.anisoMap = ExportAnyTexture(uMaterial, k_AnisoMap, gltf);
            ext.specularAdjust = uMaterial.GetFloat(k_SpecularAdjust);
            return true;
        }

        new static NormalTextureInfo ExportNormalTextureInfo(
            UnityEngine.Texture texture,
            UnityEngine.Material material,
            IGltfWritable gltf
        )
        {
            var texture2d = texture as Texture2D;
            if (texture2d == null)
            {
                return null;
            }
            var imageExport = new NormalImageExport(texture2d);
            if (AddImageExport(gltf, imageExport, out var textureId))
            {
                var info = new NormalTextureInfo
                {
                    index = textureId,
                    // texCoord = 0 // TODO: figure out which UV set was used
                };

                if (material.HasProperty(k_NormalIntensity))
                {
                    info.scale = material.GetFloat(k_NormalIntensity);
                }
                return info;
            }
            return null;
        }

        private bool ConvertClothCommonMaterial(UnityEngine.Material uMaterial, Material material, IGltfWritable gltf, ICodeLogger logger, bool doubleSided)
        {
            material.occlusionTexture = ExportOcclusionTextureInfo(uMaterial, k_AOTex, gltf);

            if (
                uMaterial.HasProperty(k_FabricNormal)
            )
            {
                var normalTex = uMaterial.GetTexture(k_FabricNormal);

                if (normalTex != null)
                {
                    if (normalTex is Texture2D)
                    {
                        material.normalTexture = ExportNormalTextureInfo(normalTex, uMaterial, gltf);
                        if (material.normalTexture != null)
                        {
                            ExportTextureTransform(material.normalTexture, uMaterial, k_NormalTex, gltf);
                        }
                    }
                    else
                    {
                        logger?.Error(LogCode.TextureInvalidType, "normal", uMaterial.name);
                        return false;
                    }
                }
            }

            var pbr = new PbrMetallicRoughness { metallicFactor = 0, roughnessFactor = 1.0f };
            material.pbrMetallicRoughness = pbr;

            if (uMaterial.HasProperty(k_Color))
            {
                pbr.baseColor = uMaterial.GetColor(k_Color);
            }
            material.pbrMetallicRoughness.baseColorTexture = ExportAnyTexture(uMaterial, k_FabricTex, gltf);
            pbr.roughnessFactor = uMaterial.GetFloat(k_RoughnessAdjust);
            pbr.metallicRoughnessTexture = ExportAnyTexture(uMaterial, k_FabricRoughness, gltf);

            var ext = new Cloth.MaterialCommon();
            material.extensions.VENDOR_materials_clothCommon = ext;

            material.doubleSided = doubleSided;
            return true;;
        }
    }
}
