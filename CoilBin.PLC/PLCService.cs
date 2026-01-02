using CoilBin.PLC.Contracts;
using NModbus;
using NModbus.Device;
using NModbus.IO;
using NModbus.Serial;
using Serilog;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CoilBin.PLC
{
    public class PLCService
    {
        private static SerialPort UsbPort = null!;
        private static IModbusMaster ModbusMaster = null!;
        private static SemaphoreSlim SemaphoreSlim = new SemaphoreSlim(1, 1);
        private static SemaphoreSlim LastWorkSemaphore = new SemaphoreSlim(1,1);
        private const string KEY = "LastPLCWork";
        private  IConfigPLC Config { get; set; }
        private readonly IConnectionMultiplexer redisCon;
        public static bool PLCStatus { get { return UsbPort is not null && ModbusMaster is not null && UsbPort.IsOpen; } }
        public static DateTime StaticLastWork = DateTime.Now;
        public PLCService(IConfigPLC config,IConnectionMultiplexer con)
        {
            Config = config;
            redisCon = con;
        }
        public async Task<DateTime> GetLastWork()
        {
            try
            {
                await LastWorkSemaphore.WaitAsync();

                var cur = redisCon.GetDatabase(8);
                string? dt = await cur.StringGetAsync(KEY);
                return dt is null ? DateTime.Now : DateTime.Parse(dt);
            }

            catch
            {
                Log.Error("Failed reading PLC redis");
                return DateTime.MinValue;
            }
            finally
            {
                LastWorkSemaphore.Release();
            }
        }
        public async Task SetLastWork(DateTime dt)
        {
            StaticLastWork = dt;
            try
            {
                await LastWorkSemaphore.WaitAsync();
                var cur = redisCon.GetDatabase(8);
                await cur.StringSetAsync(KEY, dt.ToString("yyyy-MM-dd HH:mm:ss"));
            }
            catch
            {
                Log.Error("Failed Writing PLC redis");
            }
            finally
            {
                LastWorkSemaphore.Release();
            }
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
            try
            {
                if (UsbPort is null)
                    return;
                UsbPort.Close();
                UsbPort = BuildSerialPort();
                UsbPort.Open();
                await Task.Delay(300);
                SerialPortAdapter adapter = new SerialPortAdapter(UsbPort);
                IModbusFactory factory = new ModbusFactory();
                (ModbusMaster as IDisposable)?.Dispose();
                ModbusMaster = factory.CreateRtuMaster(adapter);
            }
            catch(Exception e)
            {
                Log.Error($"Reconnect: {e.Message}");
            }
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
                await SetLastWork(DateTime.Now);
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
        public async Task<bool> WriteData(byte address, byte value, byte clientId = 1)
        {
            await SemaphoreSlim.WaitAsync();
            try
            {
                await OpenConnection();
                await ModbusMaster.WriteSingleRegisterAsync(clientId, address, value);
                await SetLastWork(DateTime.Now);
                //  Log.Information($"Data Send {address} {value}");
                return true;
            }
            catch (Exception ex)
            {
                //Log.Error($"Write PLC {ex.Message} ");
                Log.Information($"Write Data: {clientId} {address} {value}");
                return false;
            }
            finally
            {
                SemaphoreSlim.Release();
            }
        }

    }
}
