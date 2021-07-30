using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace FastGithub.ReverseProxy
{
    /// <summary>
    /// windows iphlpapi
    /// </summary>
    [SupportedOSPlatform("windows")]
    unsafe static class TcpTable
    {
        private const int ERROR_INSUFFICIENT_BUFFER = 122;

        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern uint GetExtendedTcpTable(void* pTcpTable, ref int pdwSize, bool bOrder, AddressFamily ulAf, TCP_TABLE_CLASS tableClass, uint reserved = 0);


        /// <summary>
        /// 杀死占用进程
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public static bool KillPortOwner(int port)
        {
            if (TryGetOwnerProcessId(port, out var pid) == false)
            {
                return true;
            }

            try
            {
                var proess = Process.GetProcessById(pid);
                proess.Kill();
                proess.WaitForExit(1000);
                return true;
            }
            catch (ArgumentException)
            {
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 获取tcp端口的占用进程id
        /// </summary>
        /// <param name="port"></param>
        /// <param name="processId"></param>
        /// <returns></returns>
        public static bool TryGetOwnerProcessId(int port, out int processId)
        {
            processId = 0;
            var pdwSize = 0;
            var result = GetExtendedTcpTable(null, ref pdwSize, false, AddressFamily.InterNetwork, TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_LISTENER);
            if (result != ERROR_INSUFFICIENT_BUFFER)
            {
                return false;
            }

            var buffer = new byte[pdwSize];
            fixed (byte* pTcpTable = &buffer[0])
            {
                result = GetExtendedTcpTable(pTcpTable, ref pdwSize, false, AddressFamily.InterNetwork, TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_LISTENER);
                if (result != 0)
                {
                    return false;
                }

                var prt = new IntPtr(pTcpTable);
                var table = Marshal.PtrToStructure<MIB_TCPTABLE_OWNER_PID>(prt);
                prt += sizeof(int);
                for (var i = 0; i < table.dwNumEntries; i++)
                {
                    var row = Marshal.PtrToStructure<MIB_TCPROW_OWNER_PID>(prt);
                    if (row.LocalPort == port)
                    {
                        processId = row.ProcessId;
                        return true;
                    }

                    prt += Marshal.SizeOf<MIB_TCPROW_OWNER_PID>();
                }
            }

            return false;
        }


        private enum TCP_TABLE_CLASS
        {
            TCP_TABLE_BASIC_LISTENER,
            TCP_TABLE_BASIC_CONNECTIONS,
            TCP_TABLE_BASIC_ALL,
            TCP_TABLE_OWNER_PID_LISTENER,
            TCP_TABLE_OWNER_PID_CONNECTIONS,
            TCP_TABLE_OWNER_PID_ALL,
            TCP_TABLE_OWNER_MODULE_LISTENER,
            TCP_TABLE_OWNER_MODULE_CONNECTIONS,
            TCP_TABLE_OWNER_MODULE_ALL
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MIB_TCPTABLE_OWNER_PID
        {
            public uint dwNumEntries;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MIB_TCPROW_OWNER_PID
        {
            public uint state;

            public uint localAddr;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] localPort;

            public uint remoteAddr;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] remotePort;

            public int owningPid;

            public int ProcessId => owningPid;

            public IPAddress LocalAddress => new(localAddr);

            public ushort LocalPort => BinaryPrimitives.ReadUInt16BigEndian(this.localPort);
        }
    }
}
