using UnityEngine;

public class ControlPlateformByAI : MonoBehaviour
{
    [SerializeField] private StockVariables stock;
    [SerializeField] private PlateformMovementByAI AI;

    private float conversion = 0.75f;
    private void FixedUpdate() {
        if (stock.GetStartConfig()) {
            stock.SetPitch(AI.GetRotation() * conversion);
        } else {
            stock.SetHeight(150);
        }
        stock.SetSend(true);
    }
}
