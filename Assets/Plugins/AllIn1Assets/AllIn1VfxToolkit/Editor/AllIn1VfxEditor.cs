using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace AllIn1Assets.AllIn1VfxToolkit
{
    [CustomEditor(typeof(AllIn1VfxComponent)), CanEditMultipleObjects]
    public class AllIn1VfxEditor : UnityEditor.Editor
    {
        private GUIStyle smallBoldLabel;
        private Texture2D imageInspector;

		private void OnEnable()
		{
			EditorApplication.update += OnEditorUpdate;
		}

		private void OnDisable()
		{
			EditorApplication.update -= OnEditorUpdate;
		}

		private void OnEditorUpdate()
		{
			for(int i = 0; i < targets.Length; i++)
			{
				AllIn1VfxComponent vfxComponent = (AllIn1VfxComponent)targets[i];
				if(vfxComponent != null) UpdateChecks(vfxComponent);
			}
		}

		//Auto assigns an AllIn1Vfx material when the component is added, but only over Unity default materials so custom materials are never clobbered
		private void UpdateChecks(AllIn1VfxComponent vfxComponent)
		{
			if(vfxComponent.matAssigned || vfxComponent.destroyed || Application.isPlaying || !vfxComponent.gameObject.activeSelf) return;
			Renderer sr = vfxComponent.GetComponent<Renderer>();
			if(sr != null)
			{
				if(sr.sharedMaterial == null)
				{
					vfxComponent.CleanMaterial();
					MakeNewMaterial(vfxComponent, notifyWhenDone: true);
				}
				if(ShouldAutoReplaceMaterial(sr.sharedMaterial)) MakeNewMaterial(vfxComponent, notifyWhenDone: true);
				else vfxComponent.matAssigned = true;
			}
			else
			{
				Graphic img = vfxComponent.GetComponent<Graphic>();
				if(img != null)
				{
					if(ShouldAutoReplaceMaterial(img.material)) MakeNewMaterial(vfxComponent, notifyWhenDone: true);
					else vfxComponent.matAssigned = true;
				}
			}
		}

		//URP's defaults (Lit, ParticlesUnlit) have no "Default" in their names, but like every built in or package material they sit outside Assets
		private static bool ShouldAutoReplaceMaterial(Material mat)
		{
			bool res = mat.name.Contains("Default") || EditorUtils.IsAssetOutsideAssetsFolder(mat);
			return res;
		}

        public override void OnInspectorGUI()
        {
            if(imageInspector == null) imageInspector = AllIn1VfxWindow.GetInspectorImage();
            if(imageInspector)
            {
                Rect rect = EditorGUILayout.GetControlRect(GUILayout.Height(32));
                GUI.DrawTexture(rect, imageInspector, ScaleMode.ScaleToFit, true);
            }

            if(GUILayout.Button("Deactivate All Effects"))
            {
                for(int i = 0; i < targets.Length; i++) ((AllIn1VfxComponent) targets[i]).ClearAllKeywords();
                AllIn1VfxWindow.ShowSceneViewNotification("AllIn1Vfx: Deactivated All Effects");
            }

            if(GUILayout.Button("New Clean Material"))
            {
                bool successOperation = true;
                for(int i = 0; i < targets.Length; i++)
                {
					AllIn1VfxComponent vfxComponent = (AllIn1VfxComponent)targets[i];
					bool tryCreateNewResult = TryCreateNew(vfxComponent);

					successOperation &= tryCreateNewResult;
                }
                if(successOperation) AllIn1VfxWindow.ShowSceneViewNotification("AllIn1Vfx: Clean Material");
            }

            if(GUILayout.Button("Create New Material With Same Properties (SEE DOC)"))
            {
                bool successOperation = true;
                for(int i = 0; i < targets.Length; i++)
				{
					AllIn1VfxComponent vfxComponent = (AllIn1VfxComponent)targets[i];
					bool makeCopyResult = MakeCopy(vfxComponent);
					successOperation &= makeCopyResult;
                }
                if(successOperation) AllIn1VfxWindow.ShowSceneViewNotification("AllIn1Vfx: Copy Created");
            }

            if(GUILayout.Button("Save Material To Folder (SEE DOC)"))
            {
                bool successOperation = true;
                for(int i = 0; i < targets.Length; i++)
                {
					AllIn1VfxComponent vfxComponent = (AllIn1VfxComponent)targets[i];
					bool saveMaterialResult = SaveMaterial(vfxComponent);
					successOperation &= saveMaterialResult;
                }
                if(successOperation) AllIn1VfxWindow.ShowSceneViewNotification("AllIn1Vfx: Material Saved");
            }

            if(GUILayout.Button("Apply Material To All Children"))
            {
                bool successOperation = true;
                for(int i = 0; i < targets.Length; i++)
                {
					AllIn1VfxComponent vfxComponent = (AllIn1VfxComponent)targets[i];
					bool applyMaterialToHierarchy = ApplyMaterialToHierarchy(vfxComponent);
					successOperation &= applyMaterialToHierarchy;
                }
                if(successOperation) AllIn1VfxWindow.ShowSceneViewNotification("AllIn1Vfx: Material Applied To Children");
            }

            if(GUILayout.Button("Render Material To Image"))
            {
                
                bool successOperation = true;
                for(int i = 0; i < targets.Length; i++)
                {
					AllIn1VfxComponent vfxComponent = (AllIn1VfxComponent)targets[i];
					bool renderToImageResult = RenderToImage(vfxComponent);
					successOperation &= renderToImageResult;
                }
                if(successOperation) AllIn1VfxWindow.ShowSceneViewNotification("AllIn1Vfx: Material Rendered To Image");
            }

            CheckIfShowParticleSystemHelperUI();

            EditorGUILayout.Space();
            DrawLine(Color.grey, 1, 3);

            if(GUILayout.Button("Remove Component"))
            {
                for(int i = targets.Length - 1; i >= 0; i--)
                {
                    DestroyImmediate(targets[i] as AllIn1VfxComponent);
                    (targets[i] as AllIn1VfxComponent)?.SetSceneDirty();
                }
                AllIn1VfxWindow.ShowSceneViewNotification("AllIn1Vfx: Component Removed");
            }

            if(GUILayout.Button("REMOVE COMPONENT AND MATERIAL"))
            {
                for(int i = 0; i < targets.Length; i++) ((AllIn1VfxComponent) targets[i]).CleanMaterial();
                for(int i = targets.Length - 1; i >= 0; i--)
                {
                    DestroyImmediate(targets[i] as AllIn1VfxComponent);
                    (targets[i] as AllIn1VfxComponent)?.SetSceneDirty();
                }
                AllIn1VfxWindow.ShowSceneViewNotification("AllIn1Vfx: Component And Material Removed");
            }
        }

		public bool MakeCopy(AllIn1VfxComponent vfxComponent)
		{
			bool res = true;
			if (vfxComponent.currMaterial == null)
			{
				if (FetchCurrentMaterial(vfxComponent))
				{
					res = false;
				}
			}
			else
			{
				string shaderName = vfxComponent.currMaterial.shader.name;
				if (shaderName.Contains("AllIn1Vfx/"))
				{
					shaderName = shaderName.Replace("AllIn1Vfx/", "");
				}

				res = SetMaterial(vfxComponent, AllIn1VfxComponent.AfterSetAction.CopyMaterial, shaderName);
			}

			return res;
		}

		public bool SaveMaterial(AllIn1VfxComponent vfxComponent)
		{
			string path = AllIn1VfxWindow.GetMaterialSavePath();
			EditorUtils.CreateFolderIfNotExist(path);
			path += "/";
			if (!System.IO.Directory.Exists(path))
			{
				EditorUtility.DisplayDialog("The desired save folder doesn't exist",
					"Go to Tools -> AllIn1 -> VFX Toolkit Window and set a valid folder", "Ok");
				return false;
			}

			path += vfxComponent.gameObject.name;
			string fullPath = path + ".mat";
			if (System.IO.File.Exists(fullPath))
			{
				SaveMaterialWithOtherName(vfxComponent, path);
			}
			else 
			{ 
				DoSaving(vfxComponent, fullPath); 
			}

			SetSceneDirty();
			return true;
		}

		private void SaveMaterialWithOtherName(AllIn1VfxComponent vfxComponent, string path, int i = 1)
		{
			int number = i;
			string newPath = path + "_" + number.ToString();
			string fullPath = newPath + ".mat";
			if (System.IO.File.Exists(fullPath))
			{
				number++;
				SaveMaterialWithOtherName(vfxComponent, path, number);
			}
			else
			{
				DoSaving(vfxComponent, fullPath);
			}
		}

		private void DoSaving(AllIn1VfxComponent vfxComponent, string fileName)
		{
			bool rendererExists = false;
			Renderer sr = vfxComponent.GetComponent<Renderer>();
			Material matToSave = null;
			Material createdMat = null;
			if (sr != null)
			{
				rendererExists = true;
				matToSave = sr.sharedMaterial;
			}
			else
			{
				Graphic img = vfxComponent.GetComponent<Graphic>();
				if (img != null)
				{
					rendererExists = true;
					matToSave = img.material;
				}
			}

			if (!rendererExists)
			{
				MissingRenderer(vfxComponent);
				return;
			}
			else
			{
				createdMat = new Material(matToSave);
				vfxComponent.currMaterial = createdMat;
				AssetDatabase.CreateAsset(createdMat, fileName);
				AllIn1VfxWindow.SceneViewNotificationAndLog(fileName + " has been saved!");
				EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath(fileName, typeof(Material)));
			}

			if (sr != null)
			{
				sr.material = createdMat;
			}
			else
			{
				Graphic img = vfxComponent.GetComponent<Graphic>();
				img.material = createdMat;
			}
		}

		private bool FetchCurrentMaterial(AllIn1VfxComponent vfxComponent)
		{
			bool rendererExists = false;
			bool res = false;

			Renderer sr = vfxComponent.GetComponent<Renderer>();
			if (sr != null)
			{
				rendererExists = true;
				vfxComponent.currMaterial = sr.sharedMaterial;
			}
			else
			{
				Graphic img = vfxComponent.GetComponent<Graphic>();
				if (img != null)
				{
					rendererExists = true;
					vfxComponent.currMaterial = img.material;
				}
			}

			if (!rendererExists)
			{
				MissingRenderer(vfxComponent);
			}

			res = !rendererExists;

			return res;
		}

		private void CheckIfShowParticleSystemHelperUI()
        {
            if(Selection.activeGameObject?.GetComponent<ParticleSystem>() == null) return;
            AllIn1ParticleHelperComponent all1VfxPsHelper = Selection.activeGameObject.GetComponent<AllIn1ParticleHelperComponent>();
            if(all1VfxPsHelper != null) return;
            DrawLine(Color.grey, 1, 3);
            EditorGUILayout.Space();
            if(GUILayout.Button("Add Particle System Helper"))
            {
                for(int i = 0; i < targets.Length; i++) ((AllIn1VfxComponent) targets[i]).AddHelperAndPlaceUnderAll1VfxMainComponent();
                AllIn1VfxWindow.ShowSceneViewNotification("AllIn1Vfx: Particle System Helper Added");
            }
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

		private bool SetMaterial(AllIn1VfxComponent vfxComponent, AllIn1VfxComponent.AfterSetAction action, string shaderName)
		{
			bool validSharedMaterial = true;
			bool res = false;

			Shader allIn1VfxShader = AllIn1VfxWindow.FindShader(shaderName);

			if (!Application.isPlaying && Application.isEditor && allIn1VfxShader != null)
			{
				bool rendererExists = false;
				Renderer sr = vfxComponent.GetComponent<Renderer>();
				if (sr != null)
				{
					rendererExists = true;
					Renderer currentRenderer = vfxComponent.GetComponent<Renderer>();
					if (currentRenderer.sharedMaterial == null)
					{
						AllIn1VfxWindow.SceneViewNotificationAndLog("Error: No Material Found");
						validSharedMaterial = false;
					}
					else
					{
						vfxComponent.SetMaterialRenderer(currentRenderer, action, allIn1VfxShader);
						DoAfterSetAction(vfxComponent, action);
					}
				}
				else
				{
					Graphic img = vfxComponent.GetComponent<Graphic>();
					if (img != null)
					{
						rendererExists = true;
						vfxComponent.SetMaterialGraphic(img, action, allIn1VfxShader);
						DoAfterSetAction(vfxComponent, action);
					}
				}

				if (!rendererExists)
				{
					MissingRenderer(vfxComponent);
				}
				else
				{
					SetSceneDirty();
				}

				res = rendererExists && validSharedMaterial;
			}
			else if (allIn1VfxShader == null)
			{
				string logErrorMessage = "You are missing shader variants. Maybe reinstall the asset? You looked for " + shaderName;
				Debug.LogError(logErrorMessage);
				AllIn1VfxWindow.ShowSceneViewNotification(logErrorMessage);

				res = false;
			}

			return res;
		}

		private void ResetAllProperties(AllIn1VfxComponent vfxComponent, string shaderName)
		{
			SetMaterial(vfxComponent, AllIn1VfxComponent.AfterSetAction.Reset, shaderName);
		}

		private void MissingRenderer(AllIn1VfxComponent vfxComponent)
		{
			EditorUtility.DisplayDialog("Missing Renderer", 
				"This GameObject (" + vfxComponent.gameObject.name + ") has no Renderer or UI Graphic component. This AllIn1Vfx component will be removed.",
				"Ok");
			vfxComponent.destroyed = true;
			DestroyImmediate(this);
		}

		public void SetSceneDirty()
		{
			if (!Application.isPlaying) EditorSceneManager.MarkAllScenesDirty();

			//If you get an error here please delete the code block below
			var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
			if (prefabStage != null) EditorSceneManager.MarkSceneDirty(prefabStage.scene);
			//Until here
		}

		private void MakeNewMaterial(AllIn1VfxComponent vfxComponent, string shaderName = "AllIn1Vfx", bool notifyWhenDone = false)
		{
			bool operationSuccessful = SetMaterial(vfxComponent, AllIn1VfxComponent.AfterSetAction.Clear, shaderName);

			if (notifyWhenDone && operationSuccessful)
			{
				AllIn1VfxWindow.ShowSceneViewNotification("AllIn1Vfx: Material Created and Assigned");
			}
		}

		public bool TryCreateNew(AllIn1VfxComponent vfxComponent)
		{
			bool rendererExists = false;
			Renderer sr = vfxComponent.GetComponent<Renderer>();
			if (sr != null)
			{
				rendererExists = true;
				if (sr != null && sr.sharedMaterial != null && sr.sharedMaterial.shader.name.Contains("Vfx"))
				{
					ResetAllProperties(vfxComponent, "AllIn1Vfx");
					vfxComponent.ClearAllKeywords();
				}
				else
				{
					vfxComponent.CleanMaterial();
					MakeNewMaterial(vfxComponent, "AllIn1Vfx");
				}
			}
			else
			{
				Graphic img = vfxComponent.GetComponent<Graphic>();
				if (img != null)
				{
					rendererExists = true;
					if (img.material.shader.name.Contains("Vfx"))
					{
						ResetAllProperties(vfxComponent, "AllIn1Vfx");
						vfxComponent.ClearAllKeywords();
					}
					else 
					{ 
						MakeNewMaterial(vfxComponent, "AllIn1Vfx"); 
					}
				}
			}

			if (!rendererExists)
			{
				MissingRenderer(vfxComponent);
			}

			SetSceneDirty();
			return rendererExists;
		}

		public void ClearAllKeywords(AllIn1VfxComponent vfxComponent)
		{
			if (!vfxComponent.destroyed) return;

			if (vfxComponent.currMaterial == null)
			{
				vfxComponent.FindCurrMaterial();
				if (vfxComponent.currMaterial == null)
				{
					MissingRenderer(vfxComponent);
					return;
				}
			}

			vfxComponent.ClearAllKeywords();

			SetSceneDirty();
		}

		private void DoAfterSetAction(AllIn1VfxComponent vfxComponent, AllIn1VfxComponent.AfterSetAction action)
		{
			switch (action)
			{
				case AllIn1VfxComponent.AfterSetAction.Clear:
					ClearAllKeywords(vfxComponent);
					break;
				case AllIn1VfxComponent.AfterSetAction.CopyMaterial:
					vfxComponent.currMaterial.CopyPropertiesFromMaterial(vfxComponent.prevMaterial);
					break;
			}
		}

		public bool RenderToImage(AllIn1VfxComponent vfxComponent)
		{
			if (vfxComponent.currMaterial == null)
			{
				vfxComponent.FindCurrMaterial();
				if (vfxComponent.currMaterial == null)
				{
					MissingRenderer(vfxComponent);
					return false;
				}
			}

			Texture tex = vfxComponent.currMaterial.GetTexture("_MainTex");
			if (tex != null) 
			{ 
				RenderAndSaveTexture(vfxComponent, vfxComponent.currMaterial, tex); 
			}
			else
			{
				SpriteRenderer sr = vfxComponent.GetComponent<SpriteRenderer>();
				Graphic i = vfxComponent.GetComponent<Graphic>();
				if (sr != null) 
				{ 
					tex = sr.sprite.texture; 
				}
				else if (i != null)
				{
					tex = i.mainTexture;
				}

				if (tex != null)
				{
					RenderAndSaveTexture(vfxComponent, vfxComponent.currMaterial, tex);
				}
				else
				{
					EditorUtility.DisplayDialog("No valid target texture found",
						"All In 1 VFX component couldn't find a valid Main Texture in this GameObject (" +
						vfxComponent.gameObject.name +
						"). This means that the material you are using has no Main Texture or that the texture couldn't be reached through the Renderer component you are using." +
						" Please make sure to have a valid Main Texture in the Material", "Ok");
				}
			}

			return tex != null;
		}

		private void RenderAndSaveTexture(AllIn1VfxComponent vfxComponent, Material targetMaterial, Texture targetTexture)
		{
			float scaleSlider = 1;
			if (PlayerPrefs.HasKey("All1VfxRenderImagesScale"))
			{
				scaleSlider = PlayerPrefs.GetFloat("All1VfxRenderImagesScale");
			}

			RenderTexture renderTarget = new RenderTexture((int)(targetTexture.width * scaleSlider),
				(int)(targetTexture.height * scaleSlider), 0, RenderTextureFormat.ARGB32);
			Graphics.Blit(targetTexture, renderTarget, targetMaterial);
			Texture2D resultTex = new Texture2D(renderTarget.width, renderTarget.height, TextureFormat.ARGB32, false);
			resultTex.ReadPixels(new Rect(0, 0, renderTarget.width, renderTarget.height), 0, 0);
			resultTex.Apply();

			string path = AllIn1VfxWindow.GetRenderImageSavePath();
			EditorUtils.CreateFolderIfNotExist(path);
			path += "/";
			if (!Directory.Exists(path))
			{
				EditorUtility.DisplayDialog("The desired Material to Image Save Path doesn't exist",
					"Go to Tools -> AllIn1 -> VFX Toolkit Window and set a valid folder", "Ok");
				return;
			}

			string fullPath = path + vfxComponent.gameObject.name + ".png";
			if (File.Exists(fullPath)) 
			{ 
				fullPath = GetNewValidPath(path + vfxComponent.gameObject.name); 
			}

			string pingPath = fullPath;

			string fileName = fullPath.Replace(path, "");
			fileName = fileName.Replace(".png", "");
			fullPath = EditorUtility.SaveFilePanel("Save Render Image", path, fileName, "png");
			if (fullPath.Length == 0) 
			{ 
				return; 
			}

			byte[] bytes = resultTex.EncodeToPNG();
			File.WriteAllBytes(pingPath, bytes);
			AssetDatabase.ImportAsset(pingPath);
			AssetDatabase.Refresh();
			DestroyImmediate(resultTex);
			EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath(pingPath, typeof(Texture)));
			AllIn1VfxWindow.SceneViewNotificationAndLog("Render Image saved to: " + fullPath + " with scale: " + scaleSlider +
														" (it can be changed in Tools -> AllIn1 -> VFX Toolkit Window)");
		}

		public bool ApplyMaterialToHierarchy(AllIn1VfxComponent vfxComponent)
		{
			Renderer sr = vfxComponent.GetComponent<Renderer>();
			Graphic img = vfxComponent.GetComponent<Graphic>();
			Material matToApply = null;
			if (sr != null) matToApply = sr.sharedMaterial;
			else if (img != null)
			{
				matToApply = img.material;
			}
			else
			{
				MissingRenderer(vfxComponent);
				return false;
			}

			List<Transform> children = new List<Transform>();
			vfxComponent.GetAllChildren(vfxComponent.transform, ref children);
			foreach (Transform t in children)
			{
				sr = t.gameObject.GetComponent<Renderer>();
				if (sr != null) sr.material = matToApply;
				else
				{
					img = t.gameObject.GetComponent<Graphic>();
					if (img != null) img.material = matToApply;
				}
			}

			return true;
		}

		private string GetNewValidPath(string path, int i = 1)
		{
			int number = i;
			string newPath = path + "_" + number.ToString();
			string fullPath = newPath + ".png";
			if (File.Exists(fullPath))
			{
				number++;
				fullPath = GetNewValidPath(path, number);
			}

			return fullPath;
		}
	}
}