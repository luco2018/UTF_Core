using UnityEngine;
using UnityEngine.UI;

namespace GraphicsTestFramework
{
    // ------------------------------------------------------------------------------------
    // ResultsContext
    // - Abstract class for referencing gameobjects for a results menu context object
    // - Display scripts handle how to initialise this

    public class ResultsContext : MonoBehaviour
    {
        // UI References
        public Button viewTestButton;
        public GameObject[] objects;
        // Data
        ResultsEntry activeResultsEntry;

        // Setup the context object
        public void Setup(ResultsEntry inputResults)
        {
            activeResultsEntry = inputResults; // Track active results entry
            viewTestButton.onClick.AddListener(delegate { OnClickViewTest(); }); // Add listener for view test
        }

        // On button click: View Test
        public void OnClickViewTest()
        {
            if(TestRunner.Instance) // If test runner exists
            {
                if(!TestRunner.Instance.isAnalytic) // If not analytic
                    Menu.Instance.GenerateTestRunner(RunnerType.Results); // Generate blank runner
            }
            else
                Menu.Instance.GenerateTestRunner(RunnerType.Results); // Generate blank runner
            TestRunner.Instance.CallLoadSpecificTest(activeResultsEntry.resultsEntryData.testEntry); // Load specific test
            ResultsViewer.Instance.SetState(4); // Disable results viewer
        }
    }
}
