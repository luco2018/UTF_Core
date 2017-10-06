using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace GraphicsTestFramework
{
	[CustomEditor(typeof(AverageFrameTimeModel))]
	public class AverageFrameTimeModelEditor : TestModelEditor
    {
        AverageFrameTimeModel m_Target; // Target
        SerializedObject m_Object; // Object

        // Serialized Properties
        SerializedProperty m_PassFailThreshold;
        SerializedProperty m_TimingType;
        SerializedProperty m_CustomTimingMultiplier;
        SerializedProperty m_SampleFrames;

        public override void OnInspectorGUI()
		{
            m_Target = (AverageFrameTimeModel)target; // Cast target
            m_Object = new SerializedObject(m_Target); // Create serialized object

            m_Object.Update(); // Update object

            // Get properties
            m_PassFailThreshold = m_Object.FindProperty("m_Settings.passFailThreshold");
            m_TimingType = m_Object.FindProperty("m_Settings.timingType");
            m_CustomTimingMultiplier = m_Object.FindProperty("m_Settings.customTimingMultiplier");
            m_SampleFrames = m_Object.FindProperty("m_Settings.sampleFrames");

            DrawCommon(m_Object); // Draw the SettingsBase settings (mandatory)

            EditorGUILayout.PropertyField(m_PassFailThreshold, new GUIContent("Pass/Fail Threshold (ms Difference)")); // Draw Pass fail
            EditorGUILayout.Space(); // Some space before custom settings

            EditorGUILayout.LabelField("Average Frame Time Settings", EditorStyles.boldLabel); // Custom settings
            EditorGUILayout.PropertyField(m_TimingType, new GUIContent("Timing Type")); // Draw timing type
            if((AverageFrameTimeSettings.TimingType)m_TimingType.intValue == AverageFrameTimeSettings.TimingType.Custom) // If using custom timing multiplier
                EditorGUILayout.PropertyField(m_CustomTimingMultiplier, new GUIContent("Custom Timing Multiplier", "This number is used to multiply the ticks output by the sampling")); // Draw custom timing multiplier
            EditorGUILayout.PropertyField(m_SampleFrames, new GUIContent("Sample Frames", "The amount of rendered frames to capture performance over")); // Draw sample frames

            DrawAdvanced(m_Object); // Draw the advanced foldout (mandatory)

            m_Object.ApplyModifiedProperties(); // Apply modified
        }

	}
}
