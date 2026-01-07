using UnityEngine;

public class PlayerMovementPhoto : MonoBehaviour
{
    [SerializeField] private StockVariables stock;

    private void FixedUpdate() {
        if (gameObject.transform.position.x < 1280) {
            float moveSpeed = stock.GetMoveSpeedPhoto();
            if (stock.GetIsGamePlaying())
                gameObject.transform.Translate(new Vector3(0, 0, moveSpeed * Time.fixedDeltaTime));
        }
    }
}

