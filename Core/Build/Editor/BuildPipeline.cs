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
            if (config)
            {
                // Iterate build targets
                for (int t = 0; t < config.buildTargets.Count; t++)
                {
                    BuildTarget target = config.buildTargets[t]; // Get current target

                    // Get directory
                    string directory = UnityEngine.Application.dataPath.Replace("/Assets", "/Builds");
                    if (target.nameOverride != null && target.nameOverride.Length > 0)
                        directory = target.nameOverride;
                    else if (config.nameOverride != null && config.nameOverride.Length > 0)
                        directory = config.nameOverride;

                    string applicationName = GetApplicationName(target, projectSettings, config.nameOverride); // Get application name
                    string build = BuildClient(target, directory, applicationName); // Build

                    while (UnityEditor.BuildPipeline.isBuildingPlayer == true) // Wait for build to finish
                        yield return null; // Wait
                    if (build != "") // If build succeeded
                        Debug.Log("Built Player, Directory: " + directory + "/" + applicationName);
                    else // If build failed
                        Debug.LogError("Failed to build Player, Directory: " + directory + "/" + applicationName);
                }
            }
            else
                Debug.LogError("No Build Configuration file assigned. Please assign a Build Configuration file i nthe Build Pipeline window");
        }

        // Set Graphics API for the target
        public static void SetGraphicsAPI(BuildTarget target)
        {
            UnityEngine.Rendering.GraphicsDeviceType[] apis = new UnityEngine.Rendering.GraphicsDeviceType[1] { target.graphicsApi }; // Make array
            PlayerSettings.SetGraphicsAPIs(target.platform, apis); // Assign
        }

        // Get Application name
        public static string GetApplicationName(BuildTarget target, ProjectSettings projectSettings, string configNameOverride)
        {
            bool ignoreExtensions = false;
            string projectName = GetProjectName(projectSettings);
            string applicationName = "";
            if (target.nameOverride != null && target.nameOverride.Length > 0)
            {
                applicationName = target.nameOverride;
                ignoreExtensions = true;
            }
            else if (configNameOverride != null && configNameOverride.Length > 0)
                applicationName = configNameOverride;
            if (applicationName == "")
                applicationName = projectName;
            if (!ignoreExtensions)
                applicationName += "-" + target.platform.ToString() + "-" + target.graphicsApi.ToString();
            switch (target.platform)
            {
                case UnityEditor.BuildTarget.StandaloneWindows:
                    applicationName += ".exe";
                    break;
                case UnityEditor.BuildTarget.StandaloneWindows64:
                    applicationName += ".exe";
                    break;
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
                    applicationName += ".app";
                    break;
                case UnityEditor.BuildTarget.Android:
                    applicationName += ".api";
                    break;
            }
            return applicationName;
        }

        // Get the project name (to match Application Identifier in PlayerSettings)
        public static string GetProjectName(ProjectSettings projectSettings)
        {
            if (projectSettings)
            {
                if (projectSettings.buildNameOverride != null && projectSettings.buildNameOverride.Length > 0)
                {
                    return projectSettings.buildNameOverride;
                }
                else
                {
                    if (projectSettings.suiteList.Count == 0)
                        return "";
                    else if (projectSettings.suiteList.Count > 1)
                        return "UTF_Various";
                    else
                        return "UTF_" + projectSettings.suiteList[0].suiteName;
                }
            }
            else
                return "";
        }

        // Build the client
        public static string BuildClient(BuildTarget target, string directory, string applicationName)
        {
            if (!System.IO.Directory.Exists(directory + "/" + ""))
                System.IO.Directory.CreateDirectory(directory + "/" + applicationName);
            Debug.Log("Building Player, Directory: " + directory + "/" + applicationName);
            return UnityEditor.BuildPipeline.BuildPlayer(GetBuildSettings(target.platform, directory + "/" + applicationName));
        }

        // Get BuildSettings object
        static BuildPlayerOptions GetBuildSettings(UnityEditor.BuildTarget target, string path)
        {
            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
            List<string> paths = new List<string>();
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
                paths.Add(scene.path);
            string[] scenes = paths.ToArray();
            buildPlayerOptions.scenes = scenes;
            buildPlayerOptions.locationPathName = path;
            buildPlayerOptions.target = target;
            buildPlayerOptions.options = BuildOptions.None;
            return buildPlayerOptions;
        }
    }
}
