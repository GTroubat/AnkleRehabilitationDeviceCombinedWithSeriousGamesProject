using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ConfigMenuBehavior : MonoBehaviour {
    [SerializeField] private StockVariables stock;

    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject configProcessMenu;

    [SerializeField] private Button applyBtn;
    [SerializeField] private Button cancelBtn;
    [SerializeField] private Button configBtn;

    [SerializeField] private Slider sensitivitySlider;
    [SerializeField] private Slider heightSlider;
    [SerializeField] private Slider pitchPosSlider;
    [SerializeField] private Slider pitchNegSlider;

    [SerializeField] private Toggle testPitch;

    private float previousPitchPos;
    private float previousPitchNeg;

    private void Start() {
        applyBtn.onClick.AddListener(OnApplyClicked);
        cancelBtn.onClick.AddListener(OnCancelClicked);
        configBtn.onClick.AddListener(OnConfigClicked);

        gameObject.SetActive(false);

        sensitivitySlider.value = stock.GetSensitivity();
        heightSlider.value = stock.GetBaseHeight()/10;
        pitchPosSlider.value = stock.GetMinMaxPitch()[1];
        pitchNegSlider.value = -stock.GetMinMaxPitch()[0];

        previousPitchNeg = pitchNegSlider.value;
        previousPitchPos = pitchPosSlider.value;
    }

    private void FixedUpdate() {
        if (testPitch.isOn) {
            stock.SetHeight(stock.GetBaseHeight());
            if (pitchNegSlider.value != previousPitchNeg) {
                stock.SetPitch(pitchNegSlider.value);
            } else if (pitchPosSlider.value != previousPitchPos) {
                stock.SetPitch(-pitchPosSlider.value);
            }
            stock.SetSend(true);
            previousPitchNeg = pitchNegSlider.value;
            previousPitchPos = pitchPosSlider.value;
        }
    }

    public void OnApplyClicked() {
        stock.SetSensitivity(sensitivitySlider.value);
        stock.SetBaseHeight(10 * (int)heightSlider.value);
        stock.SetHeight(10 * (int)heightSlider.value);
        stock.SetSend(true);
        stock.SetMinMaxPitch(new int[] { -(int)pitchNegSlider.value, (int)pitchPosSlider.value });
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
