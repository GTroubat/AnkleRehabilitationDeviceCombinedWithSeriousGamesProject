using UnityEngine;
using UnityEngine.UI;

public class MainMenuBehaviorPhoto: MonoBehaviour {
    [SerializeField] private StockVariables stock;

    [SerializeField] private GameObject configMenu;
    [SerializeField] private GameObject gameUI;
    [SerializeField] private GameObject configProcessMenu;
    [SerializeField] private GameObject modeSelectionMenu;

    [SerializeField] private DynamixelReplaceMatlab dynamixelController;
    [SerializeField] private DynamixelBulk dynamixelBulkController;

    [SerializeField] private Button playBtn;
    [SerializeField] private Button modeBtn;
    [SerializeField] private Button configBtn;
    [SerializeField] private Button gameBtn;
    [SerializeField] private Button exitBtn;

    private bool firstTime = true;
    private bool once = true;

    private void Start() {
        playBtn.onClick.AddListener(OnPlayClicked);
        modeBtn.onClick.AddListener(OnModeClicked);
        configBtn.onClick.AddListener(OnConfigClicked);
        exitBtn.onClick.AddListener(OnExitClicked);
        gameBtn.onClick.AddListener(OnGameClicked);

        gameObject.SetActive(true);
    }

    public void OnPlayClicked() {
        if (!firstTime && once) {
            stock.SetIsGamePlaying(true);
            gameUI.SetActive(true);
            once = false;
        } else if (once) {
            configProcessMenu.SetActive(true);
        }
        gameObject.SetActive(false);
    }
    public void OnExitClicked() {
        Debug.Log("Ended");
        stock.SetStopLoop(true);
        if (dynamixelBulkController != null)
            dynamixelBulkController.ClosePort();
        if (dynamixelController != null)
            dynamixelController.ClosePort();
        Application.Quit();
    }
    public void OnConfigClicked() {
        configMenu.SetActive(true);
        gameObject.SetActive(false);
    }
    public void OnGameClicked() {
    }
    public void OnModeClicked() { 
        modeSelectionMenu.SetActive(true);
        gameObject.SetActive(false);
    }

    public void SetFirstTimeFalse() {
        firstTime = false;
    }

    public void SetOnceTrue() {
        once = true;
    }
}
