using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GraphicsTestFramework
{
    // ------------------------------------------------------------------------------------
    // ResultsViewer
    // - The main ResultsViewer controller
    // - Controls results viewer context
    // - Distributes context viewer operations to correct Display scripts

    public class ResultsViewer : MonoBehaviour
    {
        // ------------------------------------------------------------------------------------
        // Variables

        // Singleton
        private static ResultsViewer _Instance = null;
        public static ResultsViewer Instance
        {
            get
            {
                if (_Instance == null)
                    _Instance = (ResultsViewer)FindObjectOfType(typeof(ResultsViewer));
                return _Instance;
            }
        }

        // Main
        int viewerState; // Track state
        bool isGenerated; // Track generation complete
        public GameObject resultsViewerParent;
        List<ResultsEntryData> filteredResultsEntries = new List<ResultsEntryData>();
        // Home buttons
        public Button homeButton;
        public Button overviewButton;
        // Filters
        public Dropdown suitesDropdown;
        public Dropdown typesDropdown;
        public Dropdown resultsDropdown;
        // Overview
        public GameObject overviewParent;
        public OverviewResultsWindow overviewResultsWindow;
        public Image piePass;
        public Image pieFail;
        // Detailed results
        public GameObject detailedResultsParent;
        public RectTransform listContentRect;
        public GameObject resultsEntryPrefab;
        public GameObject resultsEntryTitlePrefab;
        List<GameObject> listEntries = new List<GameObject>();
        float entryHeight;
        float listHeight;
        // Detailed results titles
        public Transform viewportCorner;
        public ResultsEntry hoverTitleEntry;
        public List<ResultsEntry> titleEntriesAbove = new List<ResultsEntry>();
        public List<ResultsEntry> titleEntriesBelow = new List<ResultsEntry>();
        // Context entry
        ResultsEntry activeContextEntry;
        GameObject activeContextObject;

        // ------------------------------------------------------------------------------------
        // Core

        private void Start()
        {
            Menu.Instance.MakeCanvasScaleWithScreenSize(resultsViewerParent);
        }

        // Every update
        private void Update()
        {
            if (viewerState == 2 && isGenerated) // If viewing detailed results and generation complete
                CheckForTitleChange(); // Check for moving titles
        }

        // ------------------------------------------------------------------------------------
        // State

        // Initial setup
        public void Setup()
        {
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Setup"); // Write to console
            GenerateSuitesDropdown(); // Generate suite dropdown
            GenerateTypesDropdown(); // Generate types dropdown
            GenerateResultsDropdown(); // Generate results dropdown
        }

        // Set the UI state
        public void SetState(int input)
        {
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Setting state"); // Write to console
            viewerState = input; // Track state
            isGenerated = false; // Track is generated
            switch (viewerState) // Switch UI objects based on state
            {
                case 0: // Back to menu
                    overviewParent.SetActive(false);
                    detailedResultsParent.SetActive(false);
                    resultsViewerParent.SetActive(false);
                    homeButton.gameObject.SetActive(false);
                    overviewButton.gameObject.SetActive(false);
                    Navigation.Instance.ReturnHome();
                    break;
                case 1: // Overview
                    overviewParent.SetActive(true);
                    detailedResultsParent.SetActive(false);
                    resultsViewerParent.SetActive(true);
                    homeButton.gameObject.SetActive(true);
                    overviewButton.gameObject.SetActive(false);
                    GenerateContent(false); // Main call for generation of viewer content]
                    Menu.Instance.SetMenuState(false);
                    break;
                case 2: // Detailed Results
                    overviewParent.SetActive(false);
                    detailedResultsParent.SetActive(true);
                    resultsViewerParent.SetActive(true);
                    homeButton.gameObject.SetActive(false);
                    overviewButton.gameObject.SetActive(true);
                    GenerateContent(false); // Main call for generation of viewer content
                    RefreshMenu(); // Refresh menu - WORKAROUND
                    Menu.Instance.SetMenuState(false);
                    break;
                case 3: // First time overview
                    Setup();
                    overviewParent.SetActive(true);
                    detailedResultsParent.SetActive(false);
                    resultsViewerParent.SetActive(true);
                    homeButton.gameObject.SetActive(true);
                    overviewButton.gameObject.SetActive(false);
                    viewerState = 1; // Set back for content generation
                    GenerateContent(true); // Main call for generation of viewer content
                    Menu.Instance.SetMenuState(false);
                    break;
                case 4: // View test
                    overviewParent.SetActive(false);
                    detailedResultsParent.SetActive(false);
                    resultsViewerParent.SetActive(false);
                    homeButton.gameObject.SetActive(false);
                    overviewButton.gameObject.SetActive(false);
                    Menu.Instance.SetMenuState(false);
                    break;
                case 5: // Analytic mode
                    Setup();
                    overviewParent.SetActive(true);
                    detailedResultsParent.SetActive(false);
                    resultsViewerParent.SetActive(true);
                    homeButton.gameObject.SetActive(true);
                    overviewButton.gameObject.SetActive(false);
                    viewerState = 1; // Set back for content generation
                    GenerateContent(true); // Main call for generation of viewer content
                    Menu.Instance.SetMenuState(false);
                    break;
            }
        }

        // TODO - Workaround for ScrollRect update bug in 2017.1+
        // Remove when bug is fixed
        void RefreshMenu()
        {
            Canvas canvas = resultsViewerParent.GetComponent<Canvas>();
            canvas.enabled = false;
            canvas.enabled = true;
        }

        // ------------------------------------------------------------------------------------
        // Filters

        // Get suite names and generate dropdown content
        void GenerateSuitesDropdown()
        {
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Generating suites dropdown"); // Write to console
            List<Dropdown.OptionData> options = new List<Dropdown.OptionData>(); // Create new options list
            options.Add(Common.ConvertStringToDropdownOptionData("All Suites"));
            for (int i = 0; i < TestStructure.Instance.testStructure.suites.Count; i++) // Iterate suites
                options.Add(Common.ConvertStringToDropdownOptionData(TestStructure.Instance.testStructure.suites[i].suiteName)); // Convert string to option data and add
            suitesDropdown.options = options; // Add options
        }

        // Get type names and generate dropdown content
        void GenerateTypesDropdown()
        {
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Generating types dropdown"); // Write to console
            List<Dropdown.OptionData> options = new List<Dropdown.OptionData>(); // Create new options list
            options.Add(Common.ConvertStringToDropdownOptionData("All Types"));
            string[] types = TestTypes.GetTypeStringList(); // Get type names
            for (int i = 0; i < types.Length; i++) // Iterate types
                options.Add(Common.ConvertStringToDropdownOptionData(types[i])); // Convert string to option data and add
            typesDropdown.options = options; // Add options
        }

        // Generate results filter dropdown content
        void GenerateResultsDropdown()
        {
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Generating results dropdown"); // Write to console
            List<Dropdown.OptionData> options = new List<Dropdown.OptionData>(); // Create new options list
            options.Add(Common.ConvertStringToDropdownOptionData("All Results"));
            string[] results = new string[4] { "Pass", "Fail", "Ran", "Not Ran" }; // Get results entries
            for (int i = 0; i < results.Length; i++) // Iterate
                options.Add(Common.ConvertStringToDropdownOptionData(results[i])); // Convert string to option data and add
            resultsDropdown.options = options; // Add options
        }

        // Set results dropdown state based on ResultsViewer state
        void SetResultsDropdownState(bool input)
        {
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Settings results dropdown state"); // Write to console
            switch (input)
            {
                case true:
                    resultsDropdown.interactable = true; // Enable
                    break;
                case false:
                    resultsDropdown.value = 0; // Reset value
                    resultsDropdown.interactable = false; // Disable
                    break;
            }
        }

        // ------------------------------------------------------------------------------------
        // Content Main

        // Generate all content for detailed results based on filters
        public void GenerateContent(bool regenFilteredResults)
        {
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Generating content"); // Write to console
            StartCoroutine(ProcessGenerateContent(regenFilteredResults)); // Process content generation
        }

        // Process generation of content
        IEnumerator ProcessGenerateContent(bool regenFilteredResults)
        {
            ProgressScreen.Instance.SetState(true, ProgressType.LocalLoad, "Getting results data"); // Enable progress screen
            while (!ProgressScreen.Instance.GetState()) // Wait for progress screen
                yield return null;
            switch (viewerState)
            {
                case 0: // Back to menu
                    break;
                case 1: // Overview
                    if (regenFilteredResults) // If regenerate filteres results list
                        yield return StartCoroutine(GenerateFilteredResultList()); // Generate filtered result list
                    GenerateOverview(); // Generate overview content
                    break;
                case 2: // Detailed Results
                    if (regenFilteredResults) // If regenerate filteres results list
                        yield return StartCoroutine(GenerateFilteredResultList()); // Generate filtered result list
                    yield return StartCoroutine(GenerateDetailedResultsList()); // Generate detailed results list
                    break;
            }
            ProgressScreen.Instance.SetState(false, ProgressType.LocalLoad, "Getting results data"); // Disable progress screen
            isGenerated = true; // Track is generated
        }

        // Generate a list of results based on selected filters
        public IEnumerator GenerateFilteredResultList()
        {
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Filtering results"); // Write to console
            filteredResultsEntries.Clear(); // Clear current
            TestStructure.Structure structure = TestStructure.Instance.GetStructure(); // Get structure
            for (int su = 0; su < structure.suites.Count; su++) // Iterate suites 
            {
                if (su == suitesDropdown.value - 1 || suitesDropdown.value == 0) // If passes suites filter
                {
                    string suiteName = structure.suites[su].suiteName; // Get suite name
                    for (int ty = 0; ty < structure.suites[su].types.Count; ty++) // Iterate types
                    {
                        int typeIndex = structure.suites[su].types[ty].typeIndex; // Get type index
                        if (typeIndex == typesDropdown.value - 1 || typesDropdown.value == 0) // If passes types filter
                        {
                            string typeName = structure.suites[su].types[ty].typeName; // Get type name
                            for (int gr = 0; gr < structure.suites[su].types[ty].groups.Count; gr++) // Iterate groups
                            {
                                string groupName = structure.suites[su].types[ty].groups[gr].groupName; // Get group name
                                for (int te = 0; te < structure.suites[su].types[ty].groups[gr].tests.Count; te++) // Iterate tests
                                {
                                    string testName = structure.suites[su].types[ty].groups[gr].tests[te].testName; // Get test name
                                    string scenePath = structure.suites[su].types[ty].groups[gr].tests[te].scenePath; // Get scene path
                                    ResultsDataCommon common = BuildResultsDataCommon(groupName, testName); // Build results data common to retrieve results
                                    ResultsIOData data = new ResultsIOData();
                                    var testResults = structure.suites[su].types[ty].groups[gr].tests[te] as TestStructure.TestResults;                                    if(TestRunner.Instance)
                                    {
                                        if (!TestRunner.Instance.isAnalytic)
                                            data = ResultsIO.Instance.RetrieveEntry(suiteName, typeName, common, false, false); // Retrieve results data
                                        else
                                        {
                                            data = testResults.dataA;
                                        }  
                                    }
                                    else
                                    {
                                        data = ResultsIO.Instance.RetrieveEntry(suiteName, typeName, common, false, false); // Retrieve results data
                                    }
                                    ResultsIOData dataB = null;
                                    if(testResults != null)
                                        dataB = testResults.dataB;

                                    if (resultsDropdown.value != 0) // If filtering based on results
                                    {
                                        int passFail = 2; // Set default state (no results)
                                        int passFailColumn = Common.FindResultsDataIOFieldIdByName(data, "PassFail"); // Find pass fail column index
                                        if (data != null) // If results data exists
                                            passFail = data.resultsRow[0].resultsColumn[passFailColumn] == "True" ? 0 : 1; // Set pass fail state
                                        
                                        

                                        switch (resultsDropdown.value)
                                        {
                                            case 1: // Pass
                                                if (passFail == 0)
                                                    filteredResultsEntries.Add(new ResultsEntryData(new TestEntry(suiteName, groupName, scenePath, typeName, testName, typeIndex, su, gr, ty, te), data, dataB)); // Add to list
                                                break;
                                            case 2: // Fail
                                                if (passFail == 1)
                                                    filteredResultsEntries.Add(new ResultsEntryData(new TestEntry(suiteName, groupName, scenePath, typeName, testName, typeIndex, su, gr, ty, te), data, dataB)); // Add to list
                                                break;
                                            case 3: // Ran
                                                if (passFail != 2)
                                                    filteredResultsEntries.Add(new ResultsEntryData(new TestEntry(suiteName, groupName, scenePath, typeName, testName, typeIndex, su, gr, ty, te), data, dataB)); // Add to list
                                                break;
                                            case 4: // Not Ran
                                                if (passFail == 2)
                                                    filteredResultsEntries.Add(new ResultsEntryData(new TestEntry(suiteName, groupName, scenePath, typeName, testName, typeIndex, su, gr, ty, te), data, dataB)); // Add to list
                                                break;
                                        }
                                    }
                                    else
                                        filteredResultsEntries.Add(new ResultsEntryData(new TestEntry(suiteName, groupName, scenePath, typeName, testName, typeIndex, su, gr, ty, te), data, dataB)); // Add to list
                                    yield return null;
                                }
                            }
                        }
                    }
                }
            }
        }

        // ------------------------------------------------------------------------------------
        // Overview

        // Generate overview content
        void GenerateOverview()
        {
            int totalTests = filteredResultsEntries.Count; // Get total tests
            int testsPassed = 0; // Track tests passed
            int testsFailed = 0; // Track tests failed
            for (int i = 0; i < filteredResultsEntries.Count; i++) // Iterate filtered results
            {
                int passFail = 2; // Set default state (no results)
                if (filteredResultsEntries[i].resultsData != null) // If results data exists
                {
                    int passFailIndex = -1;
                    for (int f = 0; f < filteredResultsEntries[i].resultsData.fieldNames.Count; f++)
                    {
                        if (filteredResultsEntries[i].resultsData.fieldNames[f] == "PassFail")
                            passFailIndex = f;
                    }
                    passFail = filteredResultsEntries[i].resultsData.resultsRow[0].resultsColumn[passFailIndex] == "True" ? 0 : 1; // Set pass fail state
                }
                switch (passFail)
                {
                    case 0: // Pass
                        testsPassed++; // Increment
                        break;
                    case 1: // Fail
                        testsFailed++; // Increment
                        break;
                }
            }
            int testsRan = testsPassed + testsFailed; // Get tests ran
            int testsNotRan = totalTests - testsRan; // Get tests not ran
            float passPercentage = Common.SafeDivision((float)testsPassed, (float)totalTests) * 100; // Get pass percentage
            float failPercentage = Common.SafeDivision((float)testsFailed, (float)totalTests) * 100; // Get fail percentage
            float ranPercentage = Common.SafeDivision((float)testsRan, (float)totalTests) * 100; // Get ran percentage
            float notRanPercentage = Common.SafeDivision((float)testsNotRan, (float)totalTests) * 100; // Get not ran percentage
            overviewResultsWindow.totalTests.text = totalTests.ToString(); // Set to UI
            overviewResultsWindow.testsRan.text = testsRan.ToString(); // Set to UI
            overviewResultsWindow.testsPassed.text = testsPassed.ToString(); // Set to UI
            overviewResultsWindow.testsFailed.text = testsFailed.ToString(); // Set to UI
            overviewResultsWindow.testsNotRan.text = testsNotRan.ToString(); // Set to UI
            overviewResultsWindow.passPercentage.text = passPercentage.ToString("0.##") + "%"; // Set to UI
            overviewResultsWindow.failPercentage.text = failPercentage.ToString("0.##") + "%"; // Set to UI
            overviewResultsWindow.ranPercentage.text = ranPercentage.ToString("0.##") + "%"; // Set to UI
            overviewResultsWindow.notRanPercentage.text = notRanPercentage.ToString("0.##") + "%"; // Set to UI
            string runID = TestRunner.runUUID == "null" ? "No Run ID" : TestRunner.runUUID;
            overviewResultsWindow.lastRunID.interactable = runID == "No Run ID" ? false : true;
            overviewResultsWindow.lastRunID.text = runID; // Set the last run ID if exists
            piePass.fillAmount = passPercentage / 100; // Set pie value
            pieFail.fillAmount = (passPercentage / 100) + (failPercentage / 100); // Set pie value
        }

        // ------------------------------------------------------------------------------------
        // Detailed Results List

        // Generate main list
        IEnumerator GenerateDetailedResultsList()
        {
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Generating list"); // Write to console
            yield return null;
            ClearDetailedResultsList(); // Clear current list
            ClearDetailedResultsTitleEntryLists(); // Clear title lists
            DestroyContextEntry(); // Destroy the context object
            TestLogicBase logic = null; // Track previous logic
            string previousSuite = ""; // Track previous suite
            string previousType = ""; // Track previous type
            for (int i = 0; i < filteredResultsEntries.Count; i++) // Iterate filtered results
            {
                if (previousSuite != filteredResultsEntries[i].testEntry.suiteName || previousType != filteredResultsEntries[i].testEntry.typeName) // New suite or type
                    GenerateNewTitleEntry(filteredResultsEntries[i].testEntry); // Generate new title entry
                if (!logic || filteredResultsEntries[i].testEntry.typeName != previousType) // If logic doesnt match previous type
                    logic = TestTypeManager.Instance.GetLogicInstanceFromName(filteredResultsEntries[i].testEntry.typeName); // Get logic instance
                previousSuite = filteredResultsEntries[i].testEntry.suiteName; // Track previous suite
                previousType = filteredResultsEntries[i].testEntry.typeName; // Track previous type
                ResultsEntry newEntry = GenerateResultsEntry(resultsEntryPrefab); // Generate a ResultsEntry
                newEntry.Setup(filteredResultsEntries[i], logic); // Setup the instance
                if (entryHeight == 0) // Track entry height
                    entryHeight = newEntry.GetComponent<RectTransform>().sizeDelta.y;
                listHeight -= entryHeight; // Track height for next entry
            }
            listContentRect.sizeDelta = new Vector2(listContentRect.sizeDelta.x, -listHeight); // Set content rect size
        }

        // Generate a results entry of a prefab type and return
        ResultsEntry GenerateResultsEntry(GameObject prefab)
        {
            GameObject go = Instantiate(prefab, listContentRect, false); // Create results title entry instance
            listEntries.Add(go); // Add to list
            RectTransform goRect = go.GetComponent<RectTransform>(); // Get rect
            goRect.anchoredPosition = new Vector2(0, listHeight); // Set position
            return go.GetComponent<ResultsEntry>(); // Get ResultsEntry reference
        }

        // Clear main list
        void ClearDetailedResultsList()
        {
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Clearing list"); // Write to console
            foreach (GameObject go in listEntries) // Iterate current list entries
                Destroy(go); // Delete them
            listEntries.Clear(); // Clear the list
            listHeight = 0; // Reset
        }

        // Find an entries index from its instance
        int FindEntryInDetailedResultsList(ResultsEntry inputEntry)
        {
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Finding entry in list"); // Write to console
            for (int i = 0; i < listEntries.Count; i++) // Iterate current entries
            {
                if (listEntries[i] == inputEntry.gameObject) // If the entry matches
                    return i; // Return the index
            }
            return -1; // Return fail
        }

        // Nudge list entries up or down to create space for context object
        void NudgeDetailedResultsListEntries(int startIndex, float nudgeAmount)
        {
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Nudging list entries from start index " + startIndex); // Write to console
            for (int i = startIndex + 1; i < listEntries.Count; i++) // Iterate entries after the start index
            {
                RectTransform entryRect = listEntries[i].GetComponent<RectTransform>(); // Get rect reference
                entryRect.anchoredPosition = new Vector2(entryRect.anchoredPosition.x, entryRect.anchoredPosition.y + nudgeAmount); // Nudge the entry
            }
        }

        // ------------------------------------------------------------------------------------
        // Detailed Results Title Entry

        // Generate a new title entry
        void GenerateNewTitleEntry(TestEntry input)
        {
            string title = input.suiteName + " - " + input.typeName; // If new suite add new suite name to title
            ResultsEntry newEntry = GenerateResultsEntry(resultsEntryTitlePrefab); // Generate a results entry
            newEntry.SetupTitle(title); // Setup the title instance
            if (titleEntriesBelow.Count == 0) // If first entry
                SetHoverTitleEntry(title); // Set hover title
            titleEntriesBelow.Add(newEntry); // Add to list of titles below top of scroll rect
            listHeight -= newEntry.GetComponent<RectTransform>().sizeDelta.y; // Track height for next entry
        }

        // Clear both detailed results entry lists
        void ClearDetailedResultsTitleEntryLists()
        {
            titleEntriesAbove.Clear(); // Clear entries above top of scroll rect
            titleEntriesBelow.Clear(); // Clear entries below top of scroll rect
        }

        // Check for a change in the active title (every frame)
        void CheckForTitleChange()
        {
            if (titleEntriesAbove.Count > 0) // If there is any titles above the viewport
            {
                if (titleEntriesAbove[0].transform.position.y < viewportCorner.position.y) // If the first entry moves below viewport top
                    MoveTitleEntryInLists(titleEntriesAbove[0]); // Move the entry
            }
            if (titleEntriesBelow.Count > 0) //  If there is any titles below the viewport
            {
                if (titleEntriesBelow[0].transform.position.y > viewportCorner.position.y) // If the first entry moves above viewport top
                    MoveTitleEntryInLists(titleEntriesBelow[0]); // Move the entry
            }
        }

        // Move a title entry from one loist to another and update hover entry
        void MoveTitleEntryInLists(ResultsEntry input)
        {
            bool isAbove = false; // Set to track list found in
            foreach (ResultsEntry r in titleEntriesAbove) // Iterate above list
            {
                if (r == input) // If requested
                    isAbove = true; // Set true
            }
            if (isAbove) // If requested is above
            {
                titleEntriesAbove.Remove(input); // Remove from above
                titleEntriesBelow.Insert(0, input); // Add to below
            }
            else
            {
                titleEntriesBelow.Remove(input); // Remove from below
                titleEntriesAbove.Insert(0, input); // Add to above
            }
            SetHoverTitleEntry(); // Set hove title
        }

        // Set hover title entry to title above the viewport
        void SetHoverTitleEntry()
        {
            if (titleEntriesAbove.Count > 0) // If title exists above the viewport
                hoverTitleEntry.SetupTitle(titleEntriesAbove[0].testNameText.text); // Setup the title instance
        }

        // Set hover title entry to specified
        void SetHoverTitleEntry(string input)
        {
            hoverTitleEntry.SetupTitle(input); // Setup the title instance
        }

        // ------------------------------------------------------------------------------------
        // Detailed Results Context Object

        // Toggle the context object
        public void ToggleContextObject(ResultsEntry inputEntry, TestDisplayBase display)
        {
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Toggling context object for " + inputEntry); // Write to console
            if (activeContextObject == null) // If context object is null
                StartCoroutine(ExpandContextObject(inputEntry, display)); // Create and expand
            else
            {
                if (activeContextEntry == inputEntry) // If selected entry matches current context
                    HideContextObject(inputEntry); // Hide it
                else
                {
                    HideContextObject(activeContextEntry); // Hide the current
                    StartCoroutine(ExpandContextObject(inputEntry, display)); // Create and expand
                }
            }
            RefreshMenu(); // Refresh menu - WORKAROUND   
        }

        // Create and expand context object
        IEnumerator ExpandContextObject(ResultsEntry inputEntry, TestDisplayBase display)
        {
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Expanding context object"); // Write to console
            int entryIndex = FindEntryInDetailedResultsList(inputEntry); // Get index of selected entry
            activeContextEntry = inputEntry; // Track selected entry
            activeContextObject = Instantiate(display.resultsContextPrefab, listContentRect, false); // Create context object instance
            RectTransform contextObjectRect = activeContextObject.GetComponent<RectTransform>(); // Get rect
            contextObjectRect.anchoredPosition = new Vector2(0, (entryIndex + 1) * -listEntries[0].GetComponent<RectTransform>().sizeDelta.y); // Set position
            listContentRect.sizeDelta = new Vector2(listContentRect.sizeDelta.x, listContentRect.sizeDelta.y + contextObjectRect.sizeDelta.y); // Set size
            NudgeDetailedResultsListEntries(entryIndex, -contextObjectRect.sizeDelta.y); // Nudge entries
            ResultsContext resultsContext = activeContextObject.GetComponent<ResultsContext>(); // Get results context reference
            resultsContext.Setup(activeContextEntry); // Setup base of results context
            ResultsIOData resultsDataFull = new ResultsIOData(); // Create output data
            yield return StartCoroutine(SQL.SQLIO.Instance.FetchSpecificEntry(inputEntry.resultsEntryData.resultsData, (value => { resultsDataFull = value; }))); // Get full results data
            ResultsIOData resultsDataFullB = new ResultsIOData(); // Create output data
            if(TestRunner.Instance)
            {
                if(TestRunner.Instance.runnerType == RunnerType.Analytic)
                {
                    yield return StartCoroutine(SQL.SQLIO.Instance.FetchBaseline(inputEntry.resultsEntryData.resultsDataB, (value => { resultsDataFullB = value; }))); // Get full results data
                }
                else if(TestRunner.Instance.runnerType == RunnerType.AnalyticComparison)
                {
                    yield return StartCoroutine(SQL.SQLIO.Instance.FetchSpecificEntry(inputEntry.resultsEntryData.resultsDataB, (value => { resultsDataFullB = value; }))); // Get full results data
                }
            }
            //if(inputEntry.resultsEntryData.resultsDataB != null)
            display.SetupResultsContext(resultsContext, resultsDataFull, resultsDataFullB); // Tell Display how to setup the results context
        }

        // Hide and destroy context object
        void HideContextObject(ResultsEntry inputEntry)
        {
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Hiding context object"); // Write to console
            int entryIndex = FindEntryInDetailedResultsList(inputEntry); // Get index of selected entry
            NudgeDetailedResultsListEntries(entryIndex, activeContextObject.GetComponent<RectTransform>().sizeDelta.y); // Nudge entries
            listContentRect.sizeDelta = new Vector2(listContentRect.sizeDelta.x, listContentRect.sizeDelta.y - activeContextObject.GetComponent<RectTransform>().sizeDelta.y); // Set size
            DestroyContextEntry(); // Destroy
            Resources.UnloadUnusedAssets(); // Cleanup previous textures
        }

        // Destroy context object
        void DestroyContextEntry()
        {
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Destroying context object"); // Write to console
            if (activeContextObject) // If exists
            {
                Destroy(activeContextObject); // Destroy object
                activeContextObject = null; // Set null
            }
        }

        // ------------------------------------------------------------------------------------
        // Helper Functions

        // TODO - Should this be global?
        ResultsDataCommon BuildResultsDataCommon(string sceneName, string testName)
        {
            ResultsDataCommon common = new ResultsDataCommon();
            SystemData systemData = Master.Instance.GetSystemData();
            common.Platform = systemData.Platform;
            common.API = systemData.API;
            common.RenderPipe = "Standard Legacy"; // TODO - Implement SRP support
            common.GroupName = sceneName;
            common.TestName = testName;
            return common;
        }

        // ------------------------------------------------------------------------------------
        // Local Data Structures

        [System.Serializable]
        public class OverviewResultsWindow
        {
            public Text totalTests;
            public Text testsRan;
            public Text testsPassed;
            public Text testsFailed;
            public Text testsNotRan;
            public Text passPercentage;
            public Text failPercentage;
            public Text ranPercentage;
            public Text notRanPercentage;
            public InputField lastRunID;
        }
    }
}
