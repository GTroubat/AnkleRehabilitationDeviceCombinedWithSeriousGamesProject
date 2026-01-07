using System.IO.Ports;
using System.Globalization;
using UnityEngine;

public class Arduino2: MonoBehaviour {
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

                stock.SetLoadCell1(value1);
                stock.SetLoadCell2(value2);
                stock.SetLoadCell3(value3);
                stock.SetLoadCell4(value4);
            }
        } else if (serialPort.IsOpen && serialPort.BytesToRead > 32) {
            serialPort.ReadExisting();
        }
    }

    public void SendValues(float v1, float v2, float v3, float v4) {
        if (serialPort == null) {
            Debug.LogWarning("Serial port not initialized.");
            return;
        }

        if (!serialPort.IsOpen) {
            Debug.LogWarning("Serial port is closed.");
            return;
        }

        try {
            // Use InvariantCulture to replace , with .
            string line = string.Format(CultureInfo.InvariantCulture, "{0};{1};{2};{3}", 
                v1, v2, v3, v4);
            serialPort.WriteLine(line); 
        } catch (System.Exception ex) {
            Debug.LogError($"Failed to send to Arduino: {ex.Message}");
        }
    }

    public void SendDigital(int pin, bool value) {
        if (serialPort == null) {
            Debug.LogWarning("Serial port not initialized.");
            return;
        }

        if (!serialPort.IsOpen) {
            Debug.LogWarning("Serial port is closed.");
            return;
        }

        try {
            // Format : "PIN;{pin};{0|1}"
            string cmd = string.Format(CultureInfo.InvariantCulture, "PIN;{0};{1}", pin, value ? 1 : 0);
            serialPort.WriteLine(cmd);
        } catch (System.Exception ex) {
            Debug.LogError($"Failed to send digital command: {ex.Message}");
        }
    }

    void OnApplicationQuit() {
        if (serialPort != null && serialPort.IsOpen)
            serialPort.Close();
    }
}