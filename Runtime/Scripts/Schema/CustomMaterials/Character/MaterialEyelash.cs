using UnityEngine;

namespace GLTFast.Schema.CustomMaterials.Character
{

    [System.Serializable]
    public class MaterialEyelash
    {
        public TextureInfo alpha = null;

        internal void GltfSerialize(JsonWriter writer)
        {
            writer.AddObject();
            if (alpha != null)
            {
                writer.AddProperty("alpha");
                alpha.GltfSerialize(writer);
            }
            writer.Close();
        }
    }
}