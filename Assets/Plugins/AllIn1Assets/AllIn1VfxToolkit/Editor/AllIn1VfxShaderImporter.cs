using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AllIn1Assets.AllIn1VfxToolkit
{
	[InitializeOnLoad]
	public static class AllIn1VfxShaderImporter
	{
		public enum UnityVersion
		{
			NONE = 0,
			UNITY_2022 = 4,
			UNITY_6 = 5,
		}

		public enum RenderPipeline
		{
			NONE = -1,
			BIRP = 0,
			URP = 1,
			HDRP = 2,
		}

		private const string LIT_SHADER_PIPELINE_KEY = "AllIn1VFX_LitShader_RenderPipeline";
		private const string LIT_SHADER_UNITY_VERSION_KEY = "AllIn1VFX_LitShader_UnityVersion";
		private const string LIT_SHADER_FIRST_TIME_PROJECT = "AllIn1VFX_LitShader_FirstTimeProject";

		private const string LIT_SHADER_PATH		= "../Shaders/AllIn1VfxLit.shader";
		private const string LIT_SHADER_FILE_NAME	= "AllIn1VfxLit.shader";

		private const string SHADER_PATH_FORMAT		= "../Shaders/LitShaders/AllIn1VfxLit_BetterShader_{0}.txt";
		private const string TEMPLATE_FILE_PREFIX	= "AllIn1VfxLit_BetterShader_";

		//True while the importer imports the shader it just wrote, so the postprocessor ignores that import
		public static bool IsWritingLitShader { get; private set; }

		private const string TEMPLATE_STANDARD		= "Standard";
		private const string TEMPLATE_NEWEST_VERSION = "2023";

		static AllIn1VfxShaderImporter()
		{
			ConfigureShader();
		}

		private static void ConfigureShader()
		{
			if (UnityEditor.MPE.ProcessService.level == UnityEditor.MPE.ProcessLevel.Secondary)
				return;

			RenderPipelineChecker.RefreshData();

			UnityVersion unityVersion = GetUnityVersion();
			RenderPipeline renderPipeline = GetRenderPipeline();

			string shaderPath = GetTemplatePath(renderPipeline, unityVersion);

			RenderPipeline lastRenderPipeline = (RenderPipeline)SessionState.GetInt(LIT_SHADER_PIPELINE_KEY, -1);
			UnityVersion lastUnityVersion = (UnityVersion)SessionState.GetInt(LIT_SHADER_UNITY_VERSION_KEY, 0);
			int firstTimeProject = SessionState.GetInt(LIT_SHADER_FIRST_TIME_PROJECT, -1);

			if (lastRenderPipeline != renderPipeline || lastUnityVersion != unityVersion || firstTimeProject != 1)
			{
				SessionState.SetInt(LIT_SHADER_PIPELINE_KEY, (int)renderPipeline);
				SessionState.SetInt(LIT_SHADER_UNITY_VERSION_KEY, (int)unityVersion);
				SessionState.SetInt(LIT_SHADER_FIRST_TIME_PROJECT, 1);

				try
				{
					string[] results = AssetDatabase.FindAssets($"t:Script {nameof(AllIn1VfxShaderImporter)}");
					if (results.Length == 0)
					{
						Debug.LogError($"Could not locate {nameof(AllIn1VfxShaderImporter)} in the project, the lit shader was not configured");
						return;
					}

					string currentFolder = Path.GetDirectoryName(AssetDatabase.GUIDToAssetPath(results[0]));

					string sourceFilePath = Path.GetFullPath(Path.Combine(currentFolder, shaderPath));
					string targetFilePath = Path.GetFullPath(Path.Combine(currentFolder, LIT_SHADER_PATH));
					string targetAssetPath = ToAssetPath(targetFilePath);

					string newShaderStr = File.ReadAllText(sourceFilePath);
					newShaderStr = newShaderStr.Replace("Shader \"AllIn1Vfx/AllIn1VfxLit_BetterShader\"", "Shader \"AllIn1Vfx/AllIn1VfxLit\"");
					//Unity's shader upgrader makes this rewrite on import, so without it the shader never equals its template again
					newShaderStr = newShaderStr.Replace("defined FOG_COMBINED_WITH_WORLD_POS", "defined (FOG_COMBINED_WITH_WORLD_POS)");
					newShaderStr = EditorUtils.UnifyEOL(newShaderStr);

					if (File.Exists(targetFilePath))
					{
						//Normalize the existing file too, its line endings depend on how it was checked out (git autocrlf and similar)
						string existingShaderStr = EditorUtils.UnifyEOL(File.ReadAllText(targetFilePath));
						if (string.Equals(existingShaderStr, newShaderStr, StringComparison.Ordinal))
							return;
					}

					File.WriteAllText(targetFilePath, newShaderStr);

					IsWritingLitShader = true;
					try
					{
						AssetDatabase.ImportAsset(targetAssetPath, ImportAssetOptions.ForceUpdate);
					}
					finally
					{
						IsWritingLitShader = false;
					}
				}
				catch (Exception e)
				{
					Debug.LogError("Shader not found: " + e);
				}
			}
		}

		private static string GetTemplatePath(RenderPipeline renderPipeline, UnityVersion unityVersion)
		{
			string pipelineName = GetPipelineTemplateName(renderPipeline);
			if (string.IsNullOrEmpty(pipelineName))
				return string.Format(SHADER_PATH_FORMAT, TEMPLATE_STANDARD);

			return string.Format(SHADER_PATH_FORMAT, pipelineName + GetUnityVersionTemplateName(unityVersion));
		}

		private static string GetPipelineTemplateName(RenderPipeline renderPipeline)
		{
			switch (renderPipeline)
			{
				case RenderPipeline.URP:
					return "URP";
				case RenderPipeline.HDRP:
					return "HDRP";
				default:
					return string.Empty;
			}
		}

		//Unrecognised versions fall back to the newest template instead of to no template at all
		private static string GetUnityVersionTemplateName(UnityVersion unityVersion)
		{
			switch (unityVersion)
			{
				case UnityVersion.UNITY_2022:
					return "2022";
				default:
					return TEMPLATE_NEWEST_VERSION;
			}
		}

		private static string ToAssetPath(string absolutePath)
		{
			string fullPath = absolutePath.Replace('\\', '/');
			string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..")).Replace('\\', '/');

			if (!projectRoot.EndsWith("/", StringComparison.Ordinal))
				projectRoot += "/";

			return fullPath.StartsWith(projectRoot, StringComparison.Ordinal)
				? fullPath.Substring(projectRoot.Length)
				: fullPath;
		}

		private static UnityVersion GetUnityVersion()
		{
			UnityVersion res = UnityVersion.NONE;

			string unityVersion = Application.unityVersion;

			if (unityVersion.Contains("2022"))
			{
				res = UnityVersion.UNITY_2022;
			}
			else if (unityVersion.Contains("6000"))
			{
				res = UnityVersion.UNITY_6;
			}

			return res;
		}

		private static RenderPipeline GetRenderPipeline()
		{
			RenderPipeline res = RenderPipeline.BIRP;

			if (RenderPipelineChecker.IsURP)
			{
				res = RenderPipeline.URP;
			}
			else if (RenderPipelineChecker.IsHDRP)
			{
				res = RenderPipeline.HDRP;
			}

			return res;
		}

		public static void ForceReimport()
		{
			InvalidateSession();
			ConfigureShader();
		}

		public static void InvalidateSession()
		{
			SessionState.EraseInt(LIT_SHADER_PIPELINE_KEY);
			SessionState.EraseInt(LIT_SHADER_UNITY_VERSION_KEY);
			SessionState.EraseInt(LIT_SHADER_FIRST_TIME_PROJECT);
		}

		//A package update writes the shipped Built-in shader over the generated one and may change the templates
		public static bool IsLitShaderFile(string assetPath)
		{
			string fileName = Path.GetFileName(assetPath);
			if (fileName.StartsWith(TEMPLATE_FILE_PREFIX, StringComparison.Ordinal) && fileName.EndsWith(".txt", StringComparison.Ordinal))
				return true;

			if (!string.Equals(fileName, LIT_SHADER_FILE_NAME, StringComparison.Ordinal))
				return false;

			//On Built-in the shipped shader is already the right one
			RenderPipelineChecker.RefreshData();
			return GetRenderPipeline() != RenderPipeline.BIRP;
		}
	}
}