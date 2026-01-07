using UnityEngine;
using System;

/// <summary>
/// A C# implementation of a mathematical model inspired by
/// "Performance analysis of a cable-driven ankle assisting device".
/// 
/// Features:
/// - Kinematic computation of cable vectors and lengths between
///   shank platform attachment points S_i and foot platform F_i.
/// - Accounts for ankle pose (three Euler angles) and a vertical
///   translation "height" (distance between platform centers).
/// - Computes cable unit vectors and builds the equilibrium matrices
///   following the paper's notation:
///     U^T * T - F_A = F_w
///     A^T * T - M_A = M_w
///   Then solves for cable tensions T in a least-squares sense using
///   a stacked equilibrium matrix (works even when the system is
///   under/over-determined). Reaction moment M_A is set to zero for a
///   spherical ankle joint (as in the paper).
/// 
/// Notes:
/// - This script is designed to be self-contained and easy to adapt.
/// - It uses a small Gaussian elimination solver for 4x4 systems (normal
///   equations) to compute the least-squares tension vector.
/// - Integrates a "height" parameter similar to the user's program.
///
/// How to use:
/// - Set the attachment coordinates for the shank (S_i) and the foot (F_i)
///   in local coordinates in inspector or code.
/// - Call UpdateKinematics(...) each FixedUpdate with the current pose
///   (roll, pitch, yaw in degrees and height in mm).
/// - Use ComputeTensions(...) to compute cable tensions given external
///   force F_w (e.g. gravity on the foot+platform) and external moment M_w.
/// </summary>
public class MathematicalModel4: MonoBehaviour {
    [Header("Platform attachment points (mm)")]
    // Upper platform (shank) attachment points S1..S4 in shank frame
    // Order: i=0..3
    [SerializeField]
    private Vector3[] S = new Vector3[4] {
        new Vector3(-35f,  60f,  80f),
        new Vector3(245f,  60f,  80f),
        new Vector3(245f, -60f,  80f),
        new Vector3(-35f, -60f,  80f)
    };

    // Lower platform (foot) attachment points F1..F4 in foot frame
    [SerializeField]
    private Vector3[] F = new Vector3[4] {
        new Vector3(-28f,  150f, -285f),
        new Vector3(195f,  150f, -285f),
        new Vector3(195f, -150f, -285f),
        new Vector3(-28f, -150f, -285f)
    };

    [Header("Model state (degrees, mm)")]
    [SerializeField] private float roll = 0f;   // rotation around x (deg)
    [SerializeField] private float pitch = 0f;  // rotation around y (deg)
    [SerializeField] private float yaw = 0f;    // rotation around z (deg)
    [SerializeField] private float height = 150f; // vertical translation of foot platform relative to shank (mm)

    // Computed values
    private Vector3[] worldS = new Vector3[4];     // shank platform S_i in world/shank ref (after pivot, rotation)
    private Vector3[] worldF = new Vector3[4];     // foot platform F_i in world ref (including height)
    private Vector3[] cableVectors = new Vector3[4];
    private float[] cableLengths = new float[4];
    private Vector3 pivot = Vector3.zero; // center of ankle joint (assume origin)

    // Convenience constants
    private const int N = 4;

    void Start() {
        // initial kinematics calculation
        UpdateKinematics(roll, pitch, yaw, height);
    }

    void FixedUpdate() {
        // If you have an external source for roll/pitch/yaw/height, update before computing tensions
        UpdateKinematics(roll, pitch, yaw, height);

        // Example: compute tensions that balance foot weight along -Z (gravity)
        // Suppose foot + platform mass -> weight W (N) acting at origin O downward.
        // Here we express forces in N. Convert mm to meters if using SI; keep consistent.
        Vector3 Fw = new Vector3(0f, 0f, -9.81f * 1.2f); // example weight (1.2 kg) => N along -Z
        Vector3 Mw = Vector3.zero; // assume no external moment (spherical joint)

        float[] tensions = ComputeTensions(Fw, Mw);

        // Debug: print lengths and tensions
        for (int i = 0; i < N; i++) {
            Debug.Log($"Cable {i + 1}: length={cableLengths[i]:F1} mm, tension={tensions[i]:F3} N");
        }
    }

