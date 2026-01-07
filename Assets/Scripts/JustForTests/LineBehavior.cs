using UnityEngine;

public class LineBehavior : MonoBehaviour{
    [Header("Lines for Motor Platform")]
    [SerializeField] private LineRenderer line1;
    [SerializeField] private LineRenderer line2;
    [SerializeField] private LineRenderer line3;
    [SerializeField] private LineRenderer line4;

    [Header("Lines for Foot Platform")]
    [SerializeField] private LineRenderer line5;
    [SerializeField] private LineRenderer line6;
    [SerializeField] private LineRenderer line7;
    [SerializeField] private LineRenderer line8;

    [Header("Axis")]
    [SerializeField] private LineRenderer xAxis;
    [SerializeField] private LineRenderer yAxis;
    [SerializeField] private LineRenderer zAxis;

    [Header("Angles")]
    [SerializeField] private LineRenderer x2Axis;
    [SerializeField] private LineRenderer y2Axis;
    [SerializeField] private LineRenderer z2Axis;

    [Header("Geometric Objects Positions")]
    [SerializeField] private Transform center;
    [SerializeField] private Transform footPlatform;

    private Vector3 M1;
    private Vector3 M2;
    private Vector3 M3;
    private Vector3 M4;

    private Vector3 F1;
    private Vector3 F2;
    private Vector3 F3;
    private Vector3 F4;

    private void Start(){
        M1 = new Vector3(-19.5f, 28.5f, -15f);
        M2 = new Vector3(-19.5f, 28.5f, 15f);
        M3 = new Vector3(2.8f, 28.5f, 15f);
        M4 = new Vector3(2.8f, 28.5f, -15f);

        F1 = new Vector3(14f, 0f, 6f);
        F2 = new Vector3(14f, 0f, -6f);
        F3 = new Vector3(-14f, 0f, -6f);
        F4 = new Vector3(-14f, 0f, 6f);

        Vector3 centerPos = center.position;
        line1.SetPosition(0, centerPos);
        line2.SetPosition(0, centerPos);
        line3.SetPosition(0, centerPos);
        line4.SetPosition(0, centerPos);
        line5.SetPosition(0, centerPos);
        line6.SetPosition(0, centerPos);
        line7.SetPosition(0, centerPos);
        line8.SetPosition(0, centerPos);
        xAxis.SetPosition(0, centerPos);
        yAxis.SetPosition(0, centerPos);
        zAxis.SetPosition(0, centerPos);
        xAxis.SetPosition(1, centerPos + Vector3.forward * 20f);
        yAxis.SetPosition(1, centerPos + Vector3.left * 20f);
        zAxis.SetPosition(1, centerPos + Vector3.down * 20f);
        x2Axis.SetPosition(0, centerPos);
        y2Axis.SetPosition(0, centerPos);
        z2Axis.SetPosition(0, centerPos);
        x2Axis.SetPosition(1, centerPos + Vector3.forward * 20f + Vector3.left * 20f);
        y2Axis.SetPosition(1, centerPos + Vector3.left * 20f + Vector3.down * 20f);
        z2Axis.SetPosition(1, centerPos + Vector3.down * 20f + Vector3.forward * 20f);
    }

    private void FixedUpdate() {
        Vector3 footPos = footPlatform.position;
        line1.SetPosition(1, footPos + F1);
        line2.SetPosition(1, footPos + F2);
        line3.SetPosition(1, footPos + F3);;
        line4.SetPosition(1, footPos + F4);
        line5.SetPosition(1, M1);
        line6.SetPosition(1, M2);
        line7.SetPosition(1, M3);
        line8.SetPosition(1, M4);

        Vector3 centerPos = center.position;
        xAxis.SetPosition(0, centerPos);
        yAxis.SetPosition(0, centerPos);
        zAxis.SetPosition(0, centerPos);
        xAxis.SetPosition(1, centerPos + Vector3.forward * 20f);
        yAxis.SetPosition(1, centerPos + Vector3.left * 20f);
        zAxis.SetPosition(1, centerPos + Vector3.down * 6f);
        x2Axis.SetPosition(0, centerPos);
        y2Axis.SetPosition(0, centerPos);
        z2Axis.SetPosition(0, centerPos);
        x2Axis.SetPosition(1, centerPos + Vector3.forward * 20f + Vector3.left * 20f);
        y2Axis.SetPosition(1, centerPos + Vector3.left * 8f + Vector3.down * 8f);
        z2Axis.SetPosition(1, centerPos + Vector3.down * 20f + Vector3.forward * 20f);

        line1.SetPosition(0, centerPos);
        line2.SetPosition(0, centerPos);
        line3.SetPosition(0, centerPos);
        line4.SetPosition(0, centerPos);
        line5.SetPosition(0, centerPos);
        line6.SetPosition(0, centerPos);
        line7.SetPosition(0, centerPos);
        line8.SetPosition(0, centerPos);
    }
}

