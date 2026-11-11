#if UNITY_EDITOR
using AllIn1Assets.AllIn1VfxToolkit;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AllIn1Assets.AllIn1VfxToolkit
{
    [CanEditMultipleObjects]
    public class AllIn1VfxLitCustomMaterialEditor : ShaderGUI
    {
        private Material targetMat;
        private UnityEngine.Rendering.BlendMode srcMode, dstMode;
        private UnityEngine.Rendering.CompareFunction zTestMode;
        private UnityEngine.Rendering.CullMode cullMode;
        private UnityEngine.Rendering.ColorWriteMask colorMask;

        private Material originalMaterialCopy;
        private MaterialEditor matEditor;
        private MaterialProperty[] matProperties;
        //URP Lit templates prepend hidden properties, so every hard-coded index is relative to _RenderingMode
        private int propertyOffset;
        private uint[] materialDrawers = new uint[] { 1, 2, 4, 8, 16, 32 };
        bool[] currEnabledDrawers;
        private const uint advancedConfigDrawer = 0;
        private const uint advancedShapeBlendDrawer = 1;
        private const uint mainShapeDrawer = 2;
        private const uint colorFxShapeDrawer = 3;
        private const uint alphaFxShapeDrawer = 4;
        private const uint uvFxShapeDrawer = 5;

        private enum RenderingPreset
        {
            Transparent,
            Additive,
            SoftAdditive,
            BlendAdd,
            Opaque,
            Custom
        }

        private RenderingPreset renderPreset;

        private string[] oldKeyWords;
        private static GUIStyle propertiesStyle, bigLabel, smallLabel, toggleButtonStyle, centeredTextStyle, smallTextStyle, smallBoldLabel;
        private const int bigFontSize = 16;
        private float tempValue;
        bool shape2On = false, shape3On = false, isPs = false;
        int effectCount = 1, shape1FirstProperty, shape2FirstProperty, shape3FirstProperty;

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            matEditor = materialEditor;
            matProperties = properties;
            propertyOffset = Mathf.Max(0, System.Array.FindIndex(properties, p => p.name == "_RenderingMode"));
            targetMat = materialEditor.target as Material;
            oldKeyWords = targetMat.shaderKeywords;
            effectCount = 1;
            currEnabledDrawers = new bool[materialDrawers.Length];
            uint iniDrawers = (uint)ShaderGUI.FindProperty("_EditorDrawers", matProperties).floatValue;
            for(int i = 0; i < materialDrawers.Length; i++) currEnabledDrawers[i] = (materialDrawers[i] & iniDrawers) > 0;

            InitStylesIfNeeded();
            if(Selection.activeGameObject != null) isPs = Selection.activeGameObject.GetComponent<ParticleSystem>() != null;


            GUILayout.Label("Configuration", bigLabel);

            //GUILayout.Label("Presets:", smallBoldLabel);
            //RenderingMode();

            ForEachTargetMat(mat => mat.SetInt("_ZWrite", 1));

            SetLabelAndFieldWidth(178, 75);
            currEnabledDrawers[advancedConfigDrawer] = GUILayout.Toggle(currEnabledDrawers[advancedConfigDrawer], new GUIContent("<size=12>Show Advanced Configuration</size>"), toggleButtonStyle);
            if(currEnabledDrawers[advancedConfigDrawer])
            {
                EditorGUILayout.BeginVertical(propertiesStyle);
                Blending();
                DrawLine(Color.grey, 1, 3);
                ZWrite();
                DrawLine(Color.grey, 1, 3);
                ZTest();
                DrawLine(Color.grey, 1, 3);
                Culling();
                DrawLine(Color.grey, 1, 3);
                ColorMask();
                DrawLine(Color.grey, 1, 3);
                DrawProperty(9);
                DrawLine(Color.grey, 1, 3);
                OneLineKeywordToggle("Use Custom Time", "TIMEISCUSTOM_ON");
                DrawLine(Color.grey, 1, 3);
                EditorGUIUtility.labelWidth = 140;
                materialEditor.EnableInstancingField();
                DrawLine(Color.grey, 1, 3);

                EditorGUI.BeginChangeCheck();
                materialEditor.RenderQueueField();
                if (EditorGUI.EndChangeCheck())
                {
                    SetShaderBasedOnEffectsAndPipeline();
                }
			
                EditorGUILayout.EndVertical();
            }

            SectionSeparatorAndHeader("Shape Properties");
            DrawProperty(8);
            DrawProperty(52);
            EditorGUILayout.Separator();
            currEnabledDrawers[mainShapeDrawer] = GUILayout.Toggle(currEnabledDrawers[mainShapeDrawer], new GUIContent("<size=12>Show Shape Configuration</size>"), toggleButtonStyle);
            if(currEnabledDrawers[mainShapeDrawer])
            {
                shape1FirstProperty = 11;
                shape2FirstProperty = 23;
                shape3FirstProperty = 35;
                ShapeProperties(1, shape1FirstProperty);
                EditorGUILayout.Separator();
                ShapeProperties(2, shape2FirstProperty, true);
                EditorGUILayout.Separator();
                if(shape2On) ShapeProperties(3, shape3FirstProperty, true);
                if(shape2On) ShapeResult();
            }
            else
            {
                ShapeProperties(2, 23, true, true);
                if(shape2On) ShapeProperties(3, 35, true, true);
            }

            EditorGUILayout.Separator();
            DrawLine(Color.grey, 1, 3);
            GUILayout.Label("Light Effect", bigLabel);
            GenericEffect("Normal Map", "NORMALMAP_ON", 159, 160);

            EditorGUILayout.Separator();
            SectionSeparatorAndHeader("Color Effects");
            currEnabledDrawers[colorFxShapeDrawer] = GUILayout.Toggle(currEnabledDrawers[colorFxShapeDrawer], new GUIContent("Show Color Effects"), toggleButtonStyle);
            if(currEnabledDrawers[colorFxShapeDrawer])
            {
                Glow("Glow", "GLOW_ON");
                ColorRamp("Color Ramp", "COLORRAMP_ON", "COLORRAMPGRAD_ON");
                GenericEffect("Color Grading", "COLORGRADING_ON", 125, 128);
                GenericEffect("Hue Shift and Saturation", "HSV_ON", 71, 73);
                GenericEffect("Fresnel / Rim Color", "RIM_ON", 135, 141);
                GenericEffect("Intersection Glow", "DEPTHGLOW_ON", 64, 68);
                GenericEffect("Posterize", "POSTERIZE_ON", 149, 149);
                GenericEffect("Backface Tint", "BACKFACETINT_ON", 82, 83);
                FakeLightAndShadow("Fake Light And Shadow", "LIGHTANDSHADOW_ON", 144, 148);
                GenericEffect("Shape 1 Mask", "SHAPE1MASK_ON", 142, 143, true, new string[]
                {
                    "It will only have a visual effect if shape 2 or 3 is enabled",
                    "Use to avoid shape 1 from being affected by distortions and other shapes"
                });
                ScreenDistort("Screen Distortion", "SCREENDISTORTION_ON", 103, 107);
            }

            SectionSeparatorAndHeader("Alpha Effects");
            currEnabledDrawers[alphaFxShapeDrawer] = GUILayout.Toggle(currEnabledDrawers[alphaFxShapeDrawer], new GUIContent("Show Alpha Effects"), toggleButtonStyle);
            if(currEnabledDrawers[alphaFxShapeDrawer])
            {
                GenericEffect("Alpha Mask", "MASK_ON", 69, 70, true);
                FadeDissolve("Fade From Noise Texture (Dissolve)", "FADE_ON", 115, 124);
                AlphaFade("Fade From Final Shape (Procedural Dissolve)", "ALPHAFADE_ON", 55, 57);
                GenericEffect("Soft Particles / Intersection Fade", "SOFTPART_ON", 47, 47, true);
                CameraDistFade("Camera Distance Fade", "CAMDISTFADE_ON", 129, 131);
                AlphaSmoothstep("Alpha Remap", "ALPHASMOOTHSTEP_ON", 53, 54);
            }

            SectionSeparatorAndHeader("UV and Vertex Effects");
            currEnabledDrawers[uvFxShapeDrawer] = GUILayout.Toggle(currEnabledDrawers[uvFxShapeDrawer], new GUIContent("Show UV Effects"), toggleButtonStyle);
            if(currEnabledDrawers[uvFxShapeDrawer])
            {
                GenericEffect("Global Distortion", "DISTORT_ON", 78, 81);
                PolarUvs("Global Polar Coordinates", "POLARUV_ON");
                if(isPs || oldKeyWords.Contains("SHAPEWEIGHTS_ON")) ShapeWeightVertexStream("Shape Weights Custom Stream", "SHAPEWEIGHTS_ON", 156);
                if(isPs || oldKeyWords.Contains("OFFSETSTREAM_ON")) OffsetVertexStream("Texture Offset Custom Stream", "OFFSETSTREAM_ON", 100);
                ShapeTextureOffset("Shape Texture Offset", "SHAPETEXOFFSET_ON", 74);
                GenericEffect("Global Texture Scroll", "TEXTURESCROLL_ON", 108, 109);
                GenericEffect("Twist", "TWISTUV_ON", 94, 97);
                GenericEffect("Wave", "WAVEUV_ON", 87, 91);
                GenericEffect("Round Wave", "ROUNDWAVEUV_ON", 92, 93);
                GenericEffect("Hand Drawn", "DOODLE_ON", 98, 99);
                GenericEffect("Pixelate", "PIXELATE_ON", 77, 77, true, new string[] { "Looks bad with distortion effects" });
                TrailWidth("Trail Width", "TRAILWIDTH_ON", 58, 59);
                GenericEffect("Shake", "SHAKEUV_ON", 84, 86);
                GenericEffect("Vertex Offset", "VERTOFFSET_ON", 110, 114);
            }

            SetAndSaveEnabledDrawers(iniDrawers);
        }

        private void SetAndSaveEnabledDrawers(uint iniDrawers)
        {
            uint currDrawers = 0;
            for(int i = 0; i < currEnabledDrawers.Length; i++)
            {
                if(currEnabledDrawers[i]) currDrawers |= materialDrawers[i];
            }

            if(iniDrawers != currDrawers) ShaderGUI.FindProperty("_EditorDrawers", matProperties).floatValue = currDrawers;
        }

        private void SectionSeparatorAndHeader(string headerText)
        {
            EditorGUILayout.Separator();
            DrawLine(Color.grey, 1, 3);
            GUILayout.Label(headerText, bigLabel);
        }

        private void ShapeResult()
        {
            EditorGUILayout.Separator();
            GUILayout.Label("Shape Result:", smallBoldLabel);

            currEnabledDrawers[advancedShapeBlendDrawer] = GUILayout.Toggle(currEnabledDrawers[advancedShapeBlendDrawer], new GUIContent("Show Shape Combination Options"), toggleButtonStyle);
            if(currEnabledDrawers[advancedShapeBlendDrawer])
            {
                EditorGUILayout.BeginVertical(propertiesStyle);

                bool splitColor = DrawEffectSubKeywordToggle("Split Color And Alpha?", "SPLITRGBA_ON");
                bool addResults = DrawEffectSubKeywordToggle("Add Shape Results?", "SHAPEADD_ON");
                char operationChar = '*';
                if(addResults) operationChar = '+';

                if(splitColor)
                {
                    GUILayout.Label("Color:", smallBoldLabel);
                    DrawProperty(21);
                    DrawProperty(33);
                    if(shape3On) DrawProperty(45);

                    GUILayout.Label("Alpha:", smallBoldLabel);
                    DrawProperty(22);
                    DrawProperty(34);
                    if(shape3On) DrawProperty(46);

                    EditorGUILayout.Separator();
                    GUILayout.Label("Final Shape Calculation:", smallBoldLabel);
                    if(shape3On)
                    {
                        GUILayout.Label("RGB = ((Sh1.rgb * " + Prop(21).floatValue + ") " + operationChar + " (Sh2.rgb *  " + Prop(33).floatValue
                                        + ")) " + operationChar + " (Sh3.rgb * " + Prop(45).floatValue + ")", smallBoldLabel, GUILayout.Height(15));
                        if(addResults)
                            GUILayout.Label("A = max(max(Sh1.a * " + Prop(22).floatValue + " , " + "Sh2.a *  " + Prop(34).floatValue
                                            + "), Sh3.a * " + Prop(46).floatValue + ")", smallBoldLabel, GUILayout.Height(15));
                        else
                            GUILayout.Label("A = ((Sh1.a * " + Prop(22).floatValue + ") " + operationChar + " (Sh2.a *  " + Prop(34).floatValue
                                            + ")) " + operationChar + " (Sh3.a * " + Prop(46).floatValue + ")", smallBoldLabel, GUILayout.Height(15));
                    }
                    else
                    {
                        GUILayout.Label("RGB = (Sh1.rgb * " + Prop(21).floatValue + ") " + operationChar + " (Sh2.rgb *  " + Prop(33).floatValue + ")", smallBoldLabel, GUILayout.Height(15));
                        if(addResults) GUILayout.Label("A = max(Sh1.a * " + Prop(22).floatValue + " , Sh2.a *  " + Prop(34).floatValue + ")", smallBoldLabel, GUILayout.Height(15));
                        else GUILayout.Label("A = (Sh1.a * " + Prop(22).floatValue + ") " + operationChar + " (Sh2.a *  " + Prop(34).floatValue + ")", smallBoldLabel, GUILayout.Height(15));
                    }
                }
                else
                {
                    GUILayout.Label("Shape weights:", smallBoldLabel);
                    DrawProperty(21);
                    DrawProperty(33);
                    if(shape3On) DrawProperty(45);
                    Prop(22).floatValue = Prop(21).floatValue;
                    Prop(34).floatValue = Prop(33).floatValue;
                    Prop(46).floatValue = Prop(45).floatValue;

                    EditorGUILayout.Separator();
                    GUILayout.Label("Final Shape Calculation:", smallBoldLabel);
                    if(shape3On)
                    {
                        if(!addResults)
                            GUILayout.Label("Result = ((Shape1 * " + Prop(21).floatValue + ") " + operationChar + " (Shape2 *  " + Prop(33).floatValue
                                            + ")) " + operationChar + " (Shape3 * " + Prop(45).floatValue + ")", smallBoldLabel, GUILayout.Height(15));
                        else
                        {
                            GUILayout.Label("RGB = ((Sh1.rgb * " + Prop(21).floatValue + ") " + operationChar + " (Sh2.rgb *  " + Prop(33).floatValue
                                            + ")) " + operationChar + " (Sh3.rgb * " + Prop(45).floatValue + ")", smallBoldLabel, GUILayout.Height(15));
                            GUILayout.Label("A = max(max(Sh1.a * " + Prop(22).floatValue + " , " + "Sh2.a *  " + Prop(34).floatValue
                                            + "), Sh3.a * " + Prop(46).floatValue + ")", smallBoldLabel, GUILayout.Height(15));
                        }
                    }
                    else
                    {
                        if(!addResults) GUILayout.Label("Result = (Shape1 * " + Prop(21).floatValue + ") " + operationChar + " (Shape2 *  " + Prop(33).floatValue + ")", smallBoldLabel, GUILayout.Height(15));
                        else
                        {
                            GUILayout.Label("RGB = (Sh1.rgb * " + Prop(21).floatValue + ") " + operationChar + " (Sh2.rgb *  " + Prop(33).floatValue + ")", smallBoldLabel, GUILayout.Height(15));
                            GUILayout.Label("A = max(Sh1.a * " + Prop(22).floatValue + " , Sh2.a *  " + Prop(34).floatValue + ")", smallBoldLabel, GUILayout.Height(15));
                        }
                    }
                }

                EditorGUILayout.EndVertical();
            }
        }

        private void RenderingMode()
        {
            EditorGUILayout.BeginVertical(propertiesStyle);
            {
                MaterialProperty renderM = Prop(0);
                tempValue = renderM.floatValue;
                SetLabelAndFieldWidth(89, 75);
                renderPreset = (RenderingPreset)renderM.floatValue;

                if(renderPreset == RenderingPreset.Custom)
                    renderPreset = (RenderingPreset)GUILayout.SelectionGrid((int)renderPreset, new string[] { "Transparent", "Additive", "Soft Add", "Blend Add / Premultiply", "Opaque", "Custom" }, 3, smallTextStyle);
                else
                    renderPreset = (RenderingPreset)GUILayout.SelectionGrid((int)renderPreset, new string[] { "Transparent", "Additive", "Soft Add", "Blend Add / Premultiply", "Opaque" }, 3, smallTextStyle);


                renderM.floatValue = (float)(renderPreset);
                if(tempValue != renderM.floatValue && !Application.isPlaying)
                {
                    ForEachTargetMat(mat => ApplyRenderingPreset(mat, renderPreset));
                    Save();
                    oldKeyWords = targetMat.shaderKeywords;
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void Blending()
        {
            SetLabelAndFieldWidth(112, 75);
            DrawEffectSubKeywordToggle("Premultiply Color?", "PREMULTIPLYCOLOR_ON", true);
        }

        private void ZWrite()
        {
            MaterialProperty zWrite = ShaderGUI.FindProperty("_ZWrite", matProperties);
            bool toggle = zWrite.floatValue > 0.9f ? true : false;
            EditorGUILayout.BeginHorizontal();
            {
                SetLabelAndFieldWidth(89, 2);
                tempValue = zWrite.floatValue;
                toggle = GUILayout.Toggle(toggle, new GUIContent("Enable Z Write"));
                if(toggle) zWrite.floatValue = 1.0f;
                else zWrite.floatValue = 0.0f;
                if(tempValue != zWrite.floatValue && !Application.isPlaying) SaveAndSetCustomConfig();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void ZTest()
        {
            MaterialProperty zTestM = ShaderGUI.FindProperty("_ZTestMode", matProperties);
            tempValue = zTestM.floatValue;
            SetLabelAndFieldWidth(72, 75);
            zTestMode = (UnityEngine.Rendering.CompareFunction)zTestM.floatValue;
            zTestMode = (UnityEngine.Rendering.CompareFunction)EditorGUILayout.EnumPopup("Z TestMode", zTestMode);
            zTestM.floatValue = (float)(zTestMode);
            if(tempValue != zTestM.floatValue && !Application.isPlaying) SaveAndSetCustomConfig();
        }

        private void Culling()
        {
            MaterialProperty cullO = Prop(3);
            tempValue = cullO.floatValue;
            SetLabelAndFieldWidth(83, 35);
            cullMode = (UnityEngine.Rendering.CullMode)cullO.floatValue;
            cullMode = (UnityEngine.Rendering.CullMode)EditorGUILayout.EnumPopup("Culling Mode", cullMode);
            cullO.floatValue = (float)(cullMode);
            if(tempValue != cullO.floatValue && !Application.isPlaying) SaveAndSetCustomConfig();
        }

        private void ColorMask()
        {
            MaterialProperty colorM = Prop(6);
            tempValue = colorM.floatValue;
            SetLabelAndFieldWidth(105, 35);
            colorMask = (UnityEngine.Rendering.ColorWriteMask)colorM.floatValue;
            colorMask = (UnityEngine.Rendering.ColorWriteMask)EditorGUILayout.EnumPopup("Color Write Mask", colorMask);
            colorM.floatValue = (float)(colorMask);
            if(tempValue != colorM.floatValue && !Application.isPlaying) SaveAndSetCustomConfig();
        }

        private void OneLineKeywordToggle(string inspector, string keyword)
        {
            bool toggle = oldKeyWords.Contains(keyword);
            bool ini = toggle;
            SetLabelAndFieldWidth(87, 50);
            DrawEffectSubKeywordToggle(inspector, keyword, true);
        }

        private void ShapeProperties(int shapeNumber, int firstProperty, bool onlyIfKeyword = false, bool getIfEnabledAndReturn = false)
        {
            string shapeText = "Shape " + shapeNumber;
            string shapeKeyword = "SHAPE" + shapeNumber;
            bool toggle = false;
            if(shapeNumber == 2) shape2On = false;
            else if(shapeNumber == 3) shape3On = false;

            if(onlyIfKeyword)
            {
                bool ini = oldKeyWords.Contains(shapeKeyword + "_ON");
                if(ini)
                {
                    if(shapeNumber == 2) shape2On = true;
                    else if(shapeNumber == 3) shape3On = true;
                }

                if(getIfEnabledAndReturn) return;
                toggle = ini;
                toggle = GUILayout.Toggle(toggle, new GUIContent("Use Shape " + shapeNumber + "?"), toggleButtonStyle);
                if(ini != toggle)
                {
                    Save();
                    if(toggle) SetKeywordOnAllTargets(shapeKeyword + "_ON", true);
                    else
                    {
                        SetKeywordOnAllTargets(shapeKeyword + "_ON", false);
                        if(shapeNumber == 2)
                        {
                            SetKeywordOnAllTargets("SHAPE3_ON", false);
                            SetKeywordOnAllTargets("SHAPE2SCREENUV_ON", false);
                            SetKeywordOnAllTargets("SHAPE3SCREENUV_ON", false);

                            SetShaderBasedOnEffectsAndPipeline();

                        }
                        else if(shapeNumber == 3)
                        {
                            SetShaderBasedOnEffectsAndPipeline();
                            SetKeywordOnAllTargets("SHAPE3SCREENUV_ON", false);
                        }
                    }

                    return;
                }
            }

            if(!onlyIfKeyword || toggle)
            {
                EditorGUILayout.BeginVertical(propertiesStyle);
                GUILayout.Label(shapeText + ":", smallBoldLabel);
                for(int i = firstProperty; i <= firstProperty + 3; i++) DrawProperty(i);
                string nextShapeKeyword = shapeKeyword + "CONTRAST_ON";
                GenericEffect(shapeText + " Contrast", nextShapeKeyword, firstProperty + 4, firstProperty + 5, false);
                nextShapeKeyword = shapeKeyword + "DISTORT_ON";
                GenericEffect(shapeText + " Distortion", nextShapeKeyword, firstProperty + 6, firstProperty + 9, false);
                nextShapeKeyword = shapeKeyword + "ROTATE_ON";
                int shapeRotationPropertyIndex = 150 + (shapeNumber - 1) * 2;
                GenericEffect("Shape " + shapeNumber + " Rotation", nextShapeKeyword, shapeRotationPropertyIndex, shapeRotationPropertyIndex + 1, false);
                nextShapeKeyword = shapeKeyword + "SHAPECOLOR_ON";
                GenericEffect("Shape " + shapeNumber + " RGB is Shape Color, Red Channel Is Alpha", nextShapeKeyword, -1, -1, false);
                nextShapeKeyword = shapeKeyword + "SCREENUV_ON";
                int shapeSSUvScalePropertyIndex = 132 - 1 + shapeNumber; //- 1 from shader index to account for shape number
                GenericEffect("Shape " + shapeNumber + " Screen Position UVs?", nextShapeKeyword, shapeSSUvScalePropertyIndex, shapeSSUvScalePropertyIndex, false);

                nextShapeKeyword = "SHAPEDEBUG_ON";
                //HDRP builds append hidden properties after _DebugShape, so it is not always last
                MaterialProperty shapeDebugProperty = ShaderGUI.FindProperty("_DebugShape", matProperties);
                bool ini = oldKeyWords.Contains(nextShapeKeyword) && shapeDebugProperty.floatValue < shapeNumber + 0.5f && shapeDebugProperty.floatValue > shapeNumber - 0.5f;
                bool toggle2 = ini;
                toggle2 = EditorGUILayout.BeginToggleGroup(shapeText + " Debug", toggle2);
                EditorGUILayout.EndToggleGroup();
                if(ini != toggle2)
                {
                    Save();
                    if(toggle2) SetKeywordOnAllTargets(nextShapeKeyword, true);
                    else SetKeywordOnAllTargets(nextShapeKeyword, false);
                    shapeDebugProperty.floatValue = shapeNumber;
                }

                if(shapeNumber == 1)
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        if(oldKeyWords.Contains("SHAPE2_ON"))
                        {
                            if(GUILayout.Button("Copy Shape 2 Properties", GUILayout.Width(200)))
                            {
                                CopyShapePropertiesAndKeywords(shape1FirstProperty, shape2FirstProperty, 1, 2);
                            }
                        }

                        if(oldKeyWords.Contains("SHAPE3_ON"))
                        {
                            if(GUILayout.Button("Copy Shape 3 Properties", GUILayout.Width(200)))
                            {
                                CopyShapePropertiesAndKeywords(shape1FirstProperty, shape3FirstProperty, 1, 3);
                            }
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
                else if(shapeNumber == 2)
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        if(GUILayout.Button("Copy Shape 1 Properties", GUILayout.Width(200)))
                        {
                            CopyShapePropertiesAndKeywords(shape2FirstProperty, shape1FirstProperty, 2, 1);
                        }

                        if(oldKeyWords.Contains("SHAPE3_ON"))
                        {
                            if(GUILayout.Button("Copy Shape 3 Properties", GUILayout.Width(200)))
                            {
                                CopyShapePropertiesAndKeywords(shape2FirstProperty, shape3FirstProperty, 2, 3);
                            }
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
                else if(shapeNumber == 3)
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        if(GUILayout.Button("Copy Shape 1 Properties", GUILayout.Width(200)))
                        {
                            CopyShapePropertiesAndKeywords(shape3FirstProperty, shape1FirstProperty, 3, 1);
                        }

                        if(GUILayout.Button("Copy Shape 2 Properties", GUILayout.Width(200)))
                        {
                            CopyShapePropertiesAndKeywords(shape3FirstProperty, shape2FirstProperty, 3, 2);
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndVertical();
            }
        }

        private void CopyShapePropertiesAndKeywords(int shapeToFirstProperty, int shapeFromFirstProperty, int shapeToNumber, int shapeFromNumber)
        {
            for(int i = 0; i < 12; i++) CopyShapeProperty(shapeToFirstProperty + i, shapeFromFirstProperty + i);
            CopyShapeProperty(129 + shapeToNumber, 129 + shapeFromNumber);
            if(oldKeyWords.Contains("SHAPE" + shapeFromNumber + "CONTRAST_ON")) SetKeywordOnAllTargets("SHAPE" + shapeToNumber + "CONTRAST_ON", true);
            else SetKeywordOnAllTargets("SHAPE" + shapeToNumber + "CONTRAST_ON", false);
            if(oldKeyWords.Contains("SHAPE" + shapeFromNumber + "DISTORT_ON")) SetKeywordOnAllTargets("SHAPE" + shapeToNumber + "DISTORT_ON", true);
            else SetKeywordOnAllTargets("SHAPE" + shapeToNumber + "DISTORT_ON", false);

            if(oldKeyWords.Contains("SHAPE" + shapeFromNumber + "ROTATE_ON")) SetKeywordOnAllTargets("SHAPE" + shapeToNumber + "ROTATE_ON", true);
            else SetKeywordOnAllTargets("SHAPE" + shapeToNumber + "ROTATE_ON", false);
            int rotationOffsetToIndex = 150 + ((shapeToNumber - 1) * 2); //150 is the first rotation property
            int rotationOffsetFromIndex = 150 + ((shapeFromNumber - 1) * 2);
            CopyShapeProperty(rotationOffsetToIndex, rotationOffsetFromIndex);
            CopyShapeProperty(rotationOffsetToIndex + 1, rotationOffsetFromIndex + 1); //Copy rotation speed

            if(oldKeyWords.Contains("SHAPE" + shapeFromNumber + "SHAPECOLOR_ON")) SetKeywordOnAllTargets("SHAPE" + shapeToNumber + "SHAPECOLOR_ON", true);
            else SetKeywordOnAllTargets("SHAPE" + shapeToNumber + "SHAPECOLOR_ON", false);
            if(oldKeyWords.Contains("SHAPE" + shapeFromNumber + "SCREENUV_ON")) SetKeywordOnAllTargets("SHAPE" + shapeToNumber + "SCREENUV_ON", true);
            else SetKeywordOnAllTargets("SHAPE" + shapeToNumber + "SCREENUV_ON", false);

            SetShaderBasedOnEffectsAndPipeline();
        }

        private void CopyShapeProperty(int propertyTo, int propertyFrom)
        {
            AllIn1ShaderPropertyType propType = EditorUtils.GetShaderTypeByMaterialProperty(Prop(propertyTo));

            if (propType == AllIn1ShaderPropertyType.Float || propType == AllIn1ShaderPropertyType.Range)
            {
                Prop(propertyTo).floatValue = Prop(propertyFrom).floatValue;
            }
            else if(propType == AllIn1ShaderPropertyType.Vector)
            {
                Prop(propertyTo).vectorValue = Prop(propertyFrom).vectorValue;
            }
            else if(propType == AllIn1ShaderPropertyType.Color)
            {
                Prop(propertyTo).colorValue = Prop(propertyFrom).colorValue;
            }
            else if(propType == AllIn1ShaderPropertyType.Texture)
            {
                Prop(propertyTo).textureValue = Prop(propertyFrom).textureValue;
                Prop(propertyTo).textureScaleAndOffset = Prop(propertyFrom).textureScaleAndOffset;
            }
        }

        private void GenericEffect(string inspector, string keyword, int first, int last, bool effectCounter = true,
            string[] preMessages = null, int[] extraProperties = null, string[] keywordsToDisable = null)
        {
            bool toggle = oldKeyWords.Contains(keyword);
            bool ini = toggle;

            GUIContent effectNameLabel = new GUIContent();
            effectNameLabel.tooltip = keyword + " (C#)";
            if(effectCounter)
            {
                effectNameLabel.text = effectCount + "." + inspector;
                toggle = EditorGUILayout.BeginToggleGroup(effectNameLabel, toggle);
                effectCount++;
            }
            else
            {
                effectNameLabel.text = inspector;
                toggle = EditorGUILayout.BeginToggleGroup(effectNameLabel, toggle);
            }

            if(ini != toggle && !Application.isPlaying)
            {
                if(toggle)
                {
                    SetKeywordOnAllTargets(keyword, true);
                    if(keywordsToDisable != null)
                        foreach(string effect in keywordsToDisable)
                            SetKeywordOnAllTargets(effect, false);
                }
                else SetKeywordOnAllTargets(keyword, false);

                SetShaderBasedOnEffectsAndPipeline();
                Save();
            }

            if(toggle && first > 0)
            {
                EditorGUILayout.BeginVertical(propertiesStyle);
                {
                    if(preMessages != null)
                    {
                        foreach(string s in preMessages)
                        {
                            GUILayout.Label(s, smallLabel);
                            EditorGUILayout.Space();
                        }
                    }

                    for(int i = first; i <= last; i++) DrawProperty(i);
                    if(extraProperties != null)
                        foreach(int i in extraProperties)
                            DrawProperty(i);
                }
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndToggleGroup();
        }

        private void Glow(string inspector, string keyword)
        {
            if(SpecialCaseEffectBodyToggle(inspector, keyword))
            {
                EditorGUILayout.BeginVertical(propertiesStyle);
                {
                    DrawProperty(62);

                    EditorGUILayout.Separator();
                    if(DrawEffectSubKeywordToggle("Use Glow Mask Tex?", "GLOWTEX_ON")) DrawProperty(63);
                    DrawProperty(60);
                    DrawProperty(61);
                }
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndToggleGroup();
        }

        private void ColorRamp(string inspector, string keyword, string keyword2)
        {
            if(SpecialCaseEffectBodyToggle(inspector, keyword))
            {
                EditorGUILayout.BeginVertical(propertiesStyle);
                {
                    bool useEditableGradient = false;
                    if(AssetDatabase.Contains(targetMat))
                    {
                        useEditableGradient = oldKeyWords.Contains(keyword2);
                        bool gradientTex = useEditableGradient;
                        gradientTex = GUILayout.Toggle(gradientTex, new GUIContent("Use Editable Gradient?"));
                        if(useEditableGradient != gradientTex)
                        {
                            if(!Application.isPlaying) Save();
                            if(gradientTex)
                            {
                                useEditableGradient = true;
                                SetKeywordOnAllTargets(keyword2, true);
                            }
                            else SetKeywordOnAllTargets(keyword2, false);
                        }

                        if(useEditableGradient) matEditor.ShaderProperty(Prop(50), Prop(50).displayName);
                    }
                    else
                    {
                        GUILayout.Label("*Save to folder to allow for dynamic Gradient property", smallLabel);
                        EditorGUILayout.Space();
                    }

                    GUILayout.Label("*Premultiply Color toggle recommended on additive setups", smallLabel);
                    EditorGUILayout.Space();

                    if(!useEditableGradient) DrawProperty(48);

                    DrawProperty(49);
                    DrawProperty(51);
                }
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndToggleGroup();
        }

        private void AlphaSmoothstep(string inspector, string keyword, int first, int last)
        {
            if(SpecialCaseEffectBodyToggle(inspector, keyword))
            {
                EditorGUILayout.BeginVertical(propertiesStyle);
                {
                    DrawMinMaxSlider("Min And Max Slider", first, last);
                }
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndToggleGroup();
        }

        private void CameraDistFade(string inspector, string keyword, int first, int last)
        {
            if(SpecialCaseEffectBodyToggle(inspector, keyword))
            {
                EditorGUILayout.BeginVertical(propertiesStyle);
                {
                    DrawMinMaxSlider("Min And Max Slider", first, first + 1, 0f, 2000f);
                    EditorGUILayout.Space();
                    DrawProperty(last);
                }
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndToggleGroup();
        }

        private void AlphaFade(string inspector, string keyword, int first, int last)
        {
            if(SpecialCaseEffectBodyToggle(inspector, keyword))
            {
                EditorGUILayout.BeginVertical(propertiesStyle);
                {
                    DrawEffectSubKeywordToggle("Fade Amount Affects Global Transparency?", "ALPHAFADETRANSPARENCYTOO_ON");
                    if(isPs)
                    {
                        if(DrawEffectSubKeywordToggle("Fade Amount Driven By Vertex Stream? Custom1.y (See Docs)", "ALPHAFADEINPUTSTREAM_ON"))
                            GUILayout.Label("*Custom Data Auto Setup button on Particle Helper\n component will do the setup for you", smallLabel);
                    }

                    DrawEffectSubKeywordToggle("Use grayscale as alpha? (Good for Additive configurations)", "ALPHAFADEUSEREDCHANNEL_ON");
                    if(DrawEffectSubKeywordToggle("Use Shape1 as fade mask?", "ALPHAFADEUSESHAPE1_ON"))
                    {
                        GUILayout.Label("*The effect is using the Shape1 result as the fade mask texture", smallLabel);
                    }
                    else GUILayout.Label("*The effect is using the global result as the fade mask texture", smallLabel);

                    DrawLine(Color.grey, 1, 3);
                    for(int i = first; i <= last; i++) DrawProperty(i);
                }
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndToggleGroup();
        }

        private void OffsetVertexStream(string inspector, string keyword, int first)
        {
            if(SpecialCaseEffectBodyToggle(inspector, keyword))
            {
                EditorGUILayout.BeginVertical(propertiesStyle);
                {
                    GUILayout.Label("*Use Particle System Custom Data Custom2.xy (See Docs)", smallLabel);
                    EditorGUILayout.Space();
                    GUILayout.Label("*Custom Data Auto Setup button on Particle Helper\n component will do the setup for you", smallLabel);
                    DrawProperty(first);
                    if(shape2On) DrawProperty(first + 1);
                    if(shape3On) DrawProperty(first + 2);
                }
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndToggleGroup();
        }

        private void ShapeWeightVertexStream(string inspector, string keyword, int first)
        {
            if(SpecialCaseEffectBodyToggle(inspector, keyword))
            {
                EditorGUILayout.BeginVertical(propertiesStyle);
                {
                    if(!shape2On && !shape3On)
                    {
                        GUILayout.Label("*THIS EFFECT NEEDS AT LEAST 2 ACTIVE SHAPES", smallLabel);
                        EditorGUILayout.Space();
                        EditorGUILayout.Space();
                    }

                    GUILayout.Label("*Use Particle System Custom Data Custom2.z (See Docs)", smallLabel);
                    EditorGUILayout.Space();
                    GUILayout.Label("*Custom Data Auto Setup button on Particle Helper\n component will do the setup for you", smallLabel);
                    EditorGUILayout.Space();
                    GUILayout.Label("*Offset is added to shape weight", smallLabel);
                    if(shape2On || shape3On) DrawProperty(first);
                    if(shape2On) DrawProperty(first + 1);
                    if(shape3On) DrawProperty(first + 2);
                }
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndToggleGroup();
        }

        private void ShapeTextureOffset(string inspector, string keyword, int first)
        {
            if(SpecialCaseEffectBodyToggle(inspector, keyword))
            {
                EditorGUILayout.BeginVertical(propertiesStyle);
                {
                    GUILayout.Label("*Uses Random Seed to offset Shape textures", smallLabel);
                    DrawProperty(first);
                    if(shape2On) DrawProperty(first + 1);
                    if(shape3On) DrawProperty(first + 2);
                }
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndToggleGroup();
        }

        private void ScreenDistort(string inspector, string keyword, int first, int last)
        {
            if(SpecialCaseEffectBodyToggle(inspector, keyword))
            {
                EditorGUILayout.BeginVertical(propertiesStyle);
                {
                    GUILayout.Label("*This effect is performant intensive", smallLabel);
                    GUILayout.Label("*Use the Transparent Preset", smallLabel);
                    EditorGUILayout.Space();
                    DrawEffectSubKeywordToggle("Distort only objects behind? (Less performant)", "DISTORTONLYBACK_ON");
                    if(!DrawEffectSubKeywordToggle("Use Global Color as distortion mask?", "DISTORTUSECOL_ON"))
                    {
                        DrawProperty(first);
                        for(int i = last - 1; i <= last; i++) DrawProperty(i);
                    }

                    for(int i = first + 1; i <= last - 2; i++) DrawProperty(i);
                }
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndToggleGroup();
        }

        private void FadeDissolve(string inspector, string keyword, int first, int last)
        {
            if(SpecialCaseEffectBodyToggle(inspector, keyword))
            {
                EditorGUILayout.BeginVertical(propertiesStyle);
                {
                    DrawEffectSubKeywordToggle("Fade Amount Affects Global Transparency?", "ALPHAFADETRANSPARENCYTOO_ON");
                    if(isPs)
                    {
                        if(DrawEffectSubKeywordToggle("Fade Amount Driven By Vertex Stream? Custom1.y (See Docs)", "ALPHAFADEINPUTSTREAM_ON"))
                            GUILayout.Label("*Custom Data Auto Setup button on Particle Helper\n component will do the setup for you", smallLabel);
                    }

                    DrawLine(Color.grey, 1, 3);
                    for(int i = first; i <= first + 5; i++) DrawProperty(i);
                    DrawLine(Color.grey, 1, 3);
                    if(DrawEffectSubKeywordToggle("Use Fade Burn Color?", "FADEBURN_ON"))
                        for(int i = first + 6; i <= last; i++)
                            DrawProperty(i);
                }
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndToggleGroup();
        }

        private void PolarUvs(string inspector, string keyword)
        {
            if(SpecialCaseEffectBodyToggle(inspector, keyword, "POLARUVDISTORT_ON"))
            {
                EditorGUILayout.BeginVertical(propertiesStyle);
                {
                    GUILayout.Label("*Disable Generate Mip Maps on textures \nto avoid transparent line visual artifact", smallLabel);
                    EditorGUILayout.Space();
                    DrawEffectSubKeywordToggle("Polar Coords affects Distortion textures?", "POLARUVDISTORT_ON");
                }
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndToggleGroup();
        }

        private void TrailWidth(string inspector, string keyword, int powerProperty, int gradientProperty)
        {
            if(SpecialCaseEffectBodyToggle(inspector, keyword))
            {
                EditorGUILayout.BeginVertical(propertiesStyle);
                {
                    GUILayout.Label("Used for a more accurate width along trail (see Docs)", smallLabel);
                    EditorGUILayout.Space();
                    GUILayout.Label("White is max width, black is 0 width", smallLabel);
                    EditorGUILayout.Space();
                    DrawProperty(powerProperty);
                    matEditor.ShaderProperty(Prop(gradientProperty), Prop(gradientProperty).displayName);
                }
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndToggleGroup();
        }

        private void FakeLightAndShadow(string inspector, string keyword, int first, int last)
        {
            if(SpecialCaseEffectBodyToggle(inspector, keyword))
            {
                EditorGUILayout.BeginVertical(propertiesStyle);
                {
                    GUILayout.Label("You'll need AllIn1VfxFakeLightDirSetter.cs in the scene (see docs)", smallLabel);
                    for(int i = first; i <= first + 2; i++) DrawProperty(i);
                    DrawMinMaxSlider("Min And Max Slider", last - 1, last);
                }
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndToggleGroup();
        }

        private bool DrawEffectSubKeywordToggle(string inspector, string keyword, bool setCustomConfigAfter = false)
        {
            GUIContent propertyLabel = new GUIContent();
            propertyLabel.text = inspector;
            propertyLabel.tooltip = keyword + " (C#)";

            bool ini = oldKeyWords.Contains(keyword);
            bool toggle = ini;
            toggle = GUILayout.Toggle(toggle, propertyLabel);
            if(ini != toggle)
            {
                if(toggle) SetKeywordOnAllTargets(keyword, true);
                else SetKeywordOnAllTargets(keyword, false);

                SetShaderBasedOnEffectsAndPipeline();
                if (!setCustomConfigAfter) Save();
                else SaveAndSetCustomConfig();
            }

            return toggle;
        }

        private bool SpecialCaseEffectBodyToggle(string inspector, string keyword, string extraKeywordToDisable = null)
        {
            bool toggle = oldKeyWords.Contains(keyword);
            bool ini = toggle;
            toggle = SpecialCaseEffectHeaderToggle(inspector, keyword, toggle);

            effectCount++;
            if(ini != toggle && !Application.isPlaying)
            {
                if(toggle) SetKeywordOnAllTargets(keyword, true);
                else
                {
                    SetKeywordOnAllTargets(keyword, false);
                    if(extraKeywordToDisable != null) SetKeywordOnAllTargets(extraKeywordToDisable, false);
                }

                SetShaderBasedOnEffectsAndPipeline();
                Save();
            }

            return toggle;
        }

        private bool SpecialCaseEffectHeaderToggle(string inspector, string keyword, bool toggle)
        {
            GUIContent effectNameLabel = new GUIContent();
            effectNameLabel.tooltip = keyword + " (C#)";
            effectNameLabel.text = effectCount + "." + inspector;
            toggle = EditorGUILayout.BeginToggleGroup(effectNameLabel, toggle);
            return toggle;
        }

        private MaterialProperty Prop(int index)
        {
            return matProperties[index + propertyOffset];
        }

        private void DrawProperty(int index, bool noReset = false)
        {
            MaterialProperty targetProperty = Prop(index);

            EditorGUILayout.BeginHorizontal();
            {
                GUIContent propertyLabel = new GUIContent();
                propertyLabel.text = targetProperty.displayName;
                propertyLabel.tooltip = targetProperty.name + " (C#)";

                matEditor.ShaderProperty(targetProperty, propertyLabel);

                if(!noReset)
                {
                    GUIContent resetButtonLabel = new GUIContent();
                    resetButtonLabel.text = "R";
                    resetButtonLabel.tooltip = "Resets to default value";
                    if(GUILayout.Button(resetButtonLabel, GUILayout.Width(20))) ResetProperty(targetProperty);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void ResetProperty(MaterialProperty targetProperty)
        {
            AllIn1ShaderPropertyType propType = EditorUtils.GetShaderTypeByMaterialProperty(targetProperty);

            if(originalMaterialCopy == null) originalMaterialCopy = new Material(targetMat.shader);
            if(propType == AllIn1ShaderPropertyType.Float || propType == AllIn1ShaderPropertyType.Range)
            {
                targetProperty.floatValue = originalMaterialCopy.GetFloat(targetProperty.name);
            }
            else if(propType == AllIn1ShaderPropertyType.Vector)
            {
                targetProperty.vectorValue = originalMaterialCopy.GetVector(targetProperty.name);
            }
            else if(propType == AllIn1ShaderPropertyType.Color)
            {
                targetProperty.colorValue = originalMaterialCopy.GetColor(targetProperty.name);
            }
            else if(propType == AllIn1ShaderPropertyType.Texture)
            {
                targetProperty.textureValue = originalMaterialCopy.GetTexture(targetProperty.name);
                targetProperty.textureScaleAndOffset = new Vector4(1, 1, 0, 0);
            }
        }

        private void DrawMinMaxSlider(string sliderText, int propMin, int propMax, float valueMin = 0f, float valueMax = 1f)
        {
            DrawProperty(propMin);
            DrawProperty(propMax);
            MaterialProperty matPropMin = Prop(propMin);
            MaterialProperty matPropMax = Prop(propMax);
            float stepMin = matPropMin.floatValue;
            float stepMax = matPropMax.floatValue;
            EditorGUILayout.MinMaxSlider(sliderText, ref stepMin, ref stepMax, valueMin, valueMax);
            if(stepMax < stepMin) stepMax = stepMin;
            matPropMin.floatValue = stepMin;
            matPropMax.floatValue = stepMax;
        }

        private static void SetLabelAndFieldWidth(int labelWidth, int fieldWidth)
        {
            EditorGUIUtility.labelWidth = labelWidth;
            EditorGUIUtility.fieldWidth = fieldWidth;
        }

        //SetLabelWidthFromText("zTestMode");
        //GUILayout.Label(EditorGUIUtility.labelWidth.ToString());
        //SetFieldWidthFromText("Less Equals");
        //GUILayout.Label(EditorGUIUtility.fieldWidth.ToString());
        private void SetLabelWidthFromText(string text)
        {
            Vector2 textDimensions = GUI.skin.label.CalcSize(new GUIContent(text));
            EditorGUIUtility.labelWidth = textDimensions.x;
            Debug.Log("Label: " + text + "   " + textDimensions.x);
        }

        private void SetFieldWidthFromText(string text)
        {
            Vector2 textDimensions = GUI.skin.label.CalcSize(new GUIContent(text));
            EditorGUIUtility.fieldWidth = textDimensions.x;
            Debug.Log("Field: " + text + "   " + textDimensions.x);
        }

        private void DrawLine(Color color, int thickness = 2, int padding = 10)
        {
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
            r.height = thickness;
            r.y += (padding / 2f);
            r.x -= 2;
            r.width += 6;
            EditorGUI.DrawRect(r, color);
        }

        private void SaveAndSetCustomConfig()
        {
            renderPreset = RenderingPreset.Custom;
            MaterialProperty renderM = Prop(0);
            renderM.floatValue = (float)(renderPreset);
            Save();
        }

        private void Save()
        {
            if(!Application.isPlaying) EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            foreach(Object obj in matEditor.targets) EditorUtility.SetDirty(obj);
        }

        private static void InitStylesIfNeeded()
        {
            if(propertiesStyle != null) return;
            propertiesStyle = new GUIStyle(EditorStyles.helpBox) { margin = new RectOffset(0, 0, 0, 0) };
            bigLabel = new GUIStyle(EditorStyles.boldLabel) { fontSize = bigFontSize };
            smallLabel = new GUIStyle { fontStyle = FontStyle.Bold };
            toggleButtonStyle = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleCenter, richText = true };
            centeredTextStyle = new GUIStyle { alignment = TextAnchor.MiddleCenter, richText = true, fontSize = 13 };
            smallTextStyle = new GUIStyle(GUI.skin.button) { fontSize = 10 };
            smallBoldLabel = new GUIStyle(EditorStyles.boldLabel) { fontSize = 11 };
        }

        private static void ApplyRenderingPreset(Material mat, RenderingPreset preset)
        {
            switch(preset)
            {
                case RenderingPreset.Transparent:
                    mat.SetOverrideTag("RenderType", "Transparent");
                    mat.SetInt("_SrcMode", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstMode", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    if(mat.renderQueue < 2050) mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                    mat.DisableKeyword("ADDITIVECONFIG_ON");
                    mat.DisableKeyword("PREMULTIPLYALPHA_ON");
                    mat.DisableKeyword("PREMULTIPLYCOLOR_ON");
                    break;
                case RenderingPreset.Additive:
                    mat.SetOverrideTag("RenderType", "Transparent");
                    mat.SetInt("_SrcMode", (int)UnityEngine.Rendering.BlendMode.One);
                    mat.SetInt("_DstMode", (int)UnityEngine.Rendering.BlendMode.One);
                    if(mat.renderQueue < 2050) mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                    mat.EnableKeyword("ADDITIVECONFIG_ON");
                    mat.DisableKeyword("PREMULTIPLYALPHA_ON");
                    mat.DisableKeyword("PREMULTIPLYCOLOR_ON");
                    break;
                case RenderingPreset.SoftAdditive:
                    mat.SetOverrideTag("RenderType", "Transparent");
                    mat.SetInt("_SrcMode", (int)UnityEngine.Rendering.BlendMode.OneMinusDstColor);
                    mat.SetInt("_DstMode", (int)UnityEngine.Rendering.BlendMode.One);
                    if(mat.renderQueue < 2050) mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                    mat.EnableKeyword("ADDITIVECONFIG_ON");
                    mat.DisableKeyword("PREMULTIPLYALPHA_ON");
                    mat.DisableKeyword("PREMULTIPLYCOLOR_ON");
                    break;
                case RenderingPreset.BlendAdd:
                    mat.SetOverrideTag("RenderType", "Transparent");
                    mat.SetInt("_SrcMode", (int)UnityEngine.Rendering.BlendMode.One);
                    mat.SetInt("_DstMode", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    if(mat.renderQueue < 2050) mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                    mat.EnableKeyword("ADDITIVECONFIG_ON");
                    mat.DisableKeyword("PREMULTIPLYALPHA_ON");
                    mat.EnableKeyword("PREMULTIPLYCOLOR_ON");
                    break;
                case RenderingPreset.Opaque:
                    mat.SetOverrideTag("RenderType", "");
                    mat.SetInt("_SrcMode", (int)UnityEngine.Rendering.BlendMode.One);
                    mat.SetInt("_DstMode", (int)UnityEngine.Rendering.BlendMode.Zero);
                    if(mat.renderQueue >= 2050) mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
                    mat.SetInt("_ZWrite", 1);
                    mat.DisableKeyword("ADDITIVECONFIG_ON");
                    mat.DisableKeyword("PREMULTIPLYALPHA_ON");
                    mat.DisableKeyword("PREMULTIPLYCOLOR_ON");
                    break;
            }
        }

        //Keyword writes go through every selected material, not just the first one
        private void SetKeywordOnAllTargets(string keyword, bool enabled)
        {
            foreach(Object obj in matEditor.targets)
            {
                Material mat = (Material)obj;
                if(enabled) mat.EnableKeyword(keyword);
                else mat.DisableKeyword(keyword);
            }
        }

        private void ForEachTargetMat(System.Action<Material> action)
        {
            foreach(Object obj in matEditor.targets) action((Material)obj);
        }

        private void SetShaderBasedOnEffectsAndPipeline()
        {
            oldKeyWords = targetMat.shaderKeywords;

            ForEachTargetMat(mat =>
            {
                bool depthGlowOn = mat.IsKeywordEnabled("DEPTHGLOW_ON");
                int currentQueue = mat.renderQueue;
                int baseQueue = currentQueue >= 2500 ? 3000 : 2000;
                int offset = currentQueue - baseQueue;

                if(depthGlowOn && baseQueue != 3000)
                {
                    mat.renderQueue = 3000 + offset;
                    EditorUtility.SetDirty(mat);
                }
                else if(!depthGlowOn && baseQueue != 2000)
                {
                    mat.renderQueue = 2000 + offset;
                    EditorUtility.SetDirty(mat);
                }
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
#endif