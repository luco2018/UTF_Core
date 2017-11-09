using System.Collections;
using UnityEngine;

namespace GraphicsTestFramework
{
    // ------------------------------------------------------------------------------------
    // ExampleLogic
    // - Serves only as example of logic custom setup

    public class ExampleLogic : TestLogic<ExampleModel, ExampleDisplay, ExampleResults, ExampleSettings> // Set types here for matching: < ModelType , DisplayType, ResultsType, SettingsType >
	{
        // ------------------------------------------------------------------------------------
        // Variables

        float timeWaited; // Used for example

        // ------------------------------------------------------------------------------------
        // Execution Overrides
        // 
        // Mandatory overrides:
        // - ProcessResult
        //
        // Mandatory methods:
        // - Process Comparison (TODO - Make this an override)
        //
        // Optional overrides:
        // - TestPreProcess
        // - TestPostProcess
        // 
        // These method calls are already wrapped in debugs and as such do not require debugs inside them
        // However, should you want to add further debugs please use Console.Write()

        // First injection point for custom code. Runs before any test logic.
        public override void TestPreProcess()
        {
            // Custom test pre-processing logic here
            StartTest(); // Start test (mandatory if overriding this method)
        }

        // Logic for creating results data
        public override IEnumerator ProcessResult()
		{
			var m_TempData = (ExampleResults)GetResultsStruct(); // Must get results struct and cast to this logics results type (mandatory)
            yield return WaitForTimer(); // Wait for timer
            var typedSettings = (ExampleSettings)model.settings; // Set settings to local type (mandatory)
            m_TempData = GetDummyData(m_TempData.common); // Just get some dummy data for the example (logic specific)
            if (baselineExists) // Comparison (mandatory)
            {
                ExampleResults referenceData = (ExampleResults)DeserializeResults(ResultsIO.Instance.RetrieveEntry(suiteName, testTypeName, m_TempData.common, true, true)); // Deserialize baseline data (mandatory)
                ExampleComparison comparisonData = (ExampleComparison)ProcessComparison(referenceData, m_TempData);  // Prrocess comparison (mandatory)
                if (comparisonData.SomeFloatDiff < model.settings.passFailThreshold)  // Pass/fail decision logic (logic specific)
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
            ExampleComparison newComparison = new ExampleComparison(); // Create new ComparisonData instance (mandatory)
            ExampleResults baselineDataTyped = (ExampleResults)baselineData;
            ExampleResults resultsDataTyped = (ExampleResults)resultsData;
            newComparison.SomeFloatDiff = resultsDataTyped.SomeFloat - baselineDataTyped.SomeFloat; // Perform comparison logic (logic specific)
            return newComparison; // Return (mandatory)
        }

        // Last injection point for custom code. Runs after all test logic.
        public override void TestPostProcess()
        {
            // Custom test post-processing logic here
            EndTest(); // End test (mandatory if overriding this method)
        }

        // ------------------------------------------------------------------------------------
        // Test Type Specific Methods

        // Just get some dummy result data for the example
        ExampleResults GetDummyData (ResultsDataCommon common)
        {
            ExampleResults output = new ExampleResults();
            output.common = common;
            output.SomeFloat = UnityEngine.Random.value;
            output.SomeInt = Mathf.RoundToInt(output.SomeFloat);
            return output;
        }
    }
}
