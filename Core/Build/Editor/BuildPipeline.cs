using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace GraphicsTestFramework
{
    public static class BuildPipeline
    {
        // Start Build Pipeline process
        public static IEnumerator ProcessBuildPipeline(BuildConfiguration config, ProjectSettings projectSettings)
        {
            if (config) // If the build configuration is not null
            {
                for (int t = 0; t < config.buildTargets.Count; t++) // Iterate build targets
                {
                    BuildTarget target = config.buildTargets[t]; // Get current target
                    string directory = UnityEngine.Application.dataPath.Replace("/Assets", "/Builds"); // Get directory
                    if (target.nameOverride != null && target.nameOverride.Length > 0) // If use name override from the target
                        directory = target.nameOverride; // Use
                    else if (config.nameOverride != null && config.nameOverride.Length > 0) // Else if use name override from the whole config
                        directory = config.nameOverride; // Use

                    string applicationName = GetApplicationName(target, projectSettings, config.nameOverride); // Get application name

                    BuildSettings.SetApplicationSettings(target, AppendProductName(target));

                    if (SetGraphicsAPI(target) == false) // Check if Graphics API can be set to the specified target
                    {
                        Debug.LogError("Failed to build Player, Directory: " + directory + "/" + applicationName); // Log error
                        continue; // Go to next target
                    }

                    string build = BuildClient(target, directory, applicationName); // Build client

                    while (UnityEditor.BuildPipeline.isBuildingPlayer == true) // Wait for build to finish
                        yield return null; // Wait
                    if (build == "") // If build succeeded
                        Debug.Log("Built Player, Directory: " + directory + "/" + applicationName); // Log success
                    else // If build failed
                    {
                        Debug.LogError("Failed to build Player, Directory: " + directory + "/" + applicationName); // Log failure
                        Debug.LogError(build); // Return build fail log
                    }
                }
            }
            else // Null build configuartion
                Debug.LogError("No Build Configuration file assigned. Please assign a Build Configuration file in the Build Pipeline window"); // Log error 
        }

        // On certain platforms append target data to product name
        public static string AppendProductName(BuildTarget target)
        {
            string modifiedAPI = target.graphicsApi.ToString().Replace(" ", "").Replace("_", "-");
            switch (target.platform) // Append target data based on target platform
            {
                case UnityEditor.BuildTarget.iOS:
                    return "_" + modifiedAPI;
                case UnityEditor.BuildTarget.Android:
                    return "_" + modifiedAPI;
                default:
                    return "";
            }
        }

        // Try to set Graphics API for the target
        public static bool SetGraphicsAPI(BuildTarget target)
        {
            UnityEngine.Rendering.GraphicsDeviceType[] apis = new UnityEngine.Rendering.GraphicsDeviceType[1] { target.graphicsApi }; // Make array from target API
            PlayerSettings.SetGraphicsAPIs(target.platform, apis); // Set API from array
            UnityEngine.Rendering.GraphicsDeviceType[] currentApis = PlayerSettings.GetGraphicsAPIs(target.platform); // Get the API list
            if (currentApis.Length == 0 || currentApis[0] != target.graphicsApi) // Check that API was assigned correctly
            {
                Debug.LogError("Invalid Graphics API supplied for platform"); // If it wasnt set return error
                return false; // Return fail
            }
            else
                return true; // Return true
        }

        // Get Application name
        public static string GetApplicationName(BuildTarget target, ProjectSettings projectSettings, string configNameOverride)
        {
            bool ignoreExtensions = false; // Create bool
            string projectName = GetProjectName(projectSettings); // Get project name
            string applicationName = ""; // Create string to fill 
            if (target.nameOverride != null && target.nameOverride.Length > 0) // If target has name override
            {
                applicationName = target.nameOverride; // Set to application name
                ignoreExtensions = true; // And ignore extensions
            }
            else if (configNameOverride != null && configNameOverride.Length > 0) // If config has name override
                applicationName = configNameOverride; // Set to application name (but dont ignore extension)
            if (applicationName == "") // If app name was not overwritten
                applicationName = projectName; // Get from project name
            if (!ignoreExtensions) // If extensions werent ignored
                applicationName += "-" + target.platform.ToString() + "-" + target.graphicsApi.ToString(); // Append platform and API
            switch (target.platform) // Append file extensions based on target platform
            {
                case UnityEditor.BuildTarget.StandaloneWindows:
                    applicationName += ".exe";
                    break;
                case UnityEditor.BuildTarget.StandaloneWindows64:
                    applicationName += ".exe";
                    break;
#if UNITY_2018_1_OR_NEWER
                case UnityEditor.BuildTarget.StandaloneOSX:
                    applicationName += ".app";
                break;
#endif
                case UnityEditor.BuildTarget.StandaloneOSXIntel:
                    applicationName += ".app";
                    break;
                case UnityEditor.BuildTarget.StandaloneOSXIntel64:
                    applicationName += ".app";
                    break;
                case UnityEditor.BuildTarget.StandaloneOSXUniversal:
                    applicationName += ".app";
                    break;
                case UnityEditor.BuildTarget.StandaloneLinux:
                    applicationName += ".app";
                    break;
                case UnityEditor.BuildTarget.StandaloneLinux64:
                    applicationName += ".app";
                    break;
                case UnityEditor.BuildTarget.iOS:
                    break;
                case UnityEditor.BuildTarget.Android:
                    applicationName += ".apk";
                    break;
            }
            return applicationName; // Return
        }

        // Get the project name (to match Application Identifier in PlayerSettings)
        public static string GetProjectName(ProjectSettings projectSettings)
        {
            if (projectSettings) // If project settings isnt null
            {
                if (projectSettings.buildNameOverride != null && projectSettings.buildNameOverride.Length > 0) // If using build override
                    return projectSettings.buildNameOverride; // Return the override
                else
                {
                    if (projectSettings.suiteList.Count == 0) // If no suites
                        return ""; // Return
                    else if (projectSettings.suiteList.Count > 1) // If multiple suites
                        return "UTF_Various"; // Various
                    else // Single suite
                        return "UTF_" + projectSettings.suiteList[0].suiteName; // Return its name
                }
            }
            else // Null project settings
                return ""; // Return
        }

        // Build the client
        public static string BuildClient(BuildTarget target, string directory, string applicationName)
        {
            if (!System.IO.Directory.Exists(directory + "/" + "")) // If directory doesnt exist
                System.IO.Directory.CreateDirectory(directory + "/" + applicationName); // Create it
            Debug.Log("Building Player, Directory: " + directory + "/" + applicationName); // Debug "building"
            return UnityEditor.BuildPipeline.BuildPlayer(GetBuildSettings(target.platform, directory + "/" + applicationName)); // Build player and return debug result
        }

        // Get BuildSettings object
        static BuildPlayerOptions GetBuildSettings(UnityEditor.BuildTarget target, string path)
        {
            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions(); // Create new BuildPlayerOptions
            List<string> paths = new List<string>(); // Create new list for paths
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes) // Iterate scenes
                paths.Add(scene.path); // Add to path list
            string[] scenes = paths.ToArray(); // Convert to array
            buildPlayerOptions.scenes = scenes; // Set scenes
            buildPlayerOptions.locationPathName = path; // Set build path
            buildPlayerOptions.target = target; // Set target
            buildPlayerOptions.options = BuildOptions.Development; // Set options (development build)
            return buildPlayerOptions; // Return BuildPlayerOptions
        }
    }
}
