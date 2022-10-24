
namespace GLTFast.Schema.CustomMaterials.Cloth
{

    [System.Serializable]
    public class MaterialCommon
    {


        internal void GltfSerialize(JsonWriter writer)
        {
            writer.AddObject();
            writer.Close();
        }
    }
}