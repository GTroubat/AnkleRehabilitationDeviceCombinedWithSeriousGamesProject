using UnityEngine;

public class PassiveFollowLine1 : MonoBehaviour {
    [SerializeField] private StockVariables stock;
    [SerializeField] private PathGeneratorOld path;

    [SerializeField] private float MaxSpeed = 0.02f;
    [SerializeField] private float platformSpeed = 0.1f;

    private float lateralOffset = 0f;
    private float command = 0f;

    private void FixedUpdate(){
        if (path == null) return;

        if (stock.GetIsGamePlaying()) {
            lateralOffset = path.GetVerticalDistanceToPath(transform.position);
            if (Mathf.Abs(lateralOffset) > 100) return;
            command = -lateralOffset * MaxSpeed;
            Debug.Log("Lateral Offset: " + lateralOffset + " Command: " + command);
            transform.position += new Vector3(command, 0f, 0f);

            MovePlatform();
        }
    }

    private void MovePlatform() {
        float platformRoll = stock.GetRoll() + command * platformSpeed;
        platformRoll = Mathf.Clamp(platformRoll, stock.GetMinMaxRoll()[0], stock.GetMinMaxRoll()[1]);
        stock.SetRoll(platformRoll);
    }
}
