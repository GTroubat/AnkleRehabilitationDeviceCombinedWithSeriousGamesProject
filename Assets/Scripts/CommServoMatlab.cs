using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class CommServoMatlab : MonoBehaviour
{
    [SerializeField] private StockVariables stock;

    public string remoteIP = "10.14.38.23";
    public int sendPort = 25000;    // Port to send data to MATLAB

    private UdpClient sendClient;

    void Start() {
        sendClient = new UdpClient();
        sendClient.Connect(remoteIP, sendPort);
        Debug.Log("Unity: UDP Communication started.");
    }

    void FixedUpdate() {
        if (stock.GetSend() || stock.GetStopLoop()) {
            SendData();
            stock.SetSend(false);
        }
    }

    private void SendData() {
        try {
            int[] data = {
            Convert.ToInt32(stock.GetStopLoop()),
            stock.GetMotorValue1(),
            stock.GetMotorValue2(),
            stock.GetMotorValue3(),
            stock.GetMotorValue4()
        };

            // Convert int array into bytes arry
            byte[] sendBytes = new byte[data.Length * 4]; // 4 bytes per int32
            Buffer.BlockCopy(data, 0, sendBytes, 0, sendBytes.Length);

            sendClient.Send(sendBytes, sendBytes.Length);
            Debug.Log("Unity: data sent to MATLAB");
            //Debug.Log("Unity: data sent to MATLAB: " + string.Join(", ", data));
        } catch (SocketException e) {
            Debug.LogError("Unity: send error: " + e.Message);
            sendClient.Close();
            sendClient = new UdpClient();
            sendClient.Connect(remoteIP, sendPort);
        }
    }
}
