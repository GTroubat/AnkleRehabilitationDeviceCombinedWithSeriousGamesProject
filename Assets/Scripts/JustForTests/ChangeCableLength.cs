using UnityEngine;

public class ChangeCableLength : MonoBehaviour
{
    [SerializeField] private StockVariables stock;
    [SerializeField] private MathematicalModel model;
    [SerializeField] private DataExporter dataExporter;
    [SerializeField] private Transform footPlatformTransform;
    [SerializeField] private Transform ankle;

    [Header("Cables (cylinder transforms)")]
    [SerializeField] private Transform cable1;
    [SerializeField] private Transform cable2;
    [SerializeField] private Transform cable3;
    [SerializeField] private Transform cable4;

    [Header("Platform rigidbodies")]
    [Tooltip("Top platform (kinematic)")]
    [SerializeField] private Rigidbody topPlatformRb;
    [Tooltip("Bottom platform (moving)")]
    [SerializeField] private Rigidbody bottomPlatformRb;

    // internal storage for the two joints per cable: [0] = joint connected to top, [1] = joint connected to bottom
    private ConfigurableJoint[] topJoints = new ConfigurableJoint[4];
    private ConfigurableJoint[] bottomJoints = new ConfigurableJoint[4];

    private Transform[] cables;
    private float[] baseLength = { 360, 360, 360, 360 };

    private static readonly int maxSteps = 10100;
    private float[,] datas = new float[6,maxSteps/100+1]; //setpoint, 4 cables length, real angle
    private int counter = 0;
    private bool isStabalized = false;

    private void Start()
    {
        stock.SetHeight(150);

        cables = new[] { cable1, cable2, cable3, cable4 };

        // discover joints for each cable and assign them to top/bottom arrays
        for (int i = 0; i < cables.Length; i++) {
            var c = cables[i];
            if (c == null) continue;

            var joints = c.GetComponents<ConfigurableJoint>();
            if (joints == null || joints.Length == 0) continue;

            // try to identify by connectedBody first
            foreach (var j in joints) {
                if (j.connectedBody == topPlatformRb) { topJoints[i] = j; Debug.Log(j + " identify first try"); } else if (j.connectedBody == bottomPlatformRb) bottomJoints[i] = j;
            }
        }
        stock.SetSend(true);
    }


    // physics-safe anchor updates (only joint.anchor, not connectedAnchor)
    private void FixedUpdate() {
        if (!isStabalized) {
            if (counter > 20000) {
                counter = 0;
                isStabalized = true;
            }
        }
        Debug.Log("isStabalized: " + isStabalized);
        if (counter < maxSteps && counter % 100 == 0 && isStabalized) {
            stock.SetRoll(stock.GetRoll() + 0.8f);
            Debug.Log("Roll set to: " + stock.GetRoll());
        }
        if (counter >= maxSteps && isStabalized) {
            dataExporter.ConvertArrayToCsv(datas);
            enabled = false; //disable this script
        }
        //visual
        var lengths = model.GetCableLengths();

        if (cable1 != null) cable1.localScale = new Vector3(cable1.localScale.x, lengths[0] / 20f, cable1.localScale.z);
        if (cable2 != null) cable2.localScale = new Vector3(cable2.localScale.x, lengths[1] / 20f, cable2.localScale.z);
        if (cable3 != null) cable3.localScale = new Vector3(cable3.localScale.x, lengths[2] / 20f, cable3.localScale.z);
        if (cable4 != null) cable4.localScale = new Vector3(cable4.localScale.x, lengths[3] / 20f, cable4.localScale.z);

        //ankle
        if (ankle != null) {
            ankle.rotation = footPlatformTransform.rotation;
            ankle.position = new Vector3(0f, footPlatformTransform.position.y + 8f, 0f);
        }

        //Anchors
        for (int i = 0; i < cables.Length; i++) {
            var c = cables[i];


            UpdateCableJointAnchorsPhysics(c, topJoints[i], bottomJoints[i], lengths[i], baseLength[i]);
        }

        if (counter % 100 == 0 && isStabalized) {
            datas[0, counter / 100] = stock.GetRoll();
            datas[1, counter / 100] = lengths[0];
            datas[2, counter / 100] = lengths[1];
            datas[3, counter / 100] = lengths[2];
            datas[4, counter / 100] = lengths[3];
            datas[5, counter / 100] = footPlatformTransform.eulerAngles.x;
        }
        counter++;

        stock.SetSend(true);
    }

    private void UpdateCableJointAnchorsPhysics(Transform cable, ConfigurableJoint topJoint, ConfigurableJoint bottomJoint, 
            float length, float baseLength) {
        float anchorPosition = length / baseLength;
        topJoint.anchor = new Vector3(0f, anchorPosition, 0f);
        bottomJoint.anchor = new Vector3(0f, -anchorPosition, 0f);
    }
}