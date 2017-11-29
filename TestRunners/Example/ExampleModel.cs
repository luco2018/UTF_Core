using System;
using UnityEngine;

namespace GraphicsTestFramework
{
    // ------------------------------------------------------------------------------------
    // Results Data Structures

    // Structure for results (Inherits from ResultsBase. Class contents can be anything)
    [Serializable]
    public class ExampleResults : ResultsBase
    {
        public float SomeFloat; // Just some example data. Well use this for comparison.
        public int SomeInt; // Just some more example data (can save most types, including arrays)
    }

    // Structure for comparison results (Do not rename class. Class contents can be anything)
    [Serializable]
    public class ExampleComparison : ComparisonBase
    {
        public float SomeFloatDiff; // Just some example data. Well use this for comparison.
    }

    // ------------------------------------------------------------------------------------
    // ExampleSettings

    // - Structure for settings 
    // - Inherits from SettingsBase
    // - Can contain any other settings needed fro the test type
    // - Should also contain a constructor for default settings
    [Serializable]
    public class ExampleSettings : SettingsBase
    {
        // Insert custom settings here

        public static ExampleSettings defaultSettings
        {
            get
            {
                return new ExampleSettings
                {
                    waitType = WaitType.Seconds, // Type of measurement for waiting
                    waitSeconds = 0f, // Count of frames or seconds to wait before capture
                    passFailThreshold = 0.1f // Threshold for comparison pass/fail
                };
            }
        }
    }

    // ------------------------------------------------------------------------------------
    // ExampleModel
    // - Contains settings for Example

    public class ExampleModel : TestModel<ExampleLogic>
    {
        // Exposed settings
        [SerializeField]
        ExampleSettings m_Settings = ExampleSettings.defaultSettings;

        // Set the exposed settings to the internal
        public override void SetSettings()
        {
            settings = m_Settings;
        }

		// Get/Set public settings
		public ExampleSettings p_Settings
		{
			get
			{
				return m_Settings;
			}
		}

    }
}
