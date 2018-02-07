using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GridDesigner
{
    [CustomEditor(typeof(GridStorageObject))]
	public class GridStorageObjectEditor : Editor
	{
        private SerializedProperty tilePositions, tilePositionsGUI;
        private SerializedProperty startIndex;

        private const int ARRAY_SIZE = 10;

        private void OnEnable()
        {
            tilePositions = serializedObject.FindProperty("tilePositions");
            tilePositionsGUI = serializedObject.FindProperty("tilePositionsGUI");
            startIndex = serializedObject.FindProperty("startIndex");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.LabelField("Startindex for displaying the array values");
            int rangeEnd = Mathf.Max(0, tilePositions.arraySize - ARRAY_SIZE);
            startIndex.intValue = EditorGUILayout.IntSlider(startIndex.intValue, 0, rangeEnd);
            serializedObject.ApplyModifiedProperties();
            UpdateVisibleArray();

            GUI.enabled = false;
            EditorGUILayout.PropertyField(tilePositionsGUI, new GUIContent("Tile Positions"), true);
            GUI.enabled = true;
            serializedObject.ApplyModifiedProperties();
        }

        private void UpdateVisibleArray()
        {
            int start = startIndex.intValue;
            if(start >= tilePositions.arraySize)
            {
                start = tilePositions.arraySize;
            }

            int end = start + ARRAY_SIZE;
            if(end >= tilePositions.arraySize)
            {
                end = tilePositions.arraySize;
            }

            tilePositionsGUI.arraySize = 0;
            serializedObject.ApplyModifiedProperties();
            tilePositionsGUI.arraySize = end - start;
            serializedObject.ApplyModifiedProperties();

            int counter = 0;
            for(int i = start; i < end; i++)
            {
                tilePositionsGUI.GetArrayElementAtIndex(counter).vector3Value = tilePositions.GetArrayElementAtIndex(i).vector3Value;
                counter++;
            }
            serializedObject.ApplyModifiedProperties();

        }
	}
}