using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A testing script for the IK system in IKTester.
/// </summary>
public class IKTester : MonoBehaviour
{
    // States of mouse movement handling.
    public enum MouseMode
    {
        None,
        // Moving the mouse moves the camera
        CamDrag,
        // Moving the mouse moves the IK target
        TargDrag
    }

    // Different IK algorithms to try.
    public enum IKType
    {
        // Simple for-loop cascading
        Cascade,        
        // Modify all nodes at the same time.
        Stepped
    }
    delegate void Pop();

    // Set to true to show Az/Ele camera controls. It's disabled 
    // by default because it takes up valuable screen real-estate.
    const bool SHOWCAMUI = false;

    private MouseMode mouseMode = MouseMode.None;
    private Vector3 pointClickedOnTarget = Vector3.zero;    // Used for mouse-dragging
    private Vector2 lastMouse = Vector2.zero;               // Used to track mouse movement deltas.

    public IKType ikType = IKType.Cascade;

    // How thick to make the bone meshes.
    const float diamondRad = 0.5f;
    // The default length of a new bone added.
    const float defDiamondLen = 2.0f;

    public Camera cam;
    // The Azimuth/Elevation camera rig root.
    public GameObject camSys;

    public float camAzimuth = 60.0f;    // Azimuth (ground viewing angle)
    public float camElevation = 30.0f;  // Elevation (height)

    // The material to give bone GameObjects
    public Material diamondMaterial;

    // The IK target
    public Transform ikTarg;
    // The IK nodes. There should always be at least 2, with the last item
    // being the end effector.
    public List<IKSys.Node> ikNodes = new List<IKSys.Node>();

    // Set to false to stop running the IK sim.
    bool runningIK = true;
    // If true, reset the IK chain each frame before running the IK sim.
    bool resetBeforeUpdate = false;

    // The number of IK iterations per frame.
    public int ikIterations = 10;
    bool increaseGreediness = true;
    public float greediness = 0.5f;

    public Vector2 scrollPos = Vector2.zero;
    Pop postOp = null;

    void Start()
    {
        this.Restart();
    }

    /// Reset the contents of the scene to look like when the application 
    /// first started.
    void Restart()
    { 
        this.RemakeIKList(defDiamondLen, 3);
        this.camAzimuth = 60.0f;
        this.camElevation = 30.0f;
        this.ikTarg.position = new Vector3(3.0f, 0.25f, 3.0f);
        this.ikType = IKType.Cascade;
        this.ikIterations = 10;
        this.increaseGreediness = true;
        this.greediness = 0.5f;
    }

    void Update()
    {
        //  Run the IK SIM
        //
        //////////////////////////////////////////////////
        if (this.resetBeforeUpdate)
            IKSys.ResetNodes(this.ikNodes);

        if(this.runningIK)
        {
            switch(ikType)
            { 
                case IKType.Cascade:
                    IKSys.PerformIK_Cascade(this.ikNodes, this.ikTarg.position, this.greediness, this.increaseGreediness, this.ikIterations, 0.001f);
                    break;
                case IKType.Stepped:
                    IKSys.PerformIK_Stepped(this.ikNodes, this.ikTarg.position, this.greediness, this.increaseGreediness, this.ikIterations, 0.001f);
                    break;
            }
        }

        //  HANDLE MOUSE MOVEMENT
        //
        //////////////////////////////////////////////////
        Vector2 mouseDelta = new Vector2(Input.mousePosition.x, Input.mousePosition.y) - this.lastMouse;
        this.lastMouse = Input.mousePosition;
        if(!Input.GetMouseButton(0))
            this.mouseMode = MouseMode.None;
        else if(this.mouseMode == MouseMode.CamDrag)
        { 
            this.camAzimuth += mouseDelta.x;
            this.camElevation -= mouseDelta.y;

            // Additional additions and modulus to keep numbers in the bounds we show with the UI sliders.
            this.camAzimuth = (this.camAzimuth % 360.0f + 360.0f) % 360.0f;
            // Additional additions and modulus to keep numbers in the bounds we show with the UI sliders.
            this.camElevation = ((this.camElevation + 180.0f) % 360.0f + 360.0f) % 360.0f - 180.0f;
        }
        else if(this.mouseMode == MouseMode.TargDrag)
        { 

            Vector3 refPt = this.ikTarg.localToWorldMatrix.MultiplyPoint(this.pointClickedOnTarget);
            Plane refPlane = new Plane(this.cam.transform.forward, refPt);
            Ray curMouseRay = this.cam.ScreenPointToRay(Input.mousePosition);
            float t;
            if(refPlane.Raycast(curMouseRay, out t))
            { 
                Vector3 newRefPt = curMouseRay.GetPoint(t);
                Vector3 diffRefs = newRefPt - refPt;
                this.ikTarg.position += diffRefs;
            }

        }
        this.camSys.transform.rotation = Quaternion.AngleAxis(this.camAzimuth, Vector3.up) * Quaternion.AngleAxis(this.camElevation, Vector3.right);
    }

