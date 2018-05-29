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

		
		// When running in editor, this function is called in editor mode before OneTimeSetup, or before doing a build
		// It will at all the test scenes + master to the build scenes list.
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

		// This should be executed after the build has been done, or after the tests have been run, but currently there is a bug that causes this function to be called just before entering play mode in editor, so all the rest would fail.
		// Hense why IPostBuildCleanup is disabled
		public void Cleanup()
		{
			#if UNITY_EDITOR

			EditorBuildSettings.scenes = preSetupBuildScenes;
			EditorSceneManager.RestoreSceneManagerSetup(preSetupSceneSetup);
			
			#endif
		}

		// One Time Setup function called once when entering play mode for each batch of tests.
		[OneTimeSetUp]
		public void OneTimeSetUp()
		{
			// Load the master scene.
			UnityEngine.SceneManagement.SceneManager.LoadScene(0, UnityEngine.SceneManagement.LoadSceneMode.Single);

			// Be sure to disable this toggle.
			resultsIOInitialized = false;
		}


		// Data passed to the test function.
		public struct TestData
		{
			public Suite suite;
			public Group group;
			public Test test;

			// Format the data in a readable way that will be shown in the TestRunner window.
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
		// ValueSource will use "TestsList" function to grab all tests in all suites in the project.
		[UnityTest]
		public IEnumerator UTF_Test( [ValueSource("TestsList")] TestData testData)
		{
			yield return null; // Skip first frame

			// If the results have not been initialized (typicaly, if this is the first test)
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

			// Run the test
			GenerateTestRunner(RunnerType.Automation);

			// Wait 15 frames (arbitrary number) to allow the Test Runner to start.
			for(int i=0 ; i<15 ; ++i) yield return null;
			// Wait for the Test Runner to end.
			while (TestRunner.Instance.isRunning) yield return null;
			// And one more frame to be sure the results have been made.
			yield return null;

			// Assert the result in Test Runner.
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