using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ConfigMenuBehaviorSki : MonoBehaviour {
    [Header("General")]
    [SerializeField] private StockVariables stock;

    [Header("Menus")]
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject configProcessMenu;

    [Header("UI Elements")]
    [Header("Buttons")]
    [SerializeField] private Button applyBtn;
    [SerializeField] private Button cancelBtn;
    [SerializeField] private Button configBtn;

    [Header("Sliders")]
    [SerializeField] private Slider sensitivitySlider;
    [SerializeField] private Slider heightSlider;
    [SerializeField] private Slider pitchPosSlider;
    [SerializeField] private Slider pitchNegSlider;
    [SerializeField] private Slider rollPosSlider;
    [SerializeField] private Slider rollNegSlider;
    [SerializeField] private Slider yawNegSlider;
    [SerializeField] private Slider yawPosSlider;

    [Header("Toggles")]
    [SerializeField] private Toggle testPitch;
    [SerializeField] private Toggle testRoll;
    [SerializeField] private Toggle testYaw;
    [SerializeField] private ToggleGroup testsToggleGroup;

    private float previousPitchPos;
    private float previousPitchNeg;
    private float previousRollPos;
    private float previousRollNeg;
    private float previousYawPos;
    private float previousYawNeg;

    private void Start() {
        applyBtn.onClick.AddListener(OnApplyClicked);
        cancelBtn.onClick.AddListener(OnCancelClicked);
        configBtn.onClick.AddListener(OnConfigClicked);

        gameObject.SetActive(false);

        sensitivitySlider.value = stock.GetSensitivity();
        heightSlider.value = stock.GetBaseHeight()/10;
        pitchPosSlider.value = stock.GetMinMaxPitch()[1];
        pitchNegSlider.value = -stock.GetMinMaxPitch()[0];
        rollPosSlider.value = stock.GetMinMaxRoll()[1];
        rollNegSlider.value = -stock.GetMinMaxRoll()[0];
        yawPosSlider.value = stock.GetMinMaxYaw()[1];
        yawNegSlider.value = -stock.GetMinMaxYaw()[0];

        previousPitchNeg = pitchNegSlider.value;
        previousPitchPos = pitchPosSlider.value;
        previousRollNeg = rollNegSlider.value;
        previousRollPos = rollPosSlider.value;
        previousYawNeg = yawNegSlider.value;
        previousYawPos = yawPosSlider.value;
    }

    /// <summary>
    /// This method changes the platform orientation in real-time based on the selected test toggle and slider values.
    /// </summary>
    /// <remarks> Only one test toggle can be active at a time. </remarks>
    private void FixedUpdate() {
        if (testPitch.isOn) {
            stock.SetRoll(0);
            stock.SetYaw(0);
            stock.SetHeight(stock.GetBaseHeight());
            if (pitchNegSlider.value != previousPitchNeg) {
                stock.SetPitch(pitchNegSlider.value);
            } else if (pitchPosSlider.value != previousPitchPos) {
                stock.SetPitch(-pitchPosSlider.value);
            }
            stock.SetSend(true);
            previousPitchNeg = pitchNegSlider.value;
            previousPitchPos = pitchPosSlider.value;
        } else if (testRoll.isOn) {
            stock.SetPitch(0);
            stock.SetYaw(0);
            stock.SetHeight(stock.GetBaseHeight());
            if (rollNegSlider.value != previousRollNeg) {
                stock.SetRoll(rollNegSlider.value);
            } else if (rollPosSlider.value != previousRollPos) {
                stock.SetRoll(-rollPosSlider.value);
            }
            stock.SetSend(true);
            previousRollNeg = rollNegSlider.value;
            previousRollPos = rollPosSlider.value;
        } else if (testYaw.isOn) {
            stock.SetPitch(0);
            stock.SetRoll(0);
            stock.SetHeight(stock.GetBaseHeight());
            if (yawNegSlider.value != previousYawNeg) {
                stock.SetYaw(yawNegSlider.value);
            } else if (yawPosSlider.value != previousYawPos) {
                stock.SetYaw(-yawPosSlider.value);
            }
            stock.SetSend(true);
            previousYawNeg = yawNegSlider.value;
            previousYawPos = yawPosSlider.value;
        }
    }

    public void OnApplyClicked() {
        stock.SetSensitivity(sensitivitySlider.value);
        stock.SetBaseHeight(10 * (int)heightSlider.value);
        stock.SetHeight(10 * (int)heightSlider.value);
        stock.SetSend(true);
        stock.SetMinMaxPitch(new int[] { -(int)pitchNegSlider.value, (int)pitchPosSlider.value });
        stock.SetMinMaxRoll(new int[] { -(int)rollNegSlider.value, (int)rollPosSlider.value });
        stock.SetMinMaxYaw(new int[] { -(int)yawNegSlider.value, (int)yawPosSlider.value });
        mainMenu.SetActive(true);
        gameObject.SetActive(false);
    }
    public void OnCancelClicked() {
        mainMenu.SetActive(true);
        gameObject.SetActive(false);
    }

    public void OnConfigClicked() { 
        configProcessMenu.SetActive(true);
        gameObject.SetActive(false);
    }
}
