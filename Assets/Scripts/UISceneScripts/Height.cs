using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Height : MonoBehaviour
{
    [SerializeField] StockVariables stock;
    [SerializeField] TMP_InputField height;
    
    private void Update() {
        // Get values in input Fields
        string inputHeight = height.text;

        // Convert string to int and set stock variables
        if (int.TryParse(inputHeight, out int intHeight)) {
            stock.SetHeight(intHeight);
        } else 
            Debug.LogWarning("Failed to convert height to int: " + inputHeight);

    }

}
