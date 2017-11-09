using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GraphicsTestFramework
{
    // ------------------------------------------------------------------------------------
    // TestStructure
    // - Gets data from SuiteManager and all TestLists
    // - Generates a structure of all available tests
    // - Handles baseline checks and selection changes on the structure

    public class TestStructure : MonoBehaviour
    {
        // ------------------------------------------------------------------------------------
        // Variables

        // Singleton
        private static TestStructure _Instance = null;
        public static TestStructure Instance
        {
            get
            {
                if (_Instance == null)
                    _Instance = (TestStructure)FindObjectOfType(typeof(TestStructure));
                return _Instance;
            }
        }

        // Data
        public Structure testStructure;

        // Generation check
        private bool m_IsGenerated = false;
        public bool IsGenerated
        {
            get { return m_IsGenerated; }
        }

        // ------------------------------------------------------------------------------------
        // Broadcast

        //Subscribe to event delegates
        void OnEnable()
        {
            ResultsIO.baselinesParsed += StartGeneration;
        }

        //Desubscribe from event delegates
        void OnDisable()
        {
            ResultsIO.baselinesParsed -= StartGeneration;
        }

        // ------------------------------------------------------------------------------------
        // DELETE ME!

        private ResultsIOData[] _tempData;

        IEnumerator GetData()
        {
            yield return StartCoroutine(GraphicsTestFramework.SQL.SQLIO.Instance.GetaData(false, (value => { _tempData = value; })));
            StartCoroutine(GenerateAnalyticStructure(_tempData));
        }

        void Update()
        {
            if(Input.GetKeyUp(KeyCode.Return))
            {
                StartCoroutine(GetData());
            }
        }

        // ------------------------------------------------------------------------------------
        // Initialization

        // Start generation process
        void StartGeneration()
        {
            if(Master.Instance.appMode == AppMode.TestRunner)
            {
                Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Initializing TestStructure"); // Write to console
                ProgressScreen.Instance.SetState(true, ProgressType.LocalLoad, "Generating Test Structure"); // Enable ProgressScreen
                StartCoroutine(GenerateStructure()); // Start generating structure
            }
        }

        // Generate test structure
        // - Loads all scenes and gets data from test lists
        // - Reorganises for menu layout
        IEnumerator GenerateStructure()
        {
            yield return null; // TODO - Remove
            testStructure = new Structure(); // Create new test structure instance
            List<TestType> typeList = GenerateTypeListAndInstances(); // Generate type list and create instances
            ProjectSettings projectSettings = SuiteManager.GetProjectSettings(); // Get the suite list
            if (projectSettings == null) // If no suite list found
                StopAllCoroutines(); // Abort

            for (int su = 0; su < projectSettings.suiteList.Count; su++) // Iterate suites on suite list
            {
                Suite newSuite = new Suite(); // Create new suite instance
                newSuite.suiteName = projectSettings.suiteList[su].suiteName; // Set suite name from suite list
                newSuite.types = CloneTestTypeList(typeList); // Clone the type list
                for (int gr = 0; gr < projectSettings.suiteList[su].groups.Count; gr++) // Iterate groups
                {
                    for (int te = 0; te < projectSettings.suiteList[su].groups[gr].tests.Count; te++) // Iterate tests
                    {
                        GraphicsTestFramework.Test test = projectSettings.suiteList[su].groups[gr].tests[te]; // Get test
                        if(Common.IsTestApplicable(test))
                        {
                            int[] types = TestTypeManager.Instance.GetTypeSelectionFromBitMask(test.testTypes); // Get type array from test's bitmask
                            for (int ty = 0; ty < types.Length; ty++) // Iterate types of the test
                            {
                                Group newGroup = FindDuplicateGroupInType(newSuite, types[ty], projectSettings.suiteList[su].groups[gr].groupName); // Find duplicate groups in the type
                                if (newGroup == null) // If not found
                                {
                                    newGroup = new Group(); // Create a new group instance
                                    newGroup.groupName = projectSettings.suiteList[su].groups[gr].groupName; // Set group name
                                    FindDuplicateTypeInSuite(newSuite, types[ty]).groups.Add(newGroup); // Add the group to the type
                                }
                                Test newTest = new Test(); // Create new test instance
                                string[] pathSplit = projectSettings.suiteList[su].groups[gr].tests[te].scenePath.Split('/'); // Split path for scene name
                                newTest.testName = pathSplit[pathSplit.Length - 1].Replace(".unity", ""); ; // Set test name
                                newTest.scenePath = projectSettings.suiteList[su].groups[gr].tests[te].scenePath; // Set scene path
                                newGroup.tests.Add(newTest); // Add test to scene
                            }
                        }
                    }
                }
                for (int ty = 0; ty < newSuite.types.Count; ty++) // Iterate types
                {
                    if (newSuite.types[ty].groups.Count == 0) // If empty
                        newSuite.types.RemoveAt(ty); // Remove it
                }
                newSuite.types.TrimExcess(); // Trim the types list
                testStructure.suites.Add(newSuite); // Add to suites list
            }
            m_IsGenerated = true; // Set generated
            Console.Instance.Write(DebugLevel.Logic, MessageLevel.Log, "TestStructure finished generating"); // Write to console
            ProgressScreen.Instance.SetState(false, ProgressType.LocalLoad, ""); // Disable ProgressScreen
        }

        IEnumerator GenerateAnalyticStructure(ResultsIOData[] results)
        {
            yield return null;
            StartCoroutine(GenerateAnalyticStructure(results, null));
        }

        IEnumerator GenerateAnalyticStructure(ResultsIOData[] resultsA, ResultsIOData[] resultsB)
        {
            yield return null;
            testStructure = new Structure(); // Create new test structure instance
            List<TestType> typeList = GenerateTypeListAndInstances(); // Generate type list and create instances

            int groupColumn = Common.FindResultsDataIOFieldIdByName(resultsA[0], "GroupName"); // Get group column ID
            if (groupColumn == -1) // If error return
                Console.Instance.Write(DebugLevel.Critical, MessageLevel.LogError, "GroupName ID not found"); // Debug
            int testColumn = Common.FindResultsDataIOFieldIdByName(resultsA[0], "TestName"); // Get test column ID
            if (testColumn == -1) // If error return
                Console.Instance.Write(DebugLevel.Critical, MessageLevel.LogError, "TestName ID not found"); // Debug

            for (int i = 0; i < resultsA.Length; i++)
            {
                Debug.LogWarning(resultsA[i].testType);
                string suiteName = resultsA[i].suite;
                Suite suite = FindDuplicateSuiteInStructure(suiteName);
                if (suite == null)
                {
                    suite = new Suite(); // Create new suite instance
                    suite.suiteName = suiteName; // Set suite name
                    suite.types = CloneTestTypeList(typeList); // Clone the type list
                    testStructure.suites.Add(suite); // Add to suites list
                }
                
                string groupName = resultsA[i].resultsRow[0].resultsColumn[groupColumn]; // Get group name
                int typeIndex = TestTypeManager.Instance.GetTestTypeIndexFromName(resultsA[i].testType); // Get type index
                if(typeIndex == -1) // If error return
                    Console.Instance.Write(DebugLevel.Critical, MessageLevel.LogError, "TestType ID not found"); // Debug
                Group group = FindDuplicateGroupInType(suite, typeIndex, groupName);
                if (group == null)
                {
                    group = new Group(); // Create new group instance
                    group.groupName = groupName; // Set group name
                    suite.types[typeIndex].groups.Add(group); // Add to groups list
                }

                string testName = resultsA[i].resultsRow[0].resultsColumn[testColumn]; // Get test name
                TestResults test = new TestResults(); // Create new TestResults
                test.testName = testName; // Set test name
                test.dataA = resultsA[i]; // Set results data A
                if (resultsB != null) // If analytic comparison
                    test.dataB = resultsB[i]; // Set results data B
                else
                    test.dataB = null; // Set null
                group.tests.Add(test); // Add to group
            }
            // Reiterate suites to remove empty type entries
            for (int i = 0; i < testStructure.suites.Count; i++) 
            {
                for (int ty = 0; ty < testStructure.suites[i].types.Count; ty++) // Iterate types
                {
                    if (testStructure.suites[i].types[ty].groups.Count == 0) // If empty
                        testStructure.suites[i].types.RemoveAt(ty); // Remove it
                }
                testStructure.suites[i].types.TrimExcess(); // Trim the types list
            }
            Console.Instance.Write(DebugLevel.Logic, MessageLevel.Log, "AnalyticTestStructure finished generating"); // Write to console

            if (resultsB == null) // If analytic
                Menu.Instance.GenerateTestRunner(RunnerType.Analytic); // Generate anyaltic test runner
            else // If analytic comparison
                Menu.Instance.GenerateTestRunner(RunnerType.AnalyticComparison);
        }

        List<TestType> GenerateTypeListAndInstances()
        {
            string[] testTypes = TestTypes.GetTypeStringList(); // Get the type list
            List<TestType> typeList = new List<TestType>(); // Create new list to fill
            for (int i = 0; i < testTypes.Length; i++) // ITerate type list
            {
                TestType newType = new TestType(); // Create new instance
                newType.typeName = testTypes[i]; // Set name
                newType.typeIndex = i; // Set index
                typeList.Add(newType); // Add to list
                TestModelBase model = (TestModelBase)Activator.CreateInstance(TestTypes.GetTypeFromIndex(i)); // Create model instance for logic references
                model.SetLogic(); // Need to set logic before generating type instances
                TestTypeManager.Instance.GenerateTestTypeInstance(model); // Generate an instance object for test logic/display
            }
            return typeList;
        }

        // ------------------------------------------------------------------------------------
        // Helper Methods

        // Check ResultsIO for baselines then write data through the test structure
        public bool CheckForBaselines()
        {
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Checking for baselines"); // Write to console
            bool output = true; // Set output
            for(int su = 0; su < testStructure.suites.Count; su++) // Iterate suites
            {
                testStructure.suites[su].baseline = true; // Set init state
                for(int ty = 0; ty < testStructure.suites[su].types.Count; ty++) // Iterate types
                {
                    testStructure.suites[su].types[ty].baseline = true; // Set init state
                    for(int gr = 0; gr < testStructure.suites[su].types[ty].groups.Count; gr++) // Iterate groups
                    {
                        testStructure.suites[su].types[ty].groups[gr].baseline = true; // Set init state
                        for(int te = 0; te < testStructure.suites[su].types[ty].groups[gr].tests.Count; te++) // Iterate tests
                        {
                            bool baseline = ResultsIO.Instance.BaselineExists(testStructure.suites[su].suiteName, "Standard Legacy", testStructure.suites[su].types[ty].typeName, testStructure.suites[su].types[ty].groups[gr].groupName, testStructure.suites[su].types[ty].groups[gr].tests[te].testName); // Get baseline state
                            testStructure.suites[su].types[ty].groups[gr].tests[te].baseline = baseline; // Set baseline state to structure
                            if(baseline == false) // If no baseline
                            {
                                testStructure.suites[su].baseline = false; // Set to suite
                                testStructure.suites[su].types[ty].baseline = false; // Set to type
                                testStructure.suites[su].types[ty].groups[gr].baseline = false; // Set to group
                                output = false; // Set to output
                            }
                        }
                    }
                }
            }
            return output; // Return
        }

        // Find duplicate group entries in a type
        Group FindDuplicateGroupInType(Suite suite, int typeId, string groupName)
        {
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Finding duplicates of group " + groupName + " in type ID " + typeId); // Write to console
            for (int ty = 0; ty < suite.types.Count; ty++) // Iterate types
            {
                if (suite.types[ty].typeIndex == typeId) // If type matches input
                {
                    for (int gr = 0; gr < suite.types[ty].groups.Count; gr++) // Iterate groups
                    {
                        if (suite.types[ty].groups[gr].groupName == groupName) // If group matches input
                        {
                            return suite.types[ty].groups[gr]; // Return group
                        }
                    }
                }
            }
            return null; // Return fail
        }

        // Find duplicate type entries in a suite
        TestType FindDuplicateTypeInSuite(Suite suite, int typeId)
        {
            for (int ty = 0; ty < suite.types.Count; ty++) // Iterate types
            {
                if (suite.types[ty].typeIndex == typeId) // If type index matches
                {
                    return suite.types[ty]; // Return the type
                }
            }
            return null; // Return fail
        }

        // Find duplicate suite entries in the structure
        Suite FindDuplicateSuiteInStructure(string suiteName)
        {
            for(int su = 0; su < testStructure.suites.Count; su++) // Iterate suites
            {
                if (testStructure.suites[su].suiteName == suiteName) // If name matches
                    return testStructure.suites[su]; // Return suite
            }
            return null; // Return fail
        }

        // Checks whether any tests are selected (true if >0 tests are selected)
        public bool CheckSelectionNotNull()
        {
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Checking for null selection"); // Write to console
            for (int su = 0; su < testStructure.suites.Count; su++) // Iterate suites
            {
                if (testStructure.suites[su].selectionState != 0) // If selection not 0
                    return true; // Return true
            }
            return false; // Return false
        }

        // ------------------------------------------------------------------------------------
        // Get Data
        // TODO - Clean and comment this

        // Get the entire structure
        public Structure GetStructure()
        {
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Getting test structure"); // Write to console
            return testStructure; // Return the TestStructure
        }

        // Get a list of entries at a specific level
        public MenuEntryData[] GetEntries(MenuTestEntry input) // TODO - Can we shorten this?
        {
            MenuEntryData[] output = null;
            if (input.currentLevel == 0)
                output = new MenuEntryData[testStructure.suites.Count];
            for (int su = 0; su < testStructure.suites.Count; su++)
            {
                if (input.currentLevel == 0)
                {
                    MenuEntryData entry = new MenuEntryData();
                    MenuTestEntry id = Menu.Instance.CloneMenuTestEntry(input);
                    id.suiteId = su;                                // Replace relevent data 
                    entry.entryName = testStructure.suites[su].suiteName;
                    entry.selectionState = testStructure.suites[su].selectionState;
                    entry.id = id;
                    output[su] = entry;
                }
                if (input.currentLevel >= 1)
                {
                    if (su == input.suiteId)
                    {
                        if (input.currentLevel == 1)
                            output = new MenuEntryData[testStructure.suites[su].types.Count];
                        for (int ty = 0; ty < testStructure.suites[su].types.Count; ty++)
                        {
                            if (input.currentLevel == 1 && su == input.suiteId)
                            {
                                MenuEntryData entry = new MenuEntryData();
                                MenuTestEntry id = Menu.Instance.CloneMenuTestEntry(input);
                                id.currentLevel = 1;                            // Replace relevent data 
                                id.suiteId = su;                                // Replace relevent data 
                                id.typeId = ty;                                 // Replace relevent data 
                                entry.entryName = testStructure.suites[su].types[ty].typeName;
                                entry.selectionState = testStructure.suites[su].types[ty].selectionState;
                                entry.id = id;
                                output[ty] = entry;
                            }
                            if (input.currentLevel >= 2)
                            {
                                if (ty == input.typeId)
                                {
                                    if (input.currentLevel == 2)
                                        output = new MenuEntryData[testStructure.suites[su].types[ty].groups.Count];
                                    for (int gr = 0; gr < testStructure.suites[su].types[ty].groups.Count; gr++)
                                    {
                                        if (input.currentLevel == 2 && ty == input.typeId)
                                        {
                                            MenuEntryData entry = new MenuEntryData();
                                            MenuTestEntry id = Menu.Instance.CloneMenuTestEntry(input);
                                            id.currentLevel = 2;                            // Replace relevent data 
                                            id.suiteId = su;                                // Replace relevent data 
                                            id.typeId = ty;                                 // Replace relevent data 
                                            id.groupId = gr;                                 // Replace relevent data 
                                            entry.entryName = testStructure.suites[su].types[ty].groups[gr].groupName;
                                            entry.selectionState = testStructure.suites[su].types[ty].groups[gr].selectionState;
                                            entry.id = id;
                                            output[gr] = entry;
                                        }
                                        if (input.currentLevel >= 3)
                                        {
                                            if (gr == input.groupId)
                                            {
                                                if (input.currentLevel == 3)
                                                    output = new MenuEntryData[testStructure.suites[su].types[ty].groups[gr].tests.Count];
                                                for (int te = 0; te < testStructure.suites[su].types[ty].groups[gr].tests.Count; te++)
                                                {
                                                    if (input.currentLevel == 3 && gr == input.groupId)
                                                    {
                                                        MenuEntryData entry = new MenuEntryData();
                                                        MenuTestEntry id = Menu.Instance.CloneMenuTestEntry(input);
                                                        id.currentLevel = 3;                            // Replace relevent data 
                                                        id.suiteId = su;                                // Replace relevent data 
                                                        id.typeId = ty;                                 // Replace relevent data 
                                                        id.groupId = gr;                                 // Replace relevent data 
                                                        id.testId = te;                                 // Replace relevent data 
                                                        entry.entryName = testStructure.suites[su].types[ty].groups[gr].tests[te].testName;
                                                        entry.selectionState = testStructure.suites[su].types[ty].groups[gr].tests[te].selectionState;
                                                        entry.id = id;
                                                        output[te] = entry;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Returned entry list for level " + input.currentLevel); // Write to console
            return output;
        }

        // Gets the name of a specific entry in the test structure
        public string GetNameOfEntry(int level, int suiteIndex, int typeIndex, int groupIndex, int testIndex) // TODO - Move this to MenuEntryData input
        {
            string output = null;
            switch(level)
            {
                case 0:     // Suite
                    output = testStructure.suites[suiteIndex].suiteName;
                    break;
                case 1:     // Type
                    output = testStructure.suites[suiteIndex].types[typeIndex].typeName;
                    break;
                case 2:     // Scene
                    output = testStructure.suites[suiteIndex].types[typeIndex].groups[groupIndex].groupName;
                    break;
                case 3:     // Test
                    output = testStructure.suites[suiteIndex].types[typeIndex].groups[groupIndex].tests[testIndex].testName;
                    break;
            }
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Returned name of entry " + output); // Write to console
            return output;
        }
        
        // ------------------------------------------------------------------------------------
        // Set Selection
        // - TODO - Clean and comment this

        public void SetSelectionState(MenuEntryData entryData)
        {
            switch (entryData.id.currentLevel)
            {
                case 0:     // Suite
                    testStructure.suites[entryData.id.suiteId].selectionState = entryData.selectionState;
                    break;
                case 1:     // Type
                    testStructure.suites[entryData.id.suiteId].types[entryData.id.typeId].selectionState = entryData.selectionState;
                    break;
                case 2:     // Scene
                    testStructure.suites[entryData.id.suiteId].types[entryData.id.typeId].groups[entryData.id.groupId].selectionState = entryData.selectionState;
                    break;
                case 3:     // Test
                    testStructure.suites[entryData.id.suiteId].types[entryData.id.typeId].groups[entryData.id.groupId].tests[entryData.id.testId].selectionState = entryData.selectionState;
                    break;
            }
            SetSelectionStateOnChildren(entryData);
            SetSelectionStateOnParents(entryData);
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Changed selection state for entry " + entryData.entryName); // Write to console
        }

        // Master class for setting selection state recursively down the hierarchy. Calls sub-function per level for every necessary level
        void SetSelectionStateOnChildren(MenuEntryData entryData)
        {
            switch (entryData.id.currentLevel)
            {
                case 0:     // Suite
                    SetSelectionOfLevelDownward(entryData, 0);
                    break;
                case 1:     // Type
                    SetSelectionOfLevelDownward(entryData, 1);
                    break;
                case 2:     // Scene
                    SetSelectionOfLevelDownward(entryData, 2);
                    break;
            }
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Changed selection state for all children of entry " + entryData.entryName); // Write to console
        }

        // Set a selection state of a specific level based on its immediate children 
        void SetSelectionOfLevelDownward(MenuEntryData entryData, int setLevel) // TODO - Can we shorten this?
        {
            switch (setLevel)
            {
                case 0:     // Suite
                    for (int ty = 0; ty < testStructure.suites[entryData.id.suiteId].types.Count; ty++)
                    {
                        testStructure.suites[entryData.id.suiteId].types[ty].selectionState = entryData.selectionState;
                        for (int gr = 0; gr < testStructure.suites[entryData.id.suiteId].types[ty].groups.Count; gr++)
                        {
                            testStructure.suites[entryData.id.suiteId].types[ty].groups[gr].selectionState = entryData.selectionState;
                            for (int te = 0; te < testStructure.suites[entryData.id.suiteId].types[ty].groups[gr].tests.Count; te++)
                            {
                                testStructure.suites[entryData.id.suiteId].types[ty].groups[gr].tests[te].selectionState = entryData.selectionState;
                            }
                        }
                    }
                    break;
                case 1:     // Type
                    for (int gr = 0; gr < testStructure.suites[entryData.id.suiteId].types[entryData.id.typeId].groups.Count; gr++)
                    {
                        testStructure.suites[entryData.id.suiteId].types[entryData.id.typeId].groups[gr].selectionState = entryData.selectionState;
                        for (int te = 0; te < testStructure.suites[entryData.id.suiteId].types[entryData.id.typeId].groups[gr].tests.Count; te++)
                        {
                            testStructure.suites[entryData.id.suiteId].types[entryData.id.typeId].groups[gr].tests[te].selectionState = entryData.selectionState;
                        }
                    }
                    break;
                case 2:     // Scene
                    for (int i = 0; i < testStructure.suites[entryData.id.suiteId].types[entryData.id.typeId].groups[entryData.id.groupId].tests.Count; i++)
                    {
                        testStructure.suites[entryData.id.suiteId].types[entryData.id.typeId].groups[entryData.id.groupId].tests[i].selectionState = entryData.selectionState;
                    }
                    break;
            }
        }

        

        // Master class for setting selection state recursively up the hierarchy. Calls sub-function per level for every necessary level
        void SetSelectionStateOnParents(MenuEntryData entryData)
        {
            switch (entryData.id.currentLevel)
            {
                case 1:     // Type
                    SetSelectionOfLevelUpward(entryData, 0);
                    break;
                case 2:     // Scene
                    SetSelectionOfLevelUpward(entryData, 1);
                    SetSelectionOfLevelUpward(entryData, 0);
                    break;
                case 3:     // Test
                    SetSelectionOfLevelUpward(entryData, 2);
                    SetSelectionOfLevelUpward(entryData, 1);
                    SetSelectionOfLevelUpward(entryData, 0);
                    break;
            }
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Changed selection state for all parents of entry " + entryData.entryName); // Write to console
        }

        // Set a selection state of a specific level based on its immediate children 
        void SetSelectionOfLevelUpward(MenuEntryData entryData, int setLevel) // TODO - Can we shorten this?
        {
            int x = 0;
            switch (setLevel)
            {
                case 0:     // Suite
                    for(int i = 0; i < testStructure.suites[entryData.id.suiteId].types.Count; i++)
                    {
                        if(testStructure.suites[entryData.id.suiteId].types[i].selectionState == 2)
                            break;
                        else if (testStructure.suites[entryData.id.suiteId].types[i].selectionState == 0)
                            x--;
                        else
                            x++;
                    }
                    if (x == testStructure.suites[entryData.id.suiteId].types.Count)
                        testStructure.suites[entryData.id.suiteId].selectionState = 1;
                    else if (x == -testStructure.suites[entryData.id.suiteId].types.Count)
                        testStructure.suites[entryData.id.suiteId].selectionState = 0;
                    else
                        testStructure.suites[entryData.id.suiteId].selectionState = 2;
                    break;
                case 1:     // Type
                    for (int i = 0; i < testStructure.suites[entryData.id.suiteId].types[entryData.id.typeId].groups.Count; i++)
                    {
                        if (testStructure.suites[entryData.id.suiteId].types[entryData.id.typeId].groups[i].selectionState == 2)
                            break;
                        else if (testStructure.suites[entryData.id.suiteId].types[entryData.id.typeId].groups[i].selectionState == 0)
                            x--;
                        else
                            x++;
                    }
                    if (x == testStructure.suites[entryData.id.suiteId].types[entryData.id.typeId].groups.Count)
                        testStructure.suites[entryData.id.suiteId].types[entryData.id.typeId].selectionState = 1;
                    else if (x == -testStructure.suites[entryData.id.suiteId].types[entryData.id.typeId].groups.Count)
                        testStructure.suites[entryData.id.suiteId].types[entryData.id.typeId].selectionState = 0;
                    else
                        testStructure.suites[entryData.id.suiteId].types[entryData.id.typeId].selectionState = 2;
                    break;
                case 2:     // Scene
                    for (int i = 0; i < testStructure.suites[entryData.id.suiteId].types[entryData.id.typeId].groups[entryData.id.groupId].tests.Count; i++)
                    {
                        if (testStructure.suites[entryData.id.suiteId].types[entryData.id.typeId].groups[entryData.id.groupId].tests[i].selectionState == 2)
                            break;
                        else if (testStructure.suites[entryData.id.suiteId].types[entryData.id.typeId].groups[entryData.id.groupId].tests[i].selectionState == 0)
                            x--;
                        else
                            x++;
                    }
                    if (x == testStructure.suites[entryData.id.suiteId].types[entryData.id.typeId].groups[entryData.id.groupId].tests.Count)
                        testStructure.suites[entryData.id.suiteId].types[entryData.id.typeId].selectionState = 1;
                    else if (x == -testStructure.suites[entryData.id.suiteId].types[entryData.id.typeId].groups[entryData.id.groupId].tests.Count)
                        testStructure.suites[entryData.id.suiteId].types[entryData.id.typeId].groups[entryData.id.groupId].selectionState = 0;
                    else
                        testStructure.suites[entryData.id.suiteId].types[entryData.id.typeId].groups[entryData.id.groupId].selectionState = 2;
                    break;
            }
        }

        // ------------------------------------------------------------------------------------
        // Helper Functions
        // - TODO - Clean and comment this

        List<TestType> CloneTestTypeList(List<TestType> input)
        {
            List < TestType > output = new List<TestType>();
            for (int i = 0; i < input.Count; i++)
            {
                TestType type = new TestType();
                type.typeName = input[i].typeName;
                type.typeIndex = input[i].typeIndex;
                type.selectionState = input[i].selectionState;
                type.baseline = input[i].baseline;
                output.Add(type);
            }
            return output;
        }

        // ------------------------------------------------------------------------------------
        // Local Data Structures

        [Serializable]
        public class Structure
        {
            public List<Suite> suites = new List<Suite>();
        }

        [Serializable]
        public class Suite
        {
            public string suiteName;
            public int selectionState;
            public bool baseline;
            public List<TestType> types = new List<TestType>();
        }

        [Serializable]
        public class TestType
        {
            public string typeName;
            public int typeIndex;
            public int selectionState;
            public bool baseline;
            public List<Group> groups = new List<Group>();
        }

        [Serializable]
        public class Group
        {
            public string groupName;
            public int selectionState;
            public bool baseline;
            public List<Test> tests = new List<Test>();
        }

        [Serializable]
        public class Test
        {
            public string testName;
            public string scenePath;
            public int selectionState;
            public bool baseline;
        }

        [Serializable]
        public class TestResults : Test
        {
            public ResultsIOData dataA;
            public ResultsIOData dataB;
        }
    }
}
