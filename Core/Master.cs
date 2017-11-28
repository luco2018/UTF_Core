using System;
using UnityEngine;
using UnityEngine.UI;

namespace GraphicsTestFramework
{
    // ------------------------------------------------------------------------------------
    // Master
    // - System data structures and returns
    // - Maintains persistence of other logic objects

    public class Master : MonoBehaviour
    {
        // ------------------------------------------------------------------------------------
        // Variables
        public SQLmode _sqlMode;

        public GameObject titleMenu;
        public Button runTestButton;

        // Singleton
        private static Master _Instance = null;
        public static Master Instance
        {
            get
            {
                if (_Instance == null)
                    _Instance = (Master)FindObjectOfType(typeof(Master));
                return _Instance;
            }
        }

        // ------------------------------------------------------------------------------------
        // Setup

        // On Awake
        private void Awake()
        {
            DontDestroyOnLoad(gameObject); // Set this object to DontDestroy
        }

        void Start()
        {
            runTestButton.interactable = SuiteManager.HasSuites();
        }

        // ------------------------------------------------------------------------------------
        // Get System Data

        // Get SystemData to use for building ResultsCommon
        public SystemData GetSystemData()
		{
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Getting system data"); // Write to console
            SystemData output = new SystemData(); // Create new class instance
            ProjectSettings projectSettings = SuiteManager.GetProjectSettings(); // Get settings
			output.UnityVersion = projectSettings.unityVersion+"|"+projectSettings.unityBranch; // Get Unity version
			output.AppVersion = Common.applicationVersion.ToString(); // Get application version
            output.OS = SystemInfo.operatingSystem; // Get OS
            output.Device = SystemInfo.deviceModel + "|" + SystemInfo.graphicsDeviceName + "|" + SystemInfo.processorType;
			output.Platform = Application.platform.ToString(); // Get platform
#if UNITY_EDITOR
            if (!Common.IsStandaloneTarget(UnityEditor.EditorUserBuildSettings.activeBuildTarget)) // Check if target platform is emulated editor
                output.Platform += "_" + UnityEditor.EditorUserBuildSettings.activeBuildTarget.ToString(); // Append build target
#endif
            output.API = SystemInfo.graphicsDeviceType.ToString(); // Get graphics device type
			return output; // Return
		}

        // Get the current system time
		public DateTime GetSystemTime()
        {
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Getting system time"); // Write to console
            return DateTime.UtcNow; // Return current DateTime
		}

        // ------------------------------------------------------------------------------------
        // Application

        // Exit the application
        public void ExitApplication()
        {
            Application.Quit(); // Quit
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false; // If editor stop play mode
#endif
        }

        public void ReturnToTitle()
        {
            titleMenu.SetActive(true);
        }

    }

    // ------------------------------------------------------------------------------------
    // Global Data Structures

    // System data class
    [System.Serializable]
	public class SystemData
	{
		public string UnityVersion;
		public string AppVersion;
        public string OS;
        public string Device;
		public string Platform;
		public string API;
	}

    //Enum for SQL path
    public enum SQLmode
    {
        Live,
        Staging,
        Disabled
    }
}
