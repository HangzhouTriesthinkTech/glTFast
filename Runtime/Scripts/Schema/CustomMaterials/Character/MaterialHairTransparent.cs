
namespace GLTFast.Schema.CustomMaterials.Character
{

    [System.Serializable]
    public class MaterialHairTransparent
    {
        public TextureInfo alpha = null;
        public float cutOff;
        public TextureInfo anisoMap = null;
        public float specularAdjust;

        internal void GltfSerialize(JsonWriter writer)
        {
            writer.AddObject();
            writer.AddProperty("cutOff", cutOff);
            writer.AddProperty("specularAdjust", specularAdjust);
            if (alpha != null)
            {
                writer.AddProperty("alpha");
                alpha.GltfSerialize(writer);
            }
            if (anisoMap != null)
            {
                writer.AddProperty("anisoMap");
                anisoMap.GltfSerialize(writer);
            }
            writer.Close();
        }
    }
}