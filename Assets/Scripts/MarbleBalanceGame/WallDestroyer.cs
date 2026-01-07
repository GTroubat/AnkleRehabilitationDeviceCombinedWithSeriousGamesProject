using UnityEngine;
using UnityEngine.InputSystem.XR;

public class WallDestroyer : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision) {
        if (collision.TryGetComponent<Marble>(out Marble marble))
        {
            marble.DestroySelf();
        }
    }
}
