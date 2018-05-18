using System;
using UnityEngine;
using UnityEngine.UI;

namespace GraphicsTestFramework
{
    // ------------------------------------------------------------------------------------
    // ResultsEntry
    // - Instance of an entry in the ResultsViewer

    public class ResultsEntry : MonoBehaviour
    {
        // ------------------------------------------------------------------------------------
        // Variables

        // UI Object References
        public Text groupNameText; 
        public Text testNameText;
        public Text passFailText;
        public Image passFailBackground;
        public Button expandButton;

        // Data
        public ResultsEntryData resultsEntryData; // Results for this entry
        public TestLogicBase logic; // Logic script reference

        // ------------------------------------------------------------------------------------
        // Initialization

        // Setup a title entry
        public void SetupTitle(string title)
        {
            testNameText.text = title; // Set title text
        }

        // Setup the entry
        public void Setup(ResultsEntryData inputData, TestLogicBase inputLogic)
        {
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Setting up results entry"); // Write to console
            resultsEntryData = inputData; // Track resultsEntry data
            logic = inputLogic; // Track logic instance
            groupNameText.text = resultsEntryData.testEntry.groupName; // Set group name label
            testNameText.text = resultsEntryData.testEntry.testName; // Set test name label

            int passFail = 2; // Set default state (no results)
            if (resultsEntryData.resultsData != null) // If results data exists
            {
                int passFailIndex = -1;
                for (int f = 0; f < resultsEntryData.resultsData.fieldNames.Count; f++)
                {
                    if (resultsEntryData.resultsData.fieldNames[f] == "PassFail")
                        passFailIndex = f;
                }
                passFail = resultsEntryData.resultsData.resultsRow[0].resultsColumn[passFailIndex] == "True" ? 1 : 0; // Set pass fail state
            }

            switch (passFail) // Switch on pass fail
            {
                case 0: //Fail
                    passFailText.text = "FAIL"; // Set passfail label
                    passFailBackground.color = Menu.Instance.colors[4]; // Set passfail object color
                    expandButton.onClick.AddListener(delegate { ToggleContext(); }); // Add expand button listener
                    break;
                case 1: // Pass
                    passFailText.text = "PASS"; // Set passfail label
                    passFailBackground.color = Menu.Instance.colors[3]; // Set passfail object color
                    expandButton.onClick.AddListener(delegate { ToggleContext(); }); // Add expand button listener
                    break; 
                case 2: // No results
                    passFailText.text = "NONE"; // Set passfail label
                    passFailBackground.color = Menu.Instance.colors[1]; // Set passfail object color
                    expandButton.interactable = false; // Disable expand button
                    break;
            }
        }

        // ------------------------------------------------------------------------------------
        // Context Object

        // Toggle the context object
        public void ToggleContext()
        {
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Toggle context object"); // Write to console
            logic.SetSuiteName(resultsEntryData.testEntry.suiteName); // Set suite name on logic instance
            ResultsViewer.Instance.ToggleContextObject(this, logic.GetComponent<TestDisplayBase>()); // Tell ResultsViewer to toggle context object on this entry
        }
    }

    [Serializable]
    public class ResultsEntryData
    {
        public TestEntry testEntry;
        public ResultsIOData resultsData;
        public ResultsIOData resultsDataB;

        public ResultsEntryData(TestEntry inputEntry, ResultsIOData inputData, ResultsIOData inputDataB)
        {
            testEntry = inputEntry;
            resultsData = inputData;
            resultsDataB = inputDataB;
        }
    }
}
