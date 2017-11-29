using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GraphicsTestFramework
{
    // ------------------------------------------------------------------------------------
    // Menu
    // - Main menu controller

    public class Menu : MonoBehaviour
    {
        // ------------------------------------------------------------------------------------
        // Variables

        // Singleton
        private static Menu _Instance = null;
        public static Menu Instance
        {
            get
            {
                if (_Instance == null)
                    _Instance = (Menu)FindObjectOfType(typeof(Menu));
                return _Instance;
            }
        }

        // References
        public GameObject menuParent;
        public List list;
        public Breadcrumb breadcrumb;
        private bool topLevel = true;//bool for checking if the breadcrumb is at the top level
        public Actions actions;
        public ResolveWindow resolveWindow;
        public Color[] colors;
        public GameObject menuListEntryPrefab;
        public GameObject listEntryPrefab;

        // Data
        public MenuTestEntry selectedId;
        MenuListEntry selectedTest;
        float entryHeight;

        // ------------------------------------------------------------------------------------
        // Initialization

        // Start
        private void Start()
        {
            StartCoroutine(WaitForTestStructure()); // Begin waiting for TestStructure
        }

        // Have to wait for Test Structure to generate before generating menus (TODO - move this to a delegate?)
        IEnumerator WaitForTestStructure()
        {
            do { yield return null; } while (!TestStructure.Instance.IsGenerated); // Wait for test structure
            if (Common.GetArg("automation") == "true") // Check for automation arg
                RunFullAutomation(); // Bypass menu and run full automation
            else
                Initialize(); // Initialize the menu
        }

        // Generate the breadcrumb
        public void GenerateBreadcrumb(MenuEntryData inputData)
        {
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Generating breadcrumb"); // Write to console
            switch (inputData.id.currentLevel) // Switch based on current menu level and setup all entries
            {
                case -1:     // Home
                    breadcrumb.suite.Setup(inputData, 1, 0);
                    breadcrumb.type.Setup(inputData, 1, 1);
                    breadcrumb.group.Setup(inputData, 1, 2);
                    topLevel = true;
                    break;
                case 0:     // Suite
                    breadcrumb.suite.Setup(inputData, 0, 0);
                    breadcrumb.type.Setup(inputData, 1, 1);
                    breadcrumb.group.Setup(inputData, 1, 2);
                    topLevel = false;
                    break;
                case 1:     // Type
                    breadcrumb.suite.Setup(inputData, 2, 0);
                    breadcrumb.type.Setup(inputData, 0, 1);
                    breadcrumb.group.Setup(inputData, 1, 2);
                    topLevel = false;
                    break;
                case 2:     // Scene
                    breadcrumb.suite.Setup(inputData, 2, 0);
                    breadcrumb.type.Setup(inputData, 2, 1);
                    breadcrumb.group.Setup(inputData, 0, 2);
                    topLevel = false;
                    break;
            }
        }

        // Setup the actions panel
        void SetupActions()
        {
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Generating actions"); // Write to console
            actions.run.onClick.RemoveAllListeners(); // Remove listeners
            actions.run.onClick.AddListener(delegate { OnRunButtonClick(); }); // Add listsner
            actions.view.onClick.RemoveAllListeners(); // Remove listeners
            actions.view.onClick.AddListener(delegate { OnViewButtonClick(); }); // Add listsner
        }

        // ------------------------------------------------------------------------------------
        // Automation

        void RunFullAutomation()
        {
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Running full automation"); // Write to console
            if (TestStructure.Instance.CheckForBaselines()) // Baselines exist
            {
                GenerateList(); // Generate list
                for (int su = 0; su < list.entryList.Count; su++) // Iterate suites
                {
                    list.entryList[su].GetComponent<MenuListEntry>().ChangeSelection(1); // Select all
                }
                OnRunButtonClick(); // Run
            }
            else // Any or all baselines are missing
            {
                Debug.LogError("No baselines found. Aborting"); // Debug
                Common.QuitApplication(); // Quick application
            }
        }

        // ------------------------------------------------------------------------------------
        // Menu State

        // First time initialize
        void Initialize()
        {
            UpdateMenu(); // Update menu
        }

        // Enable/disable menu
        public void SetMenuState(bool state)
        {
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Setting menu state to " + state); // Write to console
            menuParent.SetActive(state); // Set active
            if (state) // If enabling
                UpdateMenu(); // Update
        }

        // Update the menu
        void UpdateMenu()
        {
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Updating menu"); // Write to console
            if (TestStructure.Instance.CheckForBaselines()) // Baselines exist
            {
                if (selectedId.currentLevel == 4) // If returning to meny from TestViewer go back one level
                    selectedId.currentLevel = 3; // Set
                ClearList(); // Clear list
                breadcrumb.home.SetupHome(); // Setup breadcrumb home button
                GenerateList(); // Generate list
                SetupActions(); // Setup actions panel
            }
            else // Any or all baselines are missing
                GenerateResolveList(); // Generate a resolve list
            RefreshMenu(); // Refresh menu - WORKAROUND
        }

        // TODO - Workaround for ScrollRect update bug in 2017.1+
        // Remove when bug is fixed
        void RefreshMenu()
        {
            Canvas canvas = menuParent.GetComponent<Canvas>();
            canvas.enabled = false;
            canvas.enabled = true;
        }

        // ------------------------------------------------------------------------------------
        // Resolve Menu
        // - Overrides main menu
        // - Used for resolving problems with test structure (Missing baselines)

        // Generate a resolve list
        void GenerateResolveList()
        {
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Generating resolve list"); // Write to console
            GenerateTestRunner(RunnerType.Resolve); // Generate a test runner of type Resolve
            ProgressScreen.Instance.SetState(false, ProgressType.LocalLoad, ""); // disable progress screen
            EnableResolveMenu(); // Enable resolve menu
        }

        // Enable the resolve menu
        void EnableResolveMenu()
        {
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Enabling resolve menu"); // Write to console
            resolveWindow.parent.SetActive(true); // Enable resolve menu
            resolveWindow.message.text = TestRunner.Instance.runner.tests.Count + " baseline files are missing. This situation must be resolved before continuing. Manually resolve baselines for the listed tests."; // Set text
            float entryHeight = 0; // Reset
            for (int i = 0; i < TestRunner.Instance.runner.tests.Count; i++) // Iterate items to resolve
            {
                TestEntry currentTest = TestRunner.Instance.runner.tests[i]; // Get data
                GameObject go = Instantiate(listEntryPrefab, resolveWindow.contentRect, false); // Create instance
                RectTransform goRect = go.GetComponent<RectTransform>(); // Get rect
                goRect.anchoredPosition = new Vector2(0, entryHeight); // Set position
                MenuResolveListEntry newEntry = go.GetComponent<MenuResolveListEntry>(); // Get script reference
                newEntry.Setup(currentTest.suiteName + " - " + currentTest.typeName, currentTest.groupName + " - " + currentTest.testName); // Setup
                entryHeight -= goRect.sizeDelta.y; // Add to position tracker
            }
            resolveWindow.contentRect.sizeDelta = new Vector2(resolveWindow.contentRect.sizeDelta.x, -entryHeight); // Set content rect size
        }

        // When chicking resolve button
        public void OnClickResolve()
        {
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Clicked: Resolve"); // Write to console
            CleanupResolveMenu(); // Clean up
            TestRunner.Instance.StartTests(); // Start tests
        }

        // Cleanup
        void CleanupResolveMenu()
        {
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Cleaning up resolve menu"); // Write to console
            int listCount = resolveWindow.contentRect.transform.childCount; // Get resolve list entries
            for (int i = 0; i < listCount; i++) // Iterate
                Destroy(resolveWindow.contentRect.GetChild(i).gameObject); // Destroy them
            resolveWindow.parent.SetActive(false); // Disable the window
        }

        // ------------------------------------------------------------------------------------
        // List Management

        // Generate a new list
        public void GenerateList()
        {
            MenuEntryData[] itemList = TestStructure.Instance.GetEntries(selectedId); // Get entries based on selected
            if (itemList != null)
            {
                list.scrollRect.enabled = false; // Disable scrollRect to update transform correctly
                Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Generating list"); // Write to console
                for (int i = 0; i < itemList.Length; i++)
                {
                    GameObject go = Instantiate(menuListEntryPrefab, list.contentRect, false); // Create instance
                    list.entryList.Add(go); // Add to object list
                    RectTransform goRect = go.GetComponent<RectTransform>(); // Get rect
                    goRect.anchoredPosition = new Vector2(0, entryHeight); // Set position
                    MenuListEntry newEntry = go.GetComponent<MenuListEntry>(); // Get script reference
                    newEntry.Setup(itemList[i]); // Setup
                    entryHeight -= goRect.sizeDelta.y; // Add to position tracker
                }
                list.contentRect.sizeDelta = new Vector2(list.contentRect.sizeDelta.x, -entryHeight); // Set content rect size
                list.scrollRect.enabled = true; // Enable scrollRect to update transform correctly
                Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Finished generating list"); // Write to console
            }
            else
                Console.Instance.Write(DebugLevel.Critical, MessageLevel.Log, "Failed to generate list"); // Write to console
            RefreshMenu();
        }

        // Clear current list
        public void ClearList()
        {
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Clearing list"); // Write to console
            entryHeight = 0; // Reset
            foreach (GameObject go in list.entryList) // Iterate current list objects
                Destroy(go); // Destroy
            list.entryList.Clear(); // Clear list
        }

        // ------------------------------------------------------------------------------------
        // Status Checks

        // Check status of run button and update (called when changing a selection via toggles)
        public void CheckRunButtonStatus()
        {
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Checking run button status"); // Write to console
            if (TestStructure.Instance.CheckSelectionNotNull()) // If there is a selection
                actions.run.interactable = true; // Set run button active
            else
                actions.run.interactable = false; // Set run button inactive
        }

        // Check status of view button and update (called when navigating breadcrumb and selecting at level4 (test))
        public void CheckViewButtonStatus()
        {
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Checking view button status"); // Write to console
            if (selectedId.currentLevel == 4) // If selecting a test
                actions.view.interactable = true; // Set view button active
            else
            {
                actions.view.interactable = false; // Set view button inactive 
                selectedTest = null; // Null selected test
            }
        }

        // ------------------------------------------------------------------------------------
        // Buttons

        // Called every time a list entry is clicked (button listener)
        public void OnListEntryClick(MenuListEntry clicked)
        {
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Clicked: List entry"); // Write to console
            if (clicked.entryData.id.currentLevel < 3) // Selecting any level with children
            {
                ClearList(); // Clear the list
                GenerateBreadcrumb(clicked.entryData); // Generate a new breadcrumb
                MenuTestEntry id = CloneMenuTestEntry(clicked.entryData.id); // Clone the entry data of the clicked entry
                id.currentLevel++; // Increment current level
                selectedId = id; // Set selected id
                GenerateList(); // Generate a new list
                CheckRunButtonStatus(); // Check run button status
            }
            else // Selecting a single test (no children)
            {
                MenuTestEntry id = CloneMenuTestEntry(clicked.entryData.id); // Clone the entry data of the clicked entry
                id.currentLevel++; // Increment current level
                selectedId = id; // Set selected id
                if (selectedTest != null) // If there is a selected test
                    selectedTest.mainButton.GetComponent<Image>().color = colors[2]; // Reset its color
                selectedTest = clicked; // Set this as selected test
                selectedTest.mainButton.GetComponent<Image>().color = colors[1]; // Set its color
                CheckViewButtonStatus(); // Check view button status
            }
        }

        // Called every time a breadcrumb entry is clicked (button listener)
        public void OnBreadcrumbEntryClick(MenuBreadcrumbEntry clicked)
        {
            if(topLevel)
            {
                Navigation.Instance.ReturnHome();
            }
            else
            {
                Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Clicked: Breadcrumb entry"); // Write to console
                MenuEntryData entryData = CloneMenuEntryData(clicked.entryData); // Clone the entry data of the clicked entry
                ClearList(); // Clear the list
                selectedId = entryData.id; // Set selected id
                GenerateBreadcrumb(clicked.entryData); // Generate new breadcrumb
                selectedId.currentLevel++; // Increment current level
                GenerateList(); // Generate a new list
                CheckViewButtonStatus(); // Check view button status
            }
        }

        // Called when results button is clicked
        public void OnResultsButtonClick()
        {
            SetMenuState(false); // Disable Menu
            ResultsViewer.Instance.SetState(3); // Open results viewer and generate filtered results
        }

        // Called when run button is clicked
        public void OnRunButtonClick()
        {
            Console.Instance.Write(DebugLevel.Logic, MessageLevel.Log, "Starting Automation run"); // Write to console
            GenerateTestRunner(RunnerType.Automation); // Generate test runner
        }

        // Called when view button is clicked
        public void OnViewButtonClick()
        {
            Console.Instance.Write(DebugLevel.Logic, MessageLevel.Log, "Starting Manual run"); // Write to console
            GenerateTestRunner(RunnerType.Manual); // Generate test runner
        }

        // ------------------------------------------------------------------------------------
        // Execution

        // Generate and execute a new test runner
        public void GenerateTestRunner(RunnerType type)
        {
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Generating a test runner"); // Write to console
            TestRunner newRunner; // Store a reference to call on
            if (!Master.Instance.gameObject.GetComponent<TestRunner>()) // If no test runner
                newRunner = Master.Instance.gameObject.AddComponent<TestRunner>(); // Generate one
            else
                newRunner = Master.Instance.gameObject.GetComponent<TestRunner>(); // Get current
            newRunner.SetupRunner(type); // Setup the runner
        }

        // ------------------------------------------------------------------------------------
        // Get Data

        // Get the name of a level for breadcrumb clearing
        public string GetLevelName(int index)
        {
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Getting name for level at index " + index); // Write to console
            switch (index) // Switch on level index
            {
                case 0:
                    return "Suite";
                case 1:
                    return "Type";
                case 2:
                    return "Group";
                case 3:
                    return "Test";
            }
            return ""; // Return failure
        }

        // Get a menu color from the palette
        public Color GetColor(int index)
        {
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Getting menu color for index " + index); // Write to console
            return colors[index]; // Return requested color
        }

        // Get the selected test. Used to start test runner at specific point when viewing tests
        public MenuTestEntry GetSelectedEntry()
        {
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Getting selected menu entry"); // Write to console
            return selectedTest.entryData.id; // Return the selected tests ID
        }

        // ------------------------------------------------------------------------------------
        // Helper Methods

        // Clone a MenuTestEntry class
        public MenuTestEntry CloneMenuTestEntry(MenuTestEntry input)
        {
            MenuTestEntry output = new MenuTestEntry();
            output.suiteId = input.suiteId;
            output.typeId = input.typeId;
            output.groupId = input.groupId;
            output.testId = input.testId;
            output.currentLevel = input.currentLevel;
            return output;
        }

        // Clone a MenuEntryData class
        public MenuEntryData CloneMenuEntryData(MenuEntryData input)
        {
            MenuEntryData output = new MenuEntryData();
            output.entryName = input.entryName;
            output.selectionState = input.selectionState;
            output.id = CloneMenuTestEntry(input.id);
            return output;
        }

        // ------------------------------------------------------------------------------------
        // Local Menu Reference Structures

        [Serializable]
        public class List
        {
            public ScrollRect scrollRect;
            public RectTransform contentRect;
            public List<GameObject> entryList = new List<GameObject>();
        }

        [Serializable]
        public class Breadcrumb
        {
            public MenuBreadcrumbEntry home;
            public MenuBreadcrumbEntry suite;
            public MenuBreadcrumbEntry type;
            public MenuBreadcrumbEntry group;
        }

        [Serializable]
        public class Actions
        {
            public Button run;
            public Button view;
        }

        [Serializable]
        public class ResolveWindow
        {
            public GameObject parent;
            public Text message;
            public RectTransform contentRect;
            public Button resolveButton;
        }
    }

    // ------------------------------------------------------------------------------------
    // Global Data Structures

    [Serializable]
    public class MenuTestEntry
    {
        public int suiteId = -1;
        public int typeId = -1;
        public int groupId = -1;
        public int testId = -1;
        public int currentLevel = 0;
    }
}
