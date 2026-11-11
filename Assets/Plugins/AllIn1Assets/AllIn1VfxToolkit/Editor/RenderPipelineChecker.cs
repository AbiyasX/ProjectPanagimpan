//////////////////////////////////////////////////////
// Shader Packager
// Copyright (c) Jason Booth
//////////////////////////////////////////////////////
using System.Linq;
using UnityEditor;
using UnityEngine.Rendering;

// Installs custom defines for render pipelines (e.g., USING_HDRP) to enable conditional compilation
// across different pipelines. Unity doesn't provide these defines out of the box, so this helper
// simplifies cross-pipeline compatibility.

namespace AllIn1Assets.AllIn1VfxToolkit
{
	public static class RenderPipelineChecker
	{
		private const string HDRP_PACKAGE = "HDRenderPipelineAsset";
		private const string URP_PACKAGE = "UniversalRenderPipelineAsset";

		public static bool IsHDRP
		{
			get; private set;
		}

		public static bool IsURP
		{
			get; private set;
		}

		public static bool IsStandardRP
		{
			get; private set;
		}

		public static void RefreshData()
		{
			IsHDRP = DoesTypeExist(HDRP_PACKAGE);
			IsURP = DoesTypeExist(URP_PACKAGE);
			IsStandardRP = !(IsHDRP || IsURP);
		}

		public static bool DoesTypeExist(string className)
		{
			return TypeCache.GetTypesDerivedFrom<RenderPipelineAsset>()
				.Any(type => type.Name == className);
		}
	}
}
