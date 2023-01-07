using UnityEngine;

namespace GLTFast.Schema.CustomMaterials.Character
{

    [System.Serializable]
    public class MaterialEye
    {
        public TextureInfo specular = null;
        public float intensity = 1;

        internal void GltfSerialize(JsonWriter writer)
        {
            writer.AddObject();
            if (specular != null)
            {
                writer.AddProperty("specular");
                specular.GltfSerialize(writer);
            }
            writer.AddProperty("intensity", this.intensity);
            writer.Close();
        }
    }
}