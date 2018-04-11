using System;
using System.Collections.Generic;
using UnityEngine;

namespace GridDesigner
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class GridBase : MonoBehaviour
    {
        public bool enableDesigner = true;
        public float tileSize = 1;
        public float tileScale = 0.95f;
        public float yOffset = 0.01f;
        public int brushIndex = 0;
        public bool alignY = false;
        public string[] tagsToIgnore = new string[0];
        public int layersToIgnore;
        public bool disableMeshRenderersAtStart = false;
        public MeshFilter meshFilter;
        public GridStorageObject storage;

        private static GridBase instance = null;
        
        public static GridBase Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = (GridBase)FindObjectOfType(typeof(GridBase));
                }
                return instance;
            }
        }

        public static void SetMesh(Mesh mesh)
        {
            Instance.GetComponent<MeshFilter>().sharedMesh = mesh;
        }

        public static Mesh GetMesh()
        {
            return Instance.GetComponent<MeshFilter>().sharedMesh;
        }

        private void OnEnable()
        {
            if (disableMeshRenderersAtStart)
            {
                SetMeshRendererEnabled(false);
            }
        }

        public void SetMeshRendererEnabled(bool enabled)
        {
            GetComponent<MeshRenderer>().enabled = enabled;
        }

        public bool ContainsTileAt(Vector3 position)
        {
            return storage.tilePositions.ContainsTileAt(position);
        }
    }
}