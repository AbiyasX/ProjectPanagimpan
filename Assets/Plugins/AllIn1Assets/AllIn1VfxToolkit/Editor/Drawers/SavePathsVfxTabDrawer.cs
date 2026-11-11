using UnityEditor;
using UnityEngine;

namespace AllIn1Assets.AllIn1VfxToolkit
{
	public class SavePathsVfxTabDrawer : AssetWindowTabDrawer
	{
		private const string TAB_NAME = "Save Paths";

		private AllIn1VfxToolkitConfig pluginConfig;

		public SavePathsVfxTabDrawer(CommonStyles commonStyles, AllIn1CommonWindow parentWindow, AbstractPluginConfig abstractPluginConfig) : base(commonStyles, parentWindow, abstractPluginConfig)
		{
			pluginConfig = (AllIn1VfxToolkitConfig)abstractPluginConfig;
		}

		public override void Draw()
		{
			GUILayout.Label("Material Save Path", commonStyles.bigLabel);
			GUILayout.Space(20);
			GUILayout.Label("Select the folder where new Materials will be saved when the Save Material To Folder button of the asset component is pressed", EditorStyles.boldLabel);
			pluginConfig.MaterialSavePath = EditorUtils.DrawSelectorFolder(pluginConfig.MaterialSavePath, "New Material Folder");
			//HandleSaveFolderEditorPref("All1VfxMaterials", basePath + materialsSavesPath, "Material");

			EditorUtils.DrawThinLine();
			GUILayout.Label("Particle Presets Save Path", commonStyles.bigLabel);
			GUILayout.Space(20);
			GUILayout.Label("Select the folder where new Particle Helper Preset or Particle System Preset is saved with the Particle Helper Component", EditorStyles.boldLabel);
			pluginConfig.ParticlePresetsSavePath = EditorUtils.DrawSelectorFolder(pluginConfig.ParticlePresetsSavePath, "New Particle Presets Folder");
			//HandleSaveFolderEditorPref("All1VfxParticlePresets", basePath + particlePresetsSavesPath, "Presets");

			EditorUtils.DrawThinLine();
			GUILayout.Label("Render Material to Image Save Path", commonStyles.bigLabel);
			GUILayout.Space(20);
			EditorGUILayout.BeginHorizontal();
			{
				float scaleSlider = 1;
				if (PlayerPrefs.HasKey("All1VfxRenderImagesScale")) scaleSlider = PlayerPrefs.GetFloat("All1VfxRenderImagesScale");
				GUILayout.Label("Rendered Image Texture Scale", GUILayout.MaxWidth(190));
				scaleSlider = EditorGUILayout.Slider(scaleSlider, 0.2f, 5f, GUILayout.MaxWidth(200));
				if (GUILayout.Button("Default Value", GUILayout.MaxWidth(100))) PlayerPrefs.SetFloat("All1VfxRenderImagesScale", 1f);
				else PlayerPrefs.SetFloat("All1VfxRenderImagesScale", scaleSlider);
			}
			EditorGUILayout.EndHorizontal();
			GUILayout.Label("Select the folder where new Images will be saved when the Render Material To Image button of the asset component is pressed", EditorStyles.boldLabel);
			pluginConfig.RenderImageSavePath = EditorUtils.DrawSelectorFolder(pluginConfig.RenderImageSavePath, "New Render Image Folder");
			//HandleSaveFolderEditorPref("All1VfxRenderImages", basePath + renderImagesSavesPath, "Images");
		}

		public override void EnteredPlayMode()
		{
		
		}

		public override string GetTabName()
		{
			return TAB_NAME;
		}

		public override void Hide()
		{
		
		}

		public override void OnDisable()
		{
		
		}

		public override void OnEnable()
		{
		
		}

		public override void Show()
		{
		
		}
	}
}