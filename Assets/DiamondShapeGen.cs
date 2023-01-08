using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A utility class to create the geometry for bone meshes.
/// 
/// The class makes the assumption that it's for an IK chain that
/// is oriented along the Z axis.
/// </summary>
public static class DiamondShapeGen
{
    public static Mesh CreateDiamond(float sqrad, float len, bool flat)
    { 
        // If the length is too short compared to the base point,
        // move the base back.
        float baseExt = Mathf.Min(sqrad, len/2.0f);

        // The positional landmarks
        List<Vector3> lstVerts = 
            new List<Vector3>
            { 
                new Vector3(0.0f, 0.0f, 0.0f),
                new Vector3(-sqrad,  sqrad, baseExt),
                new Vector3( sqrad,  sqrad, baseExt),
                new Vector3( sqrad, -sqrad, baseExt),
                new Vector3(-sqrad, -sqrad, baseExt),
                new Vector3(0.0f,    0.0f,  len)
            };

        // How the positions are stitched.
        int[] ridxs = 
            new int[]
            { 
                // Bot
                0, 1, 2,
                0, 2, 3,
                0, 3, 4,
                0, 4, 1,
                //// Top
                5, 2, 1,
                5, 3, 2,
                5, 4, 3,
                5, 1, 4
            };

        Mesh m = new Mesh();

        // The data above is sparse, and if used directly, it will result in per-vertex
        // normals, which will make the object smooth shaded.
        if(flat)
        {

            // If we want flat shading, we need to duplicate the vertices
            // so each triangle has its own vertice instead of sharing them
            // between triangles.
            List<Vector3> flatVerts = new List<Vector3>();
            foreach(int idx in ridxs)
                flatVerts.Add(lstVerts[idx]);

            // Each 3 indices is a vert, but that's already been handled in the previous loop,
            // so we just need an simple enumerate to draw each vert in order.
            int[] flatIdxs = new int[flatVerts.Count];
            for(int i = 0; i < flatIdxs.Length; ++i)
                flatIdxs[i] = i;

            m.SetVertices(flatVerts);
            m.SetIndices(flatIdxs, MeshTopology.Triangles, 0);
        }
        else
        {
            // Per vert for smooth shading
            m.SetVertices(lstVerts);
            m.SetIndices(ridxs, MeshTopology.Triangles, 0);
        }
        
        m.RecalculateNormals();
        return m;
    }
}
