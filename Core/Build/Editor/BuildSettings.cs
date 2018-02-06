using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace GraphicsTestFramework
{
    public class BuildSettings : EditorWindow
    {
        public BuildConfiguration buildConfiguration;

        // Scripting defines for the core
        static string[] coreScriptingDefines = new string[1]
        {
            "UTF_EXISTS"
        };

        // Menu Item
        [MenuItem("UTF/Build Pipeline")]
        public static void ShowWindow()
        {
            GetWindow(typeof(BuildSettings)); // Get window
        }

        // GUI
        void OnGUI()
        {
            GUILayout.Label("Project Preperation", EditorStyles.boldLabel); // Label
            if (GUILayout.Button("Prepare Project")) // If button
                PrepareBuild(); // Prepare build
            if (GUILayout.Button("Prepare Project (Debug)")) // If button
                PrepareDebugBuild(); // Prepare debug build

            EditorGUILayout.Space();

            GUILayout.Label("Build Pipeline", EditorStyles.boldLabel); // Label

            //EditorGUILayout.PropertyField(obj);
            buildConfiguration = (BuildConfiguration)EditorGUILayout.ObjectField(buildConfiguration, typeof(BuildConfiguration), false);
            if (GUILayout.Button("Execute Build Pipeline")) // If button
                RunBuildPipeline(); // Prepare build
        }

        public void RunBuildPipeline()
        {
            GetUnityVersionInfo(); // Get unity version info
            //SetApplicationSettings(null); // Set application settings
            SetScriptingDefines(); // Set defines
            SetPlayerSettings(); // Set player settings
            SetQualitySettings(); // Set quality settings
            ProjectSettings projectSettings = SuiteManager.GetProjectSettings();
            this.StartCoroutine(BuildPipeline.ProcessBuildPipeline(buildConfiguration, projectSettings));
        }

        // Setup for build
        public static void PrepareBuild()
        {
            GetUnityVersionInfo(); // Get unity version info
            SuiteManager.GenerateSceneList(false); // Create suite structure
            SetApplicationSettings(null); // Set application settings
            SetScriptingDefines(); // Set defines
            SetPlayerSettings(); // Set player settings
            SetQualitySettings(); // Set quality settings
            PlayerSettings.bundleVersion = Common.applicationVersion; // Set application version
        }

        // Setup for debug build
        public static void PrepareDebugBuild()
        {
            GetUnityVersionInfo(); // Get unity version info
            SuiteManager.GenerateSceneList(true); // Create suite structure
            SetApplicationSettings(null); // Set application settings
            SetScriptingDefines(); // Set defines
            SetPlayerSettings(); // Set player settings
            SetQualitySettings(); // Set quality settings
            PlayerSettings.bundleVersion = Common.applicationVersion; // Set application version
        }

        public static void GetUnityVersionInfo()
        {
            ProjectSettings projectSettings = SuiteManager.GetProjectSettings(); // Get settings
            projectSettings.unityVersion = UnityEditorInternal.InternalEditorUtility.GetFullUnityVersion(); // Set unity version
            projectSettings.unityBranch = UnityEditorInternal.InternalEditorUtility.GetUnityBuildBranch(); // Set unity branch
            SuiteManager.SetProjectSettings(projectSettings);
        }

        // Set scripting define symbols
        public static void SetScriptingDefines()
        {
            ProjectSettings projectSettings = SuiteManager.GetProjectSettings(); // Get settings
            string output = ""; // Create output string
            for (int i = 0; i < coreScriptingDefines.Length; i++) // Iterate core defines
                output += coreScriptingDefines[i] + ";"; // Add
            if (projectSettings.scriptingDefines != null) // Check for null
            {
                for (int i = 0; i < projectSettings.scriptingDefines.Length; i++) // Iterate settings defines
                    output += projectSettings.scriptingDefines[i] + ";"; // Add
            }
            int platformCount = Enum.GetNames(typeof(BuildTargetGroup)).Length; // Get platform count
            for (int i = 0; i < platformCount; i++) // Iterate all platforms
            {
                if(!depreciatedBuiltTargets.Contains(i))
                    PlayerSettings.SetScriptingDefineSymbolsForGroup((BuildTargetGroup)i, output); // Add custom to current
            }
        }

        // Set player settings
        public static void SetPlayerSettings()
        {
            PlayerSettings.gpuSkinning = true;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            //QualitySettings.vSyncCount = 0;
            Application.runInBackground = true; //ignored on mobiles
        }

        // Set quality settings
        public static void SetQualitySettings()
        {
            TestSettings testSettings = Resources.Load<TestSettings>("DefaultTestSettings"); // Get default
            if (testSettings == null) // If still none found
                return; // Fail, return
            Common.SetTestSettings(testSettings); // Set settings
        }

        // Set various application specific settings
        public static void SetApplicationSettings(BuildTarget target)
        {
            PlayerSettings.companyName = "Unity Technologies";
            string productName = "";
            ProjectSettings projectSettings = SuiteManager.GetProjectSettings();
            if(projectSettings)
            {
                if(projectSettings.buildNameOverride != null && projectSettings.buildNameOverride.Length > 0)
                {
                    productName = projectSettings.buildNameOverride;
                }
                else
                {
                    if (projectSettings.suiteList.Count == 0)
                    {
                        Debug.LogError("No suites found on Settings object. Aborting.");
                        return;
                    }
                    else if (projectSettings.suiteList.Count > 1)
                        productName = "UTF_Various";
                    else
                        productName = "UTF_" + projectSettings.suiteList[0].suiteName;
                }
            }
            else
            {
                Debug.LogError("No Settings object found. Aborting.");
                return;
            }
            productName += BuildPipeline.AppendProductName(target);
            PlayerSettings.productName = productName;
            
            int platformCount = Enum.GetNames(typeof(BuildTargetGroup)).Length; // Get platform count
            for (int i = 0; i < platformCount; i++) // Iterate all platforms
            {
                if (!depreciatedBuiltTargets.Contains(i))
                {
                    if((BuildTargetGroup)i == BuildTargetGroup.iOS)
                        PlayerSettings.SetApplicationIdentifier((BuildTargetGroup)i, "com.UnityTechnologies." + productName.Replace("_", "-")); // Set bundle identifier for iOS
                    else
                        PlayerSettings.SetApplicationIdentifier((BuildTargetGroup)i, "com.UnityTechnologies." + productName); // Set bundle identifiers for other
                }
            }
        }

        #if UNITY_2017_1_OR_NEWER
        private static List<int> depreciatedBuiltTargets = new List<int>(){ 0 , 2, 3, 5, 6, 8, 9, 10, 11, 12, 15, 16, 17, 20, 22};
        #else
        private static List<int> depreciatedBuiltTargets = new List<int>() { 0, 3, 5, 6, 8, 9, 10, 11, 12, 15, 16 };
        #endif
    }
}
