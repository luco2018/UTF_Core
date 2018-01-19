using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEditorInternal;
using System.Text.RegularExpressions; // needed for Regex

namespace GraphicsTestFramework.Experimental
{
    
    // ------------------------------------------------------------------------------------
    // Suite Scriptable Object GUI
    // - Draw GUI for Suite Scriptable Object
    
    [CustomEditor(typeof(Suite))]
    public class SuiteEditor : Editor
    {
        // ------------------------------------------------------------------------------------
        // Draw Custom Inspector

        //Things
        private ReorderableList groupList;
        private List<ReorderableList> testList;
        private List<string> grpNames = new List<string>();
        private int duplicateCount = 0;
        private GUIStyle bigLabels = new GUIStyle();
        private bool emptyTests = false;

        //Tooltips
        private string suiteNameTip = "This is the name of the suite, make this clear and short.\nNote:Do not include the word 'Suite'.";
        private string testSettingsTip = "This is the settings object used for the suite, it contains common runtime settings which you may want control over. If not assigned the default object is used.";
        private string srpTip = "This is the Scriptable Render Pipeline asset to use for all tests in this suite, this will be overriden if a test has one set. If not assigned the legacy rendering pipeline(Forward/Deferred) will be used.";
        private string grpNameTip = "This is the name for the test group, keep this short and clear.\nNote:Do not add the test type here as different test types will recieve their own subcatergory in the app, also take care not to double up on names within the same suite.";
        //private string runTip = "This checkbox tells the system to include this test, unchecking will essentially disable the test.";
        //private string typeTip = "This list is all the current test types available, select which ones you want this scene to run.";
        //private string platformTip = "This is a list of all platforms, here you can specify a test to only run on a selection of platforms.";
        //private string versionTip = "This is the minimum version this test supports.";

