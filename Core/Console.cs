using UnityEngine;

namespace GraphicsTestFramework
{
    // ------------------------------------------------------------------------------------
    // Global Enums

    public enum DebugLevel
    {
        None, // No messages
        Critical, // Only critical messages
        File, // Messages only when writing to local or cloud files
        Intersystem, // Messages only when systems communicate with each other
        Key, // Messages for key events
        Logic, // Messages for logic events
        Full // Full messaging for all functions
    };

    public enum MessageLevel { Log, LogWarning, LogError };

    // ------------------------------------------------------------------------------------
    // Console
    // - Handle debug messaging
    // - Only writes messages of a "lower" priority than the current debug level

    public class Console : MonoBehaviour
    {
        // ------------------------------------------------------------------------------------
        // Variables

        // Singleton
        private static Console _Instance = null;
        public static Console Instance
        {
            get
            {
                if (_Instance == null)
                    _Instance = (Console)FindObjectOfType(typeof(Console));
                return _Instance;
            }
        }

        public DebugLevel debug;
        public bool playerConsole = false;

        // ------------------------------------------------------------------------------------
        // Methods

        // Write to the console
        public void Write(DebugLevel debugLevel, MessageLevel messageLevel, string message)
        {
			if (Application.platform != RuntimePlatform.IPhonePlayer && Debug.isDebugBuild) {
				System.Diagnostics.StackFrame stackFrame = new System.Diagnostics.StackTrace (1, true).GetFrame (0); // Get a stack frame from the caller
				string fileName = stackFrame.GetFileName (); // Get file name of caller
				string[] fileNameSplit = fileName.Split (new char[] { '\\' }); // Split file name
				string scriptName = fileNameSplit [fileNameSplit.Length - 1]; // Extract script name from file name
				string methodName = stackFrame.GetMethod ().ToString (); // Get method name of caller
				int lineNumber = stackFrame.GetFileLineNumber (); // Get line number of caller
				message += System.Environment.NewLine + scriptName + " - " + methodName + " - L" + lineNumber; // Append stack data to message
			}

            if ((int)debugLevel <= (int)debug) // Filter messages of higher level than requested
            {
                if (playerConsole) // If print all messages in player console
                    Debug.LogError(message); // Print a LogError
                else
                {
                    switch (messageLevel) // Switch on message level
                    {
                        case MessageLevel.Log:
                            Debug.Log(message); // Print a Log
                            break;
                        case MessageLevel.LogWarning:
                            Debug.LogWarning(message); // Print a LogWarning
                            break;
                        case MessageLevel.LogError:
                            Debug.LogError(message); // Print a LogError
                            break;
                    }
                }
            }
        }
    }
}
