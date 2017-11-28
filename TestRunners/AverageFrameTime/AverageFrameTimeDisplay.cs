using UnityEngine.UI;

namespace GraphicsTestFramework
{
    // ------------------------------------------------------------------------------------
    // AverageFrameTimeDisplay
    // - Controls context logic for TestViewer and ResultsViewer

    public class AverageFrameTimeDisplay : TestDisplay<AverageFrameTimeLogic>
    {
        // ------------------------------------------------------------------------------------
        // TestViewer

        // Setup viewer tabs
        public override TestViewerTabData[] GetViewerTabs(ResultsBase resultsObject)
        {
            TestViewerTabData[] output = new TestViewerTabData[1]; // Create empty output (mandatory)
            var localResultData = (AverageFrameTimeResults)resultsObject; // Convert the input results object to this types class (mandatory)
            switch (logic.baselineExists) // Switch on baseline exists
            {
                case false:
                    output = new TestViewerTabData[1] // Only want one tab
                    {
                        new TestViewerTabData ("Live Camera", TestViewerTabType.DefaultCamera, null, new TestViewerTabData.TestViewerTabStatistic[1] // Set the tab to use the default camera
                            {
                                new TestViewerTabData.TestViewerTabStatistic("Average Frame Time", localResultData.avgFrameTime.ToString()) // Enable the statistics window and display the avg frame time
                            })
                    };
                    break;
                case true:
                    var comparisonData = (AverageFrameTimeComparison)logic.ProcessComparison(resultsObject); // Get the comparison data for this test in this types class (mandatory)
                    output = new TestViewerTabData[1] // Only want one tab
                    {
                        new TestViewerTabData ("Live Camera", TestViewerTabType.DefaultCamera, null, new TestViewerTabData.TestViewerTabStatistic[2] // Set the tab to use the default camera
                            {
                                new TestViewerTabData.TestViewerTabStatistic("Average Frame Time", localResultData.avgFrameTime.ToString()), // Enable the statistics window and display the avg frame time
                                new TestViewerTabData.TestViewerTabStatistic("Delta", comparisonData.delta.ToString()) // Also display the delta from the comparison
                            })
                    };
                    break;
            }
            return output; // Return the tabs
        }

        // ------------------------------------------------------------------------------------
        // ResultsViewer

        AverageFrameTimeComparison comparisonData;

        // Setup the results context object
        public override void SetupResultsContext(ResultsContext context, ResultsIOData inputData, ResultsIOData inputDataB)
        {
            AverageFrameTimeResults inputResults = (AverageFrameTimeResults)logic.DeserializeResults(inputData); // Deserialize input and cast to typed results

            if(!TestRunner.Instance.isAnalytic)
                comparisonData = (AverageFrameTimeComparison)logic.ProcessComparison(inputResults); // Get comparison data
            else
            {
                AverageFrameTimeResults inputResultsB = (AverageFrameTimeResults)logic.DeserializeResults(inputDataB); // Deserialize input and cast to typed results
                comparisonData = (AverageFrameTimeComparison)logic.ProcessComparison(inputResultsB, inputResults);
            }

            context.objects[0].GetComponent<Text>().text = inputResults.avgFrameTime.ToString(); // Set average frame time
            context.objects[1].GetComponent<Text>().text = comparisonData.delta.ToString(); // Set delta
        }
    }
}
