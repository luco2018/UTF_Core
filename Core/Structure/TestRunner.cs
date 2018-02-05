using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using GraphicsTestFramework.SQL;

namespace GraphicsTestFramework
{
    // ------------------------------------------------------------------------------------
    // Global Enums

    public enum RunnerType
    {
        Automation,
        Manual,
        Results,
        Resolve,
        Analytic,
        AnalyticComparison
    };

    // ------------------------------------------------------------------------------------
    // TestRunner
    // - Global test execution management
    // - Converts TestStructure to a runner based on menu selections
    // - Handles running of tests across multiple TestList

    public class TestRunner : MonoBehaviour
    {
        // ------------------------------------------------------------------------------------
        // Variables

        // Singleton
        private static TestRunner _Instance = null;
        public static TestRunner Instance
        {
            get
            {
                if (_Instance == null)
                    _Instance = (TestRunner)FindObjectOfType(typeof(TestRunner));
                return _Instance;
            }
        }

        // Data
        public RunnerType runnerType;
        public Runner runner;
        bool runnerIsWaiting;
        public int currentTestIndex;
        public Test activeTest;

        // UUID for automated run
        public static string runUUID = "null";

        // Level load (TODO - Update API)
        private bool levelWasLoaded = false;
        private void OnLevelWasLoaded(int iLevel)
        {
            levelWasLoaded = true;
        }

        public bool isAnalytic
        {
            get { return (runnerType == RunnerType.Analytic || runnerType == RunnerType.AnalyticComparison) ? true : false; }
        }

        // ------------------------------------------------------------------------------------
        // Broadcast

        //Subscribe to event delegates
        void OnEnable()
        {
            TestLogicBase.endTestAction += EndTest;
        }

        //Desubscribe from event delegates
        void OnDisable()
        {
            TestLogicBase.endTestAction -= EndTest;
        }

        // ------------------------------------------------------------------------------------
        // Initialization

        // Setup the runner after instantiation
        public void SetupRunner(RunnerType runType)
        {
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Setting up runner"); // Write to console
            ProgressScreen.Instance.SetState(true, ProgressType.LocalLoad, "Generating and processing new Test Runner"); // Enable ProgressScreen
            runnerType = runType; // Set runner type
            if (runnerType != RunnerType.Results && runnerType != RunnerType.Analytic) // If not results or analytic
                GenerateTestRunner(TestStructure.Instance.GetStructure()); // Generate test runner
            else if (runnerType == RunnerType.Analytic) // If analytic
                ResultsViewer.Instance.SetState(5); // Enable results viewer
        }

