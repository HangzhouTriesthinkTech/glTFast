
namespace GLTFast.Schema.CustomMaterials.Cloth
{

    [System.Serializable]
    public class MaterialCommon
    {
        public TextureInfo mask = null;
        public TextureInfo craftNormal = null;

        internal void GltfSerialize(JsonWriter writer)
        {
            writer.AddObject();
            if (mask != null)
            {
                writer.AddProperty("mask");
                mask.GltfSerialize(writer);
            }
            if (craftNormal != null)
            {
                writer.AddProperty("craftNormal");
                craftNormal.GltfSerialize(writer);
            }
            writer.Close();
        }
    }
}