
namespace GLTFast.Schema.CustomMaterials.Character
{

    [System.Serializable]
    public class MaterialHairOpaque
    {
        public TextureInfo anisoMap = null;
        public float specularAdjust;

        internal void GltfSerialize(JsonWriter writer)
        {
            writer.AddObject();
            writer.AddProperty("specularAdjust", specularAdjust);
            if (anisoMap != null)
            {
                writer.AddProperty("anisoMap");
                anisoMap.GltfSerialize(writer);
            }
            writer.Close();
        }
    }
}