    /// <summary>
    /// Update kinematic quantities (platform placement) from pose and height.
    /// roll/pitch/yaw in degrees, height in mm (vertical offset of foot platform).
    /// After this call cableVectors and cableLengths are set.
    /// </summary>
    public void UpdateKinematics(float rollDeg, float pitchDeg, float yawDeg, float heightMm) {
        roll = rollDeg;
        pitch = pitchDeg;
        yaw = yawDeg;
        height = heightMm;

        // Convert Euler angles to rotation matrix (shank->foot orientation).
        float phi = roll * Mathf.Deg2Rad;
        float theta = -pitch * Mathf.Deg2Rad; // note: sign convention copied from user's program
        float psi = yaw * Mathf.Deg2Rad;

        float[,] R = EulerRotationMatrix(phi, theta, psi); // 3x3

        // worldS = S rotated around pivot (if S given in shank frame)
        for (int i = 0; i < N; i++) {
            Vector3 sLocal = S[i];
            Vector3 sRot = MulMatVec(R, sLocal);
            worldS[i] = pivot + sRot;
        }

        // worldF = rotate foot local points by current orientation and translate down/up by height
        // Here we assume F points are given in foot frame and foot center located at (0,0,height) relative to shank
        for (int i = 0; i < N; i++) {
            Vector3 fLocal = F[i];
            Vector3 fRot = MulMatVec(R, fLocal); // rotate F by same orientation (foot orientation relative to shank)
            // translate foot platform along global Z by "height"
            worldF[i] = pivot + fRot + new Vector3(0f, 0f, height);
        }

        // cable vectors from shank S_i to foot F_i: l_i_vec = F_i - S_i
        for (int i = 0; i < N; i++) {
            cableVectors[i] = worldF[i] - worldS[i];
            cableLengths[i] = cableVectors[i].magnitude;
        }
    }

    /// <summary>
    /// Compute cable unit vectors u_i (from shank to foot) and build equilibrium matrices:
    /// The stacked matrix K (6x4) = [U^T; A^T] where:
    /// - U^T is 3x4 with columns u_i
    /// - A^T is 3x4 with columns (f_i x u_i) where f_i is vector from center O to foot attachment (worldF)
    /// Then we solve for tensions T in the least-squares sense for:
    ///   K * T = b = [F_w + F_A; M_w + M_A]
    /// For spherical joint M_A = 0 and we keep F_A (reaction force) unknown; however the paper arranges
    /// the full equilibrium including F_A. To avoid augmenting with unknown F_A, we treat F_A as zero
    /// (or external known assistive force) and solve K*T = [F_w; M_w].
    /// This returns a tension vector of length 4 (N).
    /// </summary>
    public float[] ComputeTensions(Vector3 F_w, Vector3 M_w, Vector3? optional_FA = null) {
        Vector3 F_A = optional_FA ?? Vector3.zero; // if known reaction/assistive force use it, else zero
        Vector3 M_A = Vector3.zero; // spherical ankle -> reaction moment null (paper assumption)

        // Build U^T (3x4) and A^T (3x4)
        float[,] UT = new float[3, 4];
        float[,] AT = new float[3, 4];

        for (int i = 0; i < N; i++) {
            Vector3 u = (cableLengths[i] > 1e-6f) ? (cableVectors[i] / cableLengths[i]) : Vector3.up;
            // UT columns are u_i
            UT[0, i] = u.x;
            UT[1, i] = u.y;
            UT[2, i] = u.z;

            // lever arm vector f_i: vector from center O to foot attachment (worldF)
            Vector3 f_i = worldF[i] - pivot;
            Vector3 cross = Vector3.Cross(f_i, u);

            AT[0, i] = cross.x;
            AT[1, i] = cross.y;
            AT[2, i] = cross.z;
        }

        // Stack to K (6x4): rows 0-2 = UT rows; rows 3-5 = AT rows
        float[,] K = new float[6, 4];
        for (int r = 0; r < 3; r++) {
            for (int c = 0; c < 4; c++) {
                K[r, c] = UT[r, c];
                K[r + 3, c] = AT[r, c];
            }
        }

        // RHS b is [F_w + F_A; M_w + M_A]
        float[] b = new float[6];
        Vector3 top = F_w + F_A;
        Vector3 bot = M_w + M_A;
        b[0] = top.x; b[1] = top.y; b[2] = top.z;
        b[3] = bot.x; b[4] = bot.y; b[5] = bot.z;

        // Solve least-squares for T: min ||K*T - b||.
        // Use normal equations: (K^T K) T = K^T b  -> 4x4 system
        float[,] KtK = new float[4, 4];
        float[] Ktb = new float[4];

        for (int i = 0; i < 4; i++) {
            for (int j = 0; j < 4; j++) {
                float sum = 0f;
                for (int r = 0; r < 6; r++) {
                    sum += K[r, i] * K[r, j];
                }
                KtK[i, j] = sum;
            }

            float sum2 = 0f;
            for (int r = 0; r < 6; r++) {
                sum2 += K[r, i] * b[r];
            }
            Ktb[i] = sum2;
        }

        // Solve 4x4 linear system KtK * T = Ktb
        float[] T = GaussianSolve4x4(KtK, Ktb);

        // Ensure non-negative tensions (cables cannot push), clamp to small positive min
        for (int i = 0; i < 4; i++) {
            if (T[i] < 0f) T[i] = 0f;
        }

        return T;
    }

