using UnityEngine.UI;

namespace GraphicsTestFramework
{
    // ------------------------------------------------------------------------------------
    // ExampleDisplay
    // - Controls context logic for TestViewer and ResultsViewer

    public class ExampleDisplay : TestDisplay<ExampleLogic>
    {
        // ------------------------------------------------------------------------------------
        // TestViewer

        // ------------------------------------------------------------------------------------
        // This sections controls customizing the TestViewer for this test type
        // 
        // Mandatory overrides:
        // - GetViewerTabs
        // This method lets you specify how many tabs you would like and what their content should be
        // Supports cameras, textures, materials etc
        // Each tab also can take an array of statistics
        // If this array is not null the statistics window will automatically populate
        //
        // These method calls are already wrapped in debugs and as such do not require debugs inside them
        // However, should you want to add further debugs please use Console.Write()

        // Setup viewer tabs
        public override TestViewerTabData[] GetViewerTabs(ResultsBase resultsObject)
        {
            TestViewerTabData[] output = new TestViewerTabData[1]; // Create empty output (mandatory)
            var localResultData = (ExampleResults)resultsObject; // Convert the input results object to this types class (mandatory)
            switch (logic.baselineExists) // Switch on baseline exists
            {
                case false:
                    output = new TestViewerTabData[1] // Only want one tab
                    {
                        new TestViewerTabData ("Live Camera", TestViewerTabType.DefaultCamera, null, new TestViewerTabData.TestViewerTabStatistic[1] // Set the tab to use the default camera
                            {
                                new TestViewerTabData.TestViewerTabStatistic("Average Frame Time", localResultData.SomeFloat.ToString()) // Enable the statistics window and display the float
                            })
                    };
                    break;
                case true:
                    var comparisonData = (ExampleComparison)logic.ProcessComparison(resultsObject); // Get the comparison data for this test in this types class (mandatory)
                    output = new TestViewerTabData[1] // Only want one tab
                    {
                        new TestViewerTabData ("Live Camera", TestViewerTabType.DefaultCamera, null, new TestViewerTabData.TestViewerTabStatistic[2] // Set the tab to use the default camera
                            {
                                new TestViewerTabData.TestViewerTabStatistic("Average Frame Time", localResultData.SomeFloat.ToString()), // Enable the statistics window and display the float
                                new TestViewerTabData.TestViewerTabStatistic("Delta", comparisonData.SomeFloatDiff.ToString()) // Also display the delta from the comparison
                            })
                    };
                    break;
            }
            return output; // Return the tabs
        }

        // ------------------------------------------------------------------------------------
        // ResultsViewer

        // ------------------------------------------------------------------------------------
        // This sections controls customizing the ResultsViewers context object for this test type
        // 
        // Mandatory overrides:
        // - SetupResultsContext
        // This method lets you specify the content of the results viewer context object
        // Input is an array of game object references
        // Context object contents are unique to the test type
        //
        // These method calls are already wrapped in debugs and as such do not require debugs inside them
        // However, should you want to add further debugs please use Console.Write()

        ExampleComparison comparisonData;

        // Setup the results context object
        public override void SetupResultsContext(ResultsContext context, ResultsIOData inputData, ResultsIOData inputDataB)
        {
            ExampleResults inputResults = (ExampleResults)logic.DeserializeResults(inputData); // Deserialize input and cast to typed results

            if(TestRunner.Instance)
            {
                if (!TestRunner.Instance.isAnalytic)
                    comparisonData = (ExampleComparison)logic.ProcessComparison(inputResults); // Get comparison data
                else
                {
                    ExampleResults inputResultsB = (ExampleResults)logic.DeserializeResults(inputDataB); // Deserialize input and cast to typed results
                    comparisonData = (ExampleComparison)logic.ProcessComparison(inputResultsB, inputResults);
                }
            }
            else
            {
                comparisonData = (ExampleComparison)logic.ProcessComparison(inputResults); // Get comparison data
            }

            context.objects[0].GetComponent<Text>().text = inputResults.SomeFloat.ToString(); // Set float
            context.objects[1].GetComponent<Text>().text = comparisonData.SomeFloatDiff.ToString(); // Set diff
        }
    }
}
