using System.IO.Ports;
using UnityEngine;

public class Arduino: MonoBehaviour {
    [SerializeField] private StockVariables stock;
    [SerializeField] private string portName = "COM10"; 
    [SerializeField] private int baudRate = 9600;

    private SerialPort serialPort;

    void Start() {
        serialPort = new SerialPort(portName, baudRate);
        serialPort.ReadTimeout = 100;
        serialPort.Open();
    }

    private void FixedUpdate() {
        if (serialPort.IsOpen && serialPort.BytesToRead == 32) {
            string data = serialPort.ReadLine();
            //data = data.Replace(".", ",");
            string[] values = data.Split(';');
            //Debug.Log($"Valeurs: {values[0]}, {values[1]}, {values[2]}, {values[3]}");
            if (values.Length == 4) {
                float value1 = float.Parse(values[0]);
                float value2 = float.Parse(values[1]);
                float value3 = float.Parse(values[2]);
                float value4 = float.Parse(values[3]);

                //Debug.Log($"Valeurs: {value1}, {value2}, {value3}, {value4}");
                if (value1 < 1 || value2 < 1 || value3 < 1 || value4 < 1) {
                    Debug.LogWarning("Negative load cell value detected.");
                }

                stock.SetLoadCell1(value1);
                stock.SetLoadCell2(value2);
                stock.SetLoadCell3(value3);
                stock.SetLoadCell4(value4);
            }
        } else if (serialPort.IsOpen && serialPort.BytesToRead > 32) {
            serialPort.ReadExisting();
        }
    }

    void IUpdate() {
        if (serialPort.IsOpen && serialPort.BytesToRead > 3) {
            string data = serialPort.ReadLine();
            data = data.Replace(".", ","); 
            string[] values = data.Split(';');
            if (values.Length == 4) {
                float value1 = float.Parse(values[0]);
                float value2 = float.Parse(values[1]);
                float value3 = float.Parse(values[2]);
                float value4 = float.Parse(values[3]);

                // Utilisez les valeurs dans Unity
                Debug.Log($"Valeurs: {value1}, {value2}, {value3}, {value4}");
            }
        }
    }

    void OnApplicationQuit() {
        if (serialPort != null && serialPort.IsOpen)
            serialPort.Close();
    }
}