        // Convert the test structure into a runner based on current selection and runner type
        void GenerateTestRunner(TestStructure.Structure inputStructure)
        {
            runner = new Runner(); // Create new Runner instance
            for (int su = 0; su < inputStructure.suites.Count; su++) // Iterate suites
            {
                if (inputStructure.suites[su].selectionState != 0 || runnerType != RunnerType.Automation) // If selected or automation
                {
                    string suiteName = inputStructure.suites[su].suiteName;
                    for (int ty = 0; ty < inputStructure.suites[su].types.Count; ty++) // Iterate types
                    {
                        if (inputStructure.suites[su].types[ty].selectionState != 0 || runnerType != RunnerType.Automation) // If selected or automation
                        {
                            string typeName = inputStructure.suites[su].types[ty].typeName;
                            int typeIndex = inputStructure.suites[su].types[ty].typeIndex;
                            for (int gr = 0; gr < inputStructure.suites[su].types[ty].groups.Count; gr++) // Iterate scenes
                            {
                                if (inputStructure.suites[su].types[ty].groups[gr].selectionState != 0 || runnerType != RunnerType.Automation) // If selected or automation
                                {
                                    string groupName = inputStructure.suites[su].types[ty].groups[gr].groupName;
                                    for (int te = 0; te < inputStructure.suites[su].types[ty].groups[gr].tests.Count; te++) // Iterate tests
                                    {
                                        if (inputStructure.suites[su].types[ty].groups[gr].tests[te].selectionState != 0 || runnerType != RunnerType.Automation) // If selected or automation
                                        {
                                            string testName = inputStructure.suites[su].types[ty].groups[gr].tests[te].testName;
                                            string scenePath = inputStructure.suites[su].types[ty].groups[gr].tests[te].scenePath;
                                            if (!inputStructure.suites[su].types[ty].groups[gr].tests[te].baseline || runnerType != RunnerType.Resolve) // If baseline resolution mode return all with no baselines
                                            {
                                                TestEntry newTest = new TestEntry(suiteName, groupName, scenePath, typeName, testName, typeIndex, su, gr, ty, te); // Create new TestEntry instance
                                                runner.tests.Add(newTest); // Add to runner
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            Console.Instance.Write(DebugLevel.Logic, MessageLevel.Log, "Generated test runner"); // Write to console
            if (runnerType != RunnerType.Resolve) // If in Resolve mode we start tests manually
                StartTests(); // Start tests
        }

        // ------------------------------------------------------------------------------------
        // Runner Execution

        public void StartTests()
        {
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Starting runner execution"); // Write to console
            Menu.Instance.SetMenuState(false); // Disable menu
            switch (runnerType) // Switch execution logic based on RunnerType
            {
                case RunnerType.Automation: // Automation ("Run" in menu)
                    StartCoroutine(IterateTests()); // Iterate the runner
                    break;
                case RunnerType.Manual: // Manual ("View" in menu)
                    LoadSelectedTest(); // Load selected from menu
                    break;
                case RunnerType.Resolve: // Resolve (When forcing baseline resolution in menu)
                    currentTestIndex = 0; // Current index is always 0
                    StartCoroutine(LoadTest()); // Load tests manually from 0
                    break;
                case RunnerType.Analytic: // Analytic Comparison (runs comparisons on recreated results data)
                    StartCoroutine(IterateTests()); // Iterate the runner
                    break;
                case RunnerType.AnalyticComparison: // Analytic Comparison (runs comparisons on recreated results data)
                    StartCoroutine(IterateTests()); // Iterate the runner
                    break;
            }
        }

        // Iterate tests (Automation)
        IEnumerator IterateTests()
        {
            Console.Instance.Write(DebugLevel.Logic, MessageLevel.Log, "Starting automation run"); // Write to console
            runUUID = "";
            StartCoroutine(SQL.SQLIO.Instance.RunUUID((value => { runUUID = value; })));
            while(runUUID == "") { yield return null; }
            for (int i = 0; i < runner.tests.Count; i++) // Iterate tests
            {
                do { yield return null; } while (runnerIsWaiting == true); // Wait for previous test to finish before next test
                runnerIsWaiting = true; // Set waiting
                currentTestIndex = i; // Set current test index
                StartCoroutine(LoadTest()); // Load test
            }
            do { yield return null; } while (runnerIsWaiting == true); // Wait for previous test to finish before enabling menus
            Console.Instance.Write(DebugLevel.Logic, MessageLevel.Log, "Ended automation run"); // Write to console
            ProgressScreen.Instance.SetState(false, ProgressType.LocalLoad, ""); // Disable ProgressScreen
            if(Master.Instance._sqlMode == SQLmode.Live && !TestRunner.Instance.isAnalytic)
            {
                yield return Slack.Instance.SendSlackResults(); // Send results to slack
            }
            if (Common.GetArg("automation") == "true") // Check for automation arg
                Common.QuitApplication(); // Quick application
            else if (isAnalytic) // IF analytic
                ResultsViewer.Instance.SetState(5); // Enable ResultViewer
            else
                Menu.Instance.SetMenuState(true); // Enable menu
        }

        // Load Test of currentTestIndex
        IEnumerator LoadTest()
        {
            if(!isAnalytic) // If not analytic
            {
                if (SceneManager.GetActiveScene().name != runner.tests[currentTestIndex].scenePath) // If current scene name does not match requested
                {
                    SceneManager.LoadScene(runner.tests[currentTestIndex].scenePath); // Load requested scene
                    while (!levelWasLoaded) // Wait for load
                        yield return null;
                    levelWasLoaded = false; // Reset
                }
            }
            Console.Instance.Write(DebugLevel.Logic, MessageLevel.Log, "Loading test "+ runner.tests[currentTestIndex].testName); // Write to console
            StartTest(runner.tests[currentTestIndex], runnerType); // Start the test
        }

        public void CallLoadSpecificTest(TestEntry inputEntry)
        {
            StartCoroutine(LoadSpecificTest(inputEntry));
        }

        // Load specified test
        public IEnumerator LoadSpecificTest(TestEntry inputEntry)
        {
            if (SceneManager.GetActiveScene().name != inputEntry.scenePath) // If current scene name does not match requested
            {
                SceneManager.LoadScene(inputEntry.scenePath); // Load requested scene
                while (!levelWasLoaded) // Wait for load
                    yield return null;
                levelWasLoaded = false; // Reset
            }
            Console.Instance.Write(DebugLevel.Logic, MessageLevel.Log, "Loading test " + inputEntry.testName); // Write to console
            StartTest(inputEntry, runnerType); // Start the test
        }

        // Load the currently selected test for view
        void LoadSelectedTest()
        {
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Loading selected test"); // Write to console
            MenuTestEntry selectedEntry = Menu.Instance.GetSelectedEntry(); // Get selected menu entry
            currentTestIndex = FindSelectedTestIndex(selectedEntry); // Find selected entry in runner (will be -1 in failure case)
            StartCoroutine(LoadTest()); // Start load
        }

        // Find and load previous test
        public void PreviousTest()
        {
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Selecting previous test"); // Write to console
            if (currentTestIndex > 0) // If not first test
                currentTestIndex--; // Select previous test
            else
                currentTestIndex = runner.tests.Count - 1; // Select last test
            StartCoroutine(LoadTest()); // Load test
        }

        // Find and load next test
        public void NextTest()
        {
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Selecting next test"); // Write to console
            if (currentTestIndex < runner.tests.Count - 1) // If not last test
                currentTestIndex++; // Select next test
            else
                currentTestIndex = 0; // Select first test
            StartCoroutine(LoadTest()); // Load test
        }

        // Finalise test (Called by TestList.EndTest)
        public void FinalizeTest()
        {
            Console.Instance.Write(DebugLevel.Logic, MessageLevel.Log, "Finalizing test " + runner.tests[currentTestIndex].testName); // Write to console
            runnerIsWaiting = false; // To waiting to false so automation can continue
        }

        // ------------------------------------------------------------------------------------
        // Test Execution

        // Start an individual test (called by TestRunner)
        public void StartTest(TestEntry inputTest, RunnerType runnerType)
        {
            Console.Instance.Write(DebugLevel.Logic, MessageLevel.Log, "Starting test " + inputTest.testName); // Write to console
            TestLogicBase activeTestLogic = null;
            if (!isAnalytic) // If not analytic mode
            {
                activeTest = SuiteManager.GetTest(inputTest); // Get the active test from suite manager
                activeTestLogic = GetLogicInstance(SuiteManager.GetSuiteName(inputTest.suiteIndex), inputTest); // Get active test logic instance
            }   
            else
            {
                activeTest = Common.GenerateTestFromTestEntry(inputTest); // Get the active test from structure
                activeTestLogic = GetLogicInstance(TestStructure.Instance.GetSuiteNameFromIndex(inputTest.suiteIndex), inputTest); // Get active test logic instance
            }
            StartCoroutine(activeTestLogic.SetupTest(inputTest, runnerType)); // Setup test
        }

        TestModelBase activeModelInstance;

        // Get a logic instance and set model instance on it
        TestLogicBase GetLogicInstance(string suiteName, TestEntry activeEntry)
        {
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Getting logic instance"); // Write to console
            TestLogicBase output; // Create logic instance
            var modelType = TestTypes.GetTypeFromIndex(activeEntry.typeValue); // Get the model type from its index

            if (!FindModelInstanceOfType(modelType, out activeModelInstance)) // Check for model instance matching criteria
                activeModelInstance = CreateNewModelInstance(modelType); // Create model instance

            activeModelInstance.SetLogic(); // Set the logic reference on the model
            output = TestTypeManager.Instance.GetLogicInstanceFromName(activeModelInstance.logic.ToString().Replace("GraphicsTestFramework.", "").Replace("Logic", "")); // Get test  logic instance
            output.SetSuiteName(suiteName); // Set suite name on the logic
            output.SetModel(activeModelInstance); // Set the active test model in the logic
            TestTypeManager.Instance.SetActiveLogic(output); // Set as active test logic
            return output; // Return
        }

        // Find model instances of a specificed type that meet requirements
        public bool FindModelInstanceOfType(Type modelType, out TestModelBase model)
        {
            TestModelBase[] foundModels = (TestModelBase[])FindObjectsOfType(modelType); // Find all models of type
            for(int i = 0; i < foundModels.Length; i++) // Iterate found models
            {
                if(Common.IsCurrentPlatformInBitMask(foundModels[i].settings.platformMask)) // If model matches platform bitmask
                {
                    model = foundModels[i]; // Out model
                    return true; // Return true
                }
            }
            model = null; // Out null
            return false; // Return false
        }

        // Create new model instance of a specified type
        public TestModelBase CreateNewModelInstance(Type modelType)
        {
            GameObject go = new GameObject(); // Generate a new object
            go.name = modelType.ToString().Replace("GraphicsTestFramework.", ""); // Set object name
            return (TestModelBase)go.AddComponent(modelType); // Add model component of correct type
        }

        // End the current Test (called by TestLogic.EndTestAction)
        public void EndTest()
        {
            Console.Instance.Write(DebugLevel.Logic, MessageLevel.Log, "Ended test " + activeTest.name.ToString()); // Write to console
            if(runnerType == RunnerType.Manual) // If manual run
                ProgressScreen.Instance.SetState(false, ProgressType.LocalLoad, ""); // Disable ProgressScreen
            FinalizeTest(); // Finalize test on TestRunner
        }

        // ------------------------------------------------------------------------------------
        // Get Data

        // Get the entry for the current test index
        public TestEntry GetCurrentTestEntry()
        {
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Getting current test entry"); // Write to console
            return runner.tests[currentTestIndex]; // Return current test entry
        }

        // ------------------------------------------------------------------------------------
        // Helper Methods

        // Find the runner index of the selected test
        int FindSelectedTestIndex(MenuTestEntry selectedEntry)
        {
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Finding selected test index"); // Write to console
            for (int i = 0; i < runner.tests.Count; i++) // Iterate tests
            {
                if (runner.tests[i].suiteIndex == selectedEntry.suiteId &&
                    runner.tests[i].groupIndex == selectedEntry.groupId &&
                    runner.tests[i].typeIndex == selectedEntry.typeId &&
                    runner.tests[i].testIndex == selectedEntry.testId) // If all data matches
                {
                    return i; // Return it
                }
            }
            Console.Instance.Write(DebugLevel.Critical, MessageLevel.LogError, "Test index not found!"); // Write to console
            return -1; // Return failure
        }

        // Check if the current test is the last in the current runner
        public bool CheckEndOfRunner()
        {
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Checking for end of runner"); // Write to console
            if (currentTestIndex < runner.tests.Count - 1) // If tests remain
                return false; // Return false
            else
                return true; // Return true
        }

        // ------------------------------------------------------------------------------------
        // Local Data Structures

        [Serializable]
        public class Runner
        {
            public List<TestEntry> tests = new List<TestEntry>();
        }
    }

    // ------------------------------------------------------------------------------------
    // Global Data Structures

    [Serializable]
    public class TestEntry
    {
        public string suiteName;
        public string groupName;
        public string scenePath;
        public string typeName;
        public string testName;
        public int typeValue;
        public int suiteIndex;
        public int groupIndex;
        public int typeIndex;
        public int testIndex;

        public TestEntry(string inputSuiteName, string inputGroupName, string inputScenePath, string inputTypeName, string inputTestName, int inputTypeValue, int inputSuiteIndex, int inputGroupIndex, int inputTypeIndex, int inputTestIndex)
        {
            suiteName = inputSuiteName;
            groupName = inputGroupName;
            scenePath = inputScenePath;
            typeName = inputTypeName;
            testName = inputTestName;
            typeValue = inputTypeValue;
            suiteIndex = inputSuiteIndex;
            groupIndex = inputGroupIndex;
            typeIndex = inputTypeIndex;
            testIndex = inputTestIndex;
        }
    }
}
