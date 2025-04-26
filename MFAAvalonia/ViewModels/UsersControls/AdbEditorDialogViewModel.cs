﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaaFramework.Binding;
using MFAAvalonia.Extensions;
using MFAAvalonia.Helper;
using MFAAvalonia.Views.UserControls;
using SukiUI.Dialogs;
using System.Threading.Tasks;

namespace MFAAvalonia.ViewModels.UsersControls;

public partial class AdbEditorDialogViewModel : ObservableObject
{
    [ObservableProperty] private string _adbName = "Emulator".ToLocalization();
    [ObservableProperty] private string _adbPath = string.Empty;
    [ObservableProperty] private string _adbSerial = string.Empty;
    [ObservableProperty] private string _adbConfig = "{}";
    public ISukiDialog Dialog { get; set; }
    public AdbEditorDialogViewModel(AdbDeviceInfo? info, ISukiDialog dialog)
    {
        AdbName = info?.Name ?? AdbName;
        AdbPath = info?.AdbPath ?? AdbPath;
        AdbSerial = info?.AdbSerial ?? AdbSerial;
        AdbConfig = info?.Config ?? AdbConfig;
        Dialog = dialog;
    }

    [RelayCommand]
    async private Task Load()
    {
        var storageProvider = Instances.RootView.StorageProvider;

        // 配置文件选择器选项
        var options = new FilePickerOpenOptions
        {
            Title = "LoadFileTitle".ToLocalization(),
            FileTypeFilter =
            [
                new FilePickerFileType("AllFilter".ToLocalization())
                {
                    Patterns = ["*"] // 支持所有文件类型
                }
            ]
        };
        
        var result = await storageProvider.OpenFilePickerAsync(options);

        // 处理选择结果
        if (result is { Count: > 0 } && result[0].TryGetLocalPath() is { } path)
        {
            AdbPath = path;
        }
    }

    [RelayCommand]
    public void Save()
    {
        Instances.TaskQueueViewModel.Devices = [Output];
        Instances.TaskQueueViewModel.CurrentDevice = Output;

        Dialog.Dismiss();
    }

    public AdbDeviceInfo Output => new(AdbName, AdbPath, AdbSerial, AdbScreencapMethods.Default,
        AdbInputMethods.MinitouchAndAdbKey, AdbConfig);
}
