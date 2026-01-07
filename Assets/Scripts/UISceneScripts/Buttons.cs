using UnityEngine;
using UnityEngine.UI;

public class Buttons : MonoBehaviour
{
    [SerializeField] private StockVariables stockVariables;
    [SerializeField] private Button send;
    [SerializeField] private Button stop;
    [SerializeField] private Button startconfig;
    [SerializeField] private DynamixelBulk dynamixel;

    private void Start() {
        send.onClick.AddListener(OnSendButtonClick);
        stop.onClick.AddListener(OnStopButtonClick);
        startconfig.onClick.AddListener(OnStartConfigButtonClick);
    }

    public void OnSendButtonClick() {
        stockVariables.SetSend(true);
    }

    public void OnStopButtonClick() {
        dynamixel.ClosePort();
        stockVariables.SetStopLoop(true);
    }

    public void OnStartConfigButtonClick() {
        stockVariables.SetStartConfig(true);
    }

    private void OnDestroy() {
        send.onClick.RemoveListener(OnSendButtonClick);
        stop.onClick.RemoveListener(OnStopButtonClick);
        startconfig.onClick.RemoveListener(OnStartConfigButtonClick);
    }
}
