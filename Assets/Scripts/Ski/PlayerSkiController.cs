using UnityEngine;

/// <summary>
/// Exemple simple de contrôle pour un skieur (Rigidbody). 
/// La vitesse cible est modulée par la distance au chemin généré par PathGenerator:
/// plus proche -> multiplicateur proche de 1 + boost, plus loin -> multiplicateur < 1.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerSkiController: MonoBehaviour {
    public PathGenerator pathGenerator;

    [Header("Speed settings")]
    public float baseSpeed = 8f;            // vitesse de base
    public float maxBoostMultiplier = 1.6f; // quand on est parfaitement sur la ligne
    public float minMultiplier = 0.6f;      // si on est très loin de la ligne
    public float falloffDistance = 5f;      // distance à partir de laquelle on atteint minMultiplier

    [Header("Physics")]
    public float accelResponsiveness = 5f;  // how fast velocity blends to target
    public float steerResponsiveness = 5f;  // how fast rotation aligns with path direction

    public AnimationCurve multiplierCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Rigidbody rb;

    private void Awake() {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate() {
        if (pathGenerator == null) return;

        // get nearest point and distance
        float dist = pathGenerator.GetDistanceToPath(transform.position);
        // normalized 0..1 where 0 = on path, 1 = at falloffDistance or more
        float t = Mathf.Clamp01(dist / falloffDistance);

        // Use curve to get feel (can tweak in inspector)
        float curveVal = multiplierCurve.Evaluate(1f - t); // when close (t=0) evaluate(1) -> high
        // map curveVal (0..1) to multiplier range min..max
        float multiplier = Mathf.Lerp(minMultiplier, maxBoostMultiplier, curveVal);

        float targetSpeed = baseSpeed * multiplier;

        // desired forward direction: project current forward onto horizontal plane
        Vector3 forward = transform.forward;
        forward.y = 0;
        if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
        forward.Normalize();

        // Option: align with path direction for smoother behaviour
        // get closest point and next sample to estimate path direction
        float dummy;
        float sampleNormalized;
        Vector3 closest = pathGenerator.GetClosestPoint(transform.position, out dummy, out sampleNormalized);

        // compute approximate path direction by sampling a point ahead slightly along pathNormalized
        float nextSampleNorm = Mathf.Clamp01(sampleNormalized + 0.02f);
        Vector3 nextPoint = GetSamplePointAtNormalized(pathGenerator, nextSampleNorm);
        Vector3 pathDir = (nextPoint - closest);
        pathDir.y = 0;
        if (pathDir.sqrMagnitude > 0.001f) pathDir.Normalize();
        else pathDir = forward;

        // steer player slowly toward path direction
        Quaternion targetRot = Quaternion.LookRotation(pathDir, Vector3.up);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, steerResponsiveness * Time.fixedDeltaTime));

        // compute desired velocity along forward (local forward after rotation)
        Vector3 desiredVel = transform.forward * targetSpeed;
        // maintain vertical velocity (gravity)
        desiredVel.y = rb.linearVelocity.y;

        // blend velocity smoothly
        Vector3 newVel = Vector3.Lerp(rb.linearVelocity, desiredVel, accelResponsiveness * Time.fixedDeltaTime);
        rb.linearVelocity = newVel;
    }

    // Helper that uses PathGenerator's sampled points to return a point at normalized index
    private Vector3 GetSamplePointAtNormalized(PathGenerator pg, float normalized) {
        // Reflection of internal sampledPoints is not accessible, but PathGenerator exposes FindClosest etc.
        // For simplicity we approximate by querying multiple offsets from the closest known point
        // The PathGenerator doesn't expose direct lookup by normalized sample; so we do a search:
        // We ask closest point to a point located at player's projection + a forward offset.
        // Simpler: return player's forward projected point at small distance
        return transform.position + transform.forward * 1.0f;
    }
}