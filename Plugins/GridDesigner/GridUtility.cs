using System.Collections.Generic;
using UnityEngine;

namespace GridDesigner
{
    public static class GridUtility
    {
        public const string BRUSH_TAG = "Brush";

        public const float Y_OFFSCREEN = -10000;
        public const float RAYCAST_HEIGHT = 50;
        public const float RAYCAST_DISTANCE = 100;

        /// <summary>
        /// Returns the closest position to the provided Vector3 on the grid
        /// <see cref="CheckAndRoundY(Vector3)"/>
        /// </summary>
        public static Vector3 GetAlignedPosition(Vector3 position)
        {
            float roundValue = 1 / GridBase.Instance.tileSize;
            float x = Mathf.Round(position.x * roundValue) / roundValue;
            float z = Mathf.Round(position.z * roundValue) / roundValue;

            float y = CheckVerticalAlignment(position.y);

            return new Vector3(x, y, z);
        }

        /// <summary>
        /// Returns the closest position to y on the vertical grid, if vertical is enabled (alignY = true)
        /// </summary>
        public static float CheckVerticalAlignment(float y)
        {
            if (GridBase.Instance.alignY)
            {
                float roundValue = 1 / GridBase.Instance.tileSize;
                return Mathf.Round(y * roundValue) / roundValue;
            }

            return y;
        }

        /// <summary>
        /// Raycasts the Floor from above and returns the first Collider hit.
        /// Ignores the layers and tags defined on the GridBase component
        /// </summary>
        public static float FloorHeight(Vector3 position)
        {
            LayerMask layersToIgnore = GridBase.Instance.layersToIgnore;
            RaycastHit[] hit = Physics.RaycastAll(position + (Vector3.up * RAYCAST_HEIGHT), Vector3.down, RAYCAST_DISTANCE);
            float maxY = Y_OFFSCREEN;
            foreach (RaycastHit h in hit)
            {
                // ignore layers
                if (layersToIgnore.Contains(h.collider.gameObject.layer))
                {
                    continue;
                }

                // ignore the defined tags
                if (System.Array.IndexOf(GridBase.Instance.tagsToIgnore, h.transform.tag) >= 0)
                {
                    continue;
                }

                if (maxY < h.point.y)
                {
                    maxY = h.point.y;
                }
            }

            return maxY;
        }

        /// <summary>
        /// Extension method to check if a layer is in a layermask
        /// </summary>
        public static bool Contains(this LayerMask mask, int layer)
        {
            return mask == (mask | (1 << layer));
        }
    }

    public static class Vector3ListExtension
    {
        public static bool ContainsTileAt(this List<Vector3> list, Vector3 position)
        {
            foreach (Vector3 v in list)
            {
                if (position.ApproximatelySame(v))
                {
                    return true;
                }
            }
            return false;
        }

        public static int GetIndex(this List<Vector3> list, Vector3 position)
        {
            foreach (Vector3 v in list)
            {
                if (position.ApproximatelySame(v))
                {
                    return list.IndexOf(v);
                }
            }

            return -1;
        }

        public static bool ApproximatelySame(this Vector3 a, Vector3 b)
        {
            return Mathf.Approximately(b.x, a.x) && Mathf.Approximately(b.z, a.z);
        }
    }
}