    // Add the diamond mesh, or update its mesh - for a bone object.
    public void UpdateDiamond(GameObject go, float len)
    { 
        MeshFilter mf = go.GetComponent<MeshFilter>();
        if(mf == null)
        {
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.material = this.diamondMaterial;

            mf = go.AddComponent<MeshFilter>();
        }
        mf.mesh = DiamondShapeGen.CreateDiamond(diamondRad, len, true);
    }

    // Clear the bones and create a new kinematics chain
    public void RemakeIKList(float len, int nodes)
    { 
        this.ClearIKList();
        for (int i = 0; i < nodes + 1; ++i)
            this.AddNode(len);
    }

    // handles adding a new node of a specified length to the kinematics chain.
    public void AddNode(float len)
    { 
        if(this.ikNodes.Count == 0)
        { 
            GameObject root = new GameObject("Root");
            this.ikNodes.Add(new IKSys.Node(root.transform, len));
        }
        else
        {

            IKSys.Node prevLast = this.ikNodes[this.ikNodes.Count - 1];
            this.UpdateDiamond(prevLast.transform.gameObject, prevLast.len);

            // Capture the last item (previously the EF) and set it up to be a joint,
            // and then add another node as the new EF
            GameObject newEF = new GameObject("Node");
            newEF.transform.SetParent(prevLast.transform);
            newEF.transform.localRotation = Quaternion.identity;
            newEF.transform.localPosition = new Vector3(0.0f, 0.0f, prevLast.len);
            //
            this.ikNodes.Add(new IKSys.Node(newEF.transform, defDiamondLen));
        }
    }

    // Clear the kinematics chain.
    public void ClearIKList()
    {
        foreach(IKSys.Node n in this.ikNodes)
            GameObject.Destroy(n.transform.gameObject);

        this.ikNodes.Clear();
    }

    // Raycast into the screen and check if the target object was clicked on.
    bool AttemptRaycastForTarget(Vector2 mousePos)
    { 
        Ray camRay = this.cam.ScreenPointToRay(mousePos);
        RaycastHit[] rhAll = Physics.RaycastAll(camRay);
        foreach(RaycastHit rh in rhAll)
        { 
            if(rh.collider.gameObject == this.ikTarg.gameObject)
            {
                this.pointClickedOnTarget = this.ikTarg.worldToLocalMatrix.MultiplyPoint(rh.point);
                return true;
            }
        }
        return false;
    }

    // When getting and passing mouse position as screen coordinates, they may 
    // have different conventions on where the Y origin is and what direction Y is.
    static Vector2 InvertMousePosY(Vector2 v)
    { 
        return new Vector2(v.x, Screen.height - v.y);
    }

