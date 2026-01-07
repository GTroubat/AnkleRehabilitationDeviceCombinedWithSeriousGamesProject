using dynamixel_sdk;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;
using static Unity.Barracuda.Model;

public class DynamixelBulk : MonoBehaviour
{
    [SerializeField] private StockVariables stock;
    [SerializeField] private string portName = "COM6";
    [SerializeField] private int baudrate = 57600;
    [SerializeField] private int dxlId1 = 1;
    [SerializeField] private int dxlId2 = 2;
    [SerializeField] private int dxlId3 = 3;
    [SerializeField] private int dxlId4 = 4;
    [SerializeField] private int protocolVersion = 2;

    // Memory addresses
    private readonly UInt16 A_TORQUE = 64;
    private readonly UInt16 A_GOAL_POSITION = 116;
    private readonly UInt16 A_OPERATING_MODE = 11;

    // Lengths
    private readonly UInt16 LEN_GOAL_POSITION = 4;

    // Other Constant values
    private readonly UInt32 EXTENDED_MODE = 4;
    private readonly byte TORQUE_ENABLE = 1;
    private readonly byte TORQUE_DISABLE = 0;

    private int portNum = -1;
    private UInt32 goalPos1 = 0;
    private UInt32 goalPos2 = 0;
    private UInt32 goalPos3 = 0;
    private UInt32 goalPos4 = 0;

    private int groupwrite_num;

    void Start() {
        try {
            portNum = dynamixel_sdk.dynamixel.portHandler(portName);
            // Initialize GroupBulkWrite Struct
            groupwrite_num = dynamixel.groupBulkWrite(portNum, protocolVersion);

            if (!dynamixel_sdk.dynamixel.openPort(portNum)) {
                Debug.LogError("Failed to open port " + portName);
                return;
            }

            if (!dynamixel_sdk.dynamixel.setBaudRate(portNum, baudrate)) {
                Debug.LogError("Failed to set baudrate " + baudrate);
                return;
            }

            // Enable torque
            dynamixel_sdk.dynamixel.write1ByteTxRx(portNum, protocolVersion, (byte)dxlId1, A_TORQUE, TORQUE_ENABLE);
            dynamixel_sdk.dynamixel.write1ByteTxRx(portNum, protocolVersion, (byte)dxlId2, A_TORQUE, TORQUE_ENABLE);
            dynamixel_sdk.dynamixel.write1ByteTxRx(portNum, protocolVersion, (byte)dxlId3, A_TORQUE, TORQUE_ENABLE);
            dynamixel_sdk.dynamixel.write1ByteTxRx(portNum, protocolVersion, (byte)dxlId4, A_TORQUE, TORQUE_ENABLE);

            Debug.Log("Dynamixel torque enabled.");

            // Set to extended position control mode
            dynamixel_sdk.dynamixel.write4ByteTxRx(portNum, protocolVersion, (byte)dxlId1, A_OPERATING_MODE, EXTENDED_MODE);
            dynamixel_sdk.dynamixel.write4ByteTxRx(portNum, protocolVersion, (byte)dxlId2, A_OPERATING_MODE, EXTENDED_MODE);
            dynamixel_sdk.dynamixel.write4ByteTxRx(portNum, protocolVersion, (byte)dxlId3, A_OPERATING_MODE, EXTENDED_MODE);
            dynamixel_sdk.dynamixel.write4ByteTxRx(portNum, protocolVersion, (byte)dxlId4, A_OPERATING_MODE, EXTENDED_MODE);

            Debug.Log("Dynamixel Extended Mode Set.");
        } catch (Exception ex) {
            Debug.LogError("Exception in DynamixelUnityController.Start: " + ex.Message);
        }
    }

    private void Update() {
        if (stock.GetSend()) {
            SendToDynamixel();
            stock.SetSend(false);
        }
    }

