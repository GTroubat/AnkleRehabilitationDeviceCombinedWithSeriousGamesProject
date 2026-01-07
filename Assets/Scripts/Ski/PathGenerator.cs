using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Génère une spline Catmull-Rom passant par les centres des checkpoints,
/// fournit des échantillons (sampledPoints) et des largeurs (sampledWidths),
/// dessine deux LineRenderers : un fil central (0.2f) et un large (6f),
/// et expose des méthodes utilitaires pour la distance / point le plus proche.
/// </summary>
[ExecuteAlways]
public class PathGenerator: MonoBehaviour {
    [Tooltip("Ordered list of checkpoints (in travelling order).")]
    public List<PostsCheckPoint> checkpoints = new List<PostsCheckPoint>();

    [Header("Line Renderers")]
    public LineRenderer centerLineRenderer; // LineRenderer pour la ligne centrale (ex: 0.2f)
    public LineRenderer fullWidthLineRenderer; // LineRenderer pour afficher la largeur (ex: 6f)

    [Header("Sampling")]
    [Range(2, 50)]
    public int samplesPerSegment = 12;

    [Header("Line widths")]
    public float centerLineWidth = 0.2f; // largeur du LineRenderer central
    public float fullLineWidth = 6f;     // largeur du LineRenderer large (toute la largeur des posts)

