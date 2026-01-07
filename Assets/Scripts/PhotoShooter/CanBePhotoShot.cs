using UnityEngine;

public class CanBePhotoShot : MonoBehaviour
{
    [SerializeField] private StockVariables stock;
    [SerializeField] private GameObject[] otherObjectsToDestroy;
    [SerializeField] private Transform reticuleTransform;
    [SerializeField] private GameObject HighlightReticulePrefab;
    [SerializeField] private GameObject Canvas;

    [Header("Outline shader property names")]
    [SerializeField] private string OutlineSizeProperty = "_Scale";
    [SerializeField] private string OutlineColorProperty = "_Color";

    [Header("Outline materials")]
    [SerializeField] private Material OutlineYellowMaterial = null;
    [SerializeField] private Material OutlineGreenMaterial = null;

    Material[] mats;
    // previous single-material references removed in favor of arrays
    private Renderer baseRenderer;
    private Renderer overlayRenderer; // overlay that renders outlines for every submesh
    private Material[] overlayYellowMats;
    private Material[] overlayGreenMats;

    private float HighlightedOutlineSize;
    private GameObject highlightReticuleInstance;

    private bool isNear = false;
    private bool IsInReticule = false;
    private float distanceToReticule = 0f;

    private void Start() {
        baseRenderer = GetComponent<Renderer>();
        if (baseRenderer == null) {
            Debug.LogError("No Renderer found on " + gameObject.name);
            return;
        }

        mats = baseRenderer.sharedMaterials;
        if (mats == null || mats.Length == 0) {
            Debug.LogError("No materials found on " + gameObject.name);
        }

        HighlightedOutlineSize = stock.GetHighlightedOutlineSize();
        highlightReticuleInstance = Instantiate(HighlightReticulePrefab, Canvas.transform);

        // Create overlay renderer that will render the outline across all submeshes.
        CreateOutlineOverlay();
    }

    private void FixedUpdate() {
        if (isNear) {
            AddHighlight();
            AddReticule();  
            isNear = false;
            IsInReticule = false;
        } else {
            RemoveHighlight();
            RemoveReticule();
        }
    }

    private void CreateOutlineOverlay() {
        int slots = (mats != null) ? mats.Length : 0;
        if (slots == 0 || OutlineYellowMaterial == null || OutlineGreenMaterial == null) {
            
            return;
        }

        // If this object is skinned, copy bones/mesh info to a SkinnedMeshRenderer; otherwise use MeshRenderer.
        var skinned = GetComponent<SkinnedMeshRenderer>();
        GameObject overlayGO = new GameObject("OutlineOverlay");
        overlayGO.transform.SetParent(transform);
        overlayGO.transform.localPosition = Vector3.zero;
        overlayGO.transform.localRotation = Quaternion.identity;
        overlayGO.transform.localScale = Vector3.one;

        if (skinned != null) {
            var overlaySkinned = overlayGO.AddComponent<SkinnedMeshRenderer>();
            overlaySkinned.sharedMesh = skinned.sharedMesh;
            overlaySkinned.bones = skinned.bones;
            overlaySkinned.rootBone = skinned.rootBone;
            overlaySkinned.updateWhenOffscreen = skinned.updateWhenOffscreen;
            overlaySkinned.quality = skinned.quality;

            overlayYellowMats = new Material[slots];
            overlayGreenMats = new Material[slots];
            for (int i = 0; i < slots; i++) {
                // instantiate so each slot has its own material instance
                overlayYellowMats[i] = Instantiate(OutlineYellowMaterial);
                overlayGreenMats[i] = Instantiate(OutlineGreenMaterial);
            }

            // Default overlay uses yellow (disabled initially)
            overlaySkinned.materials = overlayYellowMats;
            overlaySkinned.enabled = false;
            overlayRenderer = overlaySkinned;
        } else {
            // Non-skinned mesh: copy mesh filter + mesh and use MeshRenderer.
            var mf = GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) {
                Destroy(overlayGO);
                return;
            }

            var overlayFilter = overlayGO.AddComponent<MeshFilter>();
            overlayFilter.sharedMesh = mf.sharedMesh;
            var overlayMeshRenderer = overlayGO.AddComponent<MeshRenderer>();

            overlayYellowMats = new Material[slots];
            overlayGreenMats = new Material[slots];
            for (int i = 0; i < slots; i++) {
                overlayYellowMats[i] = Instantiate(OutlineYellowMaterial);
                overlayGreenMats[i] = Instantiate(OutlineGreenMaterial);
            }

            overlayMeshRenderer.materials = overlayYellowMats;
            overlayMeshRenderer.enabled = false;
            overlayRenderer = overlayMeshRenderer;
        }

        // Make overlay ignore raycasts or collisions if necessary (optional)
        overlayGO.layer = gameObject.layer;
    }

    private void AddHighlight(){
        if (overlayRenderer != null) {
            // Choose the color set depending on whether the object is in reticule.
            Material[] useMats = IsInReticule ? overlayGreenMats : overlayYellowMats;
            if (useMats != null) {
                // Ensure overlay uses the chosen set
                overlayRenderer.materials = useMats;
                overlayRenderer.enabled = true;

                // Set the outline size on every overlay material instance
                float size = HighlightedOutlineSize;
                for (int i = 0; i < useMats.Length; i++) {
                    if (useMats[i] != null) {
                        useMats[i].SetFloat(OutlineSizeProperty, size);
                    }
                }
            }
            return;
        }

        // Fallback to older single-material approach (keeps backward compatibility if overlay couldn't be built)
        foreach (Material mat in mats) {
            if (mat == null) continue;
            if (mat.name.Contains("Green")) {
                mat.SetFloat(OutlineSizeProperty, HighlightedOutlineSize);
            } else if (mat.name.Contains("Yellow")) {
                if (IsInReticule) mat.SetFloat(OutlineSizeProperty, 1f);
                else mat.SetFloat(OutlineSizeProperty, HighlightedOutlineSize);
            }
        }
    }

    private void AddReticule() {
        if (highlightReticuleInstance != null) {
            highlightReticuleInstance.SetActive(true);
            Vector3 screenPos = Camera.main.WorldToScreenPoint(reticuleTransform.position);
            highlightReticuleInstance.transform.position = screenPos;
            float scale = 1f - distanceToReticule / 30f;
            highlightReticuleInstance.transform.localScale = Vector3.one * scale;
        }
    }
    private void RemoveReticule() {
        if (highlightReticuleInstance != null) {
            highlightReticuleInstance.SetActive(false);
        }
    }
    private void RemoveHighlight() {
        if (overlayRenderer != null) {
            overlayRenderer.enabled = false;
            return;
        }

        // fallback
        foreach (Material mat in mats) {
            if (mat == null) continue;
            mat.SetFloat(OutlineSizeProperty, 1f);
        }
    }

    public void SetIsNear() {
        isNear = true;
    }

    public void SetIsInReticule() {
        IsInReticule = true;
    }

    public void SetDistance(float distance) {
        distanceToReticule = distance;
    }

    public Transform GetReticuleTransform() {
        return reticuleTransform;
    }

    public void DestroySelf() {
        stock.SetPoints(stock.GetPoints() + 1);
        gameObject.SetActive(false);
        foreach (GameObject obj in otherObjectsToDestroy) {
            if (obj != null) {
                obj.SetActive(false);
            }
        }
        highlightReticuleInstance.SetActive(false);
        //Destroy(this.gameObject);
    }
}
