using UnityEngine;
using UnityEngine.UI;

public class ModeMenuPhoto : MonoBehaviour
{
    [SerializeField] private StockVariables stock;

    [SerializeField] private Button activeModeButton;
    [SerializeField] private Button passiveModeButton;
    [SerializeField] private Button adaptativeModeButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button applyButton;

    [SerializeField] private GameObject mainMenuUI;

    private EGameMode selectedGameMode = EGameMode.Undefined;

    private void Start()
    {
        activeModeButton.onClick.AddListener(() => OnModeButtonClicked(EGameMode.Active));
        passiveModeButton.onClick.AddListener(() => OnModeButtonClicked(EGameMode.Passive));
        adaptativeModeButton.onClick.AddListener(() => OnModeButtonClicked(EGameMode.Adaptative));
        cancelButton.onClick.AddListener(OnCancelButtonClicked);
        applyButton.onClick.AddListener(OnApplyButtonClicked);

        gameObject.SetActive(false);
    }
    private void OnEnable() {
        selectedGameMode = stock.GetGameMode();
    }   

    private void OnModeButtonClicked(EGameMode mode)
    {
        selectedGameMode = mode;
    }
    private void OnCancelButtonClicked()
    {
        mainMenuUI.SetActive(true);
        gameObject.SetActive(false);
    }
    private void OnApplyButtonClicked()
    {
        if (selectedGameMode != EGameMode.Undefined) {
            stock.SetGameMode(selectedGameMode);
        }
        else {
            Debug.Log("No mode selected to apply.");
        }
        mainMenuUI.SetActive(true);
        gameObject.SetActive(false);
    }

    private void FixedUpdate() {
        switch (selectedGameMode) {
            case EGameMode.Active:
                activeModeButton.interactable = false;
                passiveModeButton.interactable = true;
                adaptativeModeButton.interactable = true;
                break;
            case EGameMode.Passive:
                passiveModeButton.interactable = false;
                activeModeButton.interactable = true;
                adaptativeModeButton.interactable = true;
                break;
            case EGameMode.Adaptative:
                adaptativeModeButton.interactable = false;
                activeModeButton.interactable = true;
                passiveModeButton.interactable = true;
                break;
            default:
                break;
        }
    }

}
