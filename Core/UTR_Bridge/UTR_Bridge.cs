using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Assertions;
using NUnit.Framework;
using System.IO;

using Assert = NUnit.Framework.Assert;

namespace GraphicsTestFramework
{
	//[TestFixtureSource("SuitesList")]
	public class UTR_Bridge
	{
		private Suite suite;

		UTR_Bridge(Suite _suite)
		{
			suite = _suite;
		}

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
		public IEnumerator UTF_Test( [ValueSource("TestsList")] TestData testData)
		{
			Debug.Log(testData);

			yield return null;

			UnityEngine.Assertions.Assert.IsTrue(true);
		}

		public static IEnumerable SuitesList()
		{
			ProjectSettings projectSettings = SuiteManager.GetProjectSettings();
			SuiteManager.GenerateSceneList(false, false);

			for (int su = 0; su < projectSettings.suiteList.Count; su++) // Iterate scriptable object list
			{
				yield return new TestFixtureData( new object[]{projectSettings.suiteList[su]});
			}
		}

		public static IEnumerable TestsInSuiteList()
		{
			/*
			for (int gr = 0; gr < suite.groups.Count; gr++) // Iterate groups on the suite
			{
				for (int te = 0; te < suite.groups[gr].tests.Count; te++) // Iterate tests on the group
				{
					string sceneName = suite.groups[gr].tests[te].name;
					sceneName = Path.GetFileName( sceneName );
					sceneName.Remove( sceneName.Length - 6 , 6);

					string name = suite.groups[gr].groupName +"."+ sceneName;

					yield return name;
				}
			}
			*/
			yield return "Pouet A";
			yield return "Pouet B";
		}

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