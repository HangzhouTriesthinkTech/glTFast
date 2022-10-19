
namespace GLTFast.Schema.CustomMaterials.Character
{

    public class MaterialEmpty
    {
        internal void GltfSerialize(JsonWriter writer)
        {
            writer.AddObject();
            writer.Close();
        }
    }
}