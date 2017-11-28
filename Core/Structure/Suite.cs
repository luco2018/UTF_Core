using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

namespace GraphicsTestFramework
{
    // ------------------------------------------------------------------------------------
    // Suite Scriptable Object
    // - One instance for each Suite entry
    // - Managed by SuiteController

    [CreateAssetMenu]
    public class Suite : ScriptableObject
    {
        [SerializeField] public string suiteName;
        [SerializeField] public bool isDebugSuite;
        [SerializeField] public TestSettings defaultTestSettings;
        [SerializeField] public RenderPipelineAsset defaultRenderPipeline;
        [SerializeField] public List<Group> groups = new List<Group>();
    }

    [Serializable]
    public class Group
    {
        [SerializeField] public string groupName;
        [SerializeField] public List<Test> tests = new List<Test>();
    }

    [Serializable]
    public class Test
    {
        [SerializeField] public UnityEngine.Object scene;
        [SerializeField] public string scenePath;
        [SerializeField] public int testTypes;
        [SerializeField] public int minimumUnityVersion;
        [SerializeField] public int platforms = -1;
        [SerializeField] public bool run = true;
        [SerializeField] public string caseID;//testrail caseID
    }
}
