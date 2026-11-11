#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Presets;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AllIn1Assets.AllIn1VfxToolkit
{
    [CustomEditor(typeof(AllIn1ParticleHelperComponent)), CanEditMultipleObjects]
    public class AllIn1ParticleHelperEditor : Editor
    {
        private GUIStyle smallRedBoldLabel, smallBoldLabel, headerBoldLabel, propertiesStyle;

        private AllIn1VfxComponent all1Vfx;
        private ParticleSystem ps;
        private ParticleSystem.MainModule mainModule;
        private Color targetColor = Color.white;
        private int gradientColorKeyIndex = 0;
        private int helperNum;
        private SerializedProperty startColorProperty;
        private float timeSinceLastFetch = -1f;
        private float TIME_BETWEEN_FETCHES = 4f;

        private void OnEnable()
        {
            startColorProperty = serializedObject.FindProperty("startColor");
            LoadParticleHelpersAndPresets();
        }

        public override void OnInspectorGUI()
        {
            InitializeStyles();

            if(Selection.activeGameObject != null) ps = Selection.activeGameObject.GetComponent<ParticleSystem>();
            if(ps == null)
            {
                GUILayout.Label("Please add a Particle System to this Game Object", smallRedBoldLabel);
                EditorGUILayout.Space();
                if(GUILayout.Button("Add New Particle System")) Selection.activeGameObject.AddComponent<ParticleSystem>();
                return;
            }

            if(Selection.activeGameObject != null) all1Vfx = Selection.activeGameObject.GetComponent<AllIn1VfxComponent>();
            if(all1Vfx == null)
            {
                if(GUILayout.Button("Add All In 1 Vfx Component"))
                {
                    AllIn1VfxComponent t = Selection.activeGameObject.AddComponent<AllIn1VfxComponent>();
                    UnityEditorInternal.ComponentUtility.MoveComponentUp(t);
                }

                DrawLine(Color.grey, 1, 3);
            }

            mainModule = ps.main;

            helperNum = 1;
            EditorGUILayout.LabelField("Generic Helpers", headerBoldLabel, GUILayout.Height(20));
            HierarchyHelpers();
            ColorChangeUI();
            EditorGUILayout.Space();
			if (GUILayout.Button("Custom Data Auto Setup"))
			{
				for (int i = 0; i < targets.Length; i++)
				{
					AllIn1ParticleHelperComponent particleHelperComponent = (AllIn1ParticleHelperComponent)targets[i];
					CustomDataAutoSetup(particleHelperComponent);
				}
			}
            EditorGUILayout.Space();

            DrawLine(Color.grey, 1, 3);
            EditorGUILayout.LabelField("Particle System Options and Presets", headerBoldLabel, GUILayout.Height(20));
            
            EditorGUI.BeginChangeCheck();
            GeneralOptions();
            EmissionOptions();
            ShapeOptions();
            OverLifetimeOptions();
            ParticleHelperPresets();
            ParticleSystemPresets();

            EditorGUILayout.Space();

            if(GUILayout.Button("Fetch Particle System Current Values"))
                for(int i = 0; i < targets.Length; i++)
                    ((AllIn1ParticleHelperComponent)targets[i]).FetchAllCurrentValues();

            bool applyEverythingOnChange = ((AllIn1ParticleHelperComponent)target).applyEverythingOnChange;

            if(!applyEverythingOnChange)
            {
				if (GUILayout.Button("Apply All Options Settings"))
				{
					for (int i = 0; i < targets.Length; i++)
					{
						AllIn1ParticleHelperComponent particleHelperComponent = (AllIn1ParticleHelperComponent)targets[i];
						ApplyCurrentSettings(particleHelperComponent);
					}
				}
            }

            bool toggle = ((AllIn1ParticleHelperComponent)target).applyEverythingOnChange;
            EditorGUIUtility.labelWidth = 190;
            toggle = EditorGUILayout.Toggle("Auto Apply On Change Property", toggle);
            EditorGUIUtility.labelWidth = 145;
            if(toggle != ((AllIn1ParticleHelperComponent)target).applyEverythingOnChange)
                for(int i = 0; i < targets.Length; i++)
                    ((AllIn1ParticleHelperComponent)targets[i]).applyEverythingOnChange = toggle;

            EditorGUILayout.Space();
            DrawLine(Color.grey, 1, 3);
            EditorGUILayout.Space();
            if(GUILayout.Button("Remove Component"))
                for(int i = targets.Length - 1; i >= 0; i--)
                    DestroyImmediate(targets[i] as AllIn1ParticleHelperComponent);

            if(EditorGUI.EndChangeCheck() && applyEverythingOnChange)
            {
                for(int i = 0; i < targets.Length; i++)
                    ((AllIn1ParticleHelperComponent)targets[i]).ApplyCurrentSettings();
            }
        }
        
        private void InitializeStyles()
        {
            if(smallRedBoldLabel == null)
            {
                smallRedBoldLabel = new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = new Color(0.86f, 0.31f, 0.3f) } };
                smallBoldLabel = new GUIStyle(EditorStyles.boldLabel);
                headerBoldLabel = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13, alignment = TextAnchor.MiddleCenter };
                propertiesStyle = new GUIStyle(EditorStyles.helpBox) { margin = new RectOffset(0, 0, 0, 0) };
            }
        }

        private void HierarchyHelpers()
        {
            bool toggle = ((AllIn1ParticleHelperComponent)target).hierarchyHelpers;
            toggle = EditorGUILayout.BeginToggleGroup("Hierarchy Helpers", toggle);
            if(toggle != ((AllIn1ParticleHelperComponent)target).hierarchyHelpers)
                for(int i = 0; i < targets.Length; i++)
                    ((AllIn1ParticleHelperComponent)targets[i]).hierarchyHelpers = toggle;
            if(toggle)
            {
                EditorGUILayout.BeginVertical(propertiesStyle);
                if(GUILayout.Button("Add Child Copy"))
                    for(int i = 0; i < targets.Length; i++)
                        ((AllIn1ParticleHelperComponent)targets[i]).CopyCurrentGameObject(false);
                if(GUILayout.Button("Add Sibling Copy"))
                    for(int i = 0; i < targets.Length; i++)
                        ((AllIn1ParticleHelperComponent)targets[i]).CopyCurrentGameObject(true);
                int nCopies = ((AllIn1ParticleHelperComponent)target).numberOfCopies;
                EditorGUI.BeginChangeCheck();
                nCopies = EditorGUILayout.IntField("New Copy Number", nCopies);
                if(EditorGUI.EndChangeCheck())
                    for(int i = 0; i < targets.Length; i++)
                        ((AllIn1ParticleHelperComponent)targets[i]).numberOfCopies = nCopies;
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndToggleGroup();
        }

        private void ColorChangeUI()
        {
            bool toggle = ((AllIn1ParticleHelperComponent)target).colorChangeOption;
            toggle = EditorGUILayout.BeginToggleGroup("Palette Color Change", toggle);
            if(toggle != ((AllIn1ParticleHelperComponent)target).colorChangeOption)
                for(int i = 0; i < targets.Length; i++)
                    ((AllIn1ParticleHelperComponent)targets[i]).colorChangeOption = toggle;
            if(toggle)
            {
                EditorGUILayout.BeginVertical(propertiesStyle);
                targetColor = EditorGUILayout.ColorField("New Color", targetColor);
                if(mainModule.startColor.mode == ParticleSystemGradientMode.Color ||
                   mainModule.startColor.mode == ParticleSystemGradientMode.TwoColors)
                {
					if (GUILayout.Button("Recolor Particle System"))
					{
						for (int i = 0; i < targets.Length; i++)
						{
							AllIn1ParticleHelperComponent particleHelperComponent = (AllIn1ParticleHelperComponent)targets[i];
							HueShiftParticleSystem(particleHelperComponent, targetColor);
						}
					}
                }
                else if(mainModule.startColor.mode == ParticleSystemGradientMode.Gradient ||
                        mainModule.startColor.mode == ParticleSystemGradientMode.RandomColor ||
                        mainModule.startColor.mode == ParticleSystemGradientMode.TwoGradients)
                {
                    EditorGUILayout.IntField("Gradient Reference Index", gradientColorKeyIndex, GUILayout.ExpandWidth(true));
                    if(GUILayout.Button("Recolor Particle System"))
                        for(int i = 0; i < targets.Length; i++)
                            ((AllIn1ParticleHelperComponent)targets[i]).HueShiftParticleSystem(targetColor, gradientColorKeyIndex);
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndToggleGroup();
        }

        private void GeneralOptions()
        {
            bool toggle = ((AllIn1ParticleHelperComponent)target).generalOptions;
            toggle = EditorGUILayout.BeginToggleGroup(helperNum + ".General Options", toggle);
            if(toggle != ((AllIn1ParticleHelperComponent)target).generalOptions)
                for(int i = 0; i < targets.Length; i++)
                    ((AllIn1ParticleHelperComponent)targets[i]).generalOptions = toggle;
            if(toggle)
            {
                EditorGUILayout.BeginVertical(propertiesStyle);

                toggle = ((AllIn1ParticleHelperComponent)target).matchDurationToLifetime;
                EditorGUIUtility.labelWidth = 170;
                toggle = EditorGUILayout.Toggle("Match Duration To Lifetime?", toggle);
                EditorGUIUtility.labelWidth = 145;
                if(toggle != ((AllIn1ParticleHelperComponent)target).matchDurationToLifetime)
                    for(int i = 0; i < targets.Length; i++)
                        ((AllIn1ParticleHelperComponent)targets[i]).matchDurationToLifetime = toggle;

                toggle = ((AllIn1ParticleHelperComponent)target).randomRotation;
                toggle = EditorGUILayout.Toggle("Random Rotation?", toggle);
                if(toggle != ((AllIn1ParticleHelperComponent)target).randomRotation)
                    for(int i = 0; i < targets.Length; i++)
                        ((AllIn1ParticleHelperComponent)targets[i]).randomRotation = toggle;

                float minLifetime = ((AllIn1ParticleHelperComponent)target).minLifetime;
                float maxLifetime = ((AllIn1ParticleHelperComponent)target).maxLifetime;
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.MinMaxSlider("Particle Lifetime", ref minLifetime, ref maxLifetime, 0, 30);
                minLifetime = EditorGUILayout.FloatField("Min Lifetime", minLifetime);
                maxLifetime = EditorGUILayout.FloatField("Max Lifetime", maxLifetime);
                if(EditorGUI.EndChangeCheck())
                {
                    minLifetime = Mathf.Clamp(minLifetime, 0, 1000);
                    maxLifetime = Mathf.Clamp(maxLifetime, 0, 1000);
                    if(minLifetime > maxLifetime) maxLifetime = minLifetime;
                    for(int i = 0; i < targets.Length; i++) ((AllIn1ParticleHelperComponent)targets[i]).minLifetime = minLifetime;
                    for(int i = 0; i < targets.Length; i++) ((AllIn1ParticleHelperComponent)targets[i]).maxLifetime = maxLifetime;
                }

                float minSpeed = ((AllIn1ParticleHelperComponent)target).minSpeed;
                float maxSpeed = ((AllIn1ParticleHelperComponent)target).maxSpeed;
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.MinMaxSlider("Particle Speed", ref minSpeed, ref maxSpeed, 0, 30);
                minSpeed = EditorGUILayout.FloatField("Min Speed", minSpeed);
                maxSpeed = EditorGUILayout.FloatField("Max Speed", maxSpeed);
                if(EditorGUI.EndChangeCheck())
                {
                    minSpeed = Mathf.Clamp(minSpeed, 0, 1000);
                    maxSpeed = Mathf.Clamp(maxSpeed, 0, 1000);
                    if(minSpeed > maxSpeed) maxSpeed = minSpeed;
                    for(int i = 0; i < targets.Length; i++) ((AllIn1ParticleHelperComponent)targets[i]).minSpeed = minSpeed;
                    for(int i = 0; i < targets.Length; i++) ((AllIn1ParticleHelperComponent)targets[i]).maxSpeed = maxSpeed;
                }

                float minSize = ((AllIn1ParticleHelperComponent)target).minSize;
                float maxSize = ((AllIn1ParticleHelperComponent)target).maxSize;
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.MinMaxSlider("Particle Size", ref minSize, ref maxSize, 0, 30);
                minSize = EditorGUILayout.FloatField("Min Size", minSize);
                maxSize = EditorGUILayout.FloatField("Max Size", maxSize);
                if(EditorGUI.EndChangeCheck())
                {
                    minSize = Mathf.Clamp(minSize, 0, 1000);
                    maxSize = Mathf.Clamp(maxSize, 0, 1000);
                    if(minSize > maxSize) maxSize = minSize;
                    for(int i = 0; i < targets.Length; i++) ((AllIn1ParticleHelperComponent)targets[i]).minSize = minSize;
                    for(int i = 0; i < targets.Length; i++) ((AllIn1ParticleHelperComponent)targets[i]).maxSize = maxSize;
                }

                EditorGUILayout.PropertyField(startColorProperty, new GUIContent("Start Color"));
                serializedObject.ApplyModifiedProperties();

                if(GUILayout.Button("Fetch Particle System General Values"))
                    for(int i = 0; i < targets.Length; i++)
                        ((AllIn1ParticleHelperComponent)targets[i]).FetchGeneralValues();

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndToggleGroup();
            helperNum++;
        }

        private void EmissionOptions()
        {
            bool toggle = ((AllIn1ParticleHelperComponent)target).emissionOptions;
            toggle = EditorGUILayout.BeginToggleGroup(helperNum + ".Emission Options", toggle);
            if(toggle != ((AllIn1ParticleHelperComponent)target).emissionOptions)
                for(int i = 0; i < targets.Length; i++)
                    ((AllIn1ParticleHelperComponent)targets[i]).emissionOptions = toggle;
            if(toggle)
            {
                EditorGUILayout.BeginVertical(propertiesStyle);

                toggle = ((AllIn1ParticleHelperComponent)target).isBurst;
                toggle = EditorGUILayout.Toggle("Use burst?", toggle);
                if(toggle != ((AllIn1ParticleHelperComponent)target).isBurst)
                    for(int i = 0; i < targets.Length; i++)
                        ((AllIn1ParticleHelperComponent)targets[i]).isBurst = toggle;

                float minNumberOfParticles = ((AllIn1ParticleHelperComponent)target).minNumberOfParticles;
                float maxNumberOfParticles = ((AllIn1ParticleHelperComponent)target).maxNumberOfParticles;
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.MinMaxSlider("Number of Particles", ref minNumberOfParticles, ref maxNumberOfParticles, 0, 100);
                minNumberOfParticles = EditorGUILayout.IntField("Min Particles", Mathf.RoundToInt(minNumberOfParticles));
                maxNumberOfParticles = EditorGUILayout.IntField("Max Particles", Mathf.RoundToInt(maxNumberOfParticles));
                if(EditorGUI.EndChangeCheck())
                {
                    minNumberOfParticles = Mathf.Clamp(minNumberOfParticles, 0, 1000);
                    maxNumberOfParticles = Mathf.Clamp(maxNumberOfParticles, 0, 1000);
                    if(minNumberOfParticles > maxNumberOfParticles) maxNumberOfParticles = minNumberOfParticles;
                    for(int i = 0; i < targets.Length; i++) ((AllIn1ParticleHelperComponent)targets[i]).minNumberOfParticles = Mathf.RoundToInt(minNumberOfParticles);
                    for(int i = 0; i < targets.Length; i++) ((AllIn1ParticleHelperComponent)targets[i]).maxNumberOfParticles = Mathf.RoundToInt(maxNumberOfParticles);
                }

                if(GUILayout.Button("Fetch Particle System Emission Values"))
                    for(int i = 0; i < targets.Length; i++)
                        ((AllIn1ParticleHelperComponent)targets[i]).FetchEmissionValues();

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndToggleGroup();
            helperNum++;
        }

        private void ShapeOptions()
        {
            bool toggle = ((AllIn1ParticleHelperComponent)target).shapeOptions;
            toggle = EditorGUILayout.BeginToggleGroup(helperNum + ".Shape Options", toggle);
            if(toggle != ((AllIn1ParticleHelperComponent)target).shapeOptions)
                for(int i = 0; i < targets.Length; i++)
                    ((AllIn1ParticleHelperComponent)targets[i]).shapeOptions = toggle;
            if(toggle)
            {
                EditorGUILayout.BeginVertical(propertiesStyle);

                AllIn1ParticleHelperComponent.EmissionShapes tempSettings = ((AllIn1ParticleHelperComponent)target).currEmissionShape;
                EditorGUI.BeginChangeCheck();
                tempSettings = (AllIn1ParticleHelperComponent.EmissionShapes)EditorGUILayout.EnumPopup("New Shape", tempSettings);
                if(EditorGUI.EndChangeCheck())
                    for(int i = 0; i < targets.Length; i++)
                        ((AllIn1ParticleHelperComponent)targets[i]).currEmissionShape = tempSettings;

                if(GUILayout.Button("Fetch Particle System Shape Values"))
                    for(int i = 0; i < targets.Length; i++)
                        ((AllIn1ParticleHelperComponent)targets[i]).FetchShapeValues();

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndToggleGroup();
            helperNum++;
        }

        private void OverLifetimeOptions()
        {
            bool toggle = ((AllIn1ParticleHelperComponent)target).overLifetimeOptions;
            toggle = EditorGUILayout.BeginToggleGroup(helperNum + ".Over Lifetime Options", toggle);
            if(toggle != ((AllIn1ParticleHelperComponent)target).overLifetimeOptions)
                for(int i = 0; i < targets.Length; i++)
                    ((AllIn1ParticleHelperComponent)targets[i]).overLifetimeOptions = toggle;
            if(toggle)
            {
                EditorGUILayout.BeginVertical(propertiesStyle);

                AllIn1ParticleHelperComponent.LifetimeSettings tempColorLifetime = ((AllIn1ParticleHelperComponent)target).colorLifetime;
                EditorGUI.BeginChangeCheck();
                tempColorLifetime = (AllIn1ParticleHelperComponent.LifetimeSettings)EditorGUILayout.EnumPopup("Alpha Over Lifetime", tempColorLifetime);
                if(EditorGUI.EndChangeCheck())
                    for(int i = 0; i < targets.Length; i++)
                        ((AllIn1ParticleHelperComponent)targets[i]).colorLifetime = tempColorLifetime;

                AllIn1ParticleHelperComponent.LifetimeSettings tempScaleLifetime = ((AllIn1ParticleHelperComponent)target).sizeLifetime;
                EditorGUI.BeginChangeCheck();
                tempScaleLifetime = (AllIn1ParticleHelperComponent.LifetimeSettings)EditorGUILayout.EnumPopup("Scale Over Lifetime", tempScaleLifetime);
                if(EditorGUI.EndChangeCheck())
                    for(int i = 0; i < targets.Length; i++)
                        ((AllIn1ParticleHelperComponent)targets[i]).sizeLifetime = tempScaleLifetime;

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndToggleGroup();
            helperNum++;
        }

        private int selectedIndex = 0;
        private AllIn1ParticleHelperSO[] particleHelpersPresets = null;

        private void ParticleHelperPresets()
        {
            Color iniBackgroundColor = GUI.backgroundColor;

            bool toggle = ((AllIn1ParticleHelperComponent)target).particleHelperPresets;
            toggle = EditorGUILayout.BeginToggleGroup(helperNum + ".Particle Helper Presets", toggle);
            if(toggle != ((AllIn1ParticleHelperComponent)target).particleHelperPresets)
                for(int i = 0; i < targets.Length; i++)
                    ((AllIn1ParticleHelperComponent)targets[i]).particleHelperPresets = toggle;
            if(toggle)
            {
                EditorGUILayout.BeginVertical(propertiesStyle);
                
                GUI.backgroundColor = Color.cyan;
                if(GUILayout.Button("Fetch Particle Helper Presets")) LoadParticleHelpersAndPresets();
                GUI.backgroundColor = iniBackgroundColor;

                GUI.backgroundColor = Color.green;
                GUILayout.Label("Save Particle Helper Presets", smallBoldLabel);
                if(GUILayout.Button("Save Particle Helper Preset"))
                {
					for (int i = 0; i < targets.Length; i++) 
					{
						AllIn1ParticleHelperComponent particleHelperComponent = (AllIn1ParticleHelperComponent)targets[i];
						SaveParticleHelperPreset(particleHelperComponent); 
					}
                    particleHelpersPresets = null;
                    LoadParticleHelpersAndPresets();
                }
                GUI.backgroundColor = iniBackgroundColor;

                DrawLine(Color.grey, 1, 3);

                GUILayout.Label("Load Particle Helper Presets", smallBoldLabel);
                bool particleHelpersPresetsAvailable = particleHelpersPresets != null && particleHelpersPresets.Length > 0;
                if(!particleHelpersPresetsAvailable)
                {
                    GUILayout.Label("No Particle Helper Presets found", smallRedBoldLabel);
                    if(Time.realtimeSinceStartup - timeSinceLastFetch > TIME_BETWEEN_FETCHES) LoadParticleHelpersAndPresets();
                }
                else
                {
                    string[] presetNames = new string[particleHelpersPresets.Length];
                    for(int i = 0; i < particleHelpersPresets.Length; i++) presetNames[i] = particleHelpersPresets[i].name;

                    GUI.backgroundColor = Color.cyan;
                    selectedIndex = EditorGUILayout.Popup("Particle Helper Presets", selectedIndex, presetNames);
                    GUI.backgroundColor = iniBackgroundColor;

                    GUI.backgroundColor = Color.yellow;
                    if(GUILayout.Button("Apply Particle Helper Preset"))
                        for(int i = 0; i < targets.Length; i++)
                            ((AllIn1ParticleHelperComponent)targets[i]).ApplyParticleHelperPreset(particleHelpersPresets[selectedIndex], true);
                    GUI.backgroundColor = iniBackgroundColor;
                }
                
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndToggleGroup();
            helperNum++;
            GUI.backgroundColor = iniBackgroundColor;
        }

		public void SaveParticleHelperPreset(AllIn1ParticleHelperComponent particleHelperComponent)
		{
			AllIn1ParticleHelperSO psHelperPresetAsset = ScriptableObject.CreateInstance<AllIn1ParticleHelperSO>();

			psHelperPresetAsset.matchDurationToLifetime = particleHelperComponent.matchDurationToLifetime;
			psHelperPresetAsset.randomRotation = particleHelperComponent.randomRotation;
			psHelperPresetAsset.minLifetime = particleHelperComponent.minLifetime;
			psHelperPresetAsset.maxLifetime = particleHelperComponent.maxLifetime;
			psHelperPresetAsset.minSpeed = particleHelperComponent.minSpeed;
			psHelperPresetAsset.maxSpeed = particleHelperComponent.maxSpeed;
			psHelperPresetAsset.minSize = particleHelperComponent.minSize;
			psHelperPresetAsset.maxSize = particleHelperComponent.maxSize;
			psHelperPresetAsset.startColor = particleHelperComponent.startColor;

			psHelperPresetAsset.isBurst = particleHelperComponent.isBurst;
			psHelperPresetAsset.minNumberOfParticles = particleHelperComponent.minNumberOfParticles;
			psHelperPresetAsset.maxNumberOfParticles = particleHelperComponent.maxNumberOfParticles;

			psHelperPresetAsset.currEmissionShape = particleHelperComponent.currEmissionShape;

			psHelperPresetAsset.colorLifetime = particleHelperComponent.colorLifetime;
			psHelperPresetAsset.sizeLifetime = particleHelperComponent.sizeLifetime;

			string path = AllIn1VfxWindow.GetParticlePresetsPath();
			EditorUtils.CreateFolderIfNotExist(path);
			path += "/";
			if (!System.IO.Directory.Exists(path))
			{
				EditorUtility.DisplayDialog("The desired Particle Presets Save Path doesn't exist",
					"Go to Tools -> AllIn1 -> VFX Toolkit Window and set a valid folder", "Ok");
				return;
			}

			string fullPath = path + particleHelperComponent.gameObject.name + "HelperPreset" + ".asset";
			if (System.IO.File.Exists(fullPath)) fullPath = GetNewValidPath(path + particleHelperComponent.gameObject.name + "HelperPreset", ".asset");

			string fileName = fullPath.Replace(path, "");
			fileName = fileName.Replace(".asset", "");
			fullPath = EditorUtility.SaveFilePanel("Save Particle Helper Preset", path, fileName, "asset");
			if (fullPath.Length == 0) return;
			fullPath = "Assets" + fullPath.Replace(Application.dataPath, "");
			AssetDatabase.CreateAsset(psHelperPresetAsset, fullPath);
			AssetDatabase.SaveAssets();

			EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath(fullPath, typeof(AllIn1ParticleHelperSO)));
		}

		private void LoadParticleHelpersAndPresets()
        {
            timeSinceLastFetch = Time.realtimeSinceStartup;
            string[] guids = AssetDatabase.FindAssets("t:AllIn1ParticleHelperSO");
            particleHelpersPresets = guids.Select(guid => AssetDatabase.LoadAssetAtPath<AllIn1ParticleHelperSO>(AssetDatabase.GUIDToAssetPath(guid))).ToArray();
            
            guids = AssetDatabase.FindAssets("t:Preset");
            particleSystemPresets = guids
                .Select(guid => AssetDatabase.LoadAssetAtPath<Preset>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(preset => preset != null && preset.GetTargetFullTypeName().Contains("ParticleSystem"))
                .ToArray();
        }

        private Preset[] particleSystemPresets = null;

        private void ParticleSystemPresets()
        {
            Color iniBackgroundColor = GUI.backgroundColor;

            bool toggle = ((AllIn1ParticleHelperComponent)target).particleSystemPresets;
            toggle = EditorGUILayout.BeginToggleGroup(helperNum + ".Particle System Presets", toggle);
            if(toggle != ((AllIn1ParticleHelperComponent)target).particleSystemPresets)
                for(int i = 0; i < targets.Length; i++)
                    ((AllIn1ParticleHelperComponent)targets[i]).particleSystemPresets = toggle;
            if(toggle)
            {
                EditorGUILayout.BeginVertical(propertiesStyle);

                GUI.backgroundColor = Color.cyan;
                if(GUILayout.Button("Fetch Particle System Presets")) LoadParticleHelpersAndPresets();
                GUI.backgroundColor = iniBackgroundColor;
                
                GUI.backgroundColor = Color.green;
                GUILayout.Label("Save Particle System Presets", smallBoldLabel);
                if(GUILayout.Button("Save Particle System Preset"))
                {
					for (int i = 0; i < targets.Length; i++) 
					{
						AllIn1ParticleHelperComponent particleHelperComponent = (AllIn1ParticleHelperComponent)targets[i];
						SaveParticleSystemPreset(particleHelperComponent); 
					}
                    particleSystemPresets = null;
                    LoadParticleHelpersAndPresets();
                }

                DrawLine(Color.grey, 1, 3);

                GUILayout.Label("Load Particle System Presets", smallBoldLabel);
                bool particleSystemPresetsAvailable = particleSystemPresets != null && particleSystemPresets.Length > 0;
                if(!particleSystemPresetsAvailable)
                {
                    GUILayout.Label("No Particle System Presets found", smallRedBoldLabel);
                    if(Time.realtimeSinceStartup - timeSinceLastFetch > TIME_BETWEEN_FETCHES) LoadParticleHelpersAndPresets();
                }
                else
                {
                    string[] presetNames = new string[particleSystemPresets.Length];
                    for(int i = 0; i < particleSystemPresets.Length; i++) presetNames[i] = particleSystemPresets[i].name;

                    GUI.backgroundColor = Color.cyan;
                    selectedIndex = EditorGUILayout.Popup("Particle System Presets", selectedIndex, presetNames);

                    GUI.backgroundColor = Color.yellow;
					if (GUILayout.Button("Apply Particle System Preset"))
					{
						for (int i = 0; i < targets.Length; i++)
						{
							AllIn1ParticleHelperComponent particleHelperComponent = (AllIn1ParticleHelperComponent)targets[i];
							ApplyParticleSystemPreset(particleHelperComponent, particleSystemPresets[selectedIndex]);
						}
					}
                }
                
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndToggleGroup();
            helperNum++;
            GUI.backgroundColor = iniBackgroundColor;
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

		public void SaveParticleSystemPreset(AllIn1ParticleHelperComponent particleHelperComponent)
		{
			particleHelperComponent.CacheParticleSystem();
			if (ps == null)
			{
				NoParticleSystemLogError(particleHelperComponent);
				return;
			}

			Preset psPreset = new Preset(ps);

			string path = AllIn1VfxWindow.GetParticlePresetsPath();
			EditorUtils.CreateFolderIfNotExist(path);
			path += "/";
			if (!System.IO.Directory.Exists(path))
			{
				EditorUtility.DisplayDialog("The desired Particle Presets Save Path doesn't exist",
					"Go to Tools -> AllIn1 -> VFX Toolkit Window and set a valid folder", "Ok");
				return;
			}

			string fullPath = path + particleHelperComponent.gameObject.name + "PsPreset" + ".preset";
			if (System.IO.File.Exists(fullPath)) fullPath = GetNewValidPath(path + particleHelperComponent.gameObject.name + "PsPreset", ".preset");

			string fileName = fullPath.Replace(path, "");
			fileName = fileName.Replace(".preset", "");
			fullPath = EditorUtility.SaveFilePanel("Save Particle Helper Preset", path, fileName, "preset");
			if (fullPath.Length == 0) return;
			fullPath = "Assets" + fullPath.Replace(Application.dataPath, "");
			AssetDatabase.CreateAsset(psPreset, fullPath);
			AssetDatabase.SaveAssets();

			EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath(fullPath, typeof(Preset)));
		}

		private void NoParticleSystemLogError(AllIn1ParticleHelperComponent particleHelperComponent)
		{
			string logErrorString = "This GameObject: " + particleHelperComponent.gameObject.name + " doesn't have a Particle System and it should, please double check what you are doing";
			Debug.LogError(logErrorString);
#if UNITY_EDITOR
			AllIn1VfxWindow.ShowSceneViewNotification(logErrorString);
#endif
		}

		private string GetNewValidPath(string path, string extension, int i = 1)
		{
			int number = i;
			string newPath = path + "_" + number.ToString();
			string fullPath = newPath + extension;
			if (System.IO.File.Exists(fullPath))
			{
				number++;
				fullPath = GetNewValidPath(path, extension, number);
			}

			return fullPath;
		}

		public void CustomDataAutoSetup(AllIn1ParticleHelperComponent particleHelperComponent)
		{
			particleHelperComponent.CacheParticleSystem();
			if (ps == null)
			{
				NoParticleSystemLogError(particleHelperComponent);
				return;
			}

			ParticleSystem.CustomDataModule customData = ps.customData;
			customData.enabled = true;

			customData.SetMode(ParticleSystemCustomData.Custom1, ParticleSystemCustomDataMode.Vector);
			customData.SetVectorComponentCount(ParticleSystemCustomData.Custom1, 1);
			customData.SetVector(ParticleSystemCustomData.Custom1, 0, new ParticleSystem.MinMaxCurve(0, 100));

			Renderer targetRenderer = ps.GetComponent<Renderer>();
			Material targetMaterial = targetRenderer.sharedMaterial;
			if (targetMaterial == null)
			{
				string logErrorString = "The Particle System in the object: " + particleHelperComponent.gameObject.name + " has no valid target material";
				Debug.LogError(logErrorString);
#if UNITY_EDITOR
				AllIn1VfxWindow.ShowSceneViewNotification(logErrorString);
#endif
				return;
			}

			bool alphaEnable = targetMaterial.IsKeywordEnabled("ALPHAFADEINPUTSTREAM_ON") && (targetMaterial.IsKeywordEnabled("FADE_ON") || targetMaterial.IsKeywordEnabled("ALPHAFADE_ON"));
			if (alphaEnable)
			{
				customData.SetVectorComponentCount(ParticleSystemCustomData.Custom1, 2);
				AnimationCurve alphaFadeCurve = new AnimationCurve();
				alphaFadeCurve.AddKey(0.0f, 1.0f);
				alphaFadeCurve.AddKey(1.0f, 0.0f);
				customData.SetVector(ParticleSystemCustomData.Custom1, 1, new ParticleSystem.MinMaxCurve(1, alphaFadeCurve));
			}
			else customData.SetVectorComponentCount(ParticleSystemCustomData.Custom1, 1);

			bool texOffsetEnable = targetMaterial.IsKeywordEnabled("OFFSETSTREAM_ON");
			if (texOffsetEnable)
			{
				customData.SetMode(ParticleSystemCustomData.Custom2, ParticleSystemCustomDataMode.Vector);
				customData.SetVectorComponentCount(ParticleSystemCustomData.Custom2, 2);
				AnimationCurve textureOffsetCurve = new AnimationCurve();
				textureOffsetCurve.AddKey(0.0f, 0.0f);
				textureOffsetCurve.AddKey(1.0f, 1.0f);
				customData.SetVector(ParticleSystemCustomData.Custom2, 0, new ParticleSystem.MinMaxCurve(1, textureOffsetCurve));
				customData.SetVector(ParticleSystemCustomData.Custom2, 1, new ParticleSystem.MinMaxCurve(1, textureOffsetCurve));
			}

			bool shapetWeightsEnable = targetMaterial.IsKeywordEnabled("SHAPEWEIGHTS_ON");
			if (shapetWeightsEnable)
			{
				customData.SetMode(ParticleSystemCustomData.Custom2, ParticleSystemCustomDataMode.Vector);
				customData.SetVectorComponentCount(ParticleSystemCustomData.Custom2, 3);
				AnimationCurve weightOffsetCurve = new AnimationCurve();
				weightOffsetCurve.AddKey(0.0f, 0.0f);
				weightOffsetCurve.AddKey(1.0f, 1.0f);
				customData.SetVector(ParticleSystemCustomData.Custom2, 2, new ParticleSystem.MinMaxCurve(1, weightOffsetCurve));
			}

			if (!texOffsetEnable && !shapetWeightsEnable) customData.SetMode(ParticleSystemCustomData.Custom2, ParticleSystemCustomDataMode.Disabled);

			ParticleSystemRenderer psRenderer = ps.GetComponent<ParticleSystemRenderer>();
			List<ParticleSystemVertexStream> rendererStreams = new List<ParticleSystemVertexStream>();
			rendererStreams.Add(ParticleSystemVertexStream.Position);
			rendererStreams.Add(ParticleSystemVertexStream.Normal);
			rendererStreams.Add(ParticleSystemVertexStream.Color);
			rendererStreams.Add(ParticleSystemVertexStream.UV);
			if (alphaEnable || texOffsetEnable || shapetWeightsEnable) rendererStreams.Add(ParticleSystemVertexStream.Custom1XY);
			else rendererStreams.Add(ParticleSystemVertexStream.Custom1X);

			//Unity throws an error warning if we don't add this also when only needing Fade for some reason, so we also send Custom2 data when only needing Fade to avoid the error warning
			if (shapetWeightsEnable) rendererStreams.Add(ParticleSystemVertexStream.Custom2XYZ);
			else if (texOffsetEnable) rendererStreams.Add(ParticleSystemVertexStream.Custom2XY);
			psRenderer.SetActiveVertexStreams(rendererStreams);
		}

		public void ApplyParticleSystemPreset(AllIn1ParticleHelperComponent particleHelperComponent, Preset particleSystemPreset)
		{
			particleHelperComponent.CacheParticleSystem();
			if (ps == null)
			{
				NoParticleSystemLogError(particleHelperComponent);
				return;
			}

			particleSystemPreset.ApplyTo(ps);
			particleHelperComponent.ResetParticleSystem();

			SetSceneDirty();
		}

		private void SetSceneDirty()
		{
			if (!Application.isPlaying) 
			{ 
				EditorSceneManager.MarkAllScenesDirty(); 
			}

			//If you get an error here please delete the code block below
			var prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();

			if (prefabStage != null) 
			{ 
				EditorSceneManager.MarkSceneDirty(prefabStage.scene); 
			}
			//Until here
		}

		public void ApplyCurrentSettings(AllIn1ParticleHelperComponent particleHelperComponent, bool applyAllModules = false)
		{
			particleHelperComponent.CacheParticleSystem();
			if (ps == null)
			{
				NoParticleSystemLogError(particleHelperComponent);
				return;
			}

			particleHelperComponent.ApplyCurrentSettings(applyAllModules);
			
			SetSceneDirty();
		}

		public void HueShiftParticleSystem(AllIn1ParticleHelperComponent particleHelperComponent, Color tColor, int gradientColorKeyIndex = 0)
		{
			particleHelperComponent.CacheParticleSystem();
			if (ps == null)
			{
				NoParticleSystemLogError(particleHelperComponent);
				return;
			}

			particleHelperComponent.HueShiftParticleSystem(tColor, gradientColorKeyIndex);


			SetSceneDirty();
		}
	}
}
#endif