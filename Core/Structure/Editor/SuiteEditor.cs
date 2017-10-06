using UnityEngine;
using UnityEditor;

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

        public override void OnInspectorGUI()
        {
            serializedObject.Update(); // Update the object
            string[] testTypeEntries = TestTypes.GetTypeStringList(); // Get the test type list for use in mask fields

            EditorGUILayout.PropertyField(serializedObject.FindProperty("suiteName"), false); // Draw suiteName;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("isDebugSuite"), false); // Draw suiteName;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("defaultTestSettings"), false); // Draw suiteName;
            var groups = serializedObject.FindProperty("groups"); // Get groups list
            EditorGUILayout.PropertyField(groups, false); // Draw groups
            if (groups.isExpanded) // If the groups list is expanded
            {
                EditorGUI.indentLevel += 1; // Add indent
                EditorGUILayout.PropertyField(groups.FindPropertyRelative("Array.size")); // Draw the array size
                int groupCount = groups.arraySize; // Get count of groups list
                for (int gr = 0; gr < groupCount; gr++) // Iterate items in the group list
                {
                    var group = groups.GetArrayElementAtIndex(gr); // Get item at this index
                    EditorGUILayout.PropertyField(group, false); // Draw item at this index
                    if (group.isExpanded) // If the group is expanded
                    {
                        EditorGUI.indentLevel += 1; // Add indent
                        EditorGUILayout.PropertyField(group.FindPropertyRelative("groupName"), false); // Draw groupName;
                        var tests = group.FindPropertyRelative("tests"); // Get tests list
                        EditorGUILayout.PropertyField(tests, false); // Draw tests

                        if (tests.isExpanded) // If the test item
                        {
                            EditorGUI.indentLevel += 1; // Add indent
                            EditorGUILayout.PropertyField(tests.FindPropertyRelative("Array.size"));
                            int testCount = tests.arraySize; // Get count of tests list
                            for (int te = 0; te < testCount; te++)
                            {
                                var test = tests.GetArrayElementAtIndex(te); // Get the item at this index
                                EditorGUILayout.PropertyField(test, GUIContent.none);
                            }
                            EditorGUI.indentLevel -= 1; // Remove indent
                        }
                        EditorGUI.indentLevel -= 1; // Remove indent
                    }
                }
                EditorGUI.indentLevel -= 1; // Remove indent
            }
            serializedObject.ApplyModifiedProperties(); // Apply to object
        }

        // Overrides everything with the sum of its parts
        void HandleEverything(int mask)
        {
            if (mask == -1) // If set to everything
            {
                int typeCount = TestTypes.GetTypeStringList().Length; // Get type count
                int value = 0; // Create an integer to track value
                for (int i = 0; i < typeCount; i++) // Iterate types
                {
                    if (i == 0) // If 0 have to return 1
                        value++; // Return 1
                    else
                        value += (int)Mathf.Pow(2, i); // Otherwise pow2
                }
                mask = value; // Set value to the sum
            }
        }
    }
    
}
