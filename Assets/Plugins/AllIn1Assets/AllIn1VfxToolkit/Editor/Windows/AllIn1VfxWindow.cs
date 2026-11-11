using AllIn1Assets.SharedExtensions;
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace AllIn1Assets.AllIn1VfxToolkit
{
    public class AllIn1VfxWindow : AllIn1CommonWindow
	{
        
        [MenuItem("Tools/AllIn1/VFX Toolkit Window", priority = 6)]
        public static void ShowAllIn1VfxToolkitWindowWindow() => GetWindow<AllIn1VfxWindow>("VFX Toolkit Window");

        public static readonly string CUSTOM_EDITOR_HEADER = "AllIn1VfxToolkit_CustomEditorHeader";
        private static string basePath = "Assets/Plugins/AllIn1Assets/AllIn1VfxToolkit";
		public static readonly string materialAutoSetupSavesPath = "/Demo & Assets/Demo/Materials";
        
        private DefaultAsset materialTargetFolder = null;
		private const int BUTTON_WIDTH = 600;



		private int currTab = 0;

		//tabDrawers cannot survive a domain reload, so a restored tabsNames would disagree with it
		[System.NonSerialized] private string[] tabsNames;

		protected Plugin3DConfig plugin3DConfig;
		private AssetWindowTabDrawer CurrentTabDrawer => tabDrawers[currTab];
		protected SavePathsVfxTabDrawer savePathsVfxTabDrawer;
		protected TextureEditorTabDrawer textureEditorTabDrawer;
		protected TextureCreatorTabDrawer textureCreatorTabDrawer;
		protected OtherVfxTabDrawer otherVfxTabDrawer;
		protected AssetWindowTabDrawer[] tabDrawers;




		protected override bool Init(AbstractPluginConfig pluginConfig, bool drawHeader = true)
		{
			if (!base.Init(pluginConfig, drawHeader)) return false;

			this.plugin3DConfig = (Plugin3DConfig)pluginConfig;

			GetBasePath();

			CreateTabs();
			ConfigureTabs();

			tabsNames = new string[tabDrawers.Length];
			for (int i = 0; i < tabsNames.Length; i++)
			{
				tabsNames[i] = tabDrawers[i].GetTabName();
			}

			currTab = Mathf.Clamp(currTab, 0, tabDrawers.Length - 1);

			return true;
		}

		private void CreateTabs()
		{
			savePathsVfxTabDrawer = new SavePathsVfxTabDrawer(commonStyles, this, plugin3DConfig);
			textureEditorTabDrawer = new TextureEditorTabDrawer(commonStyles, this, plugin3DConfig);
			textureCreatorTabDrawer = new TextureCreatorTabDrawer(commonStyles, this, plugin3DConfig);
			otherVfxTabDrawer = new OtherVfxTabDrawer(commonStyles, this, plugin3DConfig);
		}

		private void ConfigureTabs()
		{
			tabDrawers = new AssetWindowTabDrawer[]
			{
				savePathsVfxTabDrawer,
				textureEditorTabDrawer,
				textureCreatorTabDrawer,
				otherVfxTabDrawer
			};
		}

		protected override void DrawEditor()
		{
			base.DrawEditor();

			int newTab = GUILayout.Toolbar(currTab, tabsNames);
			AssetWindowTabDrawer newTabDrawer = tabDrawers[newTab];

			if (newTabDrawer != CurrentTabDrawer)
			{
				CurrentTabDrawer.Hide();
				newTabDrawer.Show();

				this.currTab = newTab;
			}

			EditorUtils.DrawThinLine();
			CurrentTabDrawer.Draw();

		}

        
        public static Texture2D GetInspectorImage()
        {
            string[] guids = AssetDatabase.FindAssets($"{CUSTOM_EDITOR_HEADER} t:texture");
            if(guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
            return null;
        }


        private void HandleSaveFolderEditorPref(string keyName, string defaultPath, string logsFeatureName)
        {
            if (!PlayerPrefs.HasKey(keyName)) PlayerPrefs.SetString(keyName, defaultPath);
            materialTargetFolder = (DefaultAsset)AssetDatabase.LoadAssetAtPath(PlayerPrefs.GetString(keyName), typeof(DefaultAsset));
            if (materialTargetFolder == null)
            {
                PlayerPrefs.SetString(keyName, defaultPath);
                materialTargetFolder = (DefaultAsset)AssetDatabase.LoadAssetAtPath(PlayerPrefs.GetString(keyName), typeof(DefaultAsset));
                if (materialTargetFolder == null)
                {
                    materialTargetFolder = (DefaultAsset)AssetDatabase.LoadAssetAtPath("Assets/", typeof(DefaultAsset));
                    if(materialTargetFolder == null)
                    {
                        EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(600));
                        EditorGUILayout.HelpBox("Folder is invalid, please select a valid one", MessageType.Error, true);
                        EditorGUILayout.EndHorizontal();
                    }
                    else PlayerPrefs.SetString("Assets/", defaultPath);
                }
            }
            materialTargetFolder = (DefaultAsset)EditorGUILayout.ObjectField("New " + logsFeatureName + " Folder", 
                materialTargetFolder, typeof(DefaultAsset), false, GUILayout.MaxWidth(500));

            if (materialTargetFolder != null && IsAssetAFolder(materialTargetFolder))
            {
                string path = AssetDatabase.GetAssetPath(materialTargetFolder);
                PlayerPrefs.SetString(keyName, path);
                EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(600));
                EditorGUILayout.HelpBox("Valid folder! " + logsFeatureName + " save path: " + path, MessageType.Info);
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(600));
                EditorGUILayout.HelpBox("Select the new " + logsFeatureName + " Folder", MessageType.Warning, true);
                EditorGUILayout.EndHorizontal();
            }
        }


        internal void OtherTab()
        {
            AutoSetupMaterialsToCurrentPipelineArea();
            GUILayout.Space(10);
            DrawLine(Color.grey, 1, 3);
            GUILayout.Space(10);
            DisableDepthAndSceneColorEffectsArea();
            GUILayout.Space(10);
            DrawLine(Color.grey, 1, 3);
            GUILayout.Space(10);
            SceneNotificationsToggle();
			DrawLine(Color.grey, 1, 3);
			GUILayout.Space(10);
			RefreshLitShader();
		}

        private void AutoSetupMaterialsToCurrentPipelineArea()
        {
            GUILayout.Label("AllIn1Vfx Materials Shader Auto Setup", commonStyles.bigLabel);
            GUILayout.Space(20);
            GUILayout.Label("Select the folder where the target AllIn1Vfx materials are contained", EditorStyles.boldLabel);
            HandleSaveFolderEditorPref("All1VfxAutoSetup", basePath + materialAutoSetupSavesPath, "Auto Setup");

            GUILayout.Space(20);
            if(GUILayout.Button("Auto Setup Shaders for Materials in selected folder", GUILayout.MaxWidth(BUTTON_WIDTH)))
            {
                string autoSetupPath = PlayerPrefs.GetString("All1VfxAutoSetup");
                SceneViewNotificationAndLog("Starting Material Auto Setup at: " + autoSetupPath);
                string[] filePaths = System.IO.Directory.GetFiles(autoSetupPath);

                bool isSuccess = true;
                Material lastTargetMat = null;
                if(filePaths != null && filePaths.Length > 0)
                {
                    for(int i = 0; i < filePaths.Length; i++)
                    {
                        Object obj = UnityEditor.AssetDatabase.LoadAssetAtPath(filePaths[i], typeof(Material));
                        if(obj is Material mat)
                        {
                            lastTargetMat = mat;
                            string shaderName = mat.shader.name;
                            if(shaderName.Contains("AllIn1Vfx/"))
                            {
                                shaderName = shaderName.Replace("AllIn1Vfx/", "");
                                if(shaderName.Contains("AllIn1Vfx")) //Means it is a variation of the asset main shader
                                {
                                    isSuccess &= AllIn1VfxShaderUtils.SetShaderBasedOnEffectsAndPipeline(mat);
                                }
                            }
                            else if(shaderName.Contains("Hidden/InternalError")) //If the material is broken we'll override it by a AllIn1Vfx one
                            {
                                isSuccess &= AllIn1VfxShaderUtils.SetShaderBasedOnEffectsAndPipeline(mat);
                            }
                        }
                    }
                    
                    if(!isSuccess){
                        string targetShader = AllIn1VfxShaderUtils.GetTargetShaderName(lastTargetMat);
                        EditorUtility.DisplayDialog("Missing Shader", 
                        $"Shader {targetShader} not found. Import the appropriate Pipeline package as explained in the Documentation first section", "Ok");
                    }
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                if(isSuccess) SceneViewNotificationAndLog("Material Auto Setup finished");
            }
        }
        
        private void DisableDepthAndSceneColorEffectsArea()
        {
            GUILayout.Label("Disable Depth And Scene Color effects from materials", commonStyles.bigLabel);
            GUILayout.Label("These effects are: Soft Particles, Intersection Glow, Screen Distortion");
            GUILayout.Space(20);
            GUILayout.Label("Select the folder where the target AllIn1Vfx materials are contained", EditorStyles.boldLabel);
            HandleSaveFolderEditorPref("DepthAndSceneColorPath", basePath + materialAutoSetupSavesPath, "Effects Disable");

            GUILayout.Space(20);
            if(GUILayout.Button("Disable Depth and Scene Color Effects for Materials in selected folder", GUILayout.MaxWidth(BUTTON_WIDTH)))
            {
                string autoSetupPath = PlayerPrefs.GetString("DepthAndSceneColorPath");
                SceneViewNotificationAndLog("Starting to Disable Depth and Scene Color Effects at: " + autoSetupPath);
                string[] filePaths = System.IO.Directory.GetFiles(autoSetupPath);

                if(filePaths != null && filePaths.Length > 0)
                {
                    for(int i = 0; i < filePaths.Length; i++)
                    {
                        Object obj = UnityEditor.AssetDatabase.LoadAssetAtPath(filePaths[i], typeof(Material));
                        if(obj is Material mat)
                        {
                            string shaderName = mat.shader.name;
                            if(shaderName.Contains("AllIn1Vfx/"))
                            {
                                shaderName = shaderName.Replace("AllIn1Vfx/", "");
                                if(shaderName.Contains("Vfx")) //Means it is a variation of the asset main shader
                                {
                                    if(mat.IsKeywordEnabled("SOFTPART_ON")) mat.DisableKeyword("SOFTPART_ON");
                                    if(mat.IsKeywordEnabled("DEPTHGLOW_ON")) mat.DisableKeyword("DEPTHGLOW_ON");
                                    if(mat.IsKeywordEnabled("SCREENDISTORTION_ON")) mat.DisableKeyword("SCREENDISTORTION_ON");
                                }
                            }
                        }
                    }
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                SceneViewNotificationAndLog("Disable Depth and Scene Color Effects finished");
            }
        }
        
        private static void SceneNotificationsToggle()
        {
            float previousLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 200f;
            bool areNotificationsEnabled = EditorPrefs.GetInt("DisplaySceneViewNotifications", 1) == 1;
            areNotificationsEnabled = EditorGUILayout.Toggle("Display Scene View Notifications", areNotificationsEnabled);
            EditorPrefs.SetInt("DisplaySceneViewNotifications", areNotificationsEnabled ? 1 : 0);
            EditorGUIUtility.labelWidth = previousLabelWidth;
        }

		private static void RefreshLitShader()
        {
            GUILayout.Label("Force the Lit Shader to be reconfigured");
            GUILayout.Label("If you are getting some error or have changed the render pipeline press the button below");
			if(GUILayout.Button("Refresh Lit Shader", GUILayout.MaxWidth(BUTTON_WIDTH)))
			{
				AllIn1VfxShaderImporter.ForceReimport();
			}
		}

        private static bool IsAssetAFolder(Object obj)
        {
            string path = "";

            if(obj == null) return false;

#if UNITY_6000_3_OR_NEWER
			path = AssetDatabase.GetAssetPath(obj.GetEntityId());
#else
			path = AssetDatabase.GetAssetPath(obj.GetInstanceID());
#endif

			if (path.Length > 0)
            {
                if(Directory.Exists(path)) return true;
                else return false;
            }

            return false;
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




		protected override AbstractPluginConfig FindPluginConfig()
		{
			return PluginRegistry.GetPluginByID<AllIn1VfxToolkitConfig>(PluginID.ALLIN1_VFX);
		}


        private static void GetBasePath()
        {
            string[] guids = AssetDatabase.FindAssets("t:folder AllIn1VfxToolkit");
            if(guids.Length > 0)
            {
                basePath = AssetDatabase.GUIDToAssetPath(guids[0]);
            }
            else
            {
                Debug.LogError("AllIn1VfxToolkit folder not found in the project.");
                basePath = "Assets/Plugins/AllIn1Assets/AllIn1VfxToolkit";
            }
        }
        
        //These read the same config asset the Save Paths tab writes to
        public static string GetMaterialSavePath()
        {
            return GetVfxConfig().MaterialSavePath;
        }

        public static string GetRenderImageSavePath()
        {
            return GetVfxConfig().RenderImageSavePath;
        }

        public static string GetParticlePresetsPath()
        {
            return GetVfxConfig().ParticlePresetsSavePath;
        }

        private static AllIn1VfxToolkitConfig GetVfxConfig()
        {
            return PluginRegistry.GetPluginByID<AllIn1VfxToolkitConfig>(PluginID.ALLIN1_VFX);
        }
        
        public static void SceneViewNotificationAndLog(string message)
        {
            Debug.Log(message);
            ShowSceneViewNotification(message);
        }

        public static Shader FindShader(string shaderName)
        {
            return AllIn1VfxShaderUtils.FindShader(shaderName);
        }

        public static void ShowSceneViewNotification(string message)
        {
            bool showNotification = EditorPrefs.GetInt("DisplaySceneViewNotifications", 1) == 1;
            if(!showNotification) return;
            
            GUIContent content = new GUIContent(message);
            SceneView.lastActiveSceneView.ShowNotification(content, 1.5f);
        }
    }
}