    private void OnGUI()
    {
        GUILayout.BeginVertical("box", GUILayout.Width(200.0f), GUILayout.Height(Screen.height));

        
            // Depending on the IK sim running state, we color the toggle differently.
            GUI.color = this.runningIK ? Color.green : Color.red;
            this.runningIK = GUILayout.Toggle(this.runningIK, "Running IK");
            GUI.color = Color.white;

            // Greediness controls. Only show the button for reset if it's not resetting
            // every frame.
            this.increaseGreediness = GUILayout.Toggle(this.increaseGreediness, "Increase Greediness");
            this.resetBeforeUpdate = GUILayout.Toggle(this.resetBeforeUpdate, "Resets");
            if(!this.resetBeforeUpdate)
            { 
                if(GUILayout.Button("Reset Transforms"))
                    IKSys.ResetNodes(this.ikNodes);
            }
            GUILayout.Label("Greediness");
            this.greediness = GUILayout.HorizontalSlider(this.greediness, 0.0f, 1.0f);

            // Simple -/+ buttons to control the IK iteration count.
            GUILayout.BeginHorizontal();
                GUILayout.Label("Its", GUILayout.ExpandWidth(false));
                if(GUILayout.Button("-", GUILayout.ExpandWidth(false)))
                    this.ikIterations = Mathf.Max(this.ikIterations - 1, 2);
                GUI.enabled = false;
                GUILayout.TextField(this.ikIterations.ToString());
                GUI.enabled = true;
                if (GUILayout.Button("+", GUILayout.ExpandWidth(false)))
                    this.ikIterations = Mathf.Min(this.ikIterations + 1, 50);
            GUILayout.EndHorizontal();
            // Allow changing the algorithm.
            GUILayout.BeginHorizontal();
                if(GUILayout.Toggle(this.ikType == IKType.Cascade, "Cascade"))
                    this.ikType = IKType.Cascade;
                if(GUILayout.Toggle(this.ikType == IKType.Stepped, "Stepped"))
                    this.ikType = IKType.Stepped;
            GUILayout.EndHorizontal();

            // Node controls
            GUILayout.Box("Nodes");
                if (GUILayout.Button("Add Node"))
                    this.AddNode(defDiamondLen);
                this.scrollPos = GUILayout.BeginScrollView(this.scrollPos);

                // Per-node listings with length and delete controls.
                for(int i = 0; i < this.ikNodes.Count - 1; ++i) // -1 to skip last which is used as EF
                { 
                    GUILayout.BeginVertical("box", GUILayout.ExpandWidth(true));
                        if(i == 0)
                            GUILayout.Label("Root");
                        else
                        { 
                            GUILayout.BeginHorizontal();
                            GUILayout.Label($"Node {i}", GUILayout.ExpandWidth(true));
                            if(this.ikNodes.Count > 2)
                            { 
                                if(GUILayout.Button("X", GUILayout.ExpandWidth(false)))
                                { 
                                    int rmIdx = i;
                                    this.postOp = 
                                        ()=>
                                        { 
                                            GameObject.Destroy(this.ikNodes[rmIdx].transform.gameObject);
                                            this.ikNodes.RemoveAt(rmIdx);
                                            this.ikNodes[rmIdx].transform.SetParent(this.ikNodes[rmIdx - 1].transform);
                                            this.ikNodes[rmIdx].transform.localPosition = new Vector3(0.0f, 0.0f, this.ikNodes[rmIdx -1].len);
                                        };
                                }
                            }
                            GUILayout.EndHorizontal();
                        }
                        float prevLen = this.ikNodes[i].len;
                        float newLen = GUILayout.HorizontalSlider(this.ikNodes[i].len, 0.5f, 10.0f, GUILayout.ExpandWidth(true));
                        if(prevLen != newLen)
                        { 
                            this.ikNodes[i].len = newLen;
                            this.UpdateDiamond(this.ikNodes[i].transform.gameObject, newLen);
                            this.ikNodes[i+1].transform.localPosition = new Vector3(0.0f, 0.0f, newLen);
                        }
                    GUILayout.EndVertical();
                }
            GUILayout.EndScrollView();

        if(SHOWCAMUI)
        {
            GUILayout.BeginVertical("box");
                GUILayout.Box("Camera");
                GUILayout.Label("Azimuth");
                this.camAzimuth = GUILayout.HorizontalSlider(this.camAzimuth, 0.0f, 360.0f);
                GUILayout.Space(5.0f);
                GUILayout.Label("Elevation");
                this.camElevation = GUILayout.HorizontalSlider(this.camElevation, -180.0f, 180.0f);
            GUILayout.EndVertical();
        }
        GUILayout.Box("", GUILayout.Height(5.0f), GUILayout.ExpandWidth(true)); // Horizontal separator
        if (GUILayout.Button("Restart"))
            this.Restart();

        GUILayout.EndVertical();

        // Check IMGUI messages to see if 
        //
        // Mouse click fell through
        if(Event.current.type == EventType.MouseDown)
        { 
            if(this.AttemptRaycastForTarget(InvertMousePosY(Event.current.mousePosition)))
                this.mouseMode = MouseMode.TargDrag;
            else
                this.mouseMode = MouseMode.CamDrag;
        }
        // The mouse button was released
        else if(Event.current.type == EventType.MouseUp)
        { 
            this.mouseMode = MouseMode.None;
        }
        // It's the end of the GUI passes.
        else if(Event.current.type == EventType.Repaint)
        { 
            if(this.postOp != null)
            { 
                Pop oldPop = this.postOp;
                this.postOp = null;
                oldPop();
            }
        }

    }
}