    // échantillons internes
    private List<Vector3> sampledPoints = new List<Vector3>();
    private List<float> sampledWidths = new List<float>(); // demi-largeurs interpolées (halfWidths)

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
    /// (Re)génère la spline et met à jour les LineRenderers.
    /// </summary>
    public void GeneratePath() {
        sampledPoints.Clear();
        sampledWidths.Clear();

        if (checkpoints == null || checkpoints.Count == 0) {
            // rien à faire
            UpdateLineRenderers();
            return;
        }

        // Construire la liste des centres et demi-largeurs à partir des checkpoints
        List<Vector3> centers = new List<Vector3>();
        List<float> halfWidths = new List<float>();
        foreach (var cp in checkpoints) {
            if (cp == null) continue;
            centers.Add(cp.GetCenter());           // ajouter le centre du checkpoint à la liste (centre = moyenne des 2 bâtons)  <-- commentaire en français demandé
            halfWidths.Add(cp.GetHalfWidth());    // ajouter la demi-largeur (distance entre les bâtons / 2)                <-- commentaire en français demandé
        }

        // Si pas de centres valides
        if (centers.Count == 0) {
            UpdateLineRenderers();
            return;
        }

        // Pad des points pour Catmull-Rom (dupliquer les extrémités pour définir tangentes aux bords)
        List<Vector3> p = new List<Vector3>();
        p.Add(centers[0]);                                 // dupliquer premier point en tête                       <-- commentaire en français demandé
        p.AddRange(centers);                               // ajouter tous les centres                              <-- commentaire en français demandé
        p.Add(centers[centers.Count - 1]);                 // dupliquer dernier point en queue                      <-- commentaire en français demandé

        // Pad des largeurs de la même façon
        List<float> w = new List<float>();
        w.Add(halfWidths[0]);                              // demi-largeur du premier (dupliquée)                   <-- commentaire en français demandé
        w.AddRange(halfWidths);                            // demi-largeurs intermédiaires                          <-- commentaire en français demandé
        w.Add(halfWidths[halfWidths.Count - 1]);           // demi-largeur du dernier (dupliquée)                   <-- commentaire en français demandé

        // Échantillonnage Catmull-Rom segment par segment
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
                float width = Mathf.Lerp(w1, w2, t);

                sampledPoints.Add(pos);
                sampledWidths.Add(width);
            }
        }

        // ajouter explicitement le dernier point (pour fermer la série d'échantillons proprement)
        sampledPoints.Add(p[p.Count - 2]);
        sampledWidths.Add(w[w.Count - 2]);

        // Mettre à jour les LineRenderers
        UpdateLineRenderers();
    }

    // interpolation Catmull-Rom standard (tension 0.5)
    private Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t) {
        float t2 = t * t;
        float t3 = t2 * t;
        return 0.5f * ((2f * p1) +
                       (-p0 + p2) * t +
                       (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                       (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
    }

    // Met à jour les deux LineRenderers (positions + largeurs)
    private void UpdateLineRenderers() {
        // Ligne centrale
        if (centerLineRenderer != null) {
            centerLineRenderer.positionCount = sampledPoints.Count;
            centerLineRenderer.SetPositions(sampledPoints.ToArray());
#if UNITY_2019_1_OR_NEWER
            centerLineRenderer.startWidth = centerLineWidth;
            centerLineRenderer.endWidth = centerLineWidth;
#else
            centerLineRenderer.SetWidth(centerLineWidth, centerLineWidth);
#endif
        }

        // Ligne "large" (utilise la même série de positions, mais plus large)
        if (fullWidthLineRenderer != null) {
            fullWidthLineRenderer.positionCount = sampledPoints.Count;
            fullWidthLineRenderer.SetPositions(sampledPoints.ToArray());
#if UNITY_2019_1_OR_NEWER
            fullWidthLineRenderer.startWidth = fullLineWidth;
            fullWidthLineRenderer.endWidth = fullLineWidth;
#else
            fullWidthLineRenderer.SetWidth(fullLineWidth, fullLineWidth);
#endif
        }
    }

    /// <summary>
    /// Retourne le point le plus proche sur la poly-ligne échantillonnée (projection sur chaque segment),
    /// la distance (distance euclidienne 3D) et l'index normalisé (0..1 le long des échantillons).
    /// </summary>
    public Vector3 GetClosestPoint(Vector3 position, out float distance, out float sampleIndexNormalized) {
        distance = float.MaxValue;
        sampleIndexNormalized = 0f;

        if (sampledPoints == null || sampledPoints.Count == 0) {
            return position;
        }

        if (sampledPoints.Count == 1) {
            distance = Vector3.Distance(position, sampledPoints[0]);
            sampleIndexNormalized = 0f;
            return sampledPoints[0];
        }

        float bestSqr = float.MaxValue;
        Vector3 bestPoint = sampledPoints[0];
        int bestSegmentIndex = 0;
        float bestSegFrac = 0f;

        for (int i = 0; i < sampledPoints.Count - 1; i++) {
            Vector3 a = sampledPoints[i];
            Vector3 b = sampledPoints[i + 1];
            Vector3 ab = b - a;
            Vector3 ap = position - a;

            float abLen2 = ab.sqrMagnitude;
            if (abLen2 <= Mathf.Epsilon) {
                float sqr = ap.sqrMagnitude;
                if (sqr < bestSqr) {
                    bestSqr = sqr;
                    bestPoint = a;
                    bestSegmentIndex = i;
                    bestSegFrac = 0f;
                }
                continue;
            }

            float t = Vector3.Dot(ap, ab) / abLen2;
            t = Mathf.Clamp01(t);
            Vector3 proj = a + ab * t;
            float distSqr = (position - proj).sqrMagnitude;
            if (distSqr < bestSqr) {
                bestSqr = distSqr;
                bestPoint = proj;
                bestSegmentIndex = i;
                bestSegFrac = t;
            }
        }

        distance = Mathf.Sqrt(bestSqr);
        sampleIndexNormalized = (sampledPoints.Count > 1)
            ? (bestSegmentIndex + bestSegFrac) / (sampledPoints.Count - 1)
            : 0f;

        return bestPoint;
    }

    /// <summary>
    /// Retourne la distance au chemin en projetant le point le plus proche à la même hauteur (y) que 'position'.
    /// Renvoie aussi (via out) le point le plus proche mais avec la coordonnée y égale à position.y.
    /// (Version compatible plus pratique pour les contrôleurs de joueur qui veulent travailler en hauteur constante.)
    /// </summary>
    public float GetDistanceToPath(Vector3 position, out Vector3 closestPointAtPlayerY) {
        float dummy;
        float tNorm;
        Vector3 closest = GetClosestPoint(position, out dummy, out tNorm);

        // Ajuster la hauteur du point trouvé pour qu'il ait la même y que le joueur (position.y)
        closest.y = position.y;

        closestPointAtPlayerY = closest;
        return Vector3.Distance(position, closest);
    }

    /// <summary>
    /// Compatibilité : ancienne signature qui ne renvoie que la distance.
    /// </summary>
    public float GetDistanceToPath(Vector3 position) {
        Vector3 tmp;
        return GetDistanceToPath(position, out tmp);
    }

    /// <summary>
    /// Retourne un point le long des échantillons selon un paramètre normalisé t dans [0,1].
    /// Utile pour estimer la tangente (p(t+eps) - p(t)).
    /// </summary>
    public Vector3 GetPointAtNormalized(float t) {
        if (sampledPoints == null || sampledPoints.Count == 0)
            return transform.position;

        t = Mathf.Clamp01(t);
        if (sampledPoints.Count == 1) return sampledPoints[0];

        float fIndex = t * (sampledPoints.Count - 1);
        int i0 = Mathf.FloorToInt(fIndex);
        int i1 = Mathf.Min(i0 + 1, sampledPoints.Count - 1);
        float frac = fIndex - i0;
        return Vector3.Lerp(sampledPoints[i0], sampledPoints[i1], frac);
    }

    // --- Accesseurs de lecture pour debug / autres scripts ---
    public IReadOnlyList<Vector3> SampledPoints => sampledPoints;
    public IReadOnlyList<float> SampledWidths => sampledWidths;
}
