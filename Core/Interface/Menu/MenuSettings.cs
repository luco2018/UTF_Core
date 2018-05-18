using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace GraphicsTestFramework
{
    // ------------------------------------------------------------------------------------
    // MenuSettings
    // - Controller for the settings menu
    //Major TODO - this scripts needs refactoring for resetting certain settings and clearing others, hot fix for testrail intergration

    public class MenuSettings : MonoBehaviour
    {
        // ------------------------------------------------------------------------------------
        // Variables

        // Singleton
        private static MenuSettings _Instance = null;
        public static MenuSettings Instance
        {
            get
            {
                if (_Instance == null)
                    _Instance = (MenuSettings)FindObjectOfType(typeof(MenuSettings));
                return _Instance;
            }
        }

        // UI Object References
        public GameObject menuSettingsParent;
        public Button saveButton;
        public Toggle testViewerOnAutomationFailToggle;

        // Data
        Configuration.Settings tempSettings;

        // ------------------------------------------------------------------------------------
        // Content & State

        // Enable or disable the settings menu
        public void SetState(bool state)
        {
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Setting state to " + state); // Write to console
            if (state == true) // If enabling window
            {
                tempSettings = CloneSettings(Configuration.Instance.settings); // Clone configuration settings to temp
                SetUiState(); // Set UI State
            }
            menuSettingsParent.SetActive(state); // Set state
        }

        // Sets UI states for incoming settings
        private void SetUiState()
        {
            testViewerOnAutomationFailToggle.isOn = tempSettings.testviewerOnAutomationTestFail; // Reset
<<<<<<< HEAD
            //saveButton.interactable = false; // Disable save button
=======
            //saveButton.interactable = false; // Disable save button TODO - Need to fix
>>>>>>> origin/testrail-integration
        }

        // ------------------------------------------------------------------------------------
        // Entries

        // On button click: Clear local data
        public void OnClickClearLocalData()
        {
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Clicked: Clear Local Data"); // Write to console
            Button[] buttons = new Button[2]; // Create button array
            bool openDialogue = Dialogue.Instance.TryDialogue(true, 1, out buttons); // Try for dialogue window and out buttons
            if (openDialogue) // If dialogue opens
            {
                buttons[0].onClick.AddListener(delegate { ClearLocalDataAction(); }); // Add listeners
                buttons[1].onClick.AddListener(delegate { Dialogue.Instance.SetState(false, 1); });
            }
            else
                ClearLocalDataAction(); // Save baseline
        }

        // On toggle change: Open TestViewer on Automation test fail
        public void OnToggleOpenTestViewerOnAutomationFail(bool input)
        {
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Open Test Viewer on Automation Fail set to "+input); // Write to console
            tempSettings.testviewerOnAutomationTestFail = input; // Set
            //saveButton.interactable = true; // Enable save button
        }

        // Clear all local data
        void ClearLocalDataAction()
        {
            LocalIO.Instance.ClearLocalData(); // Tell IO to clear all local data
        }

        // Save settings to Configuration
        public void OnClickSave()
        {
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Clicked: Save Settings"); // Write to console
            Configuration.Instance.settings = CloneSettings(tempSettings); // Clone settings and save to configuration
            //saveButton.interactable = false; // Disable save button
<<<<<<< HEAD
=======
            SetState(false);
>>>>>>> origin/testrail-integration
        }

        Configuration.Settings CloneSettings (Configuration.Settings input)
        {
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Cloning configuration settings"); // Write to console
            Configuration.Settings output = new Configuration.Settings(); // Create new instance
            BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static; // Set binding flags
            FieldInfo[] settingsFields = typeof(Configuration.Settings).GetFields(bindingFlags); // Get fields
            for (int i = 0; i < settingsFields.Length; i++)
            {
                FieldInfo field = input.GetType().GetField(settingsFields[i].Name); // Geyt field info
                var value = field.GetValue(input); // Get value from  local temp
                field.SetValue(output, System.Convert.ChangeType(value, field.FieldType)); // Set value to configuration
            }
            return output; // Return
        }

        // Settings save and revert delagates
        public static event Broadcast.SaveMenuSettings saveSettings;
        public void SaveSettings()
        {
            if (saveSettings != null)
                saveSettings();
            SetState(false);
        }

        public static event Broadcast.RevertMenuSettings revertSettings;
        public void RevertSettings()
        {
            if (revertSettings != null)
                revertSettings();
        }
    }
}
