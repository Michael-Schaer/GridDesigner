using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GridDesigner
{
    public class GridStorageObject : ScriptableObject
    {
        public List<Vector3> tilePositions = new List<Vector3>();
        public int startIndex = 0;
        public List<Vector3> tilePositionsGUI = new List<Vector3>();
    }

}
