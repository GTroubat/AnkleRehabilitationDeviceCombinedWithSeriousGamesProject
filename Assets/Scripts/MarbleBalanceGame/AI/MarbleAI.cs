using UnityEngine;

public class MarbleAI: MonoBehaviour {
    [SerializeField] private EColors marbleColor;

    public EColors GetColor() { return marbleColor; }

    public void SetColor(EColors color) { marbleColor = color; }
}
