﻿// Copyright 2020-2022 Andreas Atteneder
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//

namespace GLTFast.FakeSchema {

    [System.Serializable]
    class MaterialExtension {
        public string KHR_materials_pbrSpecularGlossiness;
        public string KHR_materials_unlit;
        public string KHR_materials_transmission;
        public string KHR_materials_clearcoat;
        public string KHR_materials_sheen;
        public string VENDOR_materials_characterEmpty;
        public string VENDOR_materials_characterSkinSSS;
        public string VENDOR_materials_characterEye;
        public string VENDOR_materials_characterEyelash;
        public string VENDOR_materials_characterCornea;
        public string VENDOR_materials_characterHairTransparent;
        public string VENDOR_materials_clothCommon;
        public string VENDOR_materials_characterLip;
        public string VENDOR_materials_characterHairOpaque;
    }
}
