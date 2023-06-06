using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;


namespace GLTFast
{
    public class ModelExtraSetting
    {
        /// <summary>
        /// 合并资源的信息
        /// </summary>
        private Dictionary<string, WebMessageDetail> resource;
        /// <summary>
        /// GLB加载出来后的GameObject
        /// </summary>
        private GameObject model;
        /// <summary>
        /// 身体上材质球
        /// </summary>
        private List<Material> bodyMaterials;
        /// <summary>
        /// 身体上的MESH
        /// </summary>
        private List<SkinnedMeshRenderer> bodyRenderers;
        /// <summary>
        /// 除身体外的模型
        /// </summary>
        private Dictionary<string, GameObject> subOBJs;
        /// <summary>
        /// 动作控制器
        /// </summary>
        private Animator bodyAnimator;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="target"></param>
        /// <param name="messageDatas"></param>
        public void Init(GameObject target, string messageDatas)
        {
            this.model = target;
            this.resource = new Dictionary<string, WebMessageDetail>();
            var webData = JsonUtility.FromJson<WebData>(messageDatas);
            if (webData != null && webData.messageDatas.Count > 0)
            {
                for(var i = 0; i < webData.messageDatas.Count; i++)
                {
                    var detail = JsonUtility.FromJson<WebMessageDetail>(webData.messageDatas[i].message);
                    if (!string.IsNullOrEmpty(detail.path))
                    {
                        var realURL = detail.path.Split('?')[0];
                        this.resource.Add(Path.GetFileNameWithoutExtension(realURL), detail);
                    }
                }
            }

            GetMaterials();
            ActionSetting();
            ShoeGradient();

            CorneaSetting();
            HairSetting();
            EyeballSetting();
            EyebrowSetting();
            ModelMask();
        }

