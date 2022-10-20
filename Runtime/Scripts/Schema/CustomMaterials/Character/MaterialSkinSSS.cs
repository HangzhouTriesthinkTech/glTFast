using UnityEngine;

namespace GLTFast.Schema.CustomMaterials.Character
{

    [System.Serializable]
    public class MaterialSkinSSS
    {
        public TextureInfo smoothTex = null;
        public TextureInfo curveTex = null;
        public TextureInfo ssslut = null;
        public TextureInfo kelemenLUT = null;

        public float sss = 1.0f;
        public float curveFactor = 1.0f;
        public float spx = 1.0f;
        public float sp = 1.0f;
        public float[] scFactor = { 1, 1, 1, 1 };

        public Color sc
        {
            get =>
                new Color(
                    scFactor[0],
                    scFactor[1],
                    scFactor[2],
                    scFactor[3]
                );
            set
            {
                scFactor = new[] { value.r, value.g, value.b, value.a };
            }
        }

        public TextureInfo mask = null;

        internal void GltfSerialize(JsonWriter writer)
        {
            writer.AddObject();
            writer.AddProperty("sss", sss);
            writer.AddProperty("curveFactor", curveFactor);
            writer.AddProperty("spx", spx);
            writer.AddProperty("sp", sp);
            writer.AddArrayProperty("sc", scFactor);

            if (smoothTex != null)
            {
                writer.AddProperty("smoothTex");
                smoothTex.GltfSerialize(writer);
            }

            if (curveTex != null)
            {
                writer.AddProperty("curveTex");
                curveTex.GltfSerialize(writer);
            }

            if (ssslut != null)
            {
                writer.AddProperty("ssslut");
                ssslut.GltfSerialize(writer);
            }

            if (kelemenLUT != null)
            {
                writer.AddProperty("kelemenLUT");
                kelemenLUT.GltfSerialize(writer);
            }

            if (mask != null)
            {
                writer.AddProperty("mask");
                mask.GltfSerialize(writer);
            }

            writer.Close();
        }
    }
}