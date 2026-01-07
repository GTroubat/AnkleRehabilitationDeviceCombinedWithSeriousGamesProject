using System;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;
using static Unity.Barracuda.Model;

public class DynamixelReplaceMatlab : MonoBehaviour {
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

    // Other Constant values
    private readonly UInt32 EXTENDED_MODE = 4;
    private readonly byte TORQUE_ENABLE = 1;
    private readonly byte TORQUE_DISABLE = 0;

    private int comm;
    private byte err;

    private int portNum = -1;
    private UInt32 goalPos1 = 0;
    private UInt32 goalPos2 = 0;
    private UInt32 goalPos3 = 0;
    private UInt32 goalPos4 = 0;

    // Worker thread fields
    private Thread sendThread;
    private AutoResetEvent sendEvent = new AutoResetEvent(false);
    private volatile bool sendThreadRunning = false;
    private readonly object goalsLock = new object();
    private uint pendingGoal1 = 0, pendingGoal2 = 0, pendingGoal3 = 0, pendingGoal4 = 0;
    private volatile string workerException = null;

    void Start() {
        try {
            portNum = dynamixel_sdk.dynamixel.portHandler(portName);
            dynamixel_sdk.dynamixel.packetHandler();

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

            // Start worker thread to avoid blocking main thread
            sendThreadRunning = true;
            sendThread = new Thread(SendThreadLoop) { IsBackground = true, Name = "DynamixelSendThread" };
            sendThread.Start();
        } catch (Exception ex) {
            Debug.LogError("Exception in DynamixelUnityController.Start: " + ex.Message);
        }
    }

    private void Update() {
        //      
        if (workerException != null) {
            Debug.LogError("Dynamixel worker error: " + workerException);
            workerException = null;
        }

        if (stock.GetSend()) {
            // Capture latest motor values quickly on main thread (cheap casts)
            lock (goalsLock) {
                pendingGoal1 = unchecked((uint)stock.GetMotorValue1());
                pendingGoal2 = unchecked((uint)stock.GetMotorValue2());
                pendingGoal3 = unchecked((uint)stock.GetMotorValue3());
                pendingGoal4 = unchecked((uint)stock.GetMotorValue4());
            }
            // Signal worker to send (non-blocking)
            sendEvent.Set();
            stock.SetSend(false);
        }
    }

    private void SendThreadLoop() {
        while (sendThreadRunning) {
            // Wait until main thread signals there is something to send (or thread stop)
            sendEvent.WaitOne();
            if (!sendThreadRunning) break;

            uint g1, g2, g3, g4;
            lock (goalsLock) {
                g1 = pendingGoal1;
                g2 = pendingGoal2;
                g3 = pendingGoal3;
                g4 = pendingGoal4;
            }

            try {
                // Perform the serial writes on the background thread (may block but doesn't affect FPS)
                dynamixel_sdk.dynamixel.write4ByteTxRx(portNum, protocolVersion, (byte)dxlId1, A_GOAL_POSITION, g1);
                dynamixel_sdk.dynamixel.write4ByteTxRx(portNum, protocolVersion, (byte)dxlId2, A_GOAL_POSITION, g2);
                dynamixel_sdk.dynamixel.write4ByteTxRx(portNum, protocolVersion, (byte)dxlId3, A_GOAL_POSITION, g3);
                dynamixel_sdk.dynamixel.write4ByteTxRx(portNum, protocolVersion, (byte)dxlId4, A_GOAL_POSITION, g4);
            } catch (Exception ex) {
                // Avoid calling Unity APIs from worker thread: store message for main thread logging
                workerException = ex.Message;
            }
        }
    }

    // kept for compatibility; not used by main thread now but retained if needed
    private void SendToDynamixel() {
        try {
            // This method is no longer called on the main thread in the new flow.
            // Left for compatibility; prefer worker thread approach implemented above.
            if (stock.GetMotorValue1() < 0) { goalPos1 = (uint)((UInt32)Math.Pow(2, 32) + stock.GetMotorValue1()); }
            else { goalPos1 = (UInt32)stock.GetMotorValue1(); }
            if (stock.GetMotorValue2() < 0) { goalPos2 = (uint)((UInt32)Math.Pow(2, 32) + stock.GetMotorValue2()); }
            else { goalPos2 = (UInt32)stock.GetMotorValue2(); }
            if (stock.GetMotorValue3() < 0) { goalPos3 = (uint)((UInt32)Math.Pow(2, 32) + stock.GetMotorValue3()); }
            else { goalPos3 = (UInt32)stock.GetMotorValue3(); }
            if (stock.GetMotorValue4() < 0) { goalPos4 = (uint)((UInt32)Math.Pow(2, 32) + stock.GetMotorValue4()); }
            else { goalPos4 = (UInt32)stock.GetMotorValue4(); }

            // Send goal positions to Dynamixel
            dynamixel_sdk.dynamixel.write4ByteTxRx(portNum, protocolVersion, (byte)dxlId1, A_GOAL_POSITION, goalPos1);
            dynamixel_sdk.dynamixel.write4ByteTxRx(portNum, protocolVersion, (byte)dxlId2, A_GOAL_POSITION, goalPos2);
            dynamixel_sdk.dynamixel.write4ByteTxRx(portNum, protocolVersion, (byte)dxlId3, A_GOAL_POSITION, goalPos3);
            dynamixel_sdk.dynamixel.write4ByteTxRx(portNum, protocolVersion, (byte)dxlId4, A_GOAL_POSITION, goalPos4);
        } catch (Exception ex) {
            Debug.LogError("Exception in DynamixelUnityController.SendToDynamixel: " + ex.Message);
        }
    }

    public void ClosePort() {
        try {
            // stop worker thread first
            sendThreadRunning = false;
            sendEvent.Set();
            if (sendThread != null && sendThread.IsAlive) {
                if (!sendThread.Join(500)) {
                    try { sendThread.Abort(); } catch { /* best effort */ }
                }
            }

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

