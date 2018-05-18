using UnityEngine;
using UnityEngine.UI;

namespace GraphicsTestFramework
{
    // ------------------------------------------------------------------------------------
    // MenuAltBaselineListEntry
    // - Instance of an entry in the Alternative Baseline list

    public class MenuAltBaselineListEntry : MonoBehaviour 
	{
        // ------------------------------------------------------------------------------------
        // Variables

        public Text line; // Platform/API
        public Button button;
        private string _platform;
        private string _api;

        // ------------------------------------------------------------------------------------
        // Initialization

        // Setup the instance
        public void Setup(string platform, string api)
		{
            _platform = platform;
            _api = api;
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Setting up alt baseline list entry"); // Write to console
            line.text = "Platform: " + platform + " API: " + api; // Set line
            button.onClick.AddListener(delegate { ResultsIO.Instance.PullAltBaselines(platform, api); });
            button.onClick.AddListener(onClick);
        }

        void onClick()
        {
            Menu.Instance.altBaselineWindow.parent.SetActive(false);
            Menu.Instance.menuInformation.UpdateInformation();
        }
	}
}
