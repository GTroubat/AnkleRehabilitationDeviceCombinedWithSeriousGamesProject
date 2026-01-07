using System;
using System.Runtime.InteropServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class TestDynamixelSDK: MonoBehaviour {
    const string dll_path = "dxl_x64_c.dll";
    int PROTOCOL_VERSION = 2;
    byte DXL_ID = 1;
    UInt16 ADDR_PRESENT_POSITION = 132;
    void IStart() {
        [DllImport(dll_path, CallingConvention = CallingConvention.Cdecl)]
        static extern int write1ByteTxRx(int port_num, int protocol_version, byte id, UInt16 address, byte data, ref byte error);
        [DllImport(dll_path, CallingConvention = CallingConvention.Cdecl)]
        static extern int write4ByteTxRx(int port_num, int protocol_version, byte id, UInt16 address, UInt32 data, ref byte error);
        [DllImport(dll_path, CallingConvention = CallingConvention.Cdecl)]
        static extern int read4ByteTxRx(int port_num, int protocol_version, byte id, UInt16 address, ref UInt32 data, ref byte error);
        [DllImport(dll_path)]
        static extern int portHandler(string port_name);
        [DllImport(dll_path)]
        static extern bool openPort(int port_num);
        [DllImport(dll_path)]
        static extern void closePort(int port_num);
        [DllImport(dll_path)]
        static extern bool setBaudRate(int port_num, int baudrate);


        int portNum = portHandler("COM6");
        bool success = openPort(portNum);
        if (!success) {
            Debug.LogError("Échec de l'ouverture du port.");
            return;
        }
        Debug.Log("Port ouvert avec succès !");

        setBaudRate(portNum, 56700);
        Debug.Log("BaudRate set");

        // Exemple d'écriture 1 byte (activation du torque)
        byte error = 0;
        int result = write1ByteTxRx(portNum, PROTOCOL_VERSION, DXL_ID, 64, 1, ref error);
        if (result == 0) {
            Debug.Log("Torque activé avec succès !");
        } else {
            Debug.LogError($"Erreur lors de l'activation du torque : {error}");
        }

        // Exemple d'écriture 4 bytes (position goal)
        result = write4ByteTxRx(portNum, PROTOCOL_VERSION, DXL_ID, 116, 4095, ref error);
        if (result == 0) {
            Debug.Log("Position goal envoyée avec succès !");
        } else {
            Debug.LogError($"Erreur lors de l'envoi de la position goal : {error}");
        }

        // Exemple de lecture 4 bytes (position actuelle)
        UInt32 position = 0;
        result = read4ByteTxRx(portNum, PROTOCOL_VERSION, DXL_ID, ADDR_PRESENT_POSITION, ref position, ref error);
        if (result == 0) {
            Debug.Log($"Position mesurée : {position}");
        } else {
            Debug.LogError($"Erreur lors de la lecture de la position : {error}");
        }

        closePort(portNum);
        Debug.Log("Port fermé");
    }

    void Start() {
        //Appel fonctions utiles
        [DllImport(dll_path)]
        static extern int portHandler(string port_name);
        [DllImport(dll_path)]
        static extern bool openPort(int port_num);
        [DllImport(dll_path)]
        static extern void closePort(int port_num);
        [DllImport(dll_path)]
        static extern bool setBaudRate(int port_num, int baudrate);
        [DllImport(dll_path)]
        static extern void write1ByteTxRx(int port_num, int protocol_version, byte id, UInt16 address, byte data);
        [DllImport(dll_path)]
        static extern void write4ByteTxRx(int port_num, int protocol_version, byte id, UInt16 address, UInt32 data);
        [DllImport(dll_path)]
        static extern UInt32 read4ByteTxRx(int port_num, int protocol_version, byte id, UInt16 address);

        // Initialiser le port
        int portNum = portHandler("COM6"); 
        bool success = openPort(portNum);

        if (success) {
            Debug.Log("Port ouvert avec succès !");
            setBaudRate(portNum, 56700); 
            Debug.Log("BaudRate set");

            write1ByteTxRx(portNum, 2, 1, 64, 1);
            Debug.Log("torque enabled");

            write4ByteTxRx(portNum, 2, 1, 116, 4095);
            Debug.Log("Commande envoyée.");

            uint position = read4ByteTxRx(portNum, PROTOCOL_VERSION, DXL_ID, ADDR_PRESENT_POSITION);
            Debug.Log("Position mesurée : " + position);

            closePort(portNum);
            Debug.Log("Port fermé");
        } else {
            Debug.LogError("Échec de l'ouverture du port.");
        }
    }
}
