using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GridDesigner
{
    public class MeshPainter
    {
        private Mesh mesh;
        private GridBaseEditor editor;
        private float offset = 0.5f;

        public void Enable(GridBaseEditor editor)
        {
            this.editor = editor;
            mesh = editor.GetMesh();
            SetOffset();
            //Debug.Log(GridBase.Instance.tilePositions[0]); /*////////*/
        }

        public void SetOffset()
        {
            offset = editor.tileScale.floatValue / 2f;
        }

        public void Disable()
        {
            //Debug.Log(GridBase.Instance.tilePositions[0] + "Disable"); /*////////*/
        }

        public void ClearMesh()
        {
            mesh.Clear();
        }

        public void DrawTile(Vector3 position)
        {
            int vertsIndex = mesh.vertices.Length;
            Vector3[] newVerts = mesh.vertices;
            System.Array.Resize(ref newVerts, vertsIndex + 4);

            //TODO height
            newVerts[vertsIndex] = new Vector3(position.x + offset, position.y + editor.yOffset.floatValue, position.z + offset);
            newVerts[vertsIndex+1] = new Vector3(position.x + offset, position.y + editor.yOffset.floatValue, position.z - offset);
            newVerts[vertsIndex+2] = new Vector3(position.x - offset, position.y + editor.yOffset.floatValue, position.z + offset);
            newVerts[vertsIndex+3] = new Vector3(position.x - offset, position.y + editor.yOffset.floatValue, position.z - offset);

            int trisIndex = mesh.triangles.Length;
            int[] newTris = mesh.triangles;
            System.Array.Resize(ref newTris, trisIndex + 6);

            newTris[trisIndex] = vertsIndex;
            newTris[trisIndex + 1] = vertsIndex + 1;
            newTris[trisIndex + 2] = vertsIndex + 2;

            newTris[trisIndex + 3] = vertsIndex + 3;
            newTris[trisIndex + 4] = vertsIndex + 2;
            newTris[trisIndex + 5] = vertsIndex + 1;

            mesh.vertices = newVerts;
            mesh.triangles = newTris;

            editor.SetMesh(mesh);
        }

        public void UndrawTile(Vector3 position)
        {
            Vector3[] verts = mesh.vertices;
            int[] tris = mesh.GetTriangles(0);

            int indexToRemove = FindTileIndex(verts, position);

            if (indexToRemove >= 0)
            {
                verts = verts.Where((val, idx) =>
                idx != indexToRemove &&
                idx != indexToRemove + 1 &&
                idx != indexToRemove + 2 &&
                idx != indexToRemove + 3
                ).ToArray();

                int trisIndexToRemove = indexToRemove / 4 * 6;

                tris = tris.Where((val, idx) =>
                idx != trisIndexToRemove &&
                idx != trisIndexToRemove + 1 &&
                idx != trisIndexToRemove + 2 &&
                idx != trisIndexToRemove + 3 &&
                idx != trisIndexToRemove + 4 &&
                idx != trisIndexToRemove + 5
                ).ToArray();

                tris = RearrangeTris(indexToRemove + 4, tris);

                ClearMesh();
                mesh.vertices = verts;
                mesh.triangles = tris;
                editor.SetMesh(mesh);
            }
        }

        private int[] RearrangeTris(int startIndex, int[] tris)
        {
            for (int i = 0; i < tris.Length; i++)
            {
                if(tris[i] >= startIndex)
                {
                    tris[i] = tris[i] - 4;
                }
            }

            return tris;
        }

        private int FindTileIndex(Vector3[] verts, Vector3 position)
        {
            for (int i = 0; i < verts.Length; i += 4)
            {
                if (position.ApproximatelySame(new Vector3(verts[i].x - offset, 0, verts[i].z - offset)))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}