using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AngleAndHeight : MonoBehaviour
{
    [SerializeField] StockVariables stock;
    [SerializeField] TMP_InputField height;
    [SerializeField] TMP_InputField pitch;
    [SerializeField] TMP_InputField roll;
    [SerializeField] TMP_InputField yaw;
    
    private void Update() {
        // Get values in input Fields
        string inputHeight = height.text;
        string inputPitch = pitch.text;
        string inputRoll = roll.text;
        string inputYaw = yaw.text;

        // Convert string to int and set stock variables
        if (int.TryParse(inputHeight, out int intHeight)) {
            stock.SetHeight(intHeight);
        } else {
            Debug.LogWarning("Failed to convert height to int: " + inputHeight);
        }

        if (int.TryParse(inputPitch, out int intPitch)) {
            stock.SetPitch(intPitch);
        } else {
            Debug.LogWarning("Failed to convert pitch to int: " + inputPitch);
        }

        if (int.TryParse(inputRoll, out int intRoll)) {
            stock.SetRoll(intRoll);
        } else {
            Debug.LogWarning("Failed to convert roll to int: " + inputRoll);
        }

        if (int.TryParse(inputYaw, out int intYaw)) {
            stock.SetYaw(intYaw);
        } else {
            Debug.LogWarning("Failed to convert yaw to int: " + inputYaw);
        }
    }

}
