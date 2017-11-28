using UnityEngine;
using UnityEngine.UI;

namespace GraphicsTestFramework
{
    // ------------------------------------------------------------------------------------
    // FrameComparisonDisplay
    // - Controls context logic for TestViewer and ResultsViewer

    public class FrameComparisonDisplay : TestDisplay<FrameComparisonLogic>
    {
        // ------------------------------------------------------------------------------------
        // Variables

        Material m_Material; // Comparison image material
        public Material material // Public get/set
        {
            get
            {
                if (m_Material == null)
                    m_Material = new Material(Shader.Find("Hidden/FrameComparison")) { hideFlags = HideFlags.DontSave };
                return m_Material;
            }
        }

        // ResultsViewer
        public RawImage resultsContextImage;
        public Button[] buttons;

        // ------------------------------------------------------------------------------------
        // TestViewer

        // Setup viewer tabs
        public override TestViewerTabData[] GetViewerTabs(ResultsBase resultsObject)
        {
            var typedSettings = (FrameComparisonSettings)logic.GetModel().settings; // Set settings to local type
            TestViewerTabData[] output = new TestViewerTabData[1]; // Create empty output (mandatory)
            switch (logic.baselineExists)  // Switch on baseline exists
            {
                case false:
                    var localResultData = (FrameComparisonResults)resultsObject; // Convert the input results object to this types class (mandatory)
                    output = new TestViewerTabData[2] // Want two tabs
                    {
                        new TestViewerTabData("Results Texture", TestViewerTabType.Texture, Common.ConvertStringToTexture("Tab_ResultsFrame", localResultData.resultFrame), null), // And the results texture
                        new TestViewerTabData("Live Camera", TestViewerTabType.Camera, typedSettings.captureCamera, null) // Live camera showing capture camera
                    };
                    break;
                case true:
                    var comparisonData = (FrameComparisonComparison)logic.ProcessComparison(resultsObject); // Get the comparison data for this test in this types class (mandatory)
                    SetupMaterial(comparisonData.baselineTex, comparisonData.resultsTex); // Setup the material
                    output = new TestViewerTabData[3] // Want three tabs
                    {
                        new TestViewerTabData("Results", TestViewerTabType.TextureSlider, new TestViewer.TextureSliderContext(comparisonData.resultsTex, "Result", comparisonData.baselineTex, "Baseline"), new TestViewerTabData.TestViewerTabStatistic[] // Create slider tab for results/baseline
                        {
                            new TestViewerTabData.TestViewerTabStatistic("Diff", comparisonData.DiffPercentage.ToString()) // Enable the statistics window and display the diff
                        }),
                        new TestViewerTabData("Comparison", TestViewerTabType.Material, material, new TestViewerTabData.TestViewerTabStatistic[] // And the material for the comparison display
                        {
                            new TestViewerTabData.TestViewerTabStatistic("Diff", comparisonData.DiffPercentage.ToString()) // Enable the statistics window and display the diff
                        }),
                        new TestViewerTabData("Live Camera", TestViewerTabType.Camera, typedSettings.captureCamera, null) // Live camera showing capture camera
                    };
                    break;
            }
            return output; // Return
        }

        // ------------------------------------------------------------------------------------
        // ResultsViewer

        FrameComparisonComparison comparisonData;

        // Setup the results context object
        public override void SetupResultsContext(ResultsContext context, ResultsIOData inputData, ResultsIOData inputDataB)
        {
            CleanupResultsContext();
            FrameComparisonResults inputResults = (FrameComparisonResults)logic.DeserializeResults(inputData); // Deserialize input and cast to typed results
            if(!TestRunner.Instance.isAnalytic)
                comparisonData = (FrameComparisonComparison)logic.ProcessComparison(inputResults); // Get comparison data
            else
            {
                FrameComparisonResults inputResultsB = (FrameComparisonResults)logic.DeserializeResults(inputDataB); // Deserialize input and cast to typed results
                comparisonData = (FrameComparisonComparison)logic.ProcessComparison(inputResultsB, inputResults);
            }
            buttons = new Button[3]; // Create button array
            for(int i = 0; i < buttons.Length; i++) // Iterate
            { 
                buttons[i] = context.objects[i].GetComponent<Button>(); // Get the button
                int index = i; // Set this so delegate doesnt get reference that iterates
                buttons[i].onClick.AddListener(delegate { SetTextureContext(comparisonData, index); }); // Add listener
            }
            resultsContextImage = context.objects[3].GetComponent<RawImage>(); // Get image
            context.objects[4].GetComponent<Text>().text = comparisonData.DiffPercentage.ToString(); // Set diff to field
            SetTextureContext(comparisonData, 0); // Set default
        }

        // Set context for textures
        public void SetTextureContext(FrameComparisonComparison comparisonData, int context)
        {
            foreach (Button b in buttons) // Iterate buttons
                b.interactable = true; // Enable
            buttons[context].interactable = false; // Disable requested
            switch (context)
            {
                case 0:    // Results
                    resultsContextImage.material = null; // Null material
                    resultsContextImage.texture = comparisonData.resultsTex; // Set texture
                    break;
                case 1:     // Comparison
                    SetupMaterial(comparisonData.baselineTex, comparisonData.resultsTex); // Setup material
                    resultsContextImage.texture = comparisonData.resultsTex; // Set texture
                    resultsContextImage.material = material; // Set material
                    break;
                case 2:     // Baseline
                    resultsContextImage.material = null; // Null material
                    resultsContextImage.texture = comparisonData.baselineTex; // Set texture
                    break;
            }
        }

        public void CleanupResultsContext()
        {
            comparisonData = null;
            if(resultsContextImage)
            {
                Texture tex = resultsContextImage.texture;
                if (tex)
                    Destroy(tex);
            }
        }

        // ------------------------------------------------------------------------------------
        // Test Type Specific Methods

        // Setup comparison material
        void SetupMaterial(Texture2D baselineTex, Texture2D resultsTex)
        {
            material.SetTexture("_ReferenceTex", baselineTex); // Set baseline texture
            material.SetTexture("_MainTex", resultsTex); // Set results texture
        }
    }
}
