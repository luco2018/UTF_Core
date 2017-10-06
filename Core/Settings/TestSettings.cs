using System;
using System.Collections.Generic;
using UnityEngine;

namespace GraphicsTestFramework
{
    // ------------------------------------------------------------------------------------
    // Test Settings Scriptable Object
    // - Used to set project settings per test

    [CreateAssetMenu]
    public class TestSettings : ScriptableObject
    {
        // Labels
        [HideInInspector]
        public string[] antiAliasingNames = new string[4] { "Disabled", "2x Multi Sampling", "4x Multi Sampling", "8x Multi Sampling" };
        [HideInInspector]
        public string[] shadowCascadeNames = new string[3] { "None", "Two Cascades", "Four Cascades" };
        [HideInInspector]
        public string[] vSyncCountNames = new string[3] { "Dont Sync", "Every V Blank", "Every Second V Sync" };

        [Header("Rendering")]
        [SerializeField]
        public int pixelLightCount = 4;
        [SerializeField]
        public AnisotropicFiltering anisotropicFiltering = AnisotropicFiltering.ForceEnable;
        [SerializeField]
        public int antiAliasing = 0;
        [SerializeField]
        public bool softParticles = true;
        [SerializeField]
        public bool realtimeReflectionProbes = true;
        [SerializeField]
        public bool billboardsFacingCameraPosition = true;

        [Header("Shadows")]
        [SerializeField]
        public ShadowQuality shadows = ShadowQuality.All;
        [SerializeField]
        public ShadowResolution shadowResolution = ShadowResolution.High;
        [SerializeField]
        public ShadowProjection shadowProjection = ShadowProjection.StableFit;
        [SerializeField]
        public float shadowDistance = 150f;
        [SerializeField]
        public float shadowNearPlaneOffset = 3f;
        [SerializeField]
        public int shadowCascades = 2;
        [SerializeField]
        public float shadowCascade2Split = .33f;
        [SerializeField]
        public Vector3 shadowCascade4Split = new Vector3(.067f, .2f, .467f);

        [Header("Other")]
        [SerializeField]
        public BlendWeights blendWeights = BlendWeights.FourBones;
        [SerializeField]
        public int vSyncCount = 0;
        [SerializeField]
        public float lodBias = 2f;
        [SerializeField]
        public int maximumLodLevel = 0;
        [SerializeField]
        public int particleRaycastBudget = 4096;
        [SerializeField]
        public int asyncUploadTimeSlice = 2;
        [SerializeField]
        public int asyncUploadBufferSize = 4;
    }
}
