using UnityEngine;

public class Tube : MonoBehaviour {
    [SerializeField] private EColors tubeColor;
    [SerializeField] private StockVariables stock;
    [SerializeField] private MarbleSpawnWithDisplayer marbleSpawner;

    public EColors GetColor() { return tubeColor; }

    public void OnTriggerEnter2D(Collider2D collision) {
        if (collision.TryGetComponent<Marble>(out Marble marble) && stock.GetGameMode() == EGameMode.Active) {
            if (marble.GetColor() == tubeColor) {
                Debug.Log("Correct tube!");
                stock.SetPoints(stock.GetPoints() + 1);
            } else {
                Debug.Log("Wrong tube!");
            }
            stock.SetMarbleCounter(stock.GetMarbleCounter() + 1);
            if (stock.GetIsGamePlaying()) {
                marbleSpawner.SetSpawn(true);
            }
        }
        //marble.DestroySelf();
    }
}
