using TMPro;
using UnityEngine;

public class LoadCellDetreminationAngle: MonoBehaviour
{
    [SerializeField] private StockVariables stockVariables;
    [SerializeField] private TMP_Text instructions;
    [SerializeField] private TMP_Text meanValuesText;
    [SerializeField] private DataExporter dataExporter;

    private double[,] lastValues;
    private double[] currentMean;
    private double[] currentMin = {10000000.0, 10000000.0, 10000000.0, 10000000.0};
    private double[] currentMax = { 0, 0, 0, 0 };
    private double[,] stepMean;
    private int counter = 0;
    private int nbValues = 100;

    private float displayTimer = 0f;
    private float displayInterval = 0.1f;

    private enum Step {
        Stable = 0,
        PitchDown = 1,
        PitchUp = 2,
        RollRight = 3,
        RollLeft = 4,
        YawRight = 5,
        YawLeft = 6,
        End = 7
    }

    private Step step;

    private void Start() {
        lastValues = new double[nbValues, 4];
        currentMean = new double[4];
        stepMean = new double[8, 12];
        step = Step.End;
        Debug.Log("start");
    }

    private void Update() {
        if (stockVariables.GetStartConfig()) {
            step = Step.Stable;
            stockVariables.SetStartConfig(false);
        }

        if(step != Step.End) {
            displayTimer += Time.deltaTime;
        }
        
        if (displayTimer >= displayInterval) {
            Display();
            displayTimer = 0f;
        }  
    }

    private void Display() {
        switch (step) {
            case Step.Stable:
                instructions.text = "Don't move!\n Finding mean Stable Value";
                break;
            case Step.PitchUp:
                instructions.text = "Pitch Up!\n Finding mean Pitch Up Value";
                break;
            case Step.PitchDown:
                instructions.text = "Pitch Down!\n Finding mean Pitch Down Value";
                break;
            case Step.RollLeft:
                instructions.text = "Roll Left!\n Finding mean Roll Left Value";
                break;
            case Step.RollRight:
                instructions.text = "Roll Right!\n Finding mean Roll Right Value";
                break;
            case Step.YawLeft:
                instructions.text = "Yaw Left!\n Finding mean Yaw Left Value";
                break;
            case Step.YawRight:
                instructions.text = "Yaw Right!\n Finding mean Yaw Right Value";
                break;
            case Step.End:
                instructions.text = "Well Done!\n Finding mean Angle Values finished";
                break;
        }
        double loadCell1 = stockVariables.GetLoadCell1();
        double loadCell2 = stockVariables.GetLoadCell2();
        double loadCell3 = stockVariables.GetLoadCell3();
        double loadCell4 = stockVariables.GetLoadCell4();

        if (loadCell1 < currentMin[0]) currentMin[0] = loadCell1;
        if (loadCell2 < currentMin[1]) currentMin[1] = loadCell2;
        if (loadCell3 < currentMin[2]) currentMin[2] = loadCell3;
        if (loadCell4 < currentMin[3]) currentMin[3] = loadCell4;

        if (loadCell1 > currentMax[0]) currentMax[0] = loadCell1;
        if (loadCell2 > currentMax[1]) currentMax[1] = loadCell2;
        if (loadCell3 > currentMax[2]) currentMax[2] = loadCell3;
        if (loadCell4 > currentMax[3]) currentMax[3] = loadCell4;

        lastValues[counter, 0] = loadCell1;
        lastValues[counter, 1] = loadCell2;
        lastValues[counter, 2] = loadCell3;
        lastValues[counter, 3] = loadCell4;
        counter++;

        if (counter == nbValues) {
            currentMean = CalculMean(lastValues);

            meanValuesText.text = "Mean of last 100 values:\n LC 1: " + currentMean[0].ToString("F2") + "\n LC 2: "
                + currentMean[1].ToString("F2") + "\n LC 3: " + currentMean[2].ToString("F2") + "\n LC 4: " 
                + currentMean[3].ToString("F2");

            for (int i = 0; i < 4; i++) {
                stepMean[((int)step), i] = currentMean[i];
            }
            for (int i = 0; i < 4; i++) {
                stepMean[((int)step), (i + 4)] = currentMin[i];
            }
            for (int i = 0; i < 4; i++) {
                stepMean[((int)step), (i + 8)] = currentMax[i];
            }

            step++;

            if (step == Step.End) { 
                instructions.text = "Well Done!\n Finding mean Angle Values finished";
                Display2DArray(stepMean);
                //dataExporter.ConvertArrayToCsv(stepMean);
                stockVariables.SetStopLoop(true);
            }

            // For next lines :
            currentMin = new double[] { 10000000.0, 10000000.0, 10000000.0, 10000000.0 };
            currentMax = new double[] { 0, 0, 0, 0 };

            counter = 0;
        }
    }

    private void Display2DArray(double[,] array) {
        int columns = array.GetLength(0);
        int lines = array.GetLength(1);

        for (int i = 0; i < columns-1; i++) {
            for (int j = 0; j < lines; j++) {
                Debug.Log("Step: " + i + "; LoadCell: " + (j+1) + "; value: " + array[i, j]);
            }
        }
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
}
