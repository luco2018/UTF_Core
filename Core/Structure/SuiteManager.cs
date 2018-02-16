using System;
using System.Collections.Generic;
using UnityEngine;

namespace GraphicsTestFramework
{
    // ------------------------------------------------------------------------------------
    // SuiteManager
    // - Collects Suite Scriptable Objects
    // - Builds local Suite and Scene structure for use by TestStructure

    public static class SuiteManager
    {
        // ------------------------------------------------------------------------------------
        // Get Data

        // Does the project object have any suites?
        public static bool HasSuites()
        {
            ProjectSettings projectSettings = GetProjectSettings(); // Get the suite list
            if(projectSettings.suiteList.Count > 0)
                return true;
            else
                return false;
        }

        // Get a string array of all suite names
        public static string[] GetSuiteNames()
        {
            ProjectSettings projectSettings = GetProjectSettings(); // Get the suite list
            string[] suiteNames = new string[projectSettings.suiteList.Count]; // Create string array of correct length
            for (int i = 0; i < suiteNames.Length; i++) // Iterate suites
                suiteNames[i] = projectSettings.suiteList[i].suiteName; // Add to array
            return suiteNames; // Return
        }

        // Get suites
        public static Suite[] GetSuites()
        {
            ProjectSettings projectSettings = GetProjectSettings();
            return projectSettings.suiteList.ToArray(); // Return
        }

        // Get a specific suite name
        public static string GetSuiteName(int index)
        {
            ProjectSettings projectSettings = GetProjectSettings(); // Get the suite list
            return projectSettings.suiteList[index].suiteName; // Return requested
        }

        // Get a specififc suite by name
        public static Suite GetSuiteByName(string name)
        {
            ProjectSettings projectSettings = GetProjectSettings(); // Get the suite list
            for(int i = 0; i < projectSettings.suiteList.Count; i++)
            {
                if (projectSettings.suiteList[i].suiteName == name)
                    return projectSettings.suiteList[i];
            }
            return null;
        }

        // Get a specific test
        public static Test GetTest(TestEntry inputEntry)
        {
            ProjectSettings projectSettings = GetProjectSettings(); // Get the suite list
            return projectSettings.suiteList[inputEntry.suiteIndex].groups[inputEntry.groupIndex].tests[inputEntry.testIndex]; // Return requested
        }

        // Get the Settings object
        public static ProjectSettings GetProjectSettings()
        {
            ProjectSettings[] projectSettingsArray = Resources.LoadAll<ProjectSettings>(""); // Find all suite lists
            if (projectSettingsArray.Length == 0) // If no suite list found
            {
#if UNITY_EDITOR
                return GenerateProjectSettings(); // Create one
#else
                Console.Instance.Write(DebugLevel.Critical, MessageLevel.LogError, "No Settings object found. Aborting."); // Write to console
                return null;
#endif
            }
            else
                return projectSettingsArray[0]; // Return suite list
        }

        // Set the Settings object
        public static void SetProjectSettings(ProjectSettings input)
        {
            ProjectSettings[] projectSettingsArray = Resources.LoadAll<ProjectSettings>(""); // Find all suite lists
            if (projectSettingsArray.Length == 0) // If no suite list found
            {
                Console.Instance.Write(DebugLevel.Critical, MessageLevel.LogError, "No Settings object found. Aborting."); // Write to console
            }
            else
            {
                projectSettingsArray[0] = input;
#if(UNITY_EDITOR)
                UnityEditor.EditorUtility.SetDirty(projectSettingsArray[0]);
#endif
            }
        }

        // ------------------------------------------------------------------------------------
        // Editor Methods

#if UNITY_EDITOR

