
namespace GLTFast.Schema.CustomMaterials.Character
{

    [System.Serializable]
    public class MaterialEmpty
    {
        internal void GltfSerialize(JsonWriter writer)
        {
            writer.AddObject();
            writer.Close();
        }
    }
}