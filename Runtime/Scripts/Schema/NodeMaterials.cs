using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GLTFast.Schema
{
    [System.Serializable]
    public class NodeMaterials
    {
        public int[] materials;

        internal void GltfSerialize(JsonWriter writer)
        {
            writer.AddObject();
            writer.AddArrayProperty("materials", materials);
            writer.Close();
        }
    }

}