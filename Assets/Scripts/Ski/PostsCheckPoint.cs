using UnityEngine;

public class PostsCheckPoint : MonoBehaviour
{
    [SerializeField] private StockVariables stock;
    [SerializeField] private Transform leftPole;
    [SerializeField] private Transform rightPole;

    private Vector3 Center;
    private float HalfWidth;

    private BoxCollider boxCollider;

    private float size = 5f;

    private bool triggered = false;

    private void Start() {
        Center = (leftPole.position + rightPole.position) * 0.5f;
        HalfWidth = Vector3.Distance(leftPole.position, rightPole.position) * 0.5f;

        boxCollider = GetComponent<BoxCollider>();
    }
   
    private void OnDrawGizmosSelected() {
        if (leftPole == null || rightPole == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(Center, 0.1f);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(leftPole.position, rightPole.position);
    }

    private void FixedUpdate() {
        leftPole.localPosition = new Vector3(size, 0f, 0f);
        rightPole.localPosition = new Vector3(-size, 0f, 0f);
    }

    public Vector3 GetCenter() {
        //return Center;
        return gameObject.transform.position;
    }
    public float GetHalfWidth() {
        return HalfWidth;
    }

    public float GetSize() {
        return size;
    }
    public void SetSize(float newSize) {
        size = newSize;
        var colliderSize = boxCollider.size;
        colliderSize.x = 1.7f * newSize;
        boxCollider.size = colliderSize;
    }

    public bool HasBeenTriggered() {
        return triggered;
    }

    public void OnTriggerEnter(Collider other) {
        stock.SetPoints(stock.GetPoints() + 1);
        triggered = true;
    }
}
