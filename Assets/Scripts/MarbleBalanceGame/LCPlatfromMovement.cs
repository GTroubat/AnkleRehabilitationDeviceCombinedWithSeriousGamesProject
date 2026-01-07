using System.Runtime.CompilerServices;
using UnityEngine;

public class LCPlatfromMovement : MonoBehaviour
{
    [SerializeField] private StockVariables stock;
    [SerializeField] private float speed = 0.1f;

    private float pitchAngle = 0f;
    private double[] hysteresis = { 10000, 0, 2000, 10000 };

    [SerializeField] private int nbValues = 10;
    private double[,] Values;
    private double[] currentMean;
    private int counter;

    private enum directionPitch { up, down, stable }
    private directionPitch direction;

    private void Start() {
        currentMean = new double[4];
        Values = new double[10, 4];
        counter = 0;
    }

    private void FixedUpdate() {
        Values[counter % nbValues, 0] = stock.GetLoadCell1();
        Values[counter % nbValues, 1] = stock.GetLoadCell2();
        Values[counter % nbValues, 2] = stock.GetLoadCell3();
        Values[counter % nbValues, 3] = stock.GetLoadCell4();
        counter++;

        if (stock.GetMeanLCStable() is not null) {
            direction = FindDirection(stock.GetMeanLCStable());
            ChangePitch();
            Display();
        }
    }

    private directionPitch FindDirection(double[] meanStable) {
        currentMean = CalculMean(Values);
        //Display1DArray(currentMean);
        if (TryUp(meanStable, currentMean)) {
            return directionPitch.up;
        } else if (TryDown(meanStable, currentMean)) {
            return directionPitch.down;
        } else {
            return directionPitch.stable;
        }
    }
    private bool TryUp(double[] meanStable, double[] currentMean) {
        return (currentMean[0] > (meanStable[0] + hysteresis[0])) 
            && ((currentMean[2] + hysteresis[2]) < meanStable[2]) 
            && (currentMean[3] > (meanStable[3] + hysteresis[3]));
    }
    private bool TryDown(double[] meanStable, double[] currentMean) {
        return ((currentMean[0] + hysteresis[0]) < meanStable[0]) 
            && (currentMean[2] > (meanStable[2] + hysteresis[2]))
            && ((currentMean[3] + hysteresis[3]) < meanStable[3] + hysteresis[3]);
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
        if (direction == directionPitch.up) {
            pitchAngle += speed;
        } else if (direction == directionPitch.down) {
            pitchAngle -= speed;
        }
        pitchAngle = Mathf.Clamp(pitchAngle, stock.GetMinMaxPitch()[0], stock.GetMinMaxPitch()[1]);
    }

    public void Display() {
        stock.SetPitch(pitchAngle);
        transform.rotation = Quaternion.Euler(0f, 0f, pitchAngle);
        stock.SetSend(true);
    }

    private void Display1DArray(double[] array) {
        int columns = array.GetLength(0);

        for (int i = 0; i < columns - 1; i++) {
            Debug.Log("; LoadCell: " + (i + 1) + "; value: " + array[i]);
        }
    }
}