        private void OnEnable(){
            //Style setup for big labels
            bigLabels.fontSize = 20;
            bigLabels.fontStyle = FontStyle.Bold;
            bigLabels.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;//test color depends on per/pro

            var groups = serializedObject.FindProperty("groups");//Ref to the groups
            groupList = new ReorderableList(serializedObject, groups, true, true, true, true);//Reorderable list for the Groups
            testList = new List<ReorderableList>();//Reorderable lists for the Tests

            int groupCount = groups.arraySize; // Get count of groups list
            for (int gr = 0; gr < groupCount; gr++) // Iterate items in the group list
            {
                var group = groups.GetArrayElementAtIndex(gr);
                var tests = group.FindPropertyRelative("tests");
                testList.Add(new ReorderableList(serializedObject, tests, true, true, true, true));
            }

            //Custom drawing for the Groups ReorderableList
            groupList.drawElementCallback = 
                (Rect rect, int index, bool isActive, bool isFocused) => {
                var element = groupList.serializedProperty.GetArrayElementAtIndex(index);//Gets the current group object
                var tests = element.FindPropertyRelative("tests");//Gets the tests for the current group
                rect.y += 2;

                //Rects for drawing group elements
                Rect textRect = new Rect(rect.x, rect.y, (rect.width) * 0.5f, EditorGUIUtility.singleLineHeight);
                Rect buttonRect = new Rect(textRect.x + textRect.width + 5, rect.y, (rect.width * 0.5f) - 5, EditorGUIUtility.singleLineHeight);
                Rect listRect = new Rect(rect.x, rect.y, 200, 200);

                var grpName = element.FindPropertyRelative("groupName");//Ref to the groupName
                EditorGUIUtility.labelWidth = 40;
                grpName.stringValue = EditorGUI.TextField(textRect, new GUIContent("Name", grpNameTip), grpName.stringValue);//Draws the group name
                grpName.stringValue = ResolveGroupName(grpName.stringValue, index);//Validates the groupName

                var testsButton = "Show " + tests.arraySize + " Tests";//Collapsed string, contains test count
                if(tests.isExpanded)//if test expanded, switch button string
                    testsButton = "Hide Tests";
                
                //button for showing/hiding tests list
                if(GUI.Button(buttonRect, testsButton)){
                    tests.isExpanded = !tests.isExpanded;
                }

                //Custom drawing for the Test ReorderableList
                testList[index].drawElementCallback =  
                (Rect testRect, int testIndex, bool testIsActive, bool testIsFocused) => {
                    var testElement = testList[index].serializedProperty.GetArrayElementAtIndex(testIndex);
                    testRect.y += 2;

                    //Rects for drawing test elements
                    Rect runRect = new Rect(testRect.x, testRect.y, 20, EditorGUIUtility.singleLineHeight);
                    Rect sceneRect = new Rect(runRect.x + runRect.width, testRect.y, (testRect.width - runRect.width) * 0.35f, EditorGUIUtility.singleLineHeight + 2);
                    Rect typeRect = new Rect(sceneRect.x + sceneRect.width + 5, testRect.y, (testRect.width) * 0.24f, EditorGUIUtility.singleLineHeight + 2);
                    Rect platformRect = new Rect(typeRect.x + typeRect.width, testRect.y, (testRect.width) * 0.24f, EditorGUIUtility.singleLineHeight + 2);
                    Rect versionRect = new Rect(platformRect.x + platformRect.width, testRect.y, (testRect.width) * 0.12f, EditorGUIUtility.singleLineHeight + 2);

                    EditorGUI.PropertyField(runRect, testElement.FindPropertyRelative("run"), GUIContent.none); // Draw run
                    var scene = testElement.FindPropertyRelative("scene");//Ref to the scene value
                    if(scene.objectReferenceValue == null)//If it's not assigned check bool to show error message
                        emptyTests = true;
                    EditorGUI.PropertyField(sceneRect, scene, GUIContent.none);//Draw he scene property
                    var testType = testElement.FindPropertyRelative("testTypes");//Ref to the test type value
                    testType.intValue = EditorGUI.MaskField(typeRect, GUIContent.none, testType.intValue, TestTypes.GetTypeStringList());//Draw the test types mask
                    testType.intValue = FixBitmask(testType.intValue);//if all current types selected(-1 everything) convert to a bitmask of just those things
                    var platformProp = testElement.FindPropertyRelative("platforms");
                    platformProp.intValue = EditorGUI.MaskField(platformRect, GUIContent.none, platformProp.intValue, System.Enum.GetNames(typeof(RuntimePlatform))); // Draw platform
                    var versionProp = testElement.FindPropertyRelative("minimumUnityVersion");
                    versionProp.intValue = EditorGUI.Popup(versionRect, versionProp.intValue, Common.unityVersionList); // Draw version
                };

                //Draw the headers of the groups test to match the group name
                testList[index].drawHeaderCallback = (Rect testHeaderRect) => {  
                    EditorGUI.LabelField(testHeaderRect, grpName.stringValue.ToString() + " Tests");
                };

                //On add callback for adding tests, sets Run to true, platforms to everything and scene to null
                testList[index].onAddCallback = (list) =>
                {
                    list.serializedProperty.arraySize++;
                    list.index = list.serializedProperty.arraySize - 1;
                    var newElement = list.serializedProperty.GetArrayElementAtIndex(list.index);
                    var run = newElement.FindPropertyRelative("run");
                    run.boolValue = true;
                    var platform = newElement.FindPropertyRelative("platforms");
                    platform.intValue = -1;
                    var scene = newElement.FindPropertyRelative("scene");
                    scene.objectReferenceValue = null;
                };

                //Handling removale of last test entry
                testList[index].onCanRemoveCallback = (ReorderableList l) => {  
                    return l.count > 1;
                };

                //if test list is expanded, draw the reorderable list
                if(tests.isExpanded)
                    testList[index].DoList(new Rect(rect.x, rect.y + (EditorGUIUtility.singleLineHeight * 1.5f), rect.width, rect.height));

            };

            //Since drawing is all custom(positional) we need to set teh height for each group entry manually
            groupList.elementHeightCallback = (index) => 
            { 
                var list = groupList.serializedProperty.GetArrayElementAtIndex(index);
                var element = list.FindPropertyRelative("tests");
                var padding = EditorGUIUtility.singleLineHeight * 2;
                if(!element.isExpanded){
                    padding = 5;
                }
                return (EditorGUI.GetPropertyHeight(element) * 1.16f) + padding;
            };

            //On add callback for adding groups, sets the name to "NewGroup", adds one blank test
            groupList.onAddCallback = (list) =>
            {
                list.serializedProperty.arraySize++;
                list.index = list.serializedProperty.arraySize - 1;
                var group = groups.GetArrayElementAtIndex(list.index);
                var grpName = group.FindPropertyRelative("groupName");
                grpName.stringValue = "NewGroup";
                var tests = group.FindPropertyRelative("tests");
                tests.arraySize = 0;
                tests.arraySize = 1;
                var test = tests.GetArrayElementAtIndex(0);
                var run = test.FindPropertyRelative("run");
                run.boolValue = true;
                var platform = test.FindPropertyRelative("platforms");
                platform.intValue = -1;
                testList.Add(new ReorderableList(serializedObject, tests, true, true, true, true));
            };

            //Draws the custom header for the reorderable list
            groupList.drawHeaderCallback = (Rect rect) => {  
                EditorGUI.LabelField(rect, "Groups");
            };

        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update(); // Update the object
            var suiteName = serializedObject.FindProperty("suiteName");//Ref to the suiteName
            EditorGUILayout.LabelField(suiteName.stringValue + " Suite Settings", bigLabels);//Draw the title, this updates based on the suites name

            EditorGUILayout.Space();//Space :D
            EditorGUILayout.Space();

            suiteName.stringValue = EditorGUILayout.TextField(new GUIContent("Suite Name", suiteNameTip), suiteName.stringValue, GUILayout.ExpandWidth(true));//Draw Suite name text box
            suiteName.stringValue = Regex.Replace(suiteName.stringValue, @"[^a-zA-Z0-9]", "");//Validate the suiteName
            EditorGUILayout.PropertyField(serializedObject.FindProperty("isDebugSuite"), false);// Draw debugCheckbox;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("defaultTestSettings"), new GUIContent("Suite Test Settings", testSettingsTip), false);// Draw test settings;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("defaultRenderPipeline"), new GUIContent("Suite SRP Asset", srpTip), false); // Draw render pipeline;
            
            EditorGUILayout.Space();//Space :D
            
            EditorGUILayout.LabelField("Tests List", bigLabels);//Title for the tests
           
            EditorGUILayout.Space();//More Space :D

            //Before drawing the Reorderable lists we need to do some resetting
            grpNames = new List<string>();
            duplicateCount = 0;
            emptyTests = false;
            groupList.DoLayoutList();//Draw the Reorderable lists, this also sets off the test Reorderable lists

            if(GUILayout.Button("Update Suite On Cloud"))
            {
                //update on cloud
            }

            if(emptyTests)//Checks if there are empty test entries, if so shows an error message
                EditorGUILayout.HelpBox("One or more tests are empty, please remove them or assign a test scene to them.", MessageType.Error);

            if(GUI.changed)
            {
                var suiteVersion = serializedObject.FindProperty("suiteVersion");
                suiteVersion.intValue += 1;
            }

            serializedObject.ApplyModifiedProperties(); // Apply to object
        }

        // Overrides everything with the sum of its parts
        int FixBitmask(int mask)
        {
            if (mask == -1) // If set to everything
            {
                int typeCount = TestTypes.GetTypeStringList().Length; // Get type count
                int value = 0; // Create an integer to track value
                for (int i = 0; i < typeCount; i++) // Iterate types
                {
                    int checkBit = mask & (int)Mathf.Pow(2, i);
                    if(checkBit != 0)
                        value |= (int)Mathf.Pow(2, i);
                }
                return value; // Set value to the sum
            }
            else
            {
                return mask;
            }
        }

        //Check for duplicate group names appends a number if exists and removes invalid characters
        string ResolveGroupName(string grpName, int index){
            grpName = Regex.Replace(grpName, @"[^a-zA-Z0-9]", "");//Only allow a-z, A-Z, 0-9

            foreach (string name in grpNames)
            {
                if(grpName == name)
                {
                    duplicateCount++;
                    grpName += duplicateCount;
                }
            }

            if(grpNames.Count < index)
                grpNames.Add(grpName);
            else
                grpNames.Insert(index, grpName);

            return grpName;
        }

    }
    
}
