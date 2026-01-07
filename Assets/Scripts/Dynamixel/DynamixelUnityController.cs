using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class DynamixelUnityController : MonoBehaviour
{
    public string portName = "COM6";
    public int baudrate = 57600;
    public int dxlId = 1;
    public int protocolVersion = 2;

    int portNum = -1;

    void Start()
    {
        try
        {
            portNum = dynamixel_sdk.dynamixel.portHandler(portName);
            dynamixel_sdk.dynamixel.packetHandler();

            if (!dynamixel_sdk.dynamixel.openPort(portNum))
            {
                Debug.LogError("Failed to open port " + portName);
                return;
            }

            if (!dynamixel_sdk.dynamixel.setBaudRate(portNum, baudrate))
            {
                Debug.LogError("Failed to set baudrate " + baudrate);
                return;
            }

            // Enable torque
            dynamixel_sdk.dynamixel.write1ByteTxRx(portNum, protocolVersion, (byte)dxlId, 64, 1);
            int comm = dynamixel_sdk.dynamixel.getLastTxRxResult(portNum, protocolVersion);
            byte err = dynamixel_sdk.dynamixel.getLastRxPacketError(portNum, protocolVersion);
            if (comm != 0)
                Debug.LogError(Marshal.PtrToStringAnsi(dynamixel_sdk.dynamixel.getTxRxResult(protocolVersion, comm)));
            else if (err != 0)
                Debug.LogError(Marshal.PtrToStringAnsi(dynamixel_sdk.dynamixel.getRxPacketError(protocolVersion, err)));
            else
                Debug.Log("Dynamixel torque enabled.");

            // simple test: move to middle position
            dynamixel_sdk.dynamixel.write4ByteTxRx(portNum, protocolVersion, (byte)dxlId, 116, 2048);
        }
        catch (Exception ex)
        {
            Debug.LogError("Exception in DynamixelUnityController.Start: " + ex.Message);
        }
    }

    void OnApplicationQuit()
    {
        try
        {
            if (portNum >= 0)
            {
                // disable torque
                dynamixel_sdk.dynamixel.write1ByteTxRx(portNum, protocolVersion, (byte)dxlId, 64, 0);
                dynamixel_sdk.dynamixel.closePort(portNum);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Exception in DynamixelUnityController.OnApplicationQuit: " + ex.Message);
        }
    }
}
