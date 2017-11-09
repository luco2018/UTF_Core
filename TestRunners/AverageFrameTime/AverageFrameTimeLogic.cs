using System.Collections;
using UnityEngine;

namespace GraphicsTestFramework
{
    // ------------------------------------------------------------------------------------
    // AverageFrameTimeLogic
    // - Results: Samples a number of frames and returns an average
    // - Comparison: Compares average frame time to baseline

    public class AverageFrameTimeLogic : TestLogic<AverageFrameTimeModel, AverageFrameTimeDisplay, AverageFrameTimeResults, AverageFrameTimeSettings, AverageFrameTimeComparison>
	{
        // ------------------------------------------------------------------------------------
        // Variables

        float time;
		int samples;

        // ------------------------------------------------------------------------------------
        // Execution Overrides

        // Logic for creating results data (mandatory override)
        public override IEnumerator ProcessResult()
        {
            var m_TempData = (AverageFrameTimeResults)GetResultsStruct(); // Get a results struct (mandatory)
            var typedSettings = (AverageFrameTimeSettings)model.settings; // Set settings to local type
            yield return WaitForTimer(); // Wait for timer
            Timestamp(false); // Perform a timestamp (logic specific)
            for (int i = 0; i < typedSettings.sampleFrames; i++) // Wait for requested sample frame count (logic specific)
                yield return new WaitForEndOfFrame();
			m_TempData.avgFrameTime = Timestamp(true); // Perform a timestamp (logic specific)
            if (baselineExists) // Comparison (mandatory)
            {
                AverageFrameTimeResults referenceData = (AverageFrameTimeResults)DeserializeResults(ResultsIO.Instance.RetrieveEntry(suiteName, testTypeName, m_TempData.common, true, true)); // Deserialize baseline data (mandatory)
                AverageFrameTimeComparison comparisonData = (AverageFrameTimeComparison)ProcessComparison(referenceData, m_TempData);  // Process comparison (mandatory)
                if (comparisonData.delta < model.settings.passFailThreshold)  // Pass/fail decision logic (logic specific)
                    m_TempData.common.PassFail = true;
                else
                    m_TempData.common.PassFail = false;
                comparisonData = null;  // Null comparison (mandatory)
            }
            BuildResultsStruct(m_TempData); // Submit (mandatory)
        }

        // Logic for comparison process (mandatory)
        // TODO - Will use last run test model, need to get this for every call from Viewers?
        public override object ProcessComparison(ResultsBase baselineData, ResultsBase resultsData)
        {
            AverageFrameTimeComparison newComparison = new AverageFrameTimeComparison(); // Create new ComparisonData instance (mandatory)
            AverageFrameTimeResults baselineDataTyped = (AverageFrameTimeResults)baselineData; // Set baseline data to local type
            AverageFrameTimeResults resultsDataTyped = (AverageFrameTimeResults)resultsData; // Set results data to local type
            newComparison.delta = resultsDataTyped.avgFrameTime - baselineDataTyped.avgFrameTime; // Perform comparison logic (logic specific)
            return newComparison; // Return (mandatory)
        }

        // ------------------------------------------------------------------------------------
        // Test Type Specific Methods

        // Calculate time and frames since last Timestamp and return an average
        float Timestamp(bool debug)
		{
			float multiplier = 1; // Create a multiplier
            var typedSettings = (AverageFrameTimeSettings)model.settings; // Set settings to local type
			switch(typedSettings.timingType) // Set multiplier based on model settings
			{
				case AverageFrameTimeSettings.TimingType.Seconds:
					multiplier = 1;
					break;
				case AverageFrameTimeSettings.TimingType.Milliseconds:
					multiplier = 1000;
					break;
				case AverageFrameTimeSettings.TimingType.Ticks:
					multiplier = 10000000;
					break;
				case AverageFrameTimeSettings.TimingType.Custom:
					multiplier = typedSettings.customTimingMultiplier;
					break;
			}
			float currentTime = Time.realtimeSinceStartup * multiplier; // Get current time
			int currentSamples = Time.frameCount; // Get current samples
			float elapsedTime = currentTime - time; // Get elapsed time since last Timestamp
			int elapsedSamples = currentSamples - samples; // Get elapsed samples since last Timestamp
			time = currentTime; // Reset time
			samples = currentSamples; // Reset samples
            Console.Instance.Write(DebugLevel.Logic, MessageLevel.Log, this.GetType().Name + " completed test with " + elapsedSamples + " samples with average frametime of " + elapsedTime / (float)elapsedSamples); // Write to console
			return elapsedTime / (float)elapsedSamples; // Return
		}
    }
}
