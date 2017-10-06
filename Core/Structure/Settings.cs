using System;
using System.Collections.Generic;
using UnityEngine;

namespace GraphicsTestFramework
{
    // ------------------------------------------------------------------------------------
    // Project Settings Scriptable Object
    // - Created automatically by Suite Manager during pre build

    //[CreateAssetMenu]
    public class ProjectSettings : ScriptableObject
    {
        [SerializeField]
        public string buildNameOverride;
        [HideInInspector] public string unityVersion;
        [HideInInspector] public string unityBranch;
        [Header("Defines")]
        [SerializeField]
        public string[] scriptingDefines;
        [Header ("Suites")]
        [SerializeField]
        public List<Suite> suiteList = new List<Suite>();
    }
}
