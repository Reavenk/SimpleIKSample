using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// Convert a mesh to display as a geometry wireframe at Start().
/// 
/// Used to create simple wireframes without bringing in extra
/// shader assets.
public class ConvertRenderingToWireframe : MonoBehaviour
{
    
    void Start()
    {
        // Get the current mesh in the GameObject.
        MeshFilter mf = this.GetComponent<MeshFilter>();
        if(mf == null)
            return;

        Mesh mToCpy = mf.mesh;


        // Make a copy of it designed to be displayed as wireframe.
        List<Vector3> verts = new List<Vector3>(mToCpy.vertices);
        List<int> lineIndices = new List<int>();
        //
        int[] origInidces = mToCpy.GetIndices(0);
        for(int i = 0; i < origInidces.Length; i += 3)
        { 
            lineIndices.Add(origInidces[i + 0]);
            lineIndices.Add(origInidces[i + 1]);
            lineIndices.Add(origInidces[i + 1]);
            lineIndices.Add(origInidces[i + 2]);
            lineIndices.Add(origInidces[i + 2]);
            lineIndices.Add(origInidces[i + 0]);
        }
        
        Mesh wireMesh = new Mesh();
        wireMesh.SetVertices(verts);
        // Make sure to use MeshTopology.Lines.
        wireMesh.SetIndices(lineIndices.ToArray(), MeshTopology.Lines, 0);

        // Replace original mesh.
        mf.mesh = wireMesh;

    }

}
