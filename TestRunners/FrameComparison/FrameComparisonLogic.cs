using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace GraphicsTestFramework
{
    // ------------------------------------------------------------------------------------
    // FrameComparisonLogic
    // - Results: Captures a screenshot from models test camera
    // - Comparison: Gets average value of pixel differences between results screenshot and baseline

    public class FrameComparisonLogic : TestLogic<FrameComparisonModel, FrameComparisonDisplay, FrameComparisonResults, FrameComparisonSettings, FrameComparisonComparison>
    {
        // ------------------------------------------------------------------------------------
        // Variables

        Camera dummyCamera;
        Camera menuCamera;
        RenderTexture temporaryRt;
        Texture2D resultsTexture;
        bool doCapture;     

        // ------------------------------------------------------------------------------------
        // Execution Overrides

        // Manage dummy camera when logic is initialized
        public override void SetupLogic()
        {
            if (dummyCamera == null) // Dummy camera isnt initialized
                dummyCamera = this.gameObject.AddComponent<Camera>(); // Create camera component
            dummyCamera.enabled = false; // Disable dummy camera
            if (menuCamera == null)
                menuCamera = GameObject.FindWithTag("Player").GetComponent<Camera>(); // Get the main menu camera
        }

        // First injection point for custom code. Runs before any test logic (optional override)
        // - Set up cameras and create RenderTexture
        public override void TestPreProcess()
        {
            var typedSettings = (FrameComparisonSettings)model.settings; // Set settings to local type
            Vector2 resolution = Vector2.zero; // Create vector2
            model.resolutionList.TryGetValue(typedSettings.frameResolution, out resolution); // Get resolution
            temporaryRt = new RenderTexture((int)resolution.x, (int)resolution.y, 24); // Get a temporary RenderTexture for blit operations
            SetupCameras(); // Setup cameras
            StartTest(); // Start test
        }

        // Logic for creating results data (mandatory override)
        public override IEnumerator ProcessResult()
        {
            var typedSettings = (FrameComparisonSettings)model.settings; // Set settings to local type
            if (!typedSettings.useBackBuffer)
            {
                typedSettings.captureCamera.targetTexture = temporaryRt; // Set capture cameras target texture to temporary RT (logic specific)
                dummyCamera.enabled = true;
            }
            else
            {
                ProgressScreen.Instance.progressObject.SetActive(false); // Hide the UI breifly to do the capture
                menuCamera.enabled = false;
            }
            var m_TempData = (FrameComparisonResults)GetResultsStruct(); // Get a results struct (mandatory)
            yield return WaitForTimer(); // Wait for timer
            if (typedSettings.useBackBuffer)
            {
                if(Debug.developerConsoleVisible)
                {
                    Debug.ClearDeveloperConsole(); // Clear the dev console if it's visible before capturing the backbuffer.
                }
                BackBufferCapture();
            }
            else
                doCapture = true; // Perform OnRenderImage logic (logic specific)
            do { yield return null; } while (resultsTexture == null); // Wait for OnRenderImage logic to complete (logic specific)
            m_TempData.resultFrame = Common.ConvertTextureToString(resultsTexture, typedSettings.imageQuality); // Convert results texture to Base64 String and save to results data
            if (baselineExists) // Comparison (mandatory)
            {
                AltBaselineSettings altBaselineSettings = Master.Instance.GetCurrentPlatformAPI(); // current chosen API/plafrom
                ResultsDataCommon m_BaselineData = m_TempData.common.SwitchPlatformAPI(altBaselineSettings.Platform, altBaselineSettings.API); // makes new ResultsDataCommon to grab baseline
                FrameComparisonResults referenceData = (FrameComparisonResults)DeserializeResults(ResultsIO.Instance.RetrieveEntry(suiteName, testTypeName, m_BaselineData, true, true)); // Deserialize baseline data (mandatory)
                m_TempData.common.PassFail = GetComparisonResult(m_TempData, referenceData); // Get comparison results
            }
            if (typedSettings.useBackBuffer)
            {
                ProgressScreen.Instance.progressObject.SetActive(true); // Show progress screen again
                menuCamera.enabled = true;
            }
            else
            {
                dummyCamera.enabled = false;
            }
            Cleanup(); // Cleanup (logic specific)
            BuildResultsStruct(m_TempData); // Submit (mandatory)
        }

        // Last injection point for custom code. Runs after all test logic.
        // - Disable camera
        public override void TestPostProcess()
        {
            dummyCamera.enabled = false; // Disable dummy camera
            EndTest(); // End test
        }

        // Get a comparison result from any given result and baseline
        public override bool GetComparisonResult(ResultsBase results, ResultsBase baseline)
        {
            FrameComparisonComparison comparisonData = (FrameComparisonComparison)ProcessComparison(baseline, results);  // Prrocess comparison (mandatory)
            bool output = false;
            if (comparisonData.DiffPercentage < model.settings.passFailThreshold)  // Pass/fail decision logic (logic specific)
                output = true;
            else
                output = false;
            comparisonData = null;  // Null comparison (mandatory)
            return output;
        }

        // Logic for comparison process (mandatory)
        // TODO - Will use last run test model, need to get this for every call from Viewers?
        public override object ProcessComparison(ResultsBase baselineData, ResultsBase resultsData)
        {
            FrameComparisonComparison newComparison = new FrameComparisonComparison(); // Create new ComparisonData instance (mandatory)
            FrameComparisonResults baselineDataTyped = (FrameComparisonResults)baselineData; // Set baseline data to local type
            FrameComparisonResults resultsDataTyped = (FrameComparisonResults)resultsData; // Set results data to local type
            newComparison.baselineTex = Common.ConvertStringToTexture(resultsDataTyped.common.TestName + "_Reference", baselineDataTyped.resultFrame); // Convert baseline frame to Texture2D (logic specific)
            newComparison.resultsTex = Common.ConvertStringToTexture(resultsDataTyped.common.TestName + "_Results", resultsDataTyped.resultFrame); // Convert result frame to Texture2D (logic specific)
            newComparison.DiffPercentage = Common.GetTextureComparisonValue(newComparison.baselineTex, newComparison.resultsTex); // Calculate diff percentage (logic specific)
            return newComparison; // Return (mandatory)
        }

        // ------------------------------------------------------------------------------------
        // Test Type Specific Methods

        // Called on render(legacy pipeline)
        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (!GraphicsSettings.renderPipelineAsset)
            {
                Graphics.Blit(source, destination); // Blit source to destination for Deferred
                if (doCapture) // If running blit operations
                {
                    doCapture = false; // Reset
                    var typedSettings = (FrameComparisonSettings)model.settings; // Set settings to local type
                    Vector2 resolution = Vector2.zero; // Create vector2
                    model.resolutionList.TryGetValue(typedSettings.frameResolution, out resolution); // Get resolution
                    var rt1 = RenderTexture.GetTemporary((int)resolution.x, (int)resolution.y, 24, temporaryRt.format, RenderTextureReadWrite.sRGB); // Get a temporary RT for blitting to
                    Graphics.Blit(temporaryRt, rt1); // Blit models camera to the RT
                    resultsTexture = Common.ConvertRenderTextureToTexture2D(activeTestEntry.testName + "_Result", rt1, resolution, typedSettings.textureFormat, typedSettings.filterMode); // Convert the resulting render texture to a Texture2D
                    typedSettings.captureCamera.targetTexture = null; // Set target texture to null
                    RenderTexture.ReleaseTemporary(rt1); // Release the temporary RT
                    temporaryRt.Release(); // Release main RT
                    Console.Instance.Write(DebugLevel.Logic, MessageLevel.Log, this.GetType().Name + " completed blit operations for test " + activeTestEntry.testName); // Write to console
                }
            }
        }

        public override void SRPBeginCamera(Camera cam)
        {
            if (doCapture) // If running blit operations
            {
                var typedSettings = (FrameComparisonSettings)model.settings; // Set settings to local type
                if(cam == typedSettings.captureCamera)
                    return;
                doCapture = false; // Reset
                Vector2 resolution = Vector2.zero; // Create vector2
                model.resolutionList.TryGetValue(typedSettings.frameResolution, out resolution); // Get resolution
                var rt1 = RenderTexture.GetTemporary((int)resolution.x, (int)resolution.y, 24, temporaryRt.format, RenderTextureReadWrite.sRGB); // Get a temporary RT for blitting to
                Graphics.Blit(temporaryRt, rt1); // Blit models camera to the RT
                resultsTexture = Common.ConvertRenderTextureToTexture2D(activeTestEntry.testName + "_Result", rt1, resolution, typedSettings.textureFormat, typedSettings.filterMode); // Convert the resulting render texture to a Texture2D
                typedSettings.captureCamera.targetTexture = null; // Set target texture to null
                RenderTexture.ReleaseTemporary(rt1); // Release the temporary RT
                temporaryRt.Release(); // Release main RT
                Console.Instance.Write(DebugLevel.Logic, MessageLevel.Log, this.GetType().Name + " completed blit operations for test " + activeTestEntry.testName); // Write to console
            }
        }

        // Backbuffer capture
        private void BackBufferCapture()
        {
            doCapture = false; // Reset
            var typedSettings = (FrameComparisonSettings)model.settings; // Set settings to local type
            Vector2 resolution = Vector2.zero; // Create vector2
            model.resolutionList.TryGetValue(typedSettings.frameResolution, out resolution); // Get resolution

            Vector2 screenRes = new Vector2(Screen.width, Screen.height); // Grab the resolution of the screen
            Texture2D tex = new Texture2D((int)screenRes.x, (int)screenRes.y, typedSettings.textureFormat, false); // new texture to fill sized to the screen
            tex.ReadPixels(new Rect(0, 0, (int)screenRes.x, (int)screenRes.y), 0, 0, false); // grab screen pixels
            tex = Common.ResizeInto(tex, (int)resolution.x, (int)resolution.y);
            resultsTexture = tex; // Set the results texture
            Console.Instance.Write(DebugLevel.Logic, MessageLevel.Log, this.GetType().Name + " completed screen read operation for test " + activeTestEntry.testName); // Write to console
        }

        // Prepare cameras for capture
        void SetupCameras()
        {
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, this.GetType().Name + " is setting up cameras"); // Write to console
            var typedSettings = (FrameComparisonSettings)model.settings; // Set settings to local type
            if (dummyCamera == null) // Dummy camera isnt initialized
                dummyCamera = this.gameObject.AddComponent<Camera>(); // Create camera component
            if (typedSettings.captureCamera == null) // If no capture camera
            {
                FrameComparisonSettings settings = typedSettings; // Clone the settings
                settings.captureCamera = Camera.main; // Attempt to set capture camera to main
                if (settings.captureCamera == null) // If no main camera found
                {
                    Camera[] cams = FindObjectsOfType<Camera>(); // Find all cameras
                    settings.captureCamera = cams[cams.Length - 1]; // Set to last in found array so avoid setting to UI or dummy cameras
                }
                if (settings.captureCamera == null) // If still not found
                {
                    dummyCamera.enabled = true; // Enable dummy camera
                    settings.captureCamera = dummyCamera; // Set to dummy camera as fallback
                    Console.Instance.Write(DebugLevel.Critical, MessageLevel.LogWarning, "Frame Comparison test found no camera inside test " + activeTestEntry.testName); // Write to console
                }
                model.settings = settings; // Set settings back
            }

            if (menuCamera == null)
                menuCamera = GameObject.FindWithTag("Player").GetComponent<Camera>(); // Get the main menu camera if not already 
        }

        // Cleanup cameras after test finishes
        void Cleanup()
        {
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, this.GetType().Name + " is cleaning up"); // Write to console
            resultsTexture = null; // Null
        }
    }
}
