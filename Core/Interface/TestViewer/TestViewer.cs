using System;
using UnityEngine;
using UnityEngine.UI;

namespace GraphicsTestFramework
{
    // ------------------------------------------------------------------------------------
    // TestViewer
    // - The main TestViewer controller
    // - Controls main viewer context
    // - Higher level control of Tool and Nav bars

    public class TestViewer : MonoBehaviour 
	{
        // ------------------------------------------------------------------------------------
        // Variables

        // Singleton
        private static TestViewer _Instance = null;
        public static TestViewer Instance
        {
            get
            {
                if (_Instance == null)
                    _Instance = (TestViewer)FindObjectOfType(typeof(TestViewer));
                return _Instance;
            }
        }

        // Data
        public GameObject testViewerParent; // Object to enable/disable
        public RawImage textureImage; // Viewer texture display
        public TestViewerSlider testViewerSlider; // Slider for TextureSlider and MaterialSlider tab types
        Camera currentCamera; // Tacking active high depth camera
        float cameraDepth; // Tracking cameras previous depth

        // ------------------------------------------------------------------------------------
        // Core

        private void Start()
        {     
            Menu.Instance.MakeCanvasScaleWithScreenSize(testViewerParent);
        }
        
        // ------------------------------------------------------------------------------------
        // Content & State

        // Enable or disable the viewer
        public void SetState(bool state)
        {
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Setting state to "+state); // Write to console
            ProgressScreen.Instance.SetState(false, ProgressType.LocalLoad, ""); // Disable ProgressScreen
            testViewerParent.SetActive(state); // Set state
        }
        
        // Update the viewer tool bar and nav bar context based on the active TestRunner RunnerType
        public void UpdateBars(TestViewerTabData[] tabDatas, string updateTime, string breadcrumbLabel, TestViewerToolbar.State toolbarState)
        {
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Updating bars"); // Write to console
            TestViewerToolbar.Instance.SetContext(toolbarState); // Set toolbar context mode
            TestViewerNavbar.Instance.Generate(breadcrumbLabel, tabDatas, updateTime); // Update navigation bar
        }

        // Sets the viewers content
        public void SetContext(TestViewerTabData tabData)
        {
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Settings context for type "+tabData.tabType); // Write to console
            ResetContext(); // Reset context
            switch (tabData.tabType) // Switch based on tab type
			{
                case TestViewerTabType.DefaultCamera:
                    break;
                case TestViewerTabType.Camera:
                    SetCameraValues(tabData); // Set camera values
                    break;
				case TestViewerTabType.Texture:
                    textureImage.texture = (Texture2D)tabData.tabObject; // Set image texture
                    textureImage.gameObject.SetActive(true); // Enable image
                    break;
                case TestViewerTabType.Material:
                    textureImage.material = (Material)tabData.tabObject; // Set image material
                    textureImage.texture = textureImage.material.GetTexture("_MainTex"); // Set image texture
                    textureImage.gameObject.SetActive(true); // Enable image
                    break;
                case TestViewerTabType.TextureSlider:
                    testViewerSlider.SetContext((TextureSliderContext)tabData.tabObject); // Set slider context
                    testViewerSlider.SetState(true); // Enable slider
                    break;
                case TestViewerTabType.MaterialSlider:
                    testViewerSlider.SetContext((MaterialSliderContext)tabData.tabObject); // Set slider context
                    testViewerSlider.SetState(true); // Enable slider
                    break;
            }
        }
        
        // Reset all context of results viewer
        void ResetContext()
        {
            textureImage.gameObject.SetActive(false); // Disable image
            textureImage.material = null; // Null material
            testViewerSlider.SetState(false); // Disable slider
        }     
        
        // Set camera values
        void SetCameraValues(TestViewerTabData tabData)
        {
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Setting camera values"); // Write to console
            if (currentCamera) // If a current camera exists
                currentCamera.depth = cameraDepth; // Reset its depth
            currentCamera = (Camera)tabData.tabObject; // Get new current camera
            if(currentCamera)
            {
                cameraDepth = currentCamera.depth; // Get current depth
                currentCamera.depth = 9; // Set its depth
            }
        }

        [Serializable]
        public class TextureSliderContext
        {
            public Texture2D image1;
            public string label1;
            public Texture2D image2;
            public string label2;

            public TextureSliderContext(Texture2D inputImage1, string inputLabel1, Texture2D inputImage2, string inputLabel2)
            {
                image1 = inputImage1;
                label1 = inputLabel1;
                image2 = inputImage2;
                label2 = inputLabel2;
            }
        }

        [Serializable]
        public class MaterialSliderContext
        {
            public Material image1;
            public string label1;
            public Material image2;
            public string label2;

            public MaterialSliderContext(Material inputImage1, string inputLabel1, Material inputImage2, string inputLabel2)
            {
                image1 = inputImage1;
                label1 = inputLabel1;
                image2 = inputImage2;
                label2 = inputLabel2;
            }
        }
    }
}
