using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;

public class BakeOcclusionCulling: MonoBehaviour
{
	public static bool debug = false;

	[MenuItem("QA/Occlusion Culling/Bake Build Settings Scenes", false, 1200)]
	private static void BakeBuild()
	{
		
		if (EditorUtility.DisplayDialog("Bake Build Settings Scenes?", "Bake Static Occlusion Culling for Build Settings Scenes?", "Yes", "No"))
		{
			Debug.Log("Starting Batch Bake Occlusion Culling...");
			AssetDatabase.SaveAssets();

			EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
			System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();

            int id = 1;
			foreach (EditorBuildSettingsScene scn in scenes)
			{
				Debug.Log("Baking: "+id+"/"+scenes.Length+" - " + scn.path);
				EditorSceneManager.OpenScene(scn.path);

				//CLEAR THE LIGHTMAPS AND THE CACHE
				//THIS IS AN ATTEMPT TO PREVENT SYSTEM HANGS SEE
				//http://forum.unity3d.com/threads/bake-runtime-job-failed-with-error-code-11-failed-reading-albedo-texture-file.326474/
				//Lightmapping.Clear();
				//Lightmapping.ClearDiskCache();
                StaticOcclusionCulling.Clear();

                watch.Reset();
				watch.Start();

				//Lightmapping.Bake();
                StaticOcclusionCulling.Compute();

                //Stop the timer
                watch.Stop();

				EditorApplication.SaveScene();

				Debug.Log("Finished Occlusion Culling Bake Time: " + id + "/" + scenes.Length + " - " + watch.Elapsed.TotalMinutes.ToString() + " Scene: " + scn.path);
                id++;
			}

			Debug.Log("Finished Batch Bake Occlusion Culling.");
		}
	}

	//THIS FUNCTION IS NOT RECURSIVE
	private static void ReimportFolder(string localPath)
	{
		if (Directory.Exists(localPath))
		{
			string[] fileEntries = Directory.GetFiles(localPath);
			foreach (string file in fileEntries)
			{
				AssetDatabase.ImportAsset(file);
			}
		}
	}

	//THIS FUNCTION IS NOT RECURSIVE
	private static void DeleteAssetsInFolder(string localPath)
	{
		if (Directory.Exists(localPath))
		{
			string[] fileEntries = Directory.GetFiles(localPath);
			foreach (string file in fileEntries)
			{
				AssetDatabase.DeleteAsset(file);
			}
		}
	}
		
}