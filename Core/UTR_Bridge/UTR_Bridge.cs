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
		static bool resultsIOInitialized = false;

		#if UNITY_EDITOR

		static EditorBuildSettingsScene[] preSetupBuildScenes;
		static SceneSetup[] preSetupSceneSetup;

		#endif

		
		public void Setup()
		{
			#if UNITY_EDITOR
			
			preSetupBuildScenes = EditorBuildSettings.scenes;
			preSetupSceneSetup = EditorSceneManager.GetSceneManagerSetup();

			SuiteManager.GenerateSceneList(false, true);

			//EditorSceneManager.OpenScene( AssetDatabase.GetAssetPath( EditorCommon.masterScene ), OpenSceneMode.Single );

			#endif

			Debug.Log("Finished Setup, start test(s).");
		}

		public void Cleanup()
		{
			#if UNITY_EDITOR

			EditorBuildSettings.scenes = preSetupBuildScenes;
			EditorSceneManager.RestoreSceneManagerSetup(preSetupSceneSetup);
			
			#endif
		}

		
		[OneTimeSetUp]
		public void OneTimeSetUp()
		{
			Debug.Log("OneTimeSetUp");

			UnityEngine.SceneManagement.SceneManager.LoadScene(0, UnityEngine.SceneManagement.LoadSceneMode.Single);

			resultsIOInitialized = false;
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

		[UnityTest]
		public IEnumerator DummyTest()
		{
			yield return null;
			Assert.True( true );
		}

		// Test function registered in the TestRunner window.
		[UnityTest]
		public IEnumerator UTF_Test( [ValueSource("TestsList")] TestData testData)
		{
			yield return null; // Skip first frame

			if (!resultsIOInitialized)
			{
				// Don't spam on slack please :-)
				Slack slackComponent = UnityEngine.GameObject.FindObjectOfType<Slack>();
				slackComponent.enableSlackIntegration = false;

				yield return ( ResultsIO.Instance.Init() ); // Init results.

				resultsIOInitialized = true;

				yield return null; // Just to be sure that the TestStructure has been initialized.
			}


			// Select the current test in the test structure.
			foreach ( var su in TestStructure.Instance.testStructure.suites )
			{
				bool suiteEnabled = su.suiteName == testData.suite.suiteName;

				su.selectionState = suiteEnabled? 1 : 0; // Select Suite if names match.

				foreach( var tp in su.types )
				{
					int typeMask = 1 << tp.typeIndex;
					int r = testData.test.testTypes & typeMask;

					bool typeEnabled = r != 0;
					typeEnabled &= suiteEnabled;

					tp.selectionState = typeEnabled? 1 : 0; // Select type if type is in typemask && suiteEnabled

					foreach( var gp in tp.groups)
					{
						bool groupEnabled = gp.groupName == testData.group.groupName;
						groupEnabled &= typeEnabled;

						gp.selectionState = groupEnabled? 1 : 0; // Select Group if names match && typeEnabled && suiteEnabled

						foreach( var te in gp.tests)
						{
							string name = Path.GetFileName(testData.test.name); // Keep only the scene name.
							name = name.Remove(name.Length-6, 6); // Remove ".unity"

							bool testEnabled = te.testName == name;
							testEnabled &= groupEnabled;

							te.selectionState = testEnabled? 1 : 0; // Select Test if names match && groupEnabled && typeEnabled && suiteEnabled
						}
					}
				}
			}

			GenerateTestRunner(RunnerType.Automation);

			for(int i=0 ; i<15 ; ++i) yield return null;
			while (TestRunner.Instance.isRunning) yield return null;
			yield return null;

			//UnityEngine.SceneManagement.SceneManager.LoadScene( testData.test.scenePath, UnityEngine.SceneManagement.LoadSceneMode.Single );

			// yield return new WaitForSeconds(2f);

			UnityEngine.Assertions.Assert.IsTrue( TestTypeManager.Instance.GetActiveTestLogic().activeResultData.common.PassFail, "Test failed" );
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

		// Commes from Menu.cs
		// Generate and execute a new test runner
        public void GenerateTestRunner(RunnerType type)
        {
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Generating a test runner"); // Write to console
            TestRunner newRunner; // Store a reference to call on
            if (!Master.Instance.gameObject.GetComponent<TestRunner>()) // If no test runner
                newRunner = Master.Instance.gameObject.AddComponent<TestRunner>(); // Generate one
            else
                newRunner = Master.Instance.gameObject.GetComponent<TestRunner>(); // Get current
            newRunner.SetupRunner(type); // Setup the runner
        }
	}
}