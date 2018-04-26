using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.IO;

namespace GraphicsTestFramework
{
    public class BuildSettings : EditorWindow
    {
        public BuildConfiguration buildConfiguration;

        bool isDebugProject = false;

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
            GUILayout.Label("Run Tests", EditorStyles.boldLabel); // Label
            if (GUILayout.Button("Run")) // If button
                BuildSettings_BackupRestore.RunUTF(isDebugProject); // Run UTF
            
            isDebugProject = EditorGUILayout.Toggle("Debug", isDebugProject);

            EditorGUILayout.Space();

            GUILayout.Label("Build Pipeline", EditorStyles.boldLabel); // Label

            //EditorGUILayout.PropertyField(obj);
            buildConfiguration = (BuildConfiguration)EditorGUILayout.ObjectField(buildConfiguration, typeof(BuildConfiguration), false);
            if (GUILayout.Button("Execute Build Pipeline")) // If button
                RunBuildPipeline(); // Prepare build
        }

        public void OpenMasterScene()
        {
            string[] guids = AssetDatabase.FindAssets("t:Scene Master");

            foreach ( string guid in guids )
            {
                string scenePath = AssetDatabase.GUIDToAssetPath(guid);
                if ( Path.GetFileName( Path.GetDirectoryName ( scenePath ) ) == "UTF_Core" ) // if the scene's parent direction is "UTF_Core"
                {
                    UnityEditor.SceneManagement.EditorSceneManager.OpenScene( scenePath, UnityEditor.SceneManagement.OpenSceneMode.Single );
                    break;
                }
            }
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

    // Static class that stores data in editor preferences to backup and restore the editor settings.
    [InitializeOnLoad]
    public static class BuildSettings_BackupRestore
    {
        // Bool value to know if play mode was trigger by the "Run UTF" button
        static bool utfIsRunning 
        {
            get { return EditorPrefs.GetBool("UTF_IsRunning"); }
            set { EditorPrefs.SetBool("UTF_IsRunning", value); }
        }

        // The play mode start scene GUID
        static SceneAsset previousPlayModeStartScene
        {
            get
            {
                SceneAsset o = null;

                string data = EditorPrefs.GetString("UTF_PreviousPlayModeStartScene");
                if (!string.IsNullOrEmpty(data))
                    o = AssetDatabase.LoadAssetAtPath<SceneAsset>( AssetDatabase.GUIDToAssetPath(data) );
                
                return o;
            }
            set
            {
                string data = AssetDatabase.AssetPathToGUID( AssetDatabase.GetAssetPath(value) );
                EditorPrefs.SetString("UTF_PreviousPlayModeStartScene", data);
            }
        }

        // Store the scenes of build settings in a big string that is split. The format is : GUID1//enabled1||GUID2//enabled2|| ...
        static EditorBuildSettingsScene[] previousBuildScenes
        {
            get
            {
                List<EditorBuildSettingsScene> o = new List<EditorBuildSettingsScene>();

                string data = EditorPrefs.GetString("UTF_PeviousBuildScenes");
                if (!string.IsNullOrEmpty(data))
                {
                    string[] datas = data.Split(new string[]{"||"}, StringSplitOptions.None);

                    for (int i=0 ; i<datas.Length ; ++i)
                    {
                        string[] sceneData = datas[i].Split(new string[]{"//"}, StringSplitOptions.None);

                        GUID sceneGUID;
                        if ( GUID.TryParse( sceneData[0], out sceneGUID ) )
                        {
                            EditorBuildSettingsScene scene = new EditorBuildSettingsScene();
                            scene.guid = sceneGUID;
                            scene.path = AssetDatabase.GUIDToAssetPath(sceneData[0]);
                            scene.enabled = bool.Parse(sceneData[1]);

                            o.Add(scene);
                        }
                    }
                }
                
                return o.ToArray();
            }
            set
            {
                string data = "";

                for (int i=0 ; i<value.Length ; ++i)
                {
                    if (i>0) data += "||";

                    data += value[i].guid.ToString() + "//"+value[i].enabled.ToString();
                }

                EditorPrefs.SetString("UTF_PeviousBuildScenes", data);
            }
        }

        // Class construction called at Domain Reload, to add the callback to PlayModeStateChange event.
        static BuildSettings_BackupRestore()
        {
            EditorApplication.playModeStateChanged += PlayModeStateTracker;
        }

        // PlayModeStateChange callback
        public static void PlayModeStateTracker( PlayModeStateChange state )
        {
            if (state == PlayModeStateChange.ExitingPlayMode) // When exiting play mode (Badicaly, end of tests)
            {
                if (utfIsRunning) StopUTF(); // If we were running UTF, call the restore function.
            }
        }

        // Called by the window button.
        public static void RunUTF( bool debug )
        {
            if (EditorApplication.isPlaying) return; // Prevent to call if it is already in play mode.

            if (EditorCommon.masterScene == null) // Check for Master Scene
            {
                return;
            }

            // Backup and setup Master scene to start at play mode.
            previousPlayModeStartScene = EditorSceneManager.playModeStartScene;
            EditorSceneManager.playModeStartScene = EditorCommon.masterScene;

            previousBuildScenes = EditorBuildSettings.scenes; // Backup scenes in build settings

            // Prepare the build.
            if (debug)
                BuildSettings.PrepareDebugBuild();
            else
                BuildSettings.PrepareBuild();

            utfIsRunning = true; // Store UTF state

            EditorApplication.isPlaying = true; // Start play mode
        }

        // Called when exiting play mode after UTF tests.
        public static void StopUTF()
        {
            utfIsRunning = false;

            EditorSceneManager.playModeStartScene = previousPlayModeStartScene; // Restore original scene to start at play mode.

            EditorBuildSettings.scenes = previousBuildScenes; // Restore scenes in build settings
        }
    }
}