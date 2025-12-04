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


        
        public static bool PLCStatus { get { return UsbPort is not null && ModbusMaster is not null && UsbPort.IsOpen; } }
        
        public PLCService(IConfigPLC config)
        {
            Config = config;
        }
        async Task OpenConnection()
        {
            if (UsbPort is null || !UsbPort.IsOpen || ModbusMaster is null)
            {
                if (UsbPort is null)
                    UsbPort = BuildSerialPort();
                if (!UsbPort.IsOpen)
                {
                    UsbPort.Open();
                    await Task.Delay(300);
                }
                SerialPortAdapter adapter = new SerialPortAdapter(UsbPort);
                IModbusFactory factory = new ModbusFactory();
                ModbusMaster = factory.CreateRtuMaster(adapter);
            }
        }
        async Task Reconnect()
        {
            UsbPort.Close();
            UsbPort = BuildSerialPort();
            UsbPort.Open();
            await Task.Delay(300);
            SerialPortAdapter adapter = new SerialPortAdapter(UsbPort);
            IModbusFactory factory = new ModbusFactory();
            (ModbusMaster as IDisposable)?.Dispose();
            ModbusMaster = factory.CreateRtuMaster(adapter);
        }
        private SerialPort BuildSerialPort()
        {
            SerialPort sp = new SerialPort(Config.USBPATH,9600,Parity.None,8,StopBits.One);
            sp.ReadTimeout = 5000;
            sp.WriteTimeout = 5000;
            return sp;
        }

        public async Task<ushort[]> ReadData(byte address,byte value, byte clientId=1)
        {
            await SemaphoreSlim.WaitAsync();
            try
            {
                await OpenConnection();
                var res = await ModbusMaster.ReadHoldingRegistersAsync(clientId, address, value);
                Log.Information($"Data read : {string.Join(",",res)}");
                SemaphoreSlim.Release();
                return res;
            }
            catch (Exception ex) 
            {
                Log.Error($"Read PLC {ex.Message} ");
                Log.Error($"Read Data: {clientId} {address} {value}");
                await Reconnect();
                SemaphoreSlim.Release();
                return new ushort[value];
            }
        }
        public async Task WriteData(byte address, byte value, byte clientId = 1)
        {
            await SemaphoreSlim.WaitAsync();
            try
            {
                await OpenConnection();
                await ModbusMaster.WriteSingleRegisterAsync(clientId, address, value);
              //  Log.Information($"Data Send {address} {value}");
            }
            catch (Exception ex)
            {
                //Log.Error($"Write PLC {ex.Message} ");
                Log.Information($"Write Data: {clientId} {address} {value}");
            }
            finally
            {
                SemaphoreSlim.Release();
            }
        }

    }
}
