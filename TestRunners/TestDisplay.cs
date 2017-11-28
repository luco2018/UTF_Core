using UnityEngine;

namespace GraphicsTestFramework
{
    // ------------------------------------------------------------------------------------
    // TestDisplayBase
    // - Lowest level TestDisplay class that all displays derive from
    // - Hides most logic away from end user

    public abstract class TestDisplayBase : MonoBehaviour
    {
        // ------------------------------------------------------------------------------------
        // Variables

        public GameObject resultsContextPrefab; // Reference to prefab for results screen context dropdown

        // ------------------------------------------------------------------------------------
        // Initialization

        // Set test logic instance
        public abstract void SetLogic(TestLogicBase inputLogic);

        // ------------------------------------------------------------------------------------
        // TestViewer

        // Return default camera tab only
        public virtual TestViewerTabData[] GetViewerTabs(ResultsBase resultsObject)
        {
            return new TestViewerTabData[1] { new TestViewerTabData("Default", TestViewerTabType.DefaultCamera, null, null) };
        }

        // Enable test viewer
        public virtual void EnableTestViewer(ResultsBase resultsObject, TestViewerToolbar.State toolbarState) { }

        // Get time since results
        public string GetResultsTimeDisplay(ResultsBase resultsObject)
        {
            string output = ""; // Create output
            System.DateTime resultTime = System.DateTime.Parse(resultsObject.common.DateTime); // Parse result time
            System.DateTime now = System.DateTime.UtcNow; // Get time now
            Common.TimePeriod period = Common.TimePeriod.Closest; // Create time period
            float result = Common.GetTimeDifference(resultTime, now, ref period); // Get time difference
            if (period == Common.TimePeriod.Second) // If seconds dont return details
                output = "just now"; // Set output
            else
                output = result.ToString() + " " + period.ToString().ToLower() + "s ago"; // Set output
            return "Results created "+output; // Return
        }

        // ------------------------------------------------------------------------------------
        // ResultsViewer

        // Get reference to results context prefab
        public void GetResultsContextObject()
        {
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, this.GetType().Name + " getting results context object prefab"); // Write to console
            string typeName = this.GetType().ToString().Replace("GraphicsTestFramework.", "").Replace("Display", ""); // Get type name
            typeName = "ResultsContext_" + typeName; // Append prefix
            resultsContextPrefab = (GameObject)Resources.Load(typeName); // Load prefab from resources
        }

        // Setup the results context entry
        public abstract void SetupResultsContext(ResultsContext contextObject, ResultsIOData inputData, ResultsIOData inputDataB);
    }

    // ------------------------------------------------------------------------------------
    // TestDisplay
    // - Next level TestDisplay class that all user facing displays derive from
    // - Adds an abstraction layer for defining logic type

    public abstract class TestDisplay<L> : TestDisplayBase where L : TestLogicBase
    {
        // ------------------------------------------------------------------------------------
        // Variables

        public L logic { get; set; } // Reference to the tests logic

        // ------------------------------------------------------------------------------------
        // Set Methods

        // Set test logic instance
        public override void SetLogic(TestLogicBase inputLogic)
        {
            logic = (L)inputLogic; // Cast to type and set
        }

        // Enable test viewer
        public override void EnableTestViewer(ResultsBase resultsObject, TestViewerToolbar.State toolbarState)
        {
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, this.GetType().Name + " enabling test viewer"); // Write to console
            string breadcrumbLabel = logic.suiteName + " - " + logic.testTypeName + " - " + resultsObject.common.GroupName + " - " + resultsObject.common.TestName; // Build breadcrumb label
            ProgressScreen.Instance.SetState(false, ProgressType.LocalSave, ""); // Disable ProgressScreen
            TestViewer.Instance.SetState(true); // Set test viewer state
            TestViewer.Instance.UpdateBars(GetViewerTabs(resultsObject), GetResultsTimeDisplay(resultsObject), breadcrumbLabel, toolbarState); // Set test viewer state
        }
    }
}
