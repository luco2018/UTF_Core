using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Assertions;
using NUnit.Framework;
using System.IO;

using Assert = NUnit.Framework.Assert;


#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace GraphicsTestFramework
{
	public class UTR_Bridge : IPrebuildSetup //, IPostBuildCleanup
	{
		#if UNITY_EDITOR

		static EditorBuildSettingsScene[] preSetupBuildScenes;
		static SceneSetup[] preSetupSceneSetup;

		#endif

		
		public void Setup()
		{
			Debug.Log("Coucou. Is playing ? "+Application.isPlaying);

			#if UNITY_EDITOR
			
			preSetupBuildScenes = EditorBuildSettings.scenes;
			preSetupSceneSetup = EditorSceneManager.GetSceneManagerSetup();

			SuiteManager.GenerateSceneList(false, true);

			#endif
		}

		public void Cleanup()
		{
			#if UNITY_EDITOR

			EditorBuildSettings.scenes = preSetupBuildScenes;
			EditorSceneManager.RestoreSceneManagerSetup(preSetupSceneSetup);
			
			#endif
		}


		// Data passed to the test function.
		public struct TestData
		{
			public Suite suite;
			public Group group;
			public Test test;

			public override string ToString()
			{
				string sceneName = test.name;
				sceneName = Path.GetFileName( sceneName );
				sceneName = sceneName.Remove( sceneName.Length - 6 , 6);

				string testName = suite.name;

				testName += "..."+group.groupName;

				testName += "..."+sceneName;

				return testName;
			}
		}

		// Test function registered in the TestRunner window.
		[UnityTest]
		public IEnumerator UTF_Test( [ValueSource("TestsList")] TestData testData)
		{
			Debug.Log(testData);

			UnityEngine.SceneManagement.SceneManager.LoadScene( testData.test.scenePath, UnityEngine.SceneManagement.LoadSceneMode.Single );

			yield return new WaitForSeconds(0.5f);

			UnityEngine.Assertions.Assert.IsTrue( UnityEngine.SceneManagement.SceneManager.GetSceneByName( testData.test.scenePath.Remove(testData.test.scenePath.Length-6, 6) ) != null );
		}

		// Tests List enumerable passed to the test function through the ValueSource attribute.
		public static IEnumerable TestsList()
		{
			ProjectSettings projectSettings = SuiteManager.GetProjectSettings();
			SuiteManager.GenerateSceneList(false, false);

			for (int su = 0; su < projectSettings.suiteList.Count; su++) // Iterate scriptable object list
			{
				for (int gr = 0; gr < projectSettings.suiteList[su].groups.Count; gr++) // Iterate groups on the suite
				{
					for (int te = 0; te < projectSettings.suiteList[su].groups[gr].tests.Count; te++) // Iterate tests on the group
					{
						TestData o = new TestData()
						{
							suite = projectSettings.suiteList[su],
							group = projectSettings.suiteList[su].groups[gr],
							test = projectSettings.suiteList[su].groups[gr].tests[te]
						};

						yield return o;
					}
				}
			}
		}
	}
}