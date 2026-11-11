#if ALLIN1_URP
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#endif

using AllIn1Assets.SharedExtensions;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AllIn1Assets.AllIn1VfxToolkit
{
	public class AllIn1VfxToolkitConfig : Plugin3DConfig
	{
		private const string PLUGIN_NAME = "AllIn1VfxToolkit";
		private const string VERSION = "2.4";
		private const PluginID PLUGIN_ID = PluginID.ALLIN1_VFX;

		private const string DEFAULT_PARTICLE_PRESETS_SAVE_FOLDER = "Particle Presets";


		private static string[] SHADER_NAMES =
		{
			"AllIn1Vfx", 
			"AllIn1VfxBuiltIn",
			"AllIn1VfxDOTS",
			"AllIn1VfxGrabPass",
			"AllIn1VfxLit",
			"AllIn1VfxSRPBatch"
		};


		//Paths
		public string particlePresetsSavePath = string.Empty;
		public string ParticlePresetsSavePath
		{
			get
			{
				return ResolveSavePath(particlePresetsSavePath, DEFAULT_PARTICLE_PRESETS_SAVE_FOLDER);
			}
			set
			{
				SetSavePath(ref particlePresetsSavePath, value);
			}
		}

		protected override void NormalizeSavePaths()
		{
			base.NormalizeSavePaths();

			if (NormalizeStoredPath(ref particlePresetsSavePath))
			{
				MarkDirtyIfAlive();
			}
		}


		public override PluginID GetPluginID()
		{
			return PLUGIN_ID;
		}

		public override string GetPluginName()
		{
			return PLUGIN_NAME;
		}

		public override Shader GetShaderAfterMaterialRemoval() 
		{
			Shader res = Shader.Find("Sprites/Default");
			return res;
		}

		public override string[] GetShaderNames()
		{
			return SHADER_NAMES;
		}

		public override string[] GetShaderTemplates()
		{
			return null;
		}

		public override string GetVersion()
		{
			return VERSION;
		}

		public override bool IsWritableFolder(string folderPath)
		{
			return false;
		}

		public override bool IsDerivedFromAllIn13DShader()
		{
			return false;
		}

		public override string GetRenderPipelineDocURL()
		{
			return $"{GlobalConstants.URL_DOCS_SITE}/vfx-toolkit/setup-and-render-pipelines";
		}

		protected override void CreateFolders()
		{
			base.CreateFolders();

			EditorUtils.CreateFolderIfNotExist(ParticlePresetsSavePath);
		}

#if ALLIN1_DEVELOP 
		public override string[] GetFoldersToExport()
		{
			string[] res = base.GetFoldersToExport();
			UnityEditor.ArrayUtility.Add(ref res, Shared3DRootFolderPath);

			return res;
		}
#endif


#if ALLIN1_URP
		public override void ConvertMaterialsFolder() 
		{

		}

		public override void ConfigureRenderPipeline(UniversalRendererData universalRendererData, RenderPipelineAsset renderPipelineAsset)
		{

		}
#endif
	}
}