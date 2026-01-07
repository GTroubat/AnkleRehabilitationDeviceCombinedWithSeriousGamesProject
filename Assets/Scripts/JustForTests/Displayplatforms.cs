using UnityEngine;

public class Displayplatforms : MonoBehaviour
{
    [SerializeField] private StockVariables stock;
    [SerializeField] private MathematicalModel model;

    [SerializeField] private GameObject upperPlatform;
    [SerializeField] private GameObject lowerPlatform;

    [SerializeField] private LineRenderer string1;
    [SerializeField] private LineRenderer string2;
    [SerializeField] private LineRenderer string3;
    [SerializeField] private LineRenderer string4;

    private Vector3 fp1 = new Vector3(15f, 28.5f, 11.162f);
    private Vector3 fp2 = new Vector3(-15f, 28.5f, 11.162f);
    private Vector3 fp3 = new Vector3(-15f, 28.5f, -11.162f);
    private Vector3 fp4 = new Vector3(15f, 28.5f, -11.162f);

    private Vector3 p1;
    private Vector3 p2;
    private Vector3 p3;
    private Vector3 p4;

    private MeshFilter lowerMeshFilter;
    private Renderer lowerRenderer;

    void Start() {
        upperPlatform.transform.position = new Vector3(0f, 28.5f, 0f);
        upperPlatform.transform.eulerAngles = new Vector3(0f, 0f, 0f);

        lowerPlatform.transform.position = new Vector3(0f, 0f, 0f);
        lowerPlatform.transform.eulerAngles = new Vector3(0f, 0f, 0f);

        // cache mesh/filter and renderer
        lowerMeshFilter = lowerPlatform.GetComponent<MeshFilter>() ?? lowerPlatform.GetComponentInChildren<MeshFilter>();
        lowerRenderer = lowerPlatform.GetComponent<Renderer>() ?? lowerPlatform.GetComponentInChildren<Renderer>();

        // compute initial corner positions
        UpdateLowerPlatformCorners();
        //UpdateStringWithModel();

        string1.SetPosition(0, fp1);
        string2.SetPosition(0, fp2);
        string3.SetPosition(0, fp3);
        string4.SetPosition(0, fp4);

        string1.SetPosition(1, p1);
        string2.SetPosition(1, p2);
        string3.SetPosition(1, p3);
        string4.SetPosition(1, p4);

        stock.SetPitch(20f);
    }

    void FixedUpdate() {
        lowerPlatform.transform.position = new Vector3(0f, stock.GetHeight()/10, 0f);
        lowerPlatform.transform.eulerAngles = new Vector3(stock.GetRoll(), stock.GetYaw(), stock.GetPitch());

        // Recompute corners after transform change
        UpdateLowerPlatformCorners();

        //UpdateStringWithModel();

        string1.SetPosition(1, p1);
        string2.SetPosition(1, p2);
        string3.SetPosition(1, p3);
        string4.SetPosition(1, p4);
    }

    private void UpdateStringWithModel() {
        //float[,] cableVectors = model.GetCableVectors();
        //p1 = new Vector3(cableVectors[0,0]/10 + fp1.x, cableVectors[0,1]/10 + fp1.y, cableVectors[0,2]/10 + fp1.z);
        //p2 = new Vector3(cableVectors[1,0]/10 + fp2.x, cableVectors[1,1]/10 + fp2.y, cableVectors[1,2]/10 + fp2.z);
        //p3 = new Vector3(cableVectors[2,0]/10 + fp3.x, cableVectors[2,1]/10 + fp3.y, cableVectors[2,2]/10 + fp3.z);
        //p4 = new Vector3(cableVectors[3,0]/10 + fp4.x, cableVectors[3,1]/10 + fp4.y, cableVectors[3,2]/10 + fp4.z);
    }

    // Compute the four top-surface corner world positions of the lowerPlatform.
    // If a MeshFilter is present we use the mesh bounds (local space) and TransformPoint.
    // Otherwise we fall back to Renderer.bounds (world AABB).
    private void UpdateLowerPlatformCorners()
    {
        if (lowerMeshFilter != null && lowerMeshFilter.sharedMesh != null)
        {
            Bounds b = lowerMeshFilter.sharedMesh.bounds; // local-space bounds

            // top face local y coordinate
            float topY = b.center.y + b.extents.y;

            // four top corners in local mesh space (order matches previous sign convention)
            Vector3 c1 = new Vector3(b.center.x + b.extents.x, topY, b.center.z + b.extents.z); // +x +z
            Vector3 c2 = new Vector3(b.center.x - b.extents.x, topY, b.center.z + b.extents.z); // -x +z
            Vector3 c3 = new Vector3(b.center.x - b.extents.x, topY, b.center.z - b.extents.z); // -x -z
            Vector3 c4 = new Vector3(b.center.x + b.extents.x, topY, b.center.z - b.extents.z); // +x -z

            p1 = lowerPlatform.transform.TransformPoint(c1);
            p2 = lowerPlatform.transform.TransformPoint(c2);
            p3 = lowerPlatform.transform.TransformPoint(c3);
            p4 = lowerPlatform.transform.TransformPoint(c4);
        }
        else if (lowerRenderer != null)
        {
            // Renderer.bounds is in world space (axis-aligned). Use its top face corners.
            Bounds rb = lowerRenderer.bounds;
            Vector3 min = rb.min;
            Vector3 max = rb.max;
            float topY = max.y;

            p1 = new Vector3(max.x, topY, max.z); // +x +z
            p2 = new Vector3(min.x, topY, max.z); // -x +z
            p3 = new Vector3(min.x, topY, min.z); // -x -z
            p4 = new Vector3(max.x, topY, min.z); // +x -z
        }
        else
        {
            // Fallback: preserve previous hardcoded-ish extents if nothing available
            p1 = lowerPlatform.transform.TransformPoint(new Vector3(14f, 0f, 6f));
            p2 = lowerPlatform.transform.TransformPoint(new Vector3(-14f, 0f, 6f));
            p3 = lowerPlatform.transform.TransformPoint(new Vector3(-14f, 0f, -6f));
            p4 = lowerPlatform.transform.TransformPoint(new Vector3(14f, 0f, -6f));
        }
    }
}
