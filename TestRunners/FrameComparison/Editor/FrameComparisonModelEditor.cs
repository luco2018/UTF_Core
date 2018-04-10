using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace GraphicsTestFramework
{
	[CustomEditor(typeof(FrameComparisonModel))]
	public class FrameComparisonModelEditor : TestModelEditor
    {
        FrameComparisonModel m_Target; // Target
        SerializedObject m_Object; // Object

        // Serialized Properties
        SerializedProperty m_PassFailThreshold;
        SerializedProperty m_CaptureCamera;
        SerializedProperty m_FrameResolution;
        SerializedProperty m_TextureFormat;
        SerializedProperty m_FilterMode;
        SerializedProperty m_useBackBuffer;
        SerializedProperty m_imageQuality;
        SerializedProperty m_sRGB;

        public override void OnInspectorGUI()
		{
            m_Target = (FrameComparisonModel)target; // Cast target
            m_Object = new SerializedObject(m_Target); // Create serialized object

            m_Object.Update(); // Update object

            // Get properties
            m_PassFailThreshold = m_Object.FindProperty("m_Settings.passFailThreshold");
            m_CaptureCamera = m_Object.FindProperty("m_Settings.captureCamera");
            m_FrameResolution = m_Object.FindProperty("m_Settings.frameResolution");
            m_TextureFormat = m_Object.FindProperty("m_Settings.textureFormat");
            m_FilterMode = m_Object.FindProperty("m_Settings.filterMode");
            m_useBackBuffer = m_Object.FindProperty("m_Settings.useBackBuffer");
            m_imageQuality = m_Object.FindProperty("m_Settings.imageQuality");

            DrawCommon(m_Object); // Draw the SettingsBase settings (mandatory)

            EditorGUILayout.Slider(m_PassFailThreshold, 0f, 100f, new GUIContent("Pass / Fail Threshold(% Difference)"), new GUILayoutOption[0]); // Slider for pass/fail as it is a percentage of pixel difference

            EditorGUILayout.Space(); // Some space before custom settings

            EditorGUILayout.LabelField("Frame Comparison Settings", EditorStyles.boldLabel); // Custom settings
            EditorGUILayout.ObjectField(m_CaptureCamera, typeof(Camera), new GUIContent("Capture Camera"), new GUILayoutOption[0]); // Draw capture camera
            if (m_Target.p_Settings.captureCamera == null) // If capture camera is null
                EditorGUILayout.HelpBox("Please select a camera for the Frame Comparison to use.", MessageType.Warning); // Draw warning
            EditorGUILayout.PropertyField(m_FrameResolution, new GUIContent("Capture Resolution")); // Draw frame resolution

            DrawAdvanced(m_Object); // Draw the advanced foldout (mandatory)

            if (m_useBackBuffer.boolValue)
                EditorGUILayout.HelpBox("Backbuffer is being used", MessageType.Warning);

            m_Object.ApplyModifiedProperties(); // Apply modified
        }

        public override void DrawAdvancedContent()
        {
            EditorGUILayout.PropertyField(m_imageQuality, new GUIContent("Image Quality")); // Use alternatice capture method(multicam)
            EditorGUILayout.PropertyField(m_TextureFormat, new GUIContent("Texture Format")); // Draw texture format
            EditorGUILayout.PropertyField(m_FilterMode, new GUIContent("Filter Mode")); // Draw filter mode
            EditorGUILayout.PropertyField(m_useBackBuffer, new GUIContent("Use Backbuffer")); // Use alternatice capture method(multicam)
        }
    }
}