using UnityEngine;
using UnityEditor;
using System.Collections;

namespace GraphicsTestFramework
{
    [CustomPropertyDrawer(typeof(Test))]
    class TestDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Init
            position = new Rect(position.x, position.y, position.width, position.height); // Set initial position
            EditorGUI.BeginProperty(position, label, property); // Begin the property

            // Label
            if(property.FindPropertyRelative("scene").objectReferenceValue) // If scene property is set
                label = new GUIContent(property.FindPropertyRelative("scene").objectReferenceValue.ToString().Replace(" (UnityEngine.SceneAsset)", "")); // Set label to scene
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label); // Draw label
            position = new Rect(position.x - 60, position.y, position.width, position.height); // Override next position

            //var indent = EditorGUI.indentLevel; // Store indent
            //EditorGUI.indentLevel = 0; // Set indent

            // Calculate rects
            Rect typeRect = new Rect(position.x, position.y, 80, position.height); // Type rect
            Rect platformRect = new Rect(position.x + 80, position.y, 80, position.height); // Platform rect
            Rect versionRect = new Rect(position.x + 160, position.y, 80, position.height); // Version rect
            Rect sceneRect = new Rect(position.x + 240, position.y, position.width, position.height); // Scene rect
            Rect runRect = new Rect(position.xMax + 85, position.y, 20, position.height); // Run rect

            // Draw fields
            property.FindPropertyRelative("testTypes").intValue = EditorGUI.MaskField(typeRect, GUIContent.none, property.FindPropertyRelative("testTypes").intValue, TestTypes.GetTypeStringList()); // Draw type
            property.FindPropertyRelative("platforms").intValue = EditorGUI.MaskField(platformRect, GUIContent.none, property.FindPropertyRelative("platforms").intValue, System.Enum.GetNames(typeof(RuntimePlatform))); // Draw platform
            property.FindPropertyRelative("minimumUnityVersion").intValue = EditorGUI.Popup(versionRect, property.FindPropertyRelative("minimumUnityVersion").intValue, Common.unityVersionList); // Draw version
            EditorGUI.PropertyField(sceneRect, property.FindPropertyRelative("scene"), GUIContent.none); // Draw scene
            EditorGUI.PropertyField(runRect, property.FindPropertyRelative("run"), GUIContent.none); // Draw run

            // Finish
            //EditorGUI.indentLevel = indent; // Reset indent
            EditorGUI.EndProperty(); // End property
        }
    }
}