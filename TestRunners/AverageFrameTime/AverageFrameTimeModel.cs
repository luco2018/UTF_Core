using System;
using UnityEngine;

namespace GraphicsTestFramework
{
    // ------------------------------------------------------------------------------------
    // Results Data Structures

    // Structure for results
    [Serializable]
    public class AverageFrameTimeResults : ResultsBase
    {
        public float avgFrameTime;
    }

    // Structure for comparison
    [Serializable]
    public class AverageFrameTimeComparison : ComparisonBase
    {
        public float delta;
    }

    // ------------------------------------------------------------------------------------
    // Settings Data Structures

    // Structure for settings
    [Serializable]
    public class AverageFrameTimeSettings : SettingsBase
    {
        // Local Enums
        public enum TimingType { Seconds, Milliseconds, Ticks, Custom }

        public TimingType timingType; // Timing multiplier
        public float customTimingMultiplier; // Custom timing multiplier
        public int sampleFrames; // Amount of frames to sample

        public static AverageFrameTimeSettings defaultSettings
        {
            get
            {
                return new AverageFrameTimeSettings
                {
                    waitType = WaitType.StableFramerate, // Type of measurement for waiting
                    waitSeconds = 0f, // Count of seconds to wait before capture
                    passFailThreshold = 1f, // Threshold for comparison pass/fail
                    timingType = TimingType.Milliseconds, // Timing multiplier
                    customTimingMultiplier = 1f, // Custom timing multiplier
                    sampleFrames = 64 // Amount of frames to sample
                };
            }
        }
    }

    // ------------------------------------------------------------------------------------
    // AverageFrameTimeModel
    // - Contains settings for AverageFrameTime

    public class AverageFrameTimeModel : TestModel<AverageFrameTimeLogic> 
	{
        // Exposed settings
        [SerializeField]
        AverageFrameTimeSettings m_Settings = AverageFrameTimeSettings.defaultSettings;

        // Set the exposed settings to the internal
        public override void SetSettings()
        {
            settings = m_Settings;
        }

		// Get/Set public settings
		public AverageFrameTimeSettings p_Settings
		{
			get
			{
				return m_Settings;
			}
		}

    }
}
