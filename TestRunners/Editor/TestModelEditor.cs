﻿using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

namespace GraphicsTestFramework
{
	[CustomEditor(typeof(TestModelBase))]
	public class TestModelEditor : Editor
    {
        // Serialized Properties
        SerializedProperty m_TestSettings;
        SerializedProperty m_RenderPipeline;
        SerializedProperty m_Platforms;
        SerializedProperty m_WaitType;
        SerializedProperty m_WaitFrames;
        SerializedProperty m_WaitSeconds;

        // Data
        [HideInInspector]
        public bool showAdvanced;

        // Draw common data parameters
        public virtual void DrawCommon(SerializedObject inputObject)
        {
            // Get properties
            m_Platforms = inputObject.FindProperty("m_Settings.platformMask");
            m_WaitType = inputObject.FindProperty("m_Settings.waitType");
            m_WaitFrames = inputObject.FindProperty("m_Settings.waitFrames");
            m_WaitSeconds = inputObject.FindProperty("m_Settings.waitSeconds");
            m_TestSettings = inputObject.FindProperty("m_Settings.testSettings");
            m_RenderPipeline = inputObject.FindProperty("m_Settings.renderPipeline");

            EditorGUILayout.LabelField ("Common Settings", EditorStyles.boldLabel); // Draw label
            m_Platforms.intValue = EditorGUILayout.MaskField(new GUIContent("Platforms"), m_Platforms.intValue, System.Enum.GetNames(typeof(RuntimePlatform))); // Draw type
            EditorGUILayout.PropertyField(m_WaitType, new GUIContent("Wait Type", "Choose the type of start delay: \nFrames = Wait 'X' rendered frames \nSeconds = Wait 'X' seconds \nStable Framerate = Wait for a steady framerate \nCallback = Wait for a custom callback from a script"));

            switch ((SettingsBase.WaitType)m_WaitType.intValue)
            {
                case SettingsBase.WaitType.Frames:
                    EditorGUILayout.PropertyField(m_WaitFrames, new GUIContent("Wait For Frames"));
                    break;
                case SettingsBase.WaitType.Seconds:
                    EditorGUILayout.PropertyField(m_WaitSeconds, new GUIContent("Wait For Seconds"));
                    break;
                default:
                    break;
            }
        }

        // Draw advanced foldout
        public void DrawAdvanced(SerializedObject inputObject)
        {
            EditorGUILayout.Space(); // Some space before advanced settings
            EditorGUILayout.LabelField("Advanced Settings", EditorStyles.boldLabel); // Draw label
            showAdvanced = EditorGUILayout.Foldout(showAdvanced, "Expand"); // Get advanced state
            if (showAdvanced) // If enabled
            {
                EditorGUI.indentLevel++; // Indent
                m_TestSettings.objectReferenceValue = EditorGUILayout.ObjectField("Test Settings", m_TestSettings.objectReferenceValue, typeof(TestSettings), false); // Draw test settings
                DrawAdvancedContent(); // Call for draw custom advanced content
                EditorGUI.indentLevel--; // Indent
            }
        }

        // Draw custom advanced foldout properties
        public virtual void DrawAdvancedContent()
        {
            // Draw custom advanced properties on override
        }
    }
}