    #region Linear algebra helpers

    // Multiply 3x3 matrix (as float[3,3]) by Vector3
    private Vector3 MulMatVec(float[,] M, Vector3 v) {
        return new Vector3(
            M[0, 0] * v.x + M[0, 1] * v.y + M[0, 2] * v.z,
            M[1, 0] * v.x + M[1, 1] * v.y + M[1, 2] * v.z,
            M[2, 0] * v.x + M[2, 1] * v.y + M[2, 2] * v.z
        );
    }

    // Euler rotation matrix from (phi, theta, psi) same construction as original code
    private float[,] EulerRotationMatrix(float phi, float theta, float psi) {
        float[,] r = new float[3, 3];
        r[0, 0] = Mathf.Cos(psi) * Mathf.Cos(theta);
        r[0, 1] = Mathf.Cos(psi) * Mathf.Sin(theta) * Mathf.Sin(phi) - Mathf.Sin(psi) * Mathf.Cos(phi);
        r[0, 2] = Mathf.Cos(psi) * Mathf.Sin(theta) * Mathf.Cos(phi) + Mathf.Sin(psi) * Mathf.Sin(phi);

        r[1, 0] = Mathf.Sin(psi) * Mathf.Cos(theta);
        r[1, 1] = Mathf.Sin(psi) * Mathf.Sin(theta) * Mathf.Sin(phi) + Mathf.Cos(psi) * Mathf.Cos(phi);
        r[1, 2] = Mathf.Sin(psi) * Mathf.Sin(theta) * Mathf.Cos(phi) - Mathf.Cos(psi) * Mathf.Sin(phi);

        r[2, 0] = -Mathf.Sin(theta);
        r[2, 1] = Mathf.Cos(theta) * Mathf.Sin(phi);
        r[2, 2] = Mathf.Cos(theta) * Mathf.Cos(phi);

        return r;
    }

    // Solve 4x4 linear system A x = b using Gaussian elimination with partial pivoting.
    // Returns solution vector x; if singular, returns zeros.
    private float[] GaussianSolve4x4(float[,] Aorig, float[] borig) {
        // Copy to avoid mutating inputs
        float[,] A = new float[4, 4];
        float[] b = new float[4];
        for (int i = 0; i < 4; i++) {
            b[i] = borig[i];
            for (int j = 0; j < 4; j++) A[i, j] = Aorig[i, j];
        }

        int n = 4;
        for (int k = 0; k < n; k++) {
            // Partial pivot
            int piv = k;
            float max = Mathf.Abs(A[k, k]);
            for (int i = k + 1; i < n; i++) {
                float val = Mathf.Abs(A[i, k]);
                if (val > max) { max = val; piv = i; }
            }
            if (max < 1e-12f) {
                Debug.LogWarning("Singular or ill-conditioned matrix in Gaussian solver.");
                return new float[4];
            }
            if (piv != k) {
                // swap rows k and piv
                for (int j = 0; j < n; j++) {
                    float tmp = A[k, j]; A[k, j] = A[piv, j]; A[piv, j] = tmp;
                }
                float tmpb = b[k]; b[k] = b[piv]; b[piv] = tmpb;
            }

            // Elimination
            for (int i = k + 1; i < n; i++) {
                float factor = A[i, k] / A[k, k];
                b[i] -= factor * b[k];
                for (int j = k; j < n; j++) {
                    A[i, j] -= factor * A[k, j];
                }
            }
        }

        // Back substitution
        float[] x = new float[n];
        for (int i = n - 1; i >= 0; i--) {
            float s = b[i];
            for (int j = i + 1; j < n; j++) s -= A[i, j] * x[j];
            x[i] = s / A[i, i];
        }
        return x;
    }

    #endregion

    #region Accessors (for debug / external use)

    public float[] GetCableLengths() => cableLengths;

    public Vector3[] GetCableVectors() => cableVectors;

    public Vector3[] GetShankAttachmentWorld() => worldS;

    public Vector3[] GetFootAttachmentWorld() => worldF;

    #endregion
}