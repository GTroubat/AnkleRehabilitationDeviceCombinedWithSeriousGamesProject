using UnityEngine;
public class LCPlatformByForce : MonoBehaviour
{
    [SerializeField] private StockVariables stock;
    [SerializeField] private float speed = 0.1f;

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

    private void Start() {
        currentMean = new double[4];
        Values = new double[10, 4];
        counter = 0;
        rapportWithSensitivity = new float[4];
        platformAngleRange = stock.GetMinMaxPitch()[1] - stock.GetMinMaxPitch()[0];
    }

    private void FixedUpdate() {
        platformAngleRange = stock.GetMinMaxPitch()[1] - stock.GetMinMaxPitch()[0];

        Values[counter % nbValues, 0] = stock.GetLoadCell1();
        Values[counter % nbValues, 1] = stock.GetLoadCell2();
        Values[counter % nbValues, 2] = stock.GetLoadCell3();
        Values[counter % nbValues, 3] = stock.GetLoadCell4();
        counter++;

        if ((stock.GetMeanLCStable() is not null) && (stock.GetIsGamePlaying()) 
            && (stock.GetGameMode() == EGameMode.Active)) {
            for (int i = 0; i < 4; i++) {
                rapportWithSensitivity[i] = rapport[i] * (0.5f + (stock.GetSensitivity() * 1f / 0.5f));
            }
            //Debug.Log((stock.GetMeanLCStable() is not null) + ", " + stock.GetIsGamePlaying());
            currentMean = CalculMean(Values);
            ChangePitch();
            Display();
        }
    }

    private float CalculateRapport(double[] array, double[] baseArray, float[] rapport) {
        int columns = array.GetLength(0);
        double newValue;
        float valuesWithRapport = 0;
        for (int i = 0; i < columns; i++) {
            newValue = (array[i] - baseArray[i]) / rapport[i];
            //Debug.Log("LoadCell: " + (i + 1) + "; newValue: " + newValue);
            valuesWithRapport += (float)newValue;
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
        gamePitchAngle -= forceAngle * speed;
        //Debug.Log("forceAngle wirh force: " + (forceAngle * speed));
        gamePitchAngle = Mathf.Clamp(gamePitchAngle, gameMinMaxRange[0], gameMinMaxRange[1]);
    }

    public void Display() {
        platformPitchAngle = stock.GetMinMaxPitch()[0] + 
            ((gamePitchAngle - gameMinMaxRange[0]) * platformAngleRange) / (gameMinMaxRange[1] - gameMinMaxRange[0]);
        //Debug.Log("Game Pitch Angle: " + gamePitchAngle + "; Platform Pitch Angle: " + platformPitchAngle);
        stock.SetPitch(platformPitchAngle);
        transform.rotation = Quaternion.Euler(0f, 0f, gamePitchAngle);
        stock.SetSend(true);
    }

    private void Display1DArray(float[] array) {
        int columns = array.GetLength(0);

        for (int i = 0; i < columns - 1; i++) {
            Debug.Log("; LoadCell: " + (i + 1) + "; value: " + array[i]);
        }
    }
}
