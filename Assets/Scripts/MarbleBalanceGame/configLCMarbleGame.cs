using TMPro;
using UnityEngine;

public class configLCMarbleGame : MonoBehaviour
{
    [SerializeField] private StockVariables stock;
    [SerializeField] private TMP_Text instructions;
    [SerializeField] private TMP_Text startConfigText;
    [SerializeField] private MarbleSpawnWithDisplayer marbleSpawner;
    private enum Step {
        waitToBegin,
        wait1s,
        config,
        configEnded
    }
    private Step currentStep;
    private int configCounter = 0;
    private int configMaxDuration = 600;

    private double[,] allLCValues;
    private double[] meanLCValues;

    private int wait1sCounter = 100;
    private void Start() {
        currentStep = Step.waitToBegin;
        instructions.text = "Press Start to begin config \n after putting your foot";
        allLCValues = new double[configMaxDuration, 4];
    }

    void FixedUpdate()
    {
        switch (currentStep) {
            case Step.waitToBegin:
                if (stock.GetStartConfig()) {
                    currentStep = Step.wait1s;
                    instructions.text = "Config in progress...\n Don't move your foot";
                    stock.SetHeight(150);
                    stock.SetSend(true);
                    stock.SetStartConfig(false);
                }
                break;
            case Step.wait1s:
                if (wait1sCounter > 0) {
                    wait1sCounter--;
                } else {
                    currentStep = Step.config;
                    wait1sCounter = 100;
                }
                break;
            case Step.config:
                if (configCounter < configMaxDuration) {
                    allLCValues = AddValuesToArray(allLCValues, new double[] {
                        stock.GetLoadCell1(),
                        stock.GetLoadCell2(),
                        stock.GetLoadCell3(),
                        stock.GetLoadCell4()
                    }, configCounter);
                    configCounter++;
                    
                } else {
                    configCounter = 0;
                    stock.SetStartConfig(true);
                }

                if (stock.GetStartConfig()) {
                    currentStep = Step.configEnded;
                    startConfigText.text = "Config";
                    instructions.enabled = false;
                    meanLCValues = CalculateMeanValues(allLCValues);
                    stock.SetMeanLCStable(meanLCValues);
                    stock.SetIsGamePlaying(true);
                    stock.SetStartConfig(false);
                    marbleSpawner.SetSpawn(true);//begin game
                }
                break;
            case Step.configEnded:
                if (stock.GetStartConfig()) {
                    stock.SetIsGamePlaying(false);
                    instructions.enabled = true;
                    foreach (var marble in Object.FindObjectsByType<Marble>(FindObjectsSortMode.None)) {
                        Destroy(marble.gameObject);
                    }
                    instructions.text = "Config in progress...\n Don't move your foot";
                    stock.SetHeight(150);
                    stock.SetPitch(0f);
                    stock.SetSend(true);
                    currentStep = Step.wait1s;
                    stock.SetStartConfig(false);
                }
                break;
        }
        if (stock.GetStopLoop()) {
            marbleSpawner.SetSpawn(false);
        }
    }

    private double[,] AddValuesToArray(double[,] array, double[] newValues, int index) {
        for (int i = 0; i < 4; i++) {
            array[index, i] = newValues[i];
        }
        return array;
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
}

