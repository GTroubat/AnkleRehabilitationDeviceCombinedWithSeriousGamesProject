using UnityEngine;
using UnityEngine.UI;

public class ButtonsMarbleBalance : MonoBehaviour {
    [SerializeField] private Button startButton;
    [SerializeField] private Button stopButton;
    [SerializeField] private StockVariables stock;
    void Start()
    {
        startButton.onClick.AddListener(OnStartClick);
        stopButton.onClick.AddListener(OnStopClick);
    }

    public void OnStartClick() {
        stock.SetStartConfig(true);
    }

    public void OnStopClick() {
        stock.SetStopLoop(true);
    }

    private void OnDestroy() {
        startButton.onClick.RemoveListener(OnStartClick);
        stopButton.onClick.RemoveListener(OnStopClick);
    }
}
