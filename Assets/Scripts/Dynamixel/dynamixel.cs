using System;
using System.Runtime.InteropServices;

namespace dynamixel_sdk
{
  public class dynamixel
  {
    const string dll_path = "dxl_x64_c"; 

    [DllImport(dll_path)]
    public static extern int portHandler(string port_name);

    [DllImport(dll_path)]
    public static extern void packetHandler();

    [DllImport(dll_path)]
    public static extern bool openPort(int port_num);
    [DllImport(dll_path)]
    public static extern void closePort(int port_num);

    [DllImport(dll_path)]
    public static extern bool setBaudRate(int port_num, int baudrate);

    [DllImport(dll_path)]
    public static extern void write1ByteTxRx(int port_num, int protocol_version, byte id, UInt16 address, byte data);

    [DllImport(dll_path)]
    public static extern void write4ByteTxRx(int port_num, int protocol_version, byte id, UInt16 address, UInt32 data);

    [DllImport(dll_path)]
    public static extern int getLastTxRxResult(int port_num, int protocol_version);

    [DllImport(dll_path)]
    public static extern byte getLastRxPacketError(int port_num, int protocol_version);

    [DllImport(dll_path)]
    public static extern IntPtr getTxRxResult(int protocol_version, int result);

    [DllImport(dll_path)]
    public static extern IntPtr getRxPacketError(int protocol_version, byte error);

    [DllImport(dll_path)]
    public static extern int groupSyncWrite(int port_num, int protocol_version, UInt16 start_address, UInt16 data_length);

    [DllImport(dll_path)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool groupSyncWriteAddParam(int group_num, byte id, byte[] data, UInt16 data_length);

    [DllImport(dll_path)]
    public static extern void groupSyncWriteTxPacket(int group_num);

    [DllImport(dll_path)]
    public static extern void groupSyncWriteClearParam(int group_num);

    [DllImport(dll_path)]
    public static extern int groupBulkRead(int port_num, int protocol_version);

    [DllImport(dll_path)]
    public static extern bool groupBulkReadAddParam(int group_num, byte id, UInt16 start_address, UInt16 data_length);
    [DllImport(dll_path)]
    public static extern void groupBulkReadRemoveParam(int group_num, byte id);
    [DllImport(dll_path)]
    public static extern void groupBulkReadClearParam(int group_num);

    [DllImport(dll_path)]
    public static extern int groupBulkWrite(int port_num, int protocol_version);

    [DllImport(dll_path)]
    public static extern bool groupBulkWriteAddParam(int group_num, byte id, UInt16 start_address, UInt16 data_length, UInt32 data, UInt16 input_length);
    [DllImport(dll_path)]
    public static extern void groupBulkWriteRemoveParam(int group_num, byte id);
    [DllImport(dll_path)]
    public static extern bool groupBulkWriteChangeParam(int group_num, byte id, UInt16 start_address, UInt16 data_length, UInt32 data, UInt16 input_length, UInt16 data_pos);
    [DllImport(dll_path)]
    public static extern void groupBulkWriteClearParam(int group_num);
    [DllImport(dll_path)]
    public static extern void groupBulkWriteTxPacket(int group_num);
    }
}
