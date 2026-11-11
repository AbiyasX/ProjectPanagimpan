using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace AllIn1Assets.AllIn1VfxToolkit
{
    public static class AllIn1VfxShaderUtils
    {
        public static bool SetShaderBasedOnEffectsAndPipeline(Material targetMat)
        {
            string targetShader = GetTargetShaderName(targetMat);

            if(!targetMat.shader.name.Equals(targetShader))
            {
                int renderingQueue = targetMat.renderQueue;
                float zWriteValue = targetMat.GetFloat("_ZWrite");
                Shader shader = FindShader(targetShader);
                if(shader == null) return false;
                targetMat.shader = shader;
                targetMat.renderQueue = renderingQueue;
                targetMat.SetFloat("_ZWrite", zWriteValue);
                EditorUtility.SetDirty(targetMat);
            }
            return true;
        }

        public static string GetTargetShaderName(Material targetMat)
        {
            string[] oldKeyWords = targetMat.shaderKeywords;
            string targetShader = "AllIn1Vfx";
            string pipeline = "Built-In";
            RenderPipelineAsset renderPipelineAsset = GraphicsSettings.defaultRenderPipeline;
            if(renderPipelineAsset != null)
            {
                switch(renderPipelineAsset.GetType().Name)
                {
                    case"UniversalRenderPipelineAsset":
                        pipeline = "URP";
                        break;
                    case"HDRenderPipelineAsset":
                        pipeline = "HDRP";
                        break;
                }
            }

            if(pipeline.Equals("Built-In"))
            {
                if(oldKeyWords.Contains("SCREENDISTORTION_ON")) targetShader = "AllIn1VfxGrabPass";
                else if(oldKeyWords.Contains("FOG_ON") || oldKeyWords.Contains("SHAPE1SCREENUV_ON") || oldKeyWords.Contains("SHAPE2SCREENUV_ON") ||
                        oldKeyWords.Contains("SHAPE3SCREENUV_ON") || oldKeyWords.Contains("SOFTPART_ON") || oldKeyWords.Contains("DEPTHGLOW_ON")) targetShader = "AllIn1VfxBuiltIn";
            }
            else if(pipeline.Equals("URP"))
            {
				if(targetMat.shader.name == "AllIn1Vfx/AllIn1VfxSRPBatch")
				{
					targetShader = "AllIn1VfxSRPBatch";
				}
				else if(targetMat.shader.name == "AllIn1Vfx/AllIn1VfxDOTS")
				{
					targetShader = "AllIn1VfxDOTS";
				}
				else
				{
					targetShader = "AllIn1VfxURP";
				}
            }
            else if(pipeline.Equals("HDRP"))
            {
				if (targetMat.shader.name == "AllIn1Vfx/AllIn1VfxSRPBatch")
				{
					targetShader = "AllIn1VfxSRPBatch";
				}
				else if(targetMat.shader.name == "AllIn1Vfx/AllIn1VfxDOTS")
				{
					targetShader = "AllIn1VfxDOTS";
				}
				else
				{
					targetShader = "AllIn1VfxHDRP";
				}
            }

            return targetShader;
        }

        public static Shader FindShader(string shaderName)
        {
            string[] guids = AssetDatabase.FindAssets($"{shaderName} t:shader");
            foreach(string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
                if(shader != null)
                {
                    string fullShaderName = shader.name;
                    string actualShaderName = fullShaderName.Substring(fullShaderName.LastIndexOf('/') + 1);
                    if(actualShaderName == shaderName) return shader;
                }
            }
            return null;
        }
    }
}
