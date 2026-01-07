using System.Drawing.Text;
using UnityEngine;

public class AI_AdaptativeControlMode: MonoBehaviour {
    [SerializeField] private StockVariables stock;
    [SerializeField] private AdaptiveRewardAgent ai;
    [SerializeField] private MarbleSpawnWithDisplayer marbleSpawner;
    [SerializeField] private Platform[] platforms;
    [SerializeField] private int tubeNumber = 3;
    [SerializeField] private float passiveSpeed = 0.01f;
    [SerializeField] private float activeSpeed = 0.015f;
    [SerializeField] private float assistanceSpeed = 0.01f;

    //Passive mode variables
    private readonly float heightChangePlatform = 1.6f;

    private float targetAngle;
    private float currentAngle;
    private float error;
    private float newAngle;

    private Marble currentMarble = null;

    float signedCurrentAngle;

    //Active mode variables
    private float gamePitchAngle = 0f;
    private float platformPitchAngle = 0f;
    private float forceAngle = 0f;
    private float[] rapport = { 20000, 4000, 4000, 20000 };
    private float[] rapportWithSensitivity;

    private float[] gameMinMaxRange = { -20f, 20f };
    private float platformAngleRange;

    [SerializeField] private int nbValues = 10;
    private double[,] Values;
    private double[] currentMean;
    private int counter;

    // Assisting mode variables
    private int nbConsecutiveErrors = 0;
    private int previousPoints = 0;
    private int previousMarbleCount = 0;

    private enum Side {
        Left,
        Right,
        Unknown
    }
    private Side side = Side.Unknown;

    private void Start() {
        currentMean = new double[4];
        Values = new double[10, 4];
        counter = 0;
        rapportWithSensitivity = new float[4];
        platformAngleRange = stock.GetMinMaxPitch()[1] - stock.GetMinMaxPitch()[0];

        //marbleSpawner.SetSpawn(true);
        //stock.SetIsGamePlaying(true);
    }

    private void FixedUpdate() {

        if (stock.GetIsGamePlaying() && stock.GetGameMode() == EGameMode.Passive) {
            currentMarble = marbleSpawner.GetCurrentMarble();
            if (currentMarble != null) {
                AdjustBoardToMarblePosition(currentMarble);
            }
            Debug.Log("Passive Mode");
        }
        else if (stock.GetIsGamePlaying() && stock.GetGameMode() == EGameMode.Active) {
            FindPitch();
            Debug.Log("Active Mode");
        }
        else if (stock.GetIsGamePlaying() && (stock.GetGameMode() == EGameMode.Adaptative)) {
            Assistance();
            Debug.Log("Adaptative Mode");
        }
    }

    //Assistance functions
    private void Assistance() {
        platformAngleRange = stock.GetMinMaxPitch()[1] - stock.GetMinMaxPitch()[0];

        Values[counter % nbValues, 0] = stock.GetLoadCell1();
        Values[counter % nbValues, 1] = stock.GetLoadCell2();
        Values[counter % nbValues, 2] = stock.GetLoadCell3();
        Values[counter % nbValues, 3] = stock.GetLoadCell4();
        counter++;

        if ((stock.GetMeanLCStable() is not null) && (stock.GetIsGamePlaying())) {
            for (int i = 0; i < 4; i++) {
                rapportWithSensitivity[i] = rapport[i] * (0.5f + (stock.GetSensitivity() * 0.5f / 0.5f));
            }
            currentMean = CalculMean(Values);
            ChangePitch();
            if (ai != null)
                ai.UpdatePlayerInput(gamePitchAngle);
        }
    }
    
    //Active Mode functions
    private void FindPitch() {
        platformAngleRange = stock.GetMinMaxPitch()[1] - stock.GetMinMaxPitch()[0];

        Values[counter % nbValues, 0] = stock.GetLoadCell1();
        Values[counter % nbValues, 1] = stock.GetLoadCell2();
        Values[counter % nbValues, 2] = stock.GetLoadCell3();
        Values[counter % nbValues, 3] = stock.GetLoadCell4();
        counter++;

        if ((stock.GetMeanLCStable() is not null) && (stock.GetIsGamePlaying())) {
            for (int i = 0; i < 4; i++) {
                rapportWithSensitivity[i] = rapport[i] * (0.5f + (stock.GetSensitivity() * 0.5f / 0.5f));
            }
            currentMean = CalculMean(Values);
            ChangePitch();
            Display();
        }
    }

    public void SetPlatformAngle(float gameAngle) {
        platformPitchAngle = stock.GetMinMaxPitch()[0] +
            ((gameAngle - gameMinMaxRange[0]) * platformAngleRange) / (gameMinMaxRange[1] - gameMinMaxRange[0]);
        if (stock.GetIsGamePlaying()) {
            stock.SetPitch(platformPitchAngle);
            stock.SetSend(true);
        }  
    }

    private float CalculateRapport(double[] array, double[] baseArray, float[] rapport) {
        int columns = array.GetLength(0);
        float[] importance = {0.75f, 1.25f, 1.25f, 0.75f};
        double newValue;
        float valuesWithRapport = 0;
        for (int i = 0; i < columns; i++) {
            newValue = (array[i] - baseArray[i]) / rapport[i];
            //Debug.Log("LoadCell: " + (i + 1) + "; newValue: " + newValue);
            valuesWithRapport += (float)newValue * importance[i];
        }
        valuesWithRapport = valuesWithRapport / columns;
        //Debug.Log("valuesWithRapport: " + valuesWithRapport);
        return valuesWithRapport;
    }

    private double[] CalculMean(double[,] values) {
        double[] mean = new double[4];
        for (int i = 0; i < 4; i++) {
            double sum = 0.0;
            for (int j = 0; j < nbValues; j++) {
                sum += values[j, i];
            }
            mean[i] = sum / nbValues;
        }
        return mean;
    }

    private void ChangePitch() {
        forceAngle = CalculateRapport(currentMean, stock.GetMeanLCStable(), rapportWithSensitivity);
        gamePitchAngle -= forceAngle * activeSpeed;
        //Debug.Log("forceAngle wirh force: " + (forceAngle * speed));
        gamePitchAngle = Mathf.Clamp(gamePitchAngle, gameMinMaxRange[0], gameMinMaxRange[1]);
    }

    public void Display() {
        platformPitchAngle = stock.GetMinMaxPitch()[0] +
            ((gamePitchAngle - gameMinMaxRange[0]) * platformAngleRange) / (gameMinMaxRange[1] - gameMinMaxRange[0]);
        //Debug.Log("Game Pitch Angle: " + gamePitchAngle + "; Platform Pitch Angle: " + platformPitchAngle);
        stock.SetPitch(platformPitchAngle);
        foreach (Platform platform in platforms) {
            platform.gameObject.transform.rotation = Quaternion.Euler(0, 0, gamePitchAngle);
        }
        stock.SetSend(true);
    }

    //Passive mode functions
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
        
        foreach (Platform platform in platforms) {
            platform.gameObject.transform.rotation = Quaternion.Euler(0, 0, unityAngle);
        }
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

        newAngle = currentAngle + error * passiveSpeed;
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

        newAngle = currentAngle + error * passiveSpeed;
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

        newAngle = currentAngle + error * passiveSpeed;
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

        newAngle = currentAngle + error * passiveSpeed;
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