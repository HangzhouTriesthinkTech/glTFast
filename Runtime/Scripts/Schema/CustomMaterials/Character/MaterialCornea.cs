
namespace GLTFast.Schema.CustomMaterials.Character
{

    [System.Serializable]
    public class MaterialCornea
    {
        public float specularPower;
        public float specularScale;

        internal void GltfSerialize(JsonWriter writer)
        {
            writer.AddObject();
            writer.AddProperty("specularPower", specularPower);
            writer.AddProperty("specularScale", specularScale);
            writer.Close();
        }
    }
}