using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace GraphicsTestFramework
{
    // ------------------------------------------------------------------------------------
    // Test Settings Scriptable Object
    // - Used to set project settings per test

    [CreateAssetMenu]
    public class BuildConfiguration : ScriptableObject
    {
        [Header("Override parameters")]
        public string nameOverride;
        public string pathOverride;
        [Header("Build Parameters")]
        public bool enableScriptLogging;
        [SerializeField]
        public BuildOptions buildOption = BuildOptions.Development;

        [Header("Build profiles")]
        public List<BuildTarget> buildTargets = new List<BuildTarget>();
    }

    [Serializable]
    public class BuildTarget
    {
        public string nameOverride;
        public string pathOverride;
        public UnityEditor.BuildTarget platform;
        public UnityEngine.Rendering.GraphicsDeviceType graphicsApi;
    }
}
