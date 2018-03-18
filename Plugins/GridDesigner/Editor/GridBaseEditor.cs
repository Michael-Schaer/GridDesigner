using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;

namespace GridDesigner
{
    public class GridStorageObjectFactory
    {
        public static GridStorageObject CreateAsset()
        {
            GridStorageObject asset = ScriptableObject.CreateInstance<GridStorageObject>();

            AssetDatabase.CreateAsset(asset, GetGridAssetPath());
            AssetDatabase.SaveAssets();

            EditorUtility.FocusProjectWindow();

            Selection.activeObject = asset;
            return asset;
        }

        public static GridStorageObject LoadAsset()
        {
            GridStorageObject result = AssetDatabase.LoadAssetAtPath<GridStorageObject>(GetGridAssetPath());
            if (result == null)
            {
                Debug.LogError("Your Gridstorage asset was not found at: " + GetGridAssetPath());
                Debug.LogError("Delete the reference to your Mesh to automatically create a new Gridstorage object");
            }

            return result;
        }

        private static string GetGridAssetPath()
        {
            return GridBaseEditor.RESOURCES_PATH + EditorSceneManager.GetActiveScene().name + "_Grid.asset";
        }
    }

    /**
    // https://forum.unity.com/threads/custom-inspector-initializing-array.144346/
    // ADDS the element to the end of the array
    YourSerializedProperty.arraySize ++;
     
    // REMOVES the element from the end of the array
    YourSerializedProperty.arraySize --;
     
    // INSERTS the element to the specified array index.
    // Note: The index has to be prepopulated, meaning this cannot be used to push to the end of the array.
    YourSerializedProperty.InsertArrayElementAtIndex(int index);
     
    // REMOVES the element from the specified array index, but not the last one.
    YourSerializedProperty.DeleteArrayElementAtIndex(int index);
     
    // SETS the value at the specified slot
    YourSerializedProperty.GetArrayElementAtIndex(index).objectReferenceValue = value;
     
    // GETS the value at the specified slot
    return YourSerializedProperty.GetArrayElementAtIndex(index).objectReferenceValue;
    **/

    [CustomEditor(typeof(GridBase))]
	public class GridBaseEditor : Editor
	{
		private string[] brushTiles;
		private int[] brushScales;
		private Vector3 offset = new Vector3(0, 0.01f, 0);
		private GridBase gridBase;
		private bool controlDown = false;
		private GameObject brush;
		private SerializedProperty enableDesigner;
		private SerializedProperty tagsToIgnore, layersToIgnore;
        private SerializedProperty disableMeshRenderersAtStart;
        private SerializedProperty alignY;
        private SerializedProperty storage;
        private MeshPainter meshPainter;
		public SerializedProperty tileScale, brushIndex, yOffset;
        private SerializedProperty tilePositionsProperty;

        public const string RESOURCES_PATH = "Assets/Plugins/GridDesigner/Resources/";
		private const string BRUSH_PREFAB_PATH = "Assets/Plugins/GridDesigner/Prefabs/Brush.prefab";
		private static Vector3 BRUSH_OFFSET = new Vector3(0, 0.02f, 0);

        public SerializedProperty TilePositionsProperty
        {
            get
            {
                if(tilePositionsProperty == null)
                {
                    if(gridBase.storage == null)
                    {
                        gridBase.storage = GridStorageObjectFactory.LoadAsset();
                    }
                    if(gridBase.storage != null)
                    {
                        tilePositionsProperty = new SerializedObject(gridBase.storage).FindProperty("tilePositions");
                    }
                }
                return tilePositionsProperty;
            }

            set
            {
                tilePositionsProperty = value;
            }
        }

        private void OnEnable()
        {
            gridBase = (GridBase)target;
            TagHelper.AddTag(GridUtility.BRUSH_TAG);

            AssignValues();

            CreateBrush();
            HandleBrushRenderer();
        }

        private void OnDisable()
		{
            if(meshPainter != null)
            {
                meshPainter.Disable();
            }

            if(brush != null && brush.GetComponent<MeshRenderer>() != null)
            {
                brush.GetComponent<MeshRenderer>().enabled = false;
            }
		}

        public void SetMesh(Mesh mesh)
        {
            Undo.RecordObject(gridBase.meshFilter, "MeshFilter mesh change");
            gridBase.meshFilter.sharedMesh = mesh;
        }

        public Mesh GetMesh()
        {
            return gridBase.meshFilter.sharedMesh;
        }

