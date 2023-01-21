using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The IK library. 
/// It's just a Node subtype and a few static utility functions.
/// </summary>
public static class IKSys
{
    // Individual nodes in the IK system.
    //
    // It's made serializable in case anyone wants to debug values in the inspector,
    // but beyond that there isn't any significance to it.
    //
    // Note that the term IK Node will be used interchangeable with "bone".
    [System.Serializable]
    public class Node
    { 
        // The origin on the bone.
        public Transform transform;

        // The length of the bone.
        internal float len;

        public Node(Transform t, float len)
        { 
            this.transform = t;
            this.len = len;
        }
    }

    /// <summary>
    /// Perform an IK solver calculation on a kinematic chain.
    /// 
    /// This version modifies the nodes as they are iterated.
    /// </summary>
    /// <param name="nodes">
    /// The kinematic chain to solve for. The last node is not calculated, but instead used as the end-effector.
    /// </param>
    /// <param name="targ">The target point the end-effector should reach.</param>
    /// <param name="rotPercent">How aggressibly any joint calculation is at reaching the end effector.</param>
    /// <param name="incrRaiseRotAmt">
    /// If true, the rotation percent gets raised as the iterations are processed.
    /// </param>
    /// <param name="iterCt">The number of times to loop through the joints.</param>
    /// <param name="eps">
    /// An early exit distance. If the EF and target are at least this close, we don't need to continue the IK calculations.
    /// </param>
    public static void PerformIK_Cascade(List<Node> nodes, Vector3 targ, float rotPercent = 0.1f, bool incrRaiseRotAmt = true, int iterCt = 20, float eps = 0.001f)
    {

        int lastNodeIdx = nodes.Count - 1;
        Transform endEffector = nodes[lastNodeIdx].transform;
        float rotAmt = rotPercent;
        // For each iteration
        for(int i = 0; i < iterCt; ++i)
        { 
            // For each bone in the kinematic chain
            for(int nit = 0; nit < lastNodeIdx; ++nit)
            { 
                
                Vector3 basePos = nodes[nit].transform.position;
                Vector3 EFPos = endEffector.position;

                // Calculate Root->EF
                Vector3 baseToEF = EFPos - basePos;

                // Calculate Root->Targ
                Vector3 baseToTarg = targ - basePos;

                // Calculate rotation
                Quaternion rotFromto = Quaternion.FromToRotation(baseToEF, baseToTarg);

                // Calculate partial rotation & apply partial rotation
                Quaternion rotRestrained = Quaternion.Lerp(Quaternion.identity, rotFromto, rotAmt);
                nodes[nit].transform.rotation = rotRestrained * nodes[nit].transform.rotation;
            }

            // If increased greediness, increase to where rotation amount is 
            // close to 1.0 on the final iteration.
            if(incrRaiseRotAmt)
                rotAmt += (1.0f - rotPercent)/(iterCt - 1);

            // Check if our IK is at a solution that's "good enough"
            float distEFtoTarg = (endEffector.position - targ).magnitude;
            if(distEFtoTarg <= eps)
                break;

        }
    }

    /// <summary>
    /// Perform an IK solver calculation on a kinematic chain.
    /// 
    /// This version stores how much each node should be modified, and performs 
    /// the modification in batch at the end of each iteration. Thus each node
    /// is evaluated with the same IK chain state.
    /// 
    /// See parameter listings for PerformIK_Cascade for more details.
    /// </summary>
    public static void PerformIK_Stepped(List<Node> nodes, Vector3 targ, float rotPercent = 0.1f, bool incrRaiseRotAmt = true, int iterCt = 20, float eps = 0.001f)
    {
        int nodeLinkCt = nodes.Count - 1;
        Quaternion[] rqmods = new Quaternion[nodeLinkCt];
        Transform endEffector = nodes[nodeLinkCt].transform;
        float rotAmt = rotPercent;
        for (int i = 0; i < iterCt; ++i)
        {

            for (int nit = 0; nit < nodeLinkCt; ++nit)
            {
                // For each iteration of processing a joint, there are
                // 3 significant references
                // 1) The base position of the thing we're rotating
                // 2) The end effector
                // 3) The target
                Vector3 basePos = nodes[nit].transform.position;
                Vector3 EFPos = endEffector.position;
                Vector3 baseToEF = EFPos - basePos;
                Vector3 baseToTarg = targ - basePos;

                // How much our joint needs to rotate to align
                Quaternion rotFromto = Quaternion.FromToRotation(baseToEF, baseToTarg);
                rqmods[nit] = Quaternion.Lerp(Quaternion.identity, rotFromto, rotAmt);
            }

            for(int nit = 0; nit < nodeLinkCt; ++nit)
                nodes[nit].transform.rotation = rqmods[nit] * nodes[nit].transform.rotation;

            if (incrRaiseRotAmt)
                rotAmt += (1.0f - rotPercent) / (iterCt - 1);

            // Check if our IK is at a solution that's "good enough"
            float distEFtoTarg = (endEffector.position - targ).magnitude;
            if (distEFtoTarg <= eps)
                break;

        }
    }

    // Straighten the IK chain along the Z axis.
    public static void ResetNodes(List<Node> nodes)
    { 
        for(int i = 0; i < nodes.Count; ++i)
        {
            Node n = nodes[i];
            if(i == 0)
                n.transform.localPosition = Vector3.zero;
            else
                n.transform.localPosition = new Vector3(0.0f, 0.0f, nodes[i-1].len);

            n.transform.localRotation = Quaternion.identity;
        }
    }
}