        // Create Suite and Scene structure
        [ExecuteInEditMode]
        public static void GenerateSceneList(bool debug)
        {
            ProjectSettings projectSettings = GetProjectSettings(); //Get the suite list
            projectSettings.suiteList.Clear(); // Clear suites list
            Suite[] foundSuites = Resources.LoadAll<Suite>(""); // Load all Suite scriptable objects into array
            for (int i = 0; i < foundSuites.Length; i++)
            {
                if (debug && foundSuites[i].isDebugSuite || !debug && !foundSuites[i].isDebugSuite)
                    projectSettings.suiteList.Add(foundSuites[i]);
            }
            UnityEditor.EditorUtility.SetDirty(projectSettings); // Set dirty
            List<UnityEditor.EditorBuildSettingsScene> buildSettingsScenes = new List<UnityEditor.EditorBuildSettingsScene>(); // Create new build settings scene list
            AddManualMasterScene(buildSettingsScenes); // Add manual master TODO - Switch this for full automation
            for (int su = 0; su < projectSettings.suiteList.Count; su++) // Iterate scriptable object list
            {
                for (int gr = 0; gr < projectSettings.suiteList[su].groups.Count; gr++) // Iterate groups on the suite
                {
                    for (int te = 0; te < projectSettings.suiteList[su].groups[gr].tests.Count; te++) // Iterate tests on the group
                    {
                        projectSettings.suiteList[su].groups[gr].tests[te].scenePath = UnityEditor.AssetDatabase.GetAssetPath(projectSettings.suiteList[su].groups[gr].tests[te].scene);
                        projectSettings.suiteList[su].groups[gr].tests[te].name = projectSettings.suiteList[su].groups[gr].tests[te].scenePath;
                        UnityEditor.EditorUtility.SetDirty(projectSettings.suiteList[su]);
                        UnityEditor.EditorBuildSettingsScene scene = new UnityEditor.EditorBuildSettingsScene(projectSettings.suiteList[su].groups[gr].tests[te].scenePath, true); // Create new build settings scene from asset path
                        if (!FindDuplicateScene(buildSettingsScenes, projectSettings.suiteList[su].groups[gr].tests[te].scenePath)) // If no duplicate scene found
                            buildSettingsScenes.Add(scene); // Add to build settings scenes list
                    }
                }
            }
            UnityEditor.EditorBuildSettings.scenes = buildSettingsScenes.ToArray(); // Set build settings scene list
        }

        // Generate a new suite list object
        static ProjectSettings GenerateProjectSettings()
        {
            ProjectSettings newProjectSettings = ScriptableObject.CreateInstance<ProjectSettings>(); // Create instance
            if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/Resources")) // Check folder exists
                UnityEditor.AssetDatabase.CreateFolder("Assets", "Resources"); // Create it
            UnityEditor.AssetDatabase.CreateAsset(newProjectSettings, "Assets/Resources/Settings.asset"); // Create asset
            UnityEditor.AssetDatabase.SaveAssets(); // Save assets
            UnityEditor.AssetDatabase.Refresh(); // Refresh database
            return newProjectSettings; // Return the suite list
        }

        // Add the manual master scene
        static void AddManualMasterScene(List<UnityEditor.EditorBuildSettingsScene> buildSettingsScenes)
        {
            string[] foundAssets = UnityEditor.AssetDatabase.FindAssets("Master t:Scene"); // Find master scene
            string masterScenePath = UnityEditor.AssetDatabase.GUIDToAssetPath(foundAssets[0]); // Get scene path for Master scene
            buildSettingsScenes.Add(new UnityEditor.EditorBuildSettingsScene(masterScenePath, true)); // Add to build settings scene list
        }

        // Find duplicate suite by name
        static bool FindDuplicateSuite(string name)
        {
            ProjectSettings projectSettings = GetProjectSettings(); // Get the suite list
            foreach (Suite suite in projectSettings.suiteList) // Iterate local suites
            {
                if (suite.suiteName == name) // If equal to input suite
                    return true; // Duplicate. Return true
            }
            return false; // No duplicate. Return false
        }

        // Find duplicate scene in build settings by asset path
        static bool FindDuplicateScene(List<UnityEditor.EditorBuildSettingsScene> buildSettingsScenes, string path)
        {
            foreach (UnityEditor.EditorBuildSettingsScene edScene in buildSettingsScenes) // Iterate build settings scenes
            {
                if (edScene.path == path) // If equal to asset path
                    return true; // Duplicate. Return true
            }
            return false; // No duplicate. Return false
        }
#endif
    }
}
