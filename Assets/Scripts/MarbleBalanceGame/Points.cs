using TMPro;
using UnityEngine;

public class Points: MonoBehaviour {
    [SerializeField] private StockVariables stock;
    [SerializeField] private TMP_Text pointsText;

    private void FixedUpdate() {
        //pointsText.text = "Points:\n" + stock.GetPoints().ToString() + "/" + stock.GetMarbleCounter().ToString();
        pointsText.text = "Points:\n" + stock.GetPoints().ToString();
    }
}
