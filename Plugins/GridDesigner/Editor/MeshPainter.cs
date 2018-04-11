using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GridDesigner
{
    public class MeshPainter
    {
        private Mesh mesh;

        private float offset = 0.5f;

        public void Enable()
        {
            SetOffset();
        }

        public void SetOffset()
        {
            offset = GridBase.Instance.tileScale / 2f;
        }

        public void ClearMesh()
        {
            GridBase.GetMesh().Clear();
        }

        public void DrawTile(Vector3 position)
        {
            mesh = GridBase.GetMesh();
            int vertsIndex = mesh.vertices.Length;
            Vector3[] newVerts = mesh.vertices;
            System.Array.Resize(ref newVerts, vertsIndex + 4);

            newVerts[vertsIndex] = new Vector3(position.x + offset, position.y + GridBase.Instance.yOffset, position.z + offset);
            newVerts[vertsIndex+1] = new Vector3(position.x + offset, position.y + GridBase.Instance.yOffset, position.z - offset);
            newVerts[vertsIndex+2] = new Vector3(position.x - offset, position.y + GridBase.Instance.yOffset, position.z + offset);
            newVerts[vertsIndex+3] = new Vector3(position.x - offset, position.y + GridBase.Instance.yOffset, position.z - offset);

            int trisIndex = mesh.triangles.Length;
            int[] newTris = mesh.triangles;
            System.Array.Resize(ref newTris, trisIndex + 6);

            newTris[trisIndex] = vertsIndex;
            newTris[trisIndex + 1] = vertsIndex + 1;
            newTris[trisIndex + 2] = vertsIndex + 2;

            newTris[trisIndex + 3] = vertsIndex + 3;
            newTris[trisIndex + 4] = vertsIndex + 2;
            newTris[trisIndex + 5] = vertsIndex + 1;

            int uvIndex = mesh.uv.Length;
            Vector2[] newUv = mesh.uv;
            System.Array.Resize(ref newUv, uvIndex + 4);

            newUv[uvIndex] = Vector2.one;
            newUv[uvIndex +1] = Vector2.right;
            newUv[uvIndex +2] = Vector2.up;
            newUv[uvIndex +3] = Vector2.zero;

            mesh.vertices = newVerts;
            mesh.triangles = newTris;
            mesh.uv = newUv;

            GridBase.SetMesh(mesh);
        }

        public void UndrawTile(Vector3 position)
        {
            mesh = GridBase.GetMesh();
            Vector3[] verts = mesh.vertices;
            int[] tris = mesh.GetTriangles(0);
            Vector2[] uv = mesh.uv;

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

                uv = uv.Where((val, idx) =>
                idx != indexToRemove &&
                idx != indexToRemove + 1 &&
                idx != indexToRemove + 2 &&
                idx != indexToRemove + 3
                ).ToArray();

                ClearMesh();
                mesh.vertices = verts;
                mesh.triangles = tris;
                mesh.uv = uv;
                GridBase.SetMesh(mesh);
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