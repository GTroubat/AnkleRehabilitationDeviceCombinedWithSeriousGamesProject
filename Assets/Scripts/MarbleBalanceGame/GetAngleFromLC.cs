using UnityEngine;
using static UnityEditor.Experimental.AssetDatabaseExperimental.AssetDatabaseCounters;

public class GetAngleFromLC : MonoBehaviour
{
    [SerializeField] private StockVariables stock;
    [SerializeField] private MarbleSpawn marbleSpawner;
    [SerializeField] private float speed = 2f;
    [SerializeField] private LCPlatfromMovement[] lCPlatfromMovement;

    private double[] meanLC;
    private double[] currentDifference;
    private double[] currentMeanLC;
    private double[,] values;

    private int counter = 0;
    private int nbValues = 10;

    private float getAngleTimer = 0f;
    private float getAngleInterval = 0.01f;

    private double[] hysteresis = { 200000, 10, 40000, 200000 };

    void Update()
    {
        getAngleTimer += Time.deltaTime;
        meanLC = stock.GetMeanLCStable();
        if (getAngleTimer >= getAngleInterval && (meanLC is not null)) {
            GetAngle();
            getAngleTimer = 0f;
        }
    }
    private void GetAngle() {
        if (meanLC is not null) {
            double loadCell1 = stock.GetLoadCell1();
            double loadCell2 = stock.GetLoadCell2();
            double loadCell3 = stock.GetLoadCell3();
            double loadCell4 = stock.GetLoadCell4();

            values[counter % nbValues, 0] = loadCell1;
            values[counter % nbValues, 1] = loadCell2;
            values[counter % nbValues, 2] = loadCell3;
            values[counter % nbValues, 3] = loadCell4;
            counter++;

            if (counter >= nbValues) {
                //marbleSpawner.SetBegin(true);
                currentMeanLC = CalculateMeanValues(values);
                currentDifference = CompareTwoArrays(currentMeanLC, stock.GetMeanLCStable());
                bool[] isInBounds = {currentDifference[0] < hysteresis[0], true,
                        currentDifference[2] > hysteresis[2], currentDifference[3] < -hysteresis[3] };
                Debug.Log("isInBounds: " + isInBounds[0] + ", " + isInBounds[1] + ", " + isInBounds[2] + ", " + isInBounds[3]);
                if (isInBounds[0] && isInBounds[1] && isInBounds[2] && isInBounds[3]) {
                    if (stock.GetMinMaxPitch()[0] < (stock.GetPitch())) {
                        stock.SetPitch(stock.GetPitch() - speed);
                    }
                    stock.SetSend(true);
                    Debug.Log("Pointe vers bas!");
                } else if (isInBounds[0] && isInBounds[1] && !isInBounds[2] && !isInBounds[3]) {
                    if (stock.GetMinMaxPitch()[1] > (stock.GetPitch())) {
                        stock.SetPitch(stock.GetPitch() + speed);
                    }
                    stock.SetSend(true);
                    Debug.Log("Pointe vers haut!");
                }
                for (int i = 0; i < lCPlatfromMovement.Length; i++) {
                    lCPlatfromMovement[i].Display();
                }
            }
        }
    }
    private double[] CalculateMeanValues(double[,] values) {
        double[] meanValues = new double[4];
        int count = values.GetLength(0);
        for (int i = 0; i < 4; i++) {
            double sum = 0;
            for (int j = 0; j < count; j++) {
                sum += values[j, i];
            }
            meanValues[i] = sum / count;
        }
        return meanValues;
    }

    private double[] CompareTwoArrays(double[] a, double[] b) {
        double[] comparison = new double[a.Length];
        for (int i = 0; i < a.Length; i++) {
            comparison[i] = a[i] - b[i];
        }
        return comparison;
    }
}
