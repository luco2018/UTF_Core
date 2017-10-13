using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace GraphicsTestFramework
{
	[CustomEditor(typeof(ExampleModel))]
	public class ExampleModelEditor : TestModelEditor
    {
        ExampleModel m_Target; // Target
        SerializedObject m_Object; // Object

        // Serialized Properties
        SerializedProperty m_PassFailThreshold;

        public override void OnInspectorGUI()
		{
            m_Target = (ExampleModel)target; // Cast target
            m_Object = new SerializedObject(m_Target); // Create serialized object

            m_Object.Update(); // Update object

            // Get properties
            m_PassFailThreshold = m_Object.FindProperty("m_Settings.passFailThreshold");

            DrawCommon(m_Object); // Draw the SettingsBase settings (mandatory)

            EditorGUILayout.PropertyField(m_PassFailThreshold, new GUIContent("Pass/Fail Threshold (ms Difference)")); // Draw Pass fail

            DrawAdvanced(m_Object); // Draw the advanced foldout (mandatory)

            m_Object.ApplyModifiedProperties(); // Apply modified
        }
	}
}
