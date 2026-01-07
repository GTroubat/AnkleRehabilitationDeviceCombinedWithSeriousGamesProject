using UnityEngine;

public class Marble: MonoBehaviour {
    [SerializeField] private EColors marbleColor;

    public EColors GetColor() { return marbleColor; }

    public void SetColor(EColors color) { marbleColor = color; }

    public void DestroySelf() {
        Destroy(gameObject);
    }
}
