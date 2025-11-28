using Avalonia.Media;
using Avalonia.Threading;
using CoilBin.Models;
using CoilBin.PLC;
using CoilBin.PLC.Eums;
using CoilBin.PLC.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore.Painting;
using LiveChartsCore.SkiaSharpView.Painting;
using System;
using System.Threading.Tasks;

namespace CoilBin.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private ushort[] data = new ushort[10];

    [ObservableProperty]
    private IBrush redLamp  = Brush.Parse("#95a5a6")!;

    [ObservableProperty]
    public IBrush yellowLamp = Brush.Parse("#95a5a6")!;

    [ObservableProperty]
    public IBrush greenLamp = Brush.Parse("#95a5a6")!;

    [ObservableProperty]
    public IBrush topSensor = Brush.Parse("#95a5a6")!;
    [ObservableProperty] private IBrush bottomSensor = Brush.Parse("#95a5a6")!;
    [ObservableProperty] private IBrush topLock = Brush.Parse("#95a5a6")!;
    [ObservableProperty] private IBrush bottomLock = Brush.Parse("#95a5a6")!;

    [ObservableProperty]
    private Paint gaugeLevel = SolidColorPaint.Parse("#95a5a6")!;

    [ObservableProperty]
    private decimal weight = 50;
    
    private decimal maxWeight = 50;
    
    [ObservableProperty]
    private decimal weightPercentage =100;

    [ObservableProperty] private bool allowReopen = false;

    [ObservableProperty] private string reopenText = "Reopen Lock";
    [ObservableProperty] private string instruksi = "";

    public string BinIP => BinService.GetBinIpAddress();
    [ObservableProperty] public string plcStatus = "Offline";
    public string Timbangan => config.Timbangan;
    private Task SensorReadingTask = null!;
    private BinService BinService;
    private ConfigModel config;
    private RunningTransactionManager Manager;
    private BinInfoManager BinInfoManager;
    partial void OnWeightChanged(decimal value)
    {
        WeightPercentage = (value / maxWeight) * 100;
        GaugeLevel = GaugeLevelFunc();
        
    }
    
    private IBrush GetColorEllipse(int index)
    {
        return data.Length <= index  ? Brush.Parse("#95a5a6") : Brush.Parse(data[index] == 1 ? "#2ecc71" : "#e74c3c");
    }
    public string XamlGaugeSeries_DataLabelsFormatter(LiveChartsCore.Kernel.ChartPoint arg)
    {
        return arg.Coordinate.PrimaryValue + "%";
    }
    public Paint GaugeLevelFunc()
    {
        
        string color = WeightPercentage < 20 ? "#2ecc71" : (WeightPercentage < 90 ? "#f1c40f" : "#e74c3c");
        return SolidColorPaint.Parse(color)!;
    }
    void UpdatePLCData()
    {
        GreenLamp = GetColorEllipse((int)BinEnum.Green);
        YellowLamp = GetColorEllipse((int)BinEnum.Yellow);
        RedLamp = GetColorEllipse((int)BinEnum.Red);
        TopSensor = GetColorEllipse((int)BinEnum.TopSensor);
        BottomSensor = GetColorEllipse((int)BinEnum.BottomSensor);
        TopLock = GetColorEllipse((int)BinEnum.TopLock);
        BottomLock = GetColorEllipse((int)BinEnum.BottomLock);
    }
    public MainViewModel(BinService binService,RunningTransactionManager manager,ConfigModel config,BinInfoManager binInfoManager)
    {
        this.config = config;
        Manager = manager;
        BinInfoManager = binInfoManager;
        BinService = binService;
        SensorReadingTask = Task.Run(SensorLoop);
    }
    private async Task SensorLoop()
    {
        while (true)
        {
            var runningTransaction = Manager.RunningTransactionData;
            var binInfo = BinInfoManager.BinInfoModel;
            maxWeight = binInfo.Max_Weight ?? maxWeight;
            Weight = binInfo.Weight ?? Weight;
            data = await BinService.ReadingSensor();
            PlcStatus = BinService.PLCStatus ? "Online" : "Offline";
            await Dispatcher.UIThread.InvokeAsync( ()=>
            {
                UpdatePLCData();
                Instruksi = runningTransaction.Message;
                if (runningTransaction.IsRunning)
                    ReopenText = runningTransaction.Type == "Collection" ? "Bottom Lock" : "Reopen Lock";
                else
                    ReopenText = "Reopen Lock";
            });

            if (runningTransaction.IsReady && data[(int)BinEnum.Yellow] == 0)
                await BinService.SwitchBinFeature(BinEnum.Yellow, true);
            if (runningTransaction.IsReady && data[(int)BinEnum.Green] == 1)
                await BinService.SwitchBinFeature(BinEnum.Green, false);

            if (runningTransaction.IsReady && runningTransaction.StartTime is not null  && runningTransaction.StartTime.Value.AddSeconds(30) <= DateTime.Now)
                AllowReopen = true;
            else
                AllowReopen = false;


            if (runningTransaction.IsRunning && runningTransaction.Type == "Collection")
                await Collection();
            else
                await Dispose();
            
            await Task.Delay(1000);
        }
    }
    private async Task Dispose()
    {
        var runningData = Manager.RunningTransactionData;
        if (!runningData.IsRunning || runningData.Type != "Dispose")
            return;
        if (runningData.Stage == 0 && data[(int)BinEnum.TopSensor] == 0)
        {
            runningData.Message = "Tutup Penutup Atas";
            runningData.TopSensor = "1";
            runningData.Stage = 1;
        }
        else if (runningData.Stage == 1 && data[(int)BinEnum.TopSensor]==1)
        {
            runningData.Message = "Lakukan Verifikasi";
            runningData.TopSensor = "0";
            runningData.Stage = 2;
        }
        await Manager.Save(runningData);
    }
    private async Task Collection()
    {
        var runningData = Manager.RunningTransactionData;
        if (!runningData.IsRunning || runningData.Type != "Collection")
            return;
        
        if (runningData.Stage == 0  && data[(int)BinEnum.BottomSensor]==0)
        {
            runningData.Message = "Tutup Penutup Bawah";
            runningData.BottomSensor = "1";
            runningData.Stage = 1;
            await Manager.Save(runningData);
        }
        else if (runningData.Stage==1 && data[(int)BinEnum.BottomSensor]==1)
        {
            await BinService.EndTransaction();
        }
    }
    [RelayCommand]
    public async Task Reopen()
    {
        var rt = Manager.RunningTransactionData;
        if (rt.Type is null)
            return;
        await BinService.SwitchBinFeature(rt.Type == "Collection" ? BinEnum.BottomLock : BinEnum.TopLock,true);
        AllowReopen = false;
        rt.StartTime = null;
        await Manager.Save(rt);
    }
}
