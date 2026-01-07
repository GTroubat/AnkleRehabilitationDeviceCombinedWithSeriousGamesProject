using TMPro;
using UnityEngine;

public class LoadCellValues : MonoBehaviour
{
    [SerializeField] private StockVariables stockVariables;
    [SerializeField] private TMP_Text loadCellText1;
    [SerializeField] private TMP_Text loadCellText2;
    [SerializeField] private TMP_Text loadCellText3;
    [SerializeField] private TMP_Text loadCellText4;
    [SerializeField] private TMP_Text meanTenValuesText;

    private double[,] lastTenValues;
    private double[] mean;
    private int counter = 0;

    private float displayTimer = 0f;
    private float displayInterval = 0.1f;

    private void Start() {
        lastTenValues = new double[10, 4];
        mean = new double[4];
    }

    private void Update() {
        displayTimer += Time.deltaTime;
        if (displayTimer >= displayInterval) {
            Display();
            displayTimer = 0f;
        }  
    }

    private void Display() {
        double loadCell1 = stockVariables.GetLoadCell1();
        double loadCell2 = stockVariables.GetLoadCell2();
        double loadCell3 = stockVariables.GetLoadCell3();
        double loadCell4 = stockVariables.GetLoadCell4();
        loadCellText1.text = "Load Cell 1: \n" + loadCell1.ToString("F2");
        loadCellText2.text = "Load Cell 2: \n" + loadCell2.ToString("F2");
        loadCellText3.text = "Load Cell 3: \n" + loadCell3.ToString("F2");
        loadCellText4.text = "Load Cell 4: \n" + loadCell4.ToString("F2");

        lastTenValues[counter, 0] = loadCell1;
        lastTenValues[counter, 1] = loadCell2;
        lastTenValues[counter, 2] = loadCell3;
        lastTenValues[counter, 3] = loadCell4;
        counter++;

        if (counter == 10) {
            mean = CalculMean(lastTenValues);

            meanTenValuesText.text = "Mean of last 10 values:\n LC 1: " + mean[0].ToString("F2") + "\n LC 2: "
                + mean[1].ToString("F2") + "\n LC 3: " + mean[2].ToString("F2") + "\n LC 4: " + mean[3].ToString("F2");

            counter = 0;
        }
    }

    private double[] CalculMean(double[,] values) {
        double[] mean = new double[4];
        for (int i = 0; i < 4; i++) {
            double sum = 0.0;
            for (int j = 0; j < 10; j++) {
                sum += values[j, i];
            }
            mean[i] = sum / 10.0;
        }
        return mean;
    }
}
