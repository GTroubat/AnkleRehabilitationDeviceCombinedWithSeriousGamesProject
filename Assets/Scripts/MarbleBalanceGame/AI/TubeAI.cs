using UnityEngine;

public class TubeAI : MonoBehaviour {
    [SerializeField] private EColors tubeColor;
    [SerializeField] private StockVariables stock;

    public EColors GetColor() { return tubeColor; }

    public void OnTriggerEnter2D(Collider2D collision) {
        if (collision.TryGetComponent<MarbleAI>(out MarbleAI marble)) {
            if (marble.GetColor() == tubeColor) {
                Debug.Log("Correct tube!");
                stock.SetPoints(stock.GetPoints() + 1);
            } else {
                Debug.Log("Wrong tube!");
            }
            stock.SetMarbleCounter(stock.GetMarbleCounter() + 1);
        }
    }
}
