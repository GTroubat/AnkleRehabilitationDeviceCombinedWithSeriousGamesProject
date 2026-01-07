using UnityEngine;

public class FullSender : MonoBehaviour
{
    [SerializeField] private StockVariables stock;
    private int counter = 0;
    private void FixedUpdate() {
        if (counter == 0) {
            stock.SetHeight(150);
            stock.SetPitch(0);
        } else if (counter >= 100) {
            stock.SetStopLoop(true);
        } else {
            stock.SetPitch(Random.Range(-20, 21));
            Debug.Log("New pitch: " + stock.GetPitch());
        }
        Debug.Log("Current Send: " + stock.GetSend());
        stock.SetSend(true);
        counter++;
    }
}
