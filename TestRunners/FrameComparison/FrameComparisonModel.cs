using System;
using System.Collections.Generic;
using UnityEngine;

namespace GraphicsTestFramework
{
    // ------------------------------------------------------------------------------------
    // Results Data Structures

    // Structure for results
    [Serializable]
    public class FrameComparisonResults : ResultsBase
    {
        public string resultFrame;
    }

    // Structure for comparison
    [Serializable]
    public class FrameComparisonComparison : ComparisonBase
    {
        public float DiffPercentage;
        public Texture2D baselineTex;
        public Texture2D resultsTex;
    }

    // ------------------------------------------------------------------------------------
    // Settings Data Structures

    // Structure for settings
    [Serializable]
    public class FrameComparisonSettings : SettingsBase
    {
        [SerializeField] public Camera captureCamera; //Reference to the camera used to capture
        [SerializeField] public FrameResolution frameResolution; //Resolution of the frame capture
        [SerializeField] public TextureFormat textureFormat; //Format of the frame capture
        [SerializeField] public FilterMode filterMode; //Filter mode of the frame capture
        [SerializeField] public bool useBackBuffer; //Uses the backbuffer for cases where you need multi cam support

        public static FrameComparisonSettings defaultSettings
        {
            get
            {
                return new FrameComparisonSettings
                {
                    waitType = WaitType.Frames, // Type of measurement for waiting
                    waitFrames = 1, // Count of frames to wait before capture
                    passFailThreshold = 0.1f, // Threshold for comparison pass/fail
                    captureCamera = null, //Reference to the camera used to capture
                    frameResolution = FrameResolution.qHD, //Resolution of the frame capture
                    textureFormat = TextureFormat.RGB24, //Format of the frame capture
                    filterMode = FilterMode.Bilinear, //Filter mode of the frame capture
                    useBackBuffer = false, // Use alternatice capture method(multicam)
                };
            }
        }
    }

    // ------------------------------------------------------------------------------------
    // FrameComparisonModel
    // - Contains settings for FrameComparison

	[Serializable]
    public class FrameComparisonModel : TestModel<FrameComparisonLogic>
    {
        public Dictionary<FrameResolution, Vector2> resolutionList { get { return Common.frameResolutionList; } }

        // Exposed settings
        [SerializeField] FrameComparisonSettings m_Settings = FrameComparisonSettings.defaultSettings;

        // Set the exposed settings to the internal
        public override void SetSettings()
        {
            settings = m_Settings;
        }

		// Get/Set public settings
		public FrameComparisonSettings p_Settings
		{
			get
			{
				return m_Settings;
			}
			set 
			{
				m_Settings = value;
			}
		}
    }
}