    private void SendToDynamixel() {
        try {
            if (stock.GetMotorValue1() < 0) { goalPos1 = (uint)((UInt32)Math.Pow(2, 32) + stock.GetMotorValue1()); } else { goalPos1 = (UInt32)stock.GetMotorValue1(); }
            if (stock.GetMotorValue2() < 0) { goalPos2 = (uint)((UInt32)Math.Pow(2, 32) + stock.GetMotorValue2()); } else { goalPos2 = (UInt32)stock.GetMotorValue2(); }
            if (stock.GetMotorValue3() < 0) { goalPos3 = (uint)((UInt32)Math.Pow(2, 32) + stock.GetMotorValue3()); } else { goalPos3 = (UInt32)stock.GetMotorValue3(); }
            if (stock.GetMotorValue4() < 0) { goalPos4 = (uint)((UInt32)Math.Pow(2, 32) + stock.GetMotorValue4()); } else { goalPos4 = (UInt32)stock.GetMotorValue4(); }

            // Send goal positions to Dynamixel
            dynamixel_sdk.dynamixel.groupBulkWriteAddParam(groupwrite_num, (byte)dxlId1, A_GOAL_POSITION, LEN_GOAL_POSITION, goalPos1, LEN_GOAL_POSITION);
            dynamixel_sdk.dynamixel.groupBulkWriteAddParam(groupwrite_num, (byte)dxlId2, A_GOAL_POSITION, LEN_GOAL_POSITION, goalPos2, LEN_GOAL_POSITION);
            dynamixel_sdk.dynamixel.groupBulkWriteAddParam(groupwrite_num, (byte)dxlId3, A_GOAL_POSITION, LEN_GOAL_POSITION, goalPos3, LEN_GOAL_POSITION);
            dynamixel_sdk.dynamixel.groupBulkWriteAddParam(groupwrite_num, (byte)dxlId4, A_GOAL_POSITION, LEN_GOAL_POSITION, goalPos4, LEN_GOAL_POSITION);
            dynamixel_sdk.dynamixel.groupBulkWriteTxPacket(groupwrite_num);
            //dynamixel_sdk.dynamixel.write4ByteTxRx(portNum, protocolVersion, (byte)dxlId1, A_GOAL_POSITION, goalPos1);
            //dynamixel_sdk.dynamixel.write4ByteTxRx(portNum, protocolVersion, (byte)dxlId2, A_GOAL_POSITION, goalPos2);
            //dynamixel_sdk.dynamixel.write4ByteTxRx(portNum, protocolVersion, (byte)dxlId3, A_GOAL_POSITION, goalPos3);
            //dynamixel_sdk.dynamixel.write4ByteTxRx(portNum, protocolVersion, (byte)dxlId4, A_GOAL_POSITION, goalPos4);
            dynamixel_sdk.dynamixel.groupBulkWriteClearParam(groupwrite_num);
        } catch (Exception ex) {
            Debug.LogError("Exception in DynamixelUnityController.SendToDynamixel: " + ex.Message);
        }
    }

    public void ClosePort() {
        try {
            if (portNum >= 0) {
                // disable torque
                dynamixel_sdk.dynamixel.write1ByteTxRx(portNum, protocolVersion, (byte)dxlId1, A_TORQUE, TORQUE_DISABLE);
                dynamixel_sdk.dynamixel.write1ByteTxRx(portNum, protocolVersion, (byte)dxlId2, A_TORQUE, TORQUE_DISABLE);
                dynamixel_sdk.dynamixel.write1ByteTxRx(portNum, protocolVersion, (byte)dxlId3, A_TORQUE, TORQUE_DISABLE);
                dynamixel_sdk.dynamixel.write1ByteTxRx(portNum, protocolVersion, (byte)dxlId4, A_TORQUE, TORQUE_DISABLE);
                dynamixel_sdk.dynamixel.closePort(portNum);
            }
        } catch (Exception ex) {
            Debug.LogError("Exception in DynamixelUnityController.OnApplicationQuit: " + ex.Message);
        }
    }
}
