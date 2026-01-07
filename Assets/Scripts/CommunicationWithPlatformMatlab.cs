using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class CommunicationWithPlatformMatlab : MonoBehaviour
{
    [SerializeField] private StockVariables stock;

    public string remoteIP = "10.14.38.23";
    public int receivePort = 25100; // Port to receive data from MATLAB
    public int sendPort = 25000;    // Port to send data to MATLAB

    private UdpClient receiveClient;
    private UdpClient sendClient;

    void Start() {
        receiveClient = new UdpClient(receivePort);
        sendClient = new UdpClient();
        sendClient.Connect(remoteIP, sendPort);
        Debug.Log("Unity: UDP Communication started.");
    }

    void FixedUpdate() {
        ReceiveData();
        if (stock.GetSend() || stock.GetStopLoop()) {
            SendData();
            stock.SetSend(false);
        }
    }

    void ReceiveData() {
        try {
            if (receiveClient.Available > 0) {
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                byte[] receivedBytes = receiveClient.Receive(ref remoteEP);

                // convert to double
                if (receivedBytes.Length >= 64) {
                    double[] receivedData = new double[8];
                    Buffer.BlockCopy(receivedBytes, 0, receivedData, 0, 64);
                    //Debug.Log("Unity: received from Matlab: " + string.Join(", ", receivedData));
                    stock.SetLoadCell1(receivedData[0]);
                    stock.SetLoadCell2(receivedData[1]);
                    stock.SetLoadCell3(receivedData[2]);
                    stock.SetLoadCell4(receivedData[3]);
                }
                //Debug.Log("Unity: data received from MATLAB.");
            }
        } catch (SocketException e) {
            Debug.LogError("Unity: reception error: " + e.Message);
            receiveClient.Close();
            receiveClient = new UdpClient(receivePort);
        }
    }

    void SendData() {
        try {
            float[] data = {
                stock.GetStopLoop() ? 1.0f : 0.0f,
                stock.GetHeight(),
                stock.GetPitch(),
                stock.GetRoll(),
                stock.GetYaw()
            };

            // Convert float array into bytes array
            byte[] sendBytes = new byte[data.Length * 4]; // 4 bytes per float
            Buffer.BlockCopy(data, 0, sendBytes, 0, sendBytes.Length);

            sendClient.Send(sendBytes, sendBytes.Length);
            //Debug.Log("Unity: data sent to MATLAB: " + string.Join(", ", data));
        } catch (SocketException e) {
            Debug.LogError("Unity: send error: " + e.Message);
            sendClient.Close();
            sendClient = new UdpClient();
            sendClient.Connect(remoteIP, sendPort);
        }
    }

    void OnDestroy() {
        receiveClient.Close();
        sendClient.Close();
    }
}
