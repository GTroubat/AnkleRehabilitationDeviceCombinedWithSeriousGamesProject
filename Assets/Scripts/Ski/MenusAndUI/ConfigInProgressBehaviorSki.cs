using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConfigInProgressBehaviorSki : MonoBehaviour
{
    [SerializeField] private StockVariables stock;

    [SerializeField] private GameObject mainMenu;

    [SerializeField] private Button cancelButton;
    [SerializeField] private Button continueButton;

    [SerializeField] private TMP_Text continueText;
    [SerializeField] private TMP_Text instructionsText;

    [SerializeField] private Image continueImage;
    [SerializeField] private Sprite playSprite;
    [SerializeField] private Sprite processingSprite;

    private bool isProcessing = false;

    private int waitCounter = 200;
    private int configCounter = 0;
    private int configMaxDuration = 600;

    private double[,] allLCValues;
    private double[] meanLCValues;

    private enum ConfigStep
    {
        WaitToConfig,
        AdjustHeightAndAngles,
        HeightAndAnglesAdjustComplete,
        CalibrateSensor,
        CalibrationComplete,
        LeaveMenu
    }
    private ConfigStep currentStep;

    private void Start()
    {
        cancelButton.onClick.AddListener(OnCancelButtonClicked);
        continueButton.onClick.AddListener(OnContinueButtonClicked);

        gameObject.SetActive(false);

        currentStep = ConfigStep.WaitToConfig;
        continueText.text = "Start";
        continueImage.sprite = playSprite;

        isProcessing = false;

        meanLCValues = new double[4];
        allLCValues = new double[configMaxDuration, 4];
    }

    public void OnCancelButtonClicked()
    {
        mainMenu.SetActive(true);
        currentStep = ConfigStep.WaitToConfig;
        continueText.text = "Start";
        continueImage.sprite = playSprite;
        waitCounter = 150;
        configCounter = 0;
        isProcessing = false;
        gameObject.SetActive(false);
    }
    public void OnContinueButtonClicked()
    {
        if(currentStep != ConfigStep.AdjustHeightAndAngles) currentStep++;
    }

    private void FixedUpdate() {
        switch (currentStep) {
            case ConfigStep.WaitToConfig:
                isProcessing = false;
                instructionsText.text = "Careful, the Platform will go up\n\nPress Start";
                break;
            case ConfigStep.AdjustHeightAndAngles:
                continueText.text = "Wait";
                isProcessing = true;
                instructionsText.text = "Adjusting the height and the angle of the platform";
                stock.SetHeight(stock.GetBaseHeight());
                stock.SetPitch(0f);
                stock.SetSend(true);
                if (waitCounter > 0) {
                    waitCounter--;
                } else {
                    waitCounter = 150;
                    currentStep++;
                }
                break;
            case ConfigStep.HeightAndAnglesAdjustComplete:
                continueText.text = "Continue";
                isProcessing = false;
                instructionsText.text = "Height and angles adjusted!\n\nPress Continue to calibrate sensors";
                break;
            case ConfigStep.CalibrateSensor:
                continueText.text = "Wait";
                isProcessing = true;
                CalibratingText();
                CalibratingSensor();
                break;
            case ConfigStep.CalibrationComplete:
                continueText.text = "Continue";
                isProcessing = false;
                instructionsText.text = "Calibration complete!\n\nPress Continue to exit and save";
                break;
            case ConfigStep.LeaveMenu:
                meanLCValues = CalculateMeanValues(allLCValues);
                stock.SetMeanLCStable(meanLCValues);
                mainMenu.SetActive(true);
                currentStep = ConfigStep.WaitToConfig;
                continueText.text = "Start";
                mainMenu.TryGetComponent<MainMenuBehaviorSki>(out MainMenuBehaviorSki mainMenuBehavior);
                mainMenuBehavior.SetFirstTimeFalse();
                gameObject.SetActive(false);
                break;
        }
        if (isProcessing) {
            continueImage.sprite = processingSprite;
            continueImage.transform.Rotate(0f, 0f, -2f);
        } else {
            continueImage.sprite = playSprite;
            continueImage.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        }
    }

    private void CalibratingText() {
        if (Time.frameCount % 180 < 60)
            instructionsText.text = "Calibrating sensors.\n\nDon't move";
        else if (Time.frameCount % 180 < 120) 
            instructionsText.text = "Calibrating sensors..\n\nDon't move";
        else
            instructionsText.text = "Calibrating sensors...\n\nDon't move";
    }
    private void CalibratingSensor() {
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
            currentStep++;
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
