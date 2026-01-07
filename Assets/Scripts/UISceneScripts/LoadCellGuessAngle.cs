using TMPro;
using UnityEngine;

public class LoadCellGuessAngle: MonoBehaviour {
    [SerializeField] private StockVariables stockVariables;
    [SerializeField] private TMP_Text instructions;
    [SerializeField] private TMP_Text meanValuesText;
    [SerializeField] private TMP_Text pitchText;
    [SerializeField] private int speed;

    private double[,] Values;
    private double[] currentMean;
    private double[] stableMean;
    private double[] currentDifference;
    private int counter = 0;
    private int nbValues = 10;

    private float displayTimer = 0f;
    private float displayInterval = 0.0025f;

    private double[] hysteresis = {200000, 10, 40000, 200000};

    private enum Step {
        Stable = 0,
        Pitch = 1,
        Roll = 2,
        Yaw = 3,
        End = 4
    }

    private Step step;

    private void Start() {
        Values = new double[nbValues, 4];
        currentMean = new double[4];
        currentDifference = new double[4];
        step = Step.End;
        Debug.Log("start");
    }

    private void Update() {
        if (stockVariables.GetStartConfig()) {
            if (step == Step.End) step = Step.Stable;
            else {
                step++;
                counter = 0;
            }
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
            case Step.Pitch:
                instructions.text = "Pitch! n Move in Pitch direction";
                break;
            case Step.Roll:
                instructions.text = "Roll!\n Move in Roll direction";
                break;;
            case Step.Yaw:
                instructions.text = "Yaw!\n FMove in Yaw direction";
                break;
            case Step.End:
                instructions.text = "Well Done!\n Finished";
                break;
        }
        double loadCell1 = stockVariables.GetLoadCell1();
        double loadCell2 = stockVariables.GetLoadCell2();
        double loadCell3 = stockVariables.GetLoadCell3();
        double loadCell4 = stockVariables.GetLoadCell4();

        Values[counter % nbValues, 0] = loadCell1;
        Values[counter % nbValues, 1] = loadCell2;
        Values[counter % nbValues, 2] = loadCell3;
        Values[counter % nbValues, 3] = loadCell4;
        counter++;

        if (counter >= nbValues) {
            currentMean = CalculMean(Values);

            switch (step) {
                case Step.Stable:
                    stableMean = (double[])currentMean.Clone();
                    meanValuesText.text = "Mean of last 10 values:\n LC 1: " + currentMean[0].ToString("F2") + "\n LC 2: "
                        + currentMean[1].ToString("F2") + "\n LC 3: " + currentMean[2].ToString("F2") + "\n LC 4: "
                        + currentMean[3].ToString("F2");
                    //Display1DArray(stableMean);
                    break;
                case Step.Pitch:
                    currentDifference = compareArrays(currentMean, stableMean);
                    meanValuesText.text = "Mean of last 10 values:\n LC 1: " + currentDifference[0].ToString("F2") + "\n LC 2: "
                        + currentDifference[1].ToString("F2") + "\n LC 3: " + currentDifference[2].ToString("F2") + "\n LC 4: "
                        + currentDifference[3].ToString("F2");
                    bool[] isInBounds = {currentDifference[0] < hysteresis[0], true,
                        currentDifference[2] > hysteresis[2], currentDifference[3] < -hysteresis[3] };
                    Debug.Log("isInBounds: " + isInBounds[0] + ", " + isInBounds[1] + ", " + isInBounds[2] + ", " + isInBounds[3]);
                    //if (currentDifference[0] > hysteresis[0] && currentDifference[1] < -hysteresis[1] &&
                    //    currentDifference[2] > hysteresis[2] && currentDifference[3] < -hysteresis[3]) {
                    if (isInBounds[0] && isInBounds[1] && isInBounds[2] && isInBounds[3]) {
                        if (stockVariables.GetMinMaxPitch()[0] < (stockVariables.GetPitch())) {
                            stockVariables.SetPitch(stockVariables.GetPitch() - speed);
                            pitchText.text = stockVariables.GetPitch().ToString();
                        }
                        stockVariables.SetSend(true);
                        Debug.Log("Pointe vers bas!");
                    } else if (isInBounds[0] && isInBounds[1] && !isInBounds[2] && !isInBounds[3]) {
                        if (stockVariables.GetMinMaxPitch()[1] > (stockVariables.GetPitch())) {
                            stockVariables.SetPitch(stockVariables.GetPitch() + speed);
                            pitchText.text = stockVariables.GetPitch().ToString();           
                        }
                        stockVariables.SetSend(true);
                        Debug.Log("Pointe vers haut!");
                    }
                    
                    //Display1DArray(currentDifference);
                    break;
                case Step.Roll:
                    instructions.text = "Roll!\n Move in Roll direction";
                    break;
                case Step.Yaw:
                    instructions.text = "Yaw!\n FMove in Yaw direction";
                    break;
                case Step.End:
                    instructions.text = "Well Done!\n Finished";
                    break;
            }

            meanValuesText.text = "Mean of last 100 values:\n LC 1: " + currentMean[0].ToString("F2") + "\n LC 2: "
                + currentMean[1].ToString("F2") + "\n LC 3: " + currentMean[2].ToString("F2") + "\n LC 4: " 
                + currentMean[3].ToString("F2");

            counter = 0;
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

    private double[] compareArrays(double[] arrayInputCurrent, double[] arrayInputStable) {
        double[] arrayOutput = new double[4];
        for (int i = 0; i < 4; i++) {
            arrayOutput[i] = arrayInputCurrent[i] - arrayInputStable[i];
        }
        return arrayOutput;
    }

    private void Display1DArray(double[] array) {
        int columns = array.GetLength(0);

        for (int i = 0; i < columns - 1; i++) {
             Debug.Log("; LoadCell: " + i + "; value: " + array[i]);
        }
    }
}
