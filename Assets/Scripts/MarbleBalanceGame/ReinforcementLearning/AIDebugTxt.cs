using TMPro;
using UnityEngine;

public class AIDebugTxt : MonoBehaviour
{
    [SerializeField] private StockVariables stock;
    [SerializeField] private TMP_Text debugTxt;

    private void FixedUpdate() {
        /*debugTxt.text = "LvlAsst: " + stock.GetAssistLevel().ToString("F3") + 
            "\r\nDis: " + stock.GetCurrentDisability().ToString() + 
            "\r\nTgtAngle: " + stock.GetTargetAngle().ToString("F3") +
            "\r\nOptAngle: " + stock.GetOptimalAngle().ToString("F3") + 
            "\r\nReward: " + stock.GetRewards().ToString("F4") + 
            "\r\nSuccess: " + stock.GetErrors().ToString();
        */
        debugTxt.text = "LvlAsst: " + stock.GetAssistLevel().ToString("F3") +
            "\r\nPatient: " + stock.GetPatientProfile() +
            "\r\nTgtAngle: " + stock.GetTargetAngle().ToString("F3") +
            "\r\nOptAngle: " + stock.GetOptimalAngle().ToString("F3") +
            "\r\nReward: " + stock.GetRewards().ToString("F4") +
            "\r\nSuccess: " + stock.GetErrors().ToString();
    }
}
