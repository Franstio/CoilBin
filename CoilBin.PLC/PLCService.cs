using CoilBin.PLC.Contracts;
using NModbus;
using NModbus.Device;
using NModbus.IO;
using NModbus.Serial;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoilBin.PLC
{
    public class PLCService
    {
        private static SerialPort UsbPort = null!;
        private static IModbusMaster ModbusMaster = null!;
        private static SemaphoreSlim SemaphoreSlim = new SemaphoreSlim(1, 1);
        private IConfigPLC Config { get; set; }


        private static Lazy<bool> PLCConnection = new Lazy<bool>(() => UsbPort is not null && ModbusMaster is not null && UsbPort.IsOpen);

        public static bool PLCStatus => PLCConnection.Value;
        
        public PLCService(IConfigPLC config)
        {
            Config = config;
            try
            {
                OpenConnection();
            }
            catch { }
        }
        void OpenConnection()
        {
            if (UsbPort is null || !UsbPort.IsOpen || ModbusMaster is null)
            {
                if (UsbPort is null)
                    UsbPort = BuildSerialPort();
                if (!UsbPort.IsOpen)
                    UsbPort.Open();
                SerialPortAdapter adapter = new SerialPortAdapter(UsbPort);
                IModbusFactory factory = new ModbusFactory();
                ModbusMaster = factory.CreateRtuMaster(adapter);
            }
        }
        private SerialPort BuildSerialPort()
        {
            SerialPort sp = new SerialPort(Config.USBPATH,9600,Parity.None,8,StopBits.One);
            sp.ReadTimeout = 1000;
            sp.WriteTimeout = 1000;
            return sp;
        }

        public async Task<ushort[]> ReadData(byte address,byte value, byte clientId=1)
        {
            await SemaphoreSlim.WaitAsync();
            try
            {
                OpenConnection();
                var res = await ModbusMaster.ReadHoldingRegistersAsync(clientId, address, value);
                await Task.Delay(50);
                return res;
            }
            catch (Exception ex) 
            {
                Log.Error($"Read PLC {ex.Message} ");
                Log.Error($"Read Data: {clientId} {address} {value}");
                SemaphoreSlim.Release();
                return new ushort[value];
            }
        }
        public async Task WriteData(byte address, byte value, byte clientId = 1)
        {
            await SemaphoreSlim.WaitAsync();
            try
            {
                OpenConnection();
                await ModbusMaster.WriteSingleRegisterAsync(clientId, address, value);
                await Task.Delay(50);
            }
            catch (Exception ex)
            {
                Log.Error($"Write PLC {ex.Message} ");
                Log.Error($"Write Data: {clientId} {address} {value}");
                SemaphoreSlim.Release();
            }
        }

    }
}
