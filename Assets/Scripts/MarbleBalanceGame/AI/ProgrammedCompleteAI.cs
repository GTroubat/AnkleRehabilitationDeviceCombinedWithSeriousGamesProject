using UnityEngine;

public class ProgrammedCompleteAI: MonoBehaviour {
    [SerializeField] private StockVariables stock;
    [SerializeField] private MarbleSpawnWithDisplayer marbleSpawner;
    [SerializeField] private int tubeNumber = 3;
    [SerializeField] private float speed = 1f;

    private readonly float heightChangePlatform = 1.6f;

    private float targetAngle;
    private float currentAngle;
    private float error;
    private float newAngle;

    private Marble currentMarble = null;

    private enum Side {
        Left,
        Right,
        Unknown
    }
    private Side side = Side.Unknown;

    private void FixedUpdate() {
        if (stock.GetIsGamePlaying() && (stock.GetGameMode() == EGameMode.Passive)) {
            currentMarble = marbleSpawner.GetCurrentMarble();
            if (currentMarble != null) {
                AdjustBoardToMarblePosition(currentMarble);
            }
        }
    }

    private void AdjustBoardToMarblePosition(Marble currentMarble) {
        Vector3 marblePos = currentMarble.transform.position;
        EColors marbleColor = currentMarble.GetColor();
        switch (marbleColor) {
            case EColors.Red:
                if (tubeNumber == 3) {
                    if (marblePos.y > heightChangePlatform) {
                        GoFullLeft();
                    } else {
                        GoFullLeft();
                    }
                }
                break;
            case EColors.Yellow:
                if (tubeNumber == 3) {
                    if (marblePos.y > heightChangePlatform) {
                        GoFullRight();
                    } else {
                        GoFullRight();
                    }
                }
                break;
            case EColors.Green:
                // nothing right now
                break;
            case EColors.Blue:
                if (tubeNumber == 3) {
                    // use signed angle of current rotation to decide side
                    float signedCurrentAngle = ToSignedAngle(gameObject.transform.rotation.eulerAngles.z);
                    if (side == Side.Unknown) {
                        side = (signedCurrentAngle > 0f) ? Side.Left : Side.Right;
                    }
                    if (side == Side.Left) {
                        if (marblePos.y > heightChangePlatform) GoMidLeft();
                        else GoFullRight();
                    } else if (side == Side.Right) {
                        if (marblePos.y > heightChangePlatform) GoMidRight();
                        else GoFullLeft();
                    }
                }
                break;
        }

        // Clamp pitch in stock (signed) domain, then apply conversion to Unity angle when setting transform
        int[] minMax = stock.GetMinMaxPitch();
        float clampedPitch = Mathf.Clamp(stock.GetPitch(), (float)minMax[0], (float)minMax[1]);
        stock.SetPitch(clampedPitch);

        float unityAngle = ToUnityAngle(clampedPitch);
        gameObject.transform.rotation = Quaternion.Euler(0, 0, unityAngle);
        stock.SetSend(true);
    }

    private void GoFullRight() {
        //Debug.Log("Go Full Right");
        targetAngle = stock.GetMinMaxPitch()[0]; // signed target (-..+)
        //Debug.Log("Target Angle: " + targetAngle);

        // current angle read from Transform (0..360), convert to signed (-180..180) for calculation
        currentAngle = ToSignedAngle(gameObject.transform.rotation.eulerAngles.z);
        //Debug.Log("Current Angle (signed): " + currentAngle);

        // shortest signed error from current to target
        error = Mathf.DeltaAngle(currentAngle, targetAngle);
        //Debug.Log("Error: " + error);

        newAngle = currentAngle + error * speed;
        // store new consigne (signed) in stock and apply to transform as Unity angle
        newAngle = Mathf.Clamp(newAngle, stock.GetMinMaxPitch()[0], stock.GetMinMaxPitch()[1]);
        stock.SetPitch(newAngle);
        float unityAngle = ToUnityAngle(newAngle);
        gameObject.transform.rotation = Quaternion.Euler(0, 0, unityAngle);
        //Debug.Log("New Angle (signed): " + newAngle + " => Unity: " + unityAngle);
        stock.SetSend(true);
    }

    private void GoFullLeft() {
        targetAngle = stock.GetMinMaxPitch()[1];

        currentAngle = ToSignedAngle(gameObject.transform.rotation.eulerAngles.z);

        error = Mathf.DeltaAngle(currentAngle, targetAngle);

        newAngle = currentAngle + error * speed;
        newAngle = Mathf.Clamp(newAngle, stock.GetMinMaxPitch()[0], stock.GetMinMaxPitch()[1]);
        stock.SetPitch(newAngle);
        float unityAngle = ToUnityAngle(newAngle);
        gameObject.transform.rotation = Quaternion.Euler(0, 0, unityAngle);
        stock.SetSend(true);
    }

    private void GoMidRight() {
        targetAngle = stock.GetMinMaxPitch()[0] / 3f;

        currentAngle = ToSignedAngle(gameObject.transform.rotation.eulerAngles.z);

        error = Mathf.DeltaAngle(currentAngle, targetAngle);

        newAngle = currentAngle + error * speed;
        newAngle = Mathf.Clamp(newAngle, stock.GetMinMaxPitch()[0], stock.GetMinMaxPitch()[1]);
        stock.SetPitch(newAngle);
        float unityAngle = ToUnityAngle(newAngle);
        gameObject.transform.rotation = Quaternion.Euler(0, 0, unityAngle);
        stock.SetSend(true);
    }

    private void GoMidLeft() {
        targetAngle = stock.GetMinMaxPitch()[1] / 3f;

        currentAngle = ToSignedAngle(gameObject.transform.rotation.eulerAngles.z);

        error = Mathf.DeltaAngle(currentAngle, targetAngle);

        newAngle = currentAngle + error * speed;
        newAngle = Mathf.Clamp(newAngle, stock.GetMinMaxPitch()[0], stock.GetMinMaxPitch()[1]);
        stock.SetPitch(newAngle);
        float unityAngle = ToUnityAngle(newAngle);
        gameObject.transform.rotation = Quaternion.Euler(0, 0, unityAngle);
        stock.SetSend(true);
    }

    // Convert Unity 0..360 angle to signed -180..180
    private float ToSignedAngle(float unityAngle) {
        return (unityAngle > 180f) ? unityAngle - 360f : unityAngle;
    }

    // Convert signed -180..180 angle to Unity 0..360
    private float ToUnityAngle(float signedAngle) {
        return (signedAngle < 0f) ? signedAngle + 360f : signedAngle;
    }

    public void ResetSide() {
        side = Side.Unknown;
    }
}