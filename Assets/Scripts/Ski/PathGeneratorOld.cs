using System.Collections.Generic;
using System.Drawing.Text;
using Unity.VisualScripting;
using UnityEngine;

[ExecuteAlways]
public class PathGeneratorOld: MonoBehaviour {
    [Tooltip("In travelling order")]
    [SerializeField] private List<PostsCheckPoint> checkpoints = new List<PostsCheckPoint>();

    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private LineRenderer distanceLineRenderer;

    [Tooltip("Samples per segment between two control points")]
    [Range(2, 50)]
    [SerializeField] private int samplesPerSegment = 12;

    // internal sampled path points
    private List<Vector3> sampledPoints = new List<Vector3>();
    private List<float> sampledWidths = new List<float>(); // width (half) at each sample (approx)

    public float MaxSampleDistanceForClosestPointSearch = 50f; // safety limit in sampling search

    private void OnValidate() {
        GeneratePath();
    }

    private void Awake() {
        GeneratePath();
    }

    private void OnEnable() {
        GeneratePath();
    }

    /// <summary>
    /// Generates a smooth path by sampling points and widths along a Catmull-Rom spline defined by the current
    /// checkpoints.
    /// </summary>
    public void GeneratePath() {
        sampledPoints.Clear();
        sampledWidths.Clear();
        if (checkpoints == null || checkpoints.Count == 0) return;

        // build center points list
        List<Vector3> centers = new List<Vector3>();
        List<float> halfWidths = new List<float>();
        foreach (var cp in checkpoints) {
            if (cp == null) continue;
            centers.Add(cp.GetCenter());
            halfWidths.Add(cp.GetHalfWidth());
        }

        if (centers.Count == 0) return;

        // For Catmull-Rom we need at least 4 control points; if less, duplicate ends
        // Create padded list
        List<Vector3> p = new List<Vector3>();
        p.Add(centers[0]); // p0 = first (will be duplicated)
        p.AddRange(centers);
        p.Add(centers[centers.Count - 1]); // duplicate last

        // Build widths padded similarly
        List<float> w = new List<float>();
        w.Add(halfWidths[0]);
        w.AddRange(halfWidths);
        w.Add(halfWidths[halfWidths.Count - 1]);

        // Sample Catmull-Rom between p[i] and p[i+1] where i runs 0..p.Count-3 (segment center between p[i+1] and p[i+2])
        for (int i = 0; i < p.Count - 3; i++) {
            Vector3 p0 = p[i];
            Vector3 p1 = p[i + 1];
            Vector3 p2 = p[i + 2];
            Vector3 p3 = p[i + 3];

            float w1 = w[i + 1];
            float w2 = w[i + 2];

            for (int s = 0; s < samplesPerSegment; s++) {
                float t = (float)s / samplesPerSegment;
                Vector3 pos = CatmullRom(p0, p1, p2, p3, t);
                // interpolate width between w1 & w2
                float width = Mathf.Lerp(w1, w2, t);

                sampledPoints.Add(pos);
                sampledWidths.Add(width);
            }
        }

        // add last point explicitly
        sampledPoints.Add(p[p.Count - 2]);
        sampledWidths.Add(w[w.Count - 2]);

        // assign to LineRenderer
        if (lineRenderer != null) {
            lineRenderer.positionCount = sampledPoints.Count;
            lineRenderer.SetPositions(sampledPoints.ToArray());
        }
    }

    // Standard Catmull-Rom interpolation
    private Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t) {
        // Catmull-Rom with tension = 0.5
        float t2 = t * t;
        float t3 = t2 * t;

        return 0.5f * ((2f * p1) +
                       (-p0 + p2) * t +
                       (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                       (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
    }

    /// <summary>
    /// returns the closest point on the path to the given position, along with the distance to that point
    /// </summary>
    public Vector3 GetClosestPoint(Vector3 position, out float distance, out float sampleIndexNormalized) {
        if (sampledPoints == null || sampledPoints.Count == 0) {
            distance = float.MaxValue;
            sampleIndexNormalized = 0f;
            return position;
        }

        int bestIndex = 0;
        float bestSqr = float.MaxValue;
        for (int i = 0; i < sampledPoints.Count; i++) {
            float sqr = (sampledPoints[i] - position).sqrMagnitude;
            if (sqr < bestSqr) {
                bestSqr = sqr;
                bestIndex = i;
            }
        }

        Vector3 closest = sampledPoints[bestIndex];
        distance = Mathf.Sqrt(bestSqr);
        sampleIndexNormalized = sampledPoints.Count > 1 ? (float)bestIndex / (sampledPoints.Count - 1) : 0f;
        return closest;
    }

    /// <summary>
    /// Returns the shortest distance from the given position to the path.
    /// </summary>
    public float GetDistanceToPath(Vector3 position) {
        float d; float t;
        GetClosestPoint(position, out d, out t);
        return d;
    }

    public float GetVerticalDistanceToPath(Vector3 position) {
        float distance = float.MaxValue;
        if (sampledPoints == null || sampledPoints.Count == 0) {
            distance = float.MaxValue;
            return distance;
        }

        int bestIndex = 0;
        for (int i = 0; i < sampledPoints.Count; i++) {
            float verticalDistance = Mathf.Abs(position.z - sampledPoints[i].z);
            float bestVerticalDistance = Mathf.Abs(position.z - sampledPoints[bestIndex].z);
            if (verticalDistance < bestVerticalDistance) {
                bestIndex = i;
                distance = position.x - sampledPoints[i].x;
            }
        }
        distanceLineRenderer.SetPosition(0, position);
        distanceLineRenderer.SetPosition(1, sampledPoints[bestIndex]);

        return distance;
    }

    //Unused
    private void OnDrawGizmos() {
        // draw sampled widths as a corridor
        if (sampledPoints != null && sampledPoints.Count > 0) {
            Gizmos.color = new Color(0f, 0.5f, 1f, 0.2f);
            for (int i = 0; i < sampledPoints.Count; i++) {
                Vector3 p = sampledPoints[i];
                float halfw = sampledWidths[i];
                // draw a small cross showing width direction roughly perpendicular to path
                Vector3 forward = Vector3.zero;
                if (i < sampledPoints.Count - 1) forward = (sampledPoints[i + 1] - p).normalized;
                else forward = (p - sampledPoints[i - 1]).normalized;

                Vector3 right = Vector3.Cross(forward, Vector3.up).normalized;
                Gizmos.DrawLine(p + right * halfw, p - right * halfw);
            }
        }

        // Draw checkpoint centers
        if (checkpoints != null) {
            Gizmos.color = Color.magenta;
            foreach (var cp in checkpoints) {
                if (cp == null) continue;
                Gizmos.DrawSphere(cp.GetCenter(), 0.08f);
            }
        }
    }
}