        public override void OnInspectorGUI()
		{
			serializedObject.Update();
			enableDesigner.boolValue = EditorGUILayout.Toggle("Enable Editor", enableDesigner.boolValue);
			HandleBrushRenderer();
            EditorGUILayout.PropertyField(storage, new GUIContent("Tiles"));

			EditorGUILayout.Separator();
            brushIndex.intValue = EditorGUILayout.Popup("Brush Size", brushIndex.intValue, brushTiles);

            EditorGUILayout.Separator();
            EditorGUILayout.PropertyField(tileScale, new GUIContent("Tile Scale"));
            EditorGUILayout.PropertyField(yOffset, new GUIContent("Y offset"));
            if (GUILayout.Button("Apply to all"))
            {
                ReplaceTiles();
            }
            EditorGUILayout.Separator();

            EditorGUILayout.PropertyField(alignY, new GUIContent("Align Vertically"));
            EditorGUILayout.PropertyField(tagsToIgnore, new GUIContent("Tags to ignore"), true);
            LayerMask tempMask = EditorGUILayout.MaskField("Layers to ignore", InternalEditorUtility.LayerMaskToConcatenatedLayersMask(layersToIgnore.intValue), InternalEditorUtility.layers);
            layersToIgnore.intValue = InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(tempMask);
            EditorGUILayout.Space();
			EditorGUILayout.LabelField("Click to place tiles, \nCtrl & Click to delete tiles", EditorStyles.helpBox);
			EditorGUILayout.Space();

			EditorGUILayout.Separator();
			EditorGUILayout.LabelField("At Runtime", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(disableMeshRenderersAtStart, new GUIContent("Disable Mesh Renderers at Runtime"));
            
            if (GUILayout.Button("Clear Grid"))
            {
                ClearGrid();
            }
            serializedObject.ApplyModifiedProperties();
		}

		private void OnSceneGUI()
		{
			if (!enableDesigner.boolValue)
			{
				return;
			}

            SerializedProperty checkTiles = TilePositionsProperty;

            Event current = Event.current;
			switch (current.type)
			{
				case EventType.MouseUp:

					Vector3 alignedPosition = Vector3.zero;
					RaycastHit hitInfo = RaycastGUIToFloor(Event.current.mousePosition);
					if (hitInfo.collider == null)
					{
						break;
					}

					alignedPosition = GridUtility.GetAlignedPosition(hitInfo.point);
					if (current.button == 0)
					{
						if (controlDown)
						{
                            Undo.RecordObject(target, "Delete Tiles");
                            RemoveTiles(alignedPosition);
						}
						else
						{
                            Undo.RecordObject(target, "Create Tiles");
                            CreateTiles(alignedPosition);
						}
						current.Use();
					}
					break;
				case EventType.MouseMove:
					MoveBrush();
					break;
				case EventType.KeyDown:
					if (Event.current.keyCode == (KeyCode.LeftControl))
					{
						controlDown = true;
					}
					break;
				case EventType.KeyUp:
					if (Event.current.keyCode == (KeyCode.LeftControl))
					{
						controlDown = false;
					}
					break;
				case EventType.Layout:
					HandleUtility.AddDefaultControl(GUIUtility.GetControlID(GetHashCode(), FocusType.Passive));
					break;
                case EventType.MouseEnterWindow:
                    if (SceneView.sceneViews.Count > 0)
                    {
                        SceneView sceneView = (SceneView)SceneView.sceneViews[0];
                        sceneView.Focus();
                    }
                    break;
            }
		}

        private void CreateSceneAssets()
        {
            if (gridBase.meshFilter == null)
            {
                gridBase.meshFilter = gridBase.GetComponent<MeshFilter>();
            }

            if (gridBase.meshFilter.sharedMesh == null)
            {
                // Create Mesh Asset
                Mesh newMesh = new Mesh();
                Undo.RegisterCreatedObjectUndo(newMesh, "MeshFilter mesh change");
                gridBase.meshFilter.sharedMesh = newMesh;
                AssetDatabase.CreateAsset(gridBase.meshFilter.sharedMesh, RESOURCES_PATH + EditorSceneManager.GetActiveScene().name + "_Mesh.asset");
                AssetDatabase.SaveAssets();

                // Create GridStorage Asset
                gridBase.storage = GridStorageObjectFactory.CreateAsset();
            }
        }

        private void AssignValues()
        {
            brushScales = new int[]
            {
                1,3,5,7,9,11,13,15
            };
            brushTiles = new string[brushScales.Length];
            for (int i = 0; i < brushScales.Length; i++)
            {
                brushTiles[i] = Mathf.RoundToInt(Mathf.Pow(brushScales[i], 2)).ToString();
            }

            enableDesigner = serializedObject.FindProperty("enableDesigner");
            tileScale = serializedObject.FindProperty("tileScale");
            yOffset = serializedObject.FindProperty("yOffset");
            brushIndex = serializedObject.FindProperty("brushIndex");
            alignY = serializedObject.FindProperty("alignY");
            tagsToIgnore = serializedObject.FindProperty("tagsToIgnore");
            layersToIgnore = serializedObject.FindProperty("layersToIgnore");
            disableMeshRenderersAtStart = serializedObject.FindProperty("disableMeshRenderersAtStart");
            storage = serializedObject.FindProperty("storage");
            CreateSceneAssets();
    
            meshPainter = new MeshPainter();
            meshPainter.Enable(this);
        }

        public void ClearGrid()
        {
            if (gridBase.meshFilter != null)
            {
                gridBase.meshFilter.sharedMesh = new Mesh();
            }

            for (int x = gridBase.storage.tilePositions.Count - 1; x >= 0; x--)
            {
                TilePositionsProperty.DeleteArrayElementAtIndex(x);
            }

            TilePositionsProperty.serializedObject.ApplyModifiedProperties();
            meshPainter.ClearMesh();
            AssetDatabase.SaveAssets();
        }

        private void CreateBrush()
		{
			GameObject[] objects = GameObject.FindGameObjectsWithTag(GridUtility.BRUSH_TAG);
			if (objects.Length == 0 || objects[0] == null)
			{
				GameObject prefab = AssetDatabase.LoadAssetAtPath(BRUSH_PREFAB_PATH, typeof(GameObject)) as GameObject;
				brush = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
				brush.transform.position = new Vector3(0, GridUtility.Y_OFFSCREEN, 0);
				brush.tag = GridUtility.BRUSH_TAG;
			}
			else
			{
				brush = objects[0];
			}
		}

        private void MoveBrush()
		{
			RaycastHit hitInfo = RaycastGUIToFloor(Event.current.mousePosition);
			if (hitInfo.collider != null)
			{
				Vector3 position = GridUtility.GetAlignedPosition(hitInfo.point);
				position.y = GridUtility.FloorHeight(position);
				brush.transform.position = position + offset + BRUSH_OFFSET;

				float scale = gridBase.tileSize * brushScales[brushIndex.intValue];
				brush.transform.localScale = new Vector3(scale, scale, scale);
			}
		}
        
        private void HandleBrushRenderer()
        {
            enableDesigner.boolValue = enableDesigner.boolValue && !EditorApplication.isPlaying;
            brush.GetComponent<MeshRenderer>().enabled = enableDesigner.boolValue;
        }

		private void CreateTiles(Vector3 position)
		{
			float tileSize = gridBase.tileSize;

			for (int x = -brushIndex.intValue; x <= brushIndex.intValue; x++)
			{
				for (int z = -brushIndex.intValue; z <= brushIndex.intValue; z++)
				{
					CreateTile(new Vector3(position.x + x * tileSize, position.y, position.z + z * tileSize));
				}
			}
            AssetDatabase.SaveAssets();
        }

		private void RemoveTiles(Vector3 position)
		{
			float tileSize = gridBase.tileSize;

			for (int x = -brushIndex.intValue; x <= brushIndex.intValue; x++)
			{
				for (int z = -brushIndex.intValue; z <= brushIndex.intValue; z++)
				{
					Vector3 delPosition = new Vector3(position.x + x * tileSize, position.y, position.z + z * tileSize);
                    RemoveTile(delPosition);
				}
			}
            AssetDatabase.SaveAssets();
        }

		private void CreateTile(Vector3 position)
		{
            // Get Floor height before generating a new Tile object
            position.y = GridUtility.FloorHeight(position);

            if (GridBase.Instance.alignY)
            {
                position.y = GridUtility.CheckVerticalAlignment(position.y);
            }

            if (!gridBase.ContainsTileAt(position))
            {
                TilePositionsProperty.arraySize++;
                TilePositionsProperty.serializedObject.ApplyModifiedProperties();
                TilePositionsProperty.GetArrayElementAtIndex(gridBase.storage.tilePositions.Count - 1).vector3Value = position;
                TilePositionsProperty.serializedObject.ApplyModifiedProperties();
                meshPainter.DrawTile(position);
            }
        }

        private void RemoveTile(Vector3 position)
        {
            int index = GridBase.Instance.storage.tilePositions.GetIndex(position);
            if (index > -1)
            {
                if(index == gridBase.storage.tilePositions.Count -1)
                {
                    TilePositionsProperty.arraySize--;
                }
                else
                {
                    TilePositionsProperty.DeleteArrayElementAtIndex(index);
                }
                TilePositionsProperty.serializedObject.ApplyModifiedProperties();
                meshPainter.UndrawTile(position);
            }
        }

        private RaycastHit RaycastGUIToFloor(Vector3 position)
		{
			Ray worldRay = UnityEditor.HandleUtility.GUIPointToWorldRay(position);
			RaycastHit hitInfo;
			Physics.Raycast(worldRay, out hitInfo, GridUtility.RAYCAST_DISTANCE);
			return hitInfo;
		}

		private void ReplaceTiles()
		{
            meshPainter.ClearMesh();
            meshPainter.SetOffset();

            SerializedProperty tempPositions = TilePositionsProperty.Copy(); // copy so we don't iterate the original
            if (tempPositions.isArray)
            {
                int arrayLength = 0;

                tempPositions.Next(true); // skip generic field
                tempPositions.Next(true); // advance to array size field

                // Get the array size
                arrayLength = tempPositions.intValue;

                tempPositions.Next(true); // advance to first array index

                // Write values to list
                List<Vector3> values = new List<Vector3>(arrayLength);
                int lastIndex = arrayLength - 1;
                for (int i = 0; i < arrayLength; i++)
                {
                    values.Add(tempPositions.vector3Value); // copy the value to the list
                    if (i < lastIndex) tempPositions.Next(false); // advance without drilling into children
                }

                // iterate over the list displaying the contents
                for (int i = 0; i < values.Count; i++)
                {
                    meshPainter.DrawTile(values[i]);
                }

                AssetDatabase.SaveAssets();
            }
        }
	}
}