        /// <summary>
        /// 
        /// </summary>
        private void GetMaterials()
        {
            bodyRenderers = new List<SkinnedMeshRenderer>();
            bodyMaterials = new List<Material>();
            subOBJs = new Dictionary<string, GameObject>();

            var rootChildCount = this.model.transform.childCount;
            for (var i = 0; i < rootChildCount; i++)
            {
                var child = this.model.transform.GetChild(i);
                if (child.name.Contains("model_"))
                {
                    subOBJs.Add(child.name, child.gameObject);
                }
                else
                {
                    var skinRender = child.GetComponent<SkinnedMeshRenderer>();
                    if (skinRender != null)
                    {
                        bodyRenderers.Add(skinRender);
                        if (skinRender.materials.Length > 0)
                        {
                            for (var j = 0; j < skinRender.materials.Length; j++)
                            {
                                var mat = skinRender.materials[j];
                                if (mat != null)
                                {
                                    bodyMaterials.Add(mat);
                                }
                            }
                        }
                    }
                }
            }

            var rootTransform = this.model.transform.Find("root");
            rootChildCount = rootTransform.childCount;

            for (var i = 0; i < rootChildCount; i++)
            {
                var child = rootTransform.GetChild(i);
                if (child.name.Contains("model_"))
                {
                    subOBJs.Add(child.name, child.gameObject);
                }
                else
                {
                    var skinRender = child.GetComponent<SkinnedMeshRenderer>();
                    if (skinRender != null && skinRender.materials.Length > 0)
                    {
                        for (var j = 0; j < skinRender.materials.Length; j++)
                        {
                            var mat = skinRender.materials[j];
                            if (mat != null)
                            {
                                bodyMaterials.Add(mat);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 全身遮罩: 身体+衣服+鞋子的遮罩
        /// </summary>
        private void ModelMask()
        {
            var clothUpNum = 0;
            foreach (var resName in this.subOBJs.Keys)
            {
                // 上衣有两种，内衣和外套
                if (resName.Contains("clothup"))
                {
                    clothUpNum++;
                }
            }
            var hasClothUpOut = clothUpNum > 1;

            // 找遮罩贴图
            var maskTextures = new Dictionary<string, List<Texture2D>>();
            foreach (var obj in subOBJs.Values)
            {
                var skinMeshs = obj.GetComponentsInChildren<SkinnedMeshRenderer>();
                if (skinMeshs != null)
                {
                    for (var i = 0; i < skinMeshs.Length; i++)
                    {
                        if(skinMeshs[i].sharedMaterial == null)
                        {
                            continue;
                        }
                        if(skinMeshs[i].sharedMaterial.name.Contains("maskTex_"))
                        {
                            var tex = skinMeshs[i].sharedMaterial.GetTexture("_MainTex");
                            if (tex != null)
                            {
                                var texture = tex as Texture2D;
                                string matName;
                                if (texture.name.Contains("_selfMask") && hasClothUpOut)
                                {
                                    // 有外衣时打开内衣遮罩
                                    matName = texture.name.Replace("_selfMask.jpg", "");
                                    matName = matName.Replace("_selfMask.png", "");
                                }
                                else
                                {
                                    matName = texture.name.Replace("_mask.jpg", "");
                                    matName = matName.Replace("_mask.png", "");
                                }
                                matName = matName.Replace("p02", "p01");

                                if (!maskTextures.ContainsKey(matName))
                                    maskTextures.Add(matName, new List<Texture2D>());
                                maskTextures[matName].Add(texture);
                            }
                        }
                    }
                }
            }

            // 生成最终遮罩贴图
            foreach (var matName in maskTextures.Keys)
            {
                Texture2D finalTexture = null;
                switch (maskTextures[matName].Count)
                {
                    case 0:
                        finalTexture = null;
                        break;
                    case 1:
                        finalTexture = maskTextures[matName][0];
                        break;
                    default:
                        for (int i = 0; i < maskTextures[matName].Count; i++)
                        {
                            var tex = maskTextures[matName][i];
                            if (finalTexture == null)
                            {
                                finalTexture = tex;
                                continue;
                            }
                            else
                            {
                                finalTexture = CombineTexture(finalTexture, tex);
                            }
                        }
                        break;
                }

                // 设置材质球上得贴图(身体)
                if (true)
                {
                    for (int i = 0; i < bodyRenderers.Count; i++)
                    {
                        var render = bodyRenderers[i];
                        for (int j = 0; j < render.materials.Length; j++)
                        {
                            var mat = render.materials[j];
                            if (mat.name.Contains(matName))
                            {
                                mat.SetTexture("_Mask", finalTexture);
                            }
                        }
                    }
                }
                // 设置材质球上得贴图（衣服）
                foreach (var obj in subOBJs.Values)
                {
                    var renders = obj.GetComponentsInChildren<SkinnedMeshRenderer>();
                    for (int i = 0; i < renders.Length; i++)
                    {
                        var render = renders[i];
                        for (int j = 0; j < render.materials.Length; j++)
                        {
                            var mat = render.materials[j];
                            if (mat.name.Contains(matName))
                            {
                                mat.SetTexture("_Mask", finalTexture);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 合成遮罩贴图（只有黑白）
        /// </summary>
        private Texture2D CombineTexture(Texture2D tex1, Texture2D tex2)
        {
            int width = tex1.width;
            int height = tex1.height;
            int total = width * height;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Color c1 = tex1.GetPixel(x, y);
                    Color c2 = tex2.GetPixel(x, y);
                    var r = c1.r <= c2.r ? c1.r : c2.r;
                    var g = c1.g <= c2.g ? c1.g : c2.g;
                    var b = c1.g <= c2.b ? c1.b : c2.b;
                    texture.SetPixel(x, y, new Color(r, g, b, 1));
                }
            }
            texture.Apply();

            return texture;
        }

        /// <summary>
        /// 动作设置
        /// </summary>
        private void ActionSetting()
        {
            bodyAnimator = this.model.GetComponent<Animator>();
            if(bodyAnimator == null)
            {
                bodyAnimator = this.model.AddComponent<Animator>();
            }
            // 女模动作控制器
            var aController = Resources.Load<RuntimeAnimatorController>("Female");
            if (aController != null) 
            {
                bodyAnimator.runtimeAnimatorController = aController;
                bodyAnimator.Play("idle");
            }

            foreach(var obj in this.subOBJs.Values)
            {
                var hip = obj.transform.Find("root/hip");
                if(hip != null)
                {
                    if (bodyAnimator != null)
                    {
                        var animator = obj.GetComponent<Animator>();
                        if (animator == null)
                        {
                            animator = obj.AddComponent<Animator>();
                        }
                        animator.runtimeAnimatorController = bodyAnimator.runtimeAnimatorController;
                        animator.avatar = bodyAnimator.avatar;
                    }

                }
            }
        }

        /// <summary>
        /// 鞋跟高度
        /// </summary>
        private void ShoeGradient()
        {
            GameObject shoe = null;
            foreach (var resName in this.subOBJs.Keys)
            {
                if (resName.ToLower().Contains("shoe"))
                {
                    shoe = this.subOBJs[resName];
                }
            }
            // 没有眉毛的话，不做处理
            if (shoe == null)
                return;
            var animator = this.model.GetComponent<Animator>();
            if (animator == null)
            {
                return;
            }
            // female_shoesAvatar，里面带有不同鞋跟高度下的动作。
            // 但这个 Avatar 动态加载不出来，需要存prefab里，再去取
            //var avatar = Resources.Load<Avatar>("female_shoesAvatar");

            var split = shoe.name.Split('_');
            var h = split[split.Length - 2 >= 0 ? split.Length - 2 : 0];
            var shoeGradient = int.Parse(h.Replace("h", ""));
            if (shoeGradient > 10)
                shoeGradient = 1;
            else if (shoeGradient == 0)
                shoeGradient = 1;

            animator.SetInteger("ShoeType", shoeGradient);
        }

        /// <summary>
        /// 
        /// </summary>
        private void HairSetting()
        {
            GameObject hair = null;
            foreach (var resName in this.subOBJs.Keys)
            {
                if (resName.ToLower().Contains("hair"))
                {
                    hair = this.subOBJs[resName];
                }
            }
            if (hair == null)
                return;

            var hairMaterials = new List<Material>();
            var meshs = hair.GetComponentsInChildren<SkinnedMeshRenderer>();
            if (meshs != null)
            {
                for (var i = 0; i < meshs.Length; i++)
                {
                    for (var j = 0; j < meshs[i].sharedMaterials.Length; j++)
                    {
                        hairMaterials.Add(meshs[i].sharedMaterials[j]);
                    }
                }
            }

            var color = Color.black;
            if (resource.ContainsKey(hair.name))
            {
                var detail = resource[hair.name];
                if (!string.IsNullOrEmpty(detail.color))
                {
                    color = StringToColor(detail.color);
                }
            }
            for (var i = 0; i < hairMaterials.Count; i++)
            {
                var mat = hairMaterials[i];
                mat.SetColor("_Color", color);
            }
        }

        /// <summary>
        /// 眉毛设置
        /// </summary>
        private void EyebrowSetting()
        {
            GameObject eyebrow = null;
            foreach (var resName in this.subOBJs.Keys)
            {
                if (resName.ToLower().Contains("eyebrow"))
                {
                    eyebrow = this.subOBJs[resName];
                }
            }
            // 没有眉毛的话，不做处理
            if (eyebrow == null)
                return;
            // 有无脸部材质球
            var faceMat = bodyMaterials.Find(o => o.name.Contains("mat_face"));
            if (faceMat == null)
                return;
            // 有无不带眉毛的脸部贴图
            var faceMat2 = bodyMaterials.Find(o => o.name.Contains("tex_face"));
            if(faceMat2 == null)
                return;

            // 显示不带眉毛的脸部贴图
            faceMat.SetTexture("_MainTex", faceMat2.GetTexture("_MainTex"));

            Texture browTex = eyebrow.GetComponentInChildren<SkinnedMeshRenderer>().sharedMaterial.GetTexture("_MainTex");
            var faceTex = faceMat2.GetTexture("_MainTex") as Texture2D;
            var color = Color.black;
            // 睫毛颜色
            if (resource.ContainsKey(eyebrow.name))
            {
                var detail = resource[eyebrow.name];
                if (!string.IsNullOrEmpty(detail.color))
                {
                    color = StringToColor(detail.color);
                }
            }
            Texture2D combine = CopyTexture(faceTex);
            CombineEyebrow(faceTex, browTex as Texture2D, color, ref combine);
            combine.name = "tex_face_base3.jpg";
            faceMat.SetTexture("_MainTex", combine);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="face"></param>
        /// <param name="browAlpha"></param>
        /// <param name="browColor"></param>
        /// <param name="result"></param>
        private void CombineEyebrow(Texture2D face, Texture2D browAlpha, Color browColor, ref Texture2D result)
        {
            Texture2D combineBrow = new Texture2D(browAlpha.width, browAlpha.height,
                TextureFormat.RGBA32, true);
            for (int x = 0; x < browAlpha.width; x++)
            {
                for (int y = 0; y < browAlpha.height; y++)
                {
                    Color color = browAlpha.GetPixel(x, y);
                    Color finalColor =
                        new Color(browColor.r, browColor.g, browColor.b, color.r);
                    combineBrow.SetPixel(x, y, finalColor);
                }
            }


            Rect LeftRange = new Rect(230, 479, 256, 95);
            Rect RightRange = new Rect(538, 479, 256, 95);
            combineBrow.Apply();
            CombineTexture(face, RightRange, combineBrow, RightRange,
                ref result);
            CombineTexture(face, LeftRange, combineBrow, LeftRange,
                ref result,
                true);
            Object.Destroy(combineBrow);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="source1"></param>
        /// <param name="source1Range"></param>
        /// <param name="source2"></param>
        /// <param name="source2Range"></param>
        /// <param name="result"></param>
        /// <param name="leftToRight"></param>
        private void CombineTexture(Texture2D source1, Rect source1Range, Texture2D source2, Rect source2Range, ref Texture2D result, bool leftToRight = true)
        {
            for (int x = 0; x < source1Range.width; x++)
            {
                for (int y = 0; y < source1Range.height; y++)
                {
                    int x1 = (int)(x + source1Range.x);
                    int y1 = (int)(y + source1Range.y);
                    float xPercent = x / source1Range.width;
                    float yPercent = y / source1Range.height;
                    if (!leftToRight) xPercent = 1 - xPercent;
                    int x2 = (int)(source2Range.width * xPercent + source2Range.x);
                    int y2 = (int)(source2Range.height * yPercent + source2Range.y);
                    Color s1c = source1.GetPixel(x1, y1);
                    Color s2c = source2.GetPixel(x2, y2);
                    Color color = Color.Lerp(s1c, s2c, s2c.a);
                    color.a = 1;
                    result.SetPixel(x1, y1, color);
                }
            }

            result.Apply();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        private Texture2D CopyTexture(Texture2D source)
        {
            Texture2D target = new Texture2D(source.width, source.height, source.format, true);
            var colors = source.GetPixels(0, 0, source.width, source.height);
            target.SetPixels(0, 0, source.width, source.height, colors);
            target.Apply();
            return target;
        }

        /// <summary>
        /// 眼睛设置
        /// </summary>
        private void EyeballSetting()
        {
            GameObject eyeball = null;
            foreach (var resName in this.subOBJs.Keys)
            {
                if (resName.ToLower().Contains("eyeball"))
                {
                    eyeball = this.subOBJs[resName];
                }
            }
            var eyeballMaterials = new List<Material>();
            // 没有眼球的话
            if (eyeball != null)
            {

            }
            else
            {
                var mat = bodyMaterials.Find(o => o.name.Contains("mat_eyeball"));
                eyeballMaterials.Add(mat);
            }

            var color = StringToColor("rgba(81, 40, 8, 1)");
            float size = 0.5f;
            if (eyeball != null && resource.ContainsKey(eyeball.name))
            {
                var detail = resource[eyeball.name];
                if (!string.IsNullOrEmpty(detail.color))
                {
                    color = StringToColor(detail.color);
                }
                if (detail.size != -1)
                {
                    float percentage = (float)detail.size;
                    var baseSize = 1f;
                    var min = 0.9f;
                    var max = 1.1f;
                    size = (min + (max - min) * percentage) * baseSize;
                }
            }
            // 设置颜色
            for (var i = 0; i < eyeballMaterials.Count; i++)
            {
                var mat = eyeballMaterials[i];
                mat.SetColor("_Color", color);
                mat.SetFloat("_Intensity", 0.452f);
            }
            // 初始眼球UV不对,不能缩放
            if (eyeball != null)
            {
                Vector4 uv = CalculatePupilUV(size);
                for (var i = 0; i < eyeballMaterials.Count; i++)
                {
                    var mat = eyeballMaterials[i];
                    mat.SetTextureScale("_MainTex", new Vector2(uv.x, uv.y));
                    mat.SetTextureOffset("_MainTex", new Vector2(uv.w, uv.z));
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        private Vector4 CalculatePupilUV(float size)
        {
            size = 2 - size;
            Vector4 result = new Vector4();
            result.x = ((int)(size * 10000)) / 10000f;
            result.y = ((int)(size * 10000)) / 10000f;
            float delta = (1f - size) / 2f;
            result.w = ((int)(delta * 10000)) / 10000f;
            result.z = ((int)(delta * 10000)) / 10000f;
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="codeString"></param>
        /// <returns></returns>
        private Color StringToColor(string codeString)
        {
            if (string.IsNullOrEmpty(codeString))
            {
                Debug.Log(codeString + "不符合颜色标准");
                return Color.white;
            }
            float[] rgb = new float[3];
            float a = 255;
            if (codeString.Contains("rgba"))
            {
                var charList = new char[3];
                charList[0] = '(';
                charList[1] = ')';
                charList[2] = ',';
                var array = codeString.Split(charList);
                if (array.Length >= 5)
                {
                    rgb[0] = int.Parse(array[1]);
                    rgb[1] = int.Parse(array[2]);
                    rgb[2] = int.Parse(array[3]);
                    a = float.Parse(array[4]) * 255;
                }
            }
            else
            {
                if (codeString.Contains("#"))
                    codeString = codeString.Replace("#", "");
                if (codeString.Contains("["))
                    codeString = codeString.Replace("[", "");
                if (codeString.Contains("]"))
                    codeString = codeString.Replace("]", "");
                if (codeString.Length != 6 && codeString.Length != 8)
                {
                    Debug.Log(codeString + "不符合颜色标准");
                    return Color.white;
                }
                rgb[0] = Convert.ToInt32(codeString.Substring(0, 2), 16);
                rgb[1] = Convert.ToInt32(codeString.Substring(2, 2), 16);
                rgb[2] = Convert.ToInt32(codeString.Substring(4, 2), 16);

                if (codeString.Length == 8)
                    a = Convert.ToInt32(codeString.Substring(6, 2), 16);

            }

            Color color = new Color(rgb[0] / 255f, rgb[1] / 255f, rgb[2] / 255f, a / 255f);
            return color;
        }

        /// <summary>
        /// 眼角膜设置
        /// </summary>
        private void CorneaSetting()
        {
            var mat = bodyMaterials.Find(o => o.name.Contains("mat_cornea"));
            if (mat)
            {
                mat.renderQueue = 3000;
            }
        }

        /// <summary>
        /// 睫毛设置
        /// </summary>
        private void EyelashSetting()
        {
            GameObject eyelash = null;
            foreach (var resName in this.subOBJs.Keys)
            {
                if (resName.ToLower().Contains("eyelash"))
                {
                    eyelash = this.subOBJs[resName];
                }
            }
            // 没有睫毛的话，不做处理
            if (eyelash == null)
                return;

            var baselashMat = bodyMaterials.Find(o => o.name.Contains("mat_eyelashes"));
            if (baselashMat == null)
                return;
            

        }

        /// <summary>
        /// 嘴巴设置
        /// </summary>
        private void MouthSetting()
        {

        }

    }

    [Serializable]
    public class WebData
    {
        public List<WebMessageData> messageDatas;
    }

    [Serializable]
    public class WebMessageData
    {
        public int type;
        public string message;
    }

    [Serializable]
    public class WebMessageDetail
    {
        /// <summary>
        /// OBS路径
        /// </summary>
        public string path;
        /// <summary>
        /// 
        /// </summary>
        public string color = "";
        /// <summary>
        /// 
        /// </summary>
        public double pos = -1;
        /// <summary>
        ///
        /// </summary>
        public double length = -1;
        /// <summary>
        /// 
        /// </summary>
        public double width = -1;
        /// <summary>
        /// 
        /// </summary>
        public double heigth = -1;


        #region 脸型

        /// <summary>
        ///脸型脸长系数信息
        /// </summary>
        //public double length = -1;
        /// <summary>
        /// 脸型脸宽系数信息
        /// </summary>
        //public double width = -1;

        // <summary>
        /// 脸型下巴宽度系数信息
        /// </summary>
        public double jawwidth = -1;
        /// <summary>
        /// 脸型下巴位置系数信息
        /// </summary>
        public double jawpos = -1;


        #endregion

        #region 眉毛
        /// <summary>
        /// 贴图
        /// </summary>
        //public string path;
        /// <summary>
        /// 眉毛相关系数信息,颜色
        /// </summary>
        //public string color = "";
        /// <summary>
        /// 眉毛相关系数信息,位置
        /// </summary>
        //public double pos = -1;
        /// <summary>
        ///眉毛相关系数信息,长度
        /// </summary>
        //public double length = -1;
        /// <summary>
        /// 眉毛相关系数信息,间距
        /// </summary>
        public double interval = -1;

        #endregion

        #region 眼睛

        /// <summary>
        /// 眼睛位置
        /// </summary>
        //public double pos = -1;
        /// <summary>
        /// 眼睛宽度
        /// </summary>
        //public double width = -1;
        /// <summary>
        /// 眼睛高度
        /// </summary>
        //public double heigth = -1;
        /// <summary>
        /// 眼睛间距
        /// </summary>
        //public double interval = -1;
        /// <summary>
        /// 瞳孔大小
        /// </summary>
        public double size = -1;

        #endregion

        #region 鼻子
        /// <summary>
        /// 鼻子位置
        /// </summary>
        //public double pos = -1;
        /// <summary>
        /// 鼻子宽度
        /// </summary>
        //public double width = -1;
        /// <summary>
        /// 鼻子高度
        /// </summary>
        //public double heigth = -1;
        /// <summary>
        /// 鼻梁高度
        /// </summary>
        public double Bridgeheigth = -1;
        /// <summary>
        /// 鼻梁宽度
        /// </summary>
        public double Bridgewidth = -1;
        /// <summary>
        /// 大小
        /// </summary>
        //public double size = -1;

        #endregion

        #region 嘴巴
        /// <summary>
        /// 嘴巴位置,下、上
        /// </summary>
        //public double pos = -1;
        /// <summary>
        ///嘴巴长度,短、长
        /// </summary>
        //public double length = -1;
        /// <summary>
        /// 嘴巴厚度,薄、厚
        /// </summary>
        public double thickness = -1;
        /// <summary>
        /// 唇色
        /// </summary>
        //public string color = "";
        /// <summary>
        /// 大小
        /// </summary>
        //public double size = -1;

        #endregion

    }


    public enum PartType
    {
        /// <summary>
        /// 
        /// </summary>
        EyeBallNegative = -12,
        /// <summary>
        /// 
        /// </summary>
        None = 0,
        /// <summary>
        /// 头发
        /// </summary>
        Hair = 1,
        /// <summary>
        /// 帽子
        /// </summary>
        Hat = 2,
        /// <summary>
        /// 套装
        /// </summary>
        Cloth = 3,
        /// <summary>
        /// 上装内
        /// </summary>
        ClothUpIn = 4,
        /// <summary>
        /// 上装外
        /// </summary>
        ClothUpOut = 5,
        /// <summary>
        /// 下装
        /// </summary>
        ClothDown = 6,
        /// <summary>
        /// 袜子
        /// </summary>
        Socks = 7,
        /// <summary>
        /// 鞋子
        /// </summary>
        Shoe = 8,
        /// <summary>
        /// 
        /// </summary>
        ZZZ = 9,
        /// <summary>
        /// 
        /// </summary>
        Body = 10,
        /// <summary>
        /// 睫毛
        /// </summary>
        Eyelash = 11,
        /// <summary>
        /// 眼睛
        /// </summary>
        Eyeball = 12,
        /// <summary>
        /// 脸型
        /// </summary>
        Face = 13,
        /// <summary>
        /// 眉毛
        /// </summary>
        Eyebrow = 14,
        /// <summary>
        /// 鼻子
        /// </summary>
        Nose = 15,
        /// <summary>
        /// 嘴巴
        /// </summary>
        Mouth = 16,
        /// <summary>
        /// 配饰
        /// </summary>
        Accessory = 17,
        /// <summary>
        /// 胡子
        /// </summary>
        Beard = 18,
    }
}
