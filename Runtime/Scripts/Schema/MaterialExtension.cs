// Copyright 2020-2022 Andreas Atteneder
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//

using Character = GLTFast.Schema.CustomMaterials.Character;
using Cloth = GLTFast.Schema.CustomMaterials.Cloth;

namespace GLTFast.Schema {

    /// <summary>
    /// Material extensions.
    /// </summary>
    [System.Serializable]
    public class MaterialExtension {
        
        // Names are identical to glTF specified property names, that's why
        // inconsistent names are ignored.
        // ReSharper disable InconsistentNaming
        
        /// <inheritdoc cref="PbrSpecularGlossiness"/>
        public PbrSpecularGlossiness KHR_materials_pbrSpecularGlossiness;

        /// <inheritdoc cref="PbrSpecularGlossiness"/>
        public MaterialUnlit KHR_materials_unlit;
        
        /// <inheritdoc cref="PbrSpecularGlossiness"/>
        public Transmission KHR_materials_transmission;
        
        /// <inheritdoc cref="PbrSpecularGlossiness"/>
        public ClearCoat KHR_materials_clearcoat;
        
        /// <inheritdoc cref="PbrSpecularGlossiness"/>
        public Sheen KHR_materials_sheen;

        public Character.MaterialEmpty VENDOR_materials_characterEmpty;
        public Character.MaterialSkinSSS VENDOR_materials_characterSkinSSS;
        public Character.MaterialEye VENDOR_materials_characterEye;
        public Character.MaterialEyelash VENDOR_materials_characterEyelash;
        public Character.MaterialLip VENDOR_materials_characterLip;
        public Character.MaterialCornea VENDOR_materials_characterCornea;
        public Character.MaterialHairOpaque VENDOR_materials_characterHairOpaque;
        public Character.MaterialHairTransparent VENDOR_materials_characterHairTransparent;
        public Cloth.MaterialCommon VENDOR_materials_clothCommon;


        // ReSharper restore InconsistentNaming

        internal void GltfSerialize(JsonWriter writer) {
            writer.AddObject();
            if(KHR_materials_pbrSpecularGlossiness!=null) {
                writer.AddProperty("KHR_materials_pbrSpecularGlossiness");
                KHR_materials_pbrSpecularGlossiness.GltfSerialize(writer);
            }
            if(KHR_materials_unlit!=null) {
                writer.AddProperty("KHR_materials_unlit");
                KHR_materials_unlit.GltfSerialize(writer);
            }
            if(KHR_materials_transmission!=null) {
                writer.AddProperty("KHR_materials_transmission");
                KHR_materials_transmission.GltfSerialize(writer);
            }
            if(KHR_materials_clearcoat!=null) {
                writer.AddProperty("KHR_materials_clearcoat");
                KHR_materials_clearcoat.GltfSerialize(writer);
            }
            if(KHR_materials_sheen!=null) {
                writer.AddProperty("KHR_materials_sheen");
                KHR_materials_sheen.GltfSerialize(writer);
            }
            if (VENDOR_materials_characterEmpty != null)
            {
                writer.AddProperty("VENDOR_materials_characterEmpty");
                VENDOR_materials_characterEmpty.GltfSerialize(writer);
            }
            if (VENDOR_materials_characterSkinSSS != null)
            {
                writer.AddProperty("VENDOR_materials_characterSkinSSS");
                VENDOR_materials_characterSkinSSS.GltfSerialize(writer);
            }
            if (VENDOR_materials_characterLip != null)
            {
                writer.AddProperty("VENDOR_materials_characterLip");
                VENDOR_materials_characterLip.GltfSerialize(writer);
            }
            if (VENDOR_materials_characterEye != null)
            {
                writer.AddProperty("VENDOR_materials_characterEye");
                VENDOR_materials_characterEye.GltfSerialize(writer);
            }
            if (VENDOR_materials_characterEyelash != null)
            {
                writer.AddProperty("VENDOR_materials_characterEyelash");
                VENDOR_materials_characterEyelash.GltfSerialize(writer);
            }
            if (VENDOR_materials_characterCornea != null)
            {
                writer.AddProperty("VENDOR_materials_characterCornea");
                VENDOR_materials_characterCornea.GltfSerialize(writer);
            }
            if (VENDOR_materials_characterHairOpaque != null)
            {
                writer.AddProperty("VENDOR_materials_characterHairOpaque");
                VENDOR_materials_characterHairOpaque.GltfSerialize(writer);
            }
            if (VENDOR_materials_characterHairTransparent != null)
            {
                writer.AddProperty("VENDOR_materials_characterHairTransparent");
                VENDOR_materials_characterHairTransparent.GltfSerialize(writer);
            }
            if (VENDOR_materials_clothCommon != null)
            {
                writer.AddProperty("VENDOR_materials_clothCommon");
                VENDOR_materials_clothCommon.GltfSerialize(writer);
            }
            writer.Close();
        }
    }
}
