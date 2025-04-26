﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using MFAAvalonia.Helper;
using MFAAvalonia.Helper.ValueType;
using System;

namespace MFAAvalonia.Views.UserControls;

public class HotKeyEditor : TemplatedControl
{
    private const string ElementButton = "PART_Button";
    public static readonly StyledProperty<MFAHotKey?> HotKeyProperty =
        AvaloniaProperty.Register<HotKeyEditor, MFAHotKey?>(
            nameof(HotKey), defaultBindingMode: BindingMode.TwoWay);

    public MFAHotKey? HotKey
    {
        get => GetValue(HotKeyProperty);
        set => SetValue(HotKeyProperty, value);
    }


    private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
    {
        var focusManager = TopLevel.GetTopLevel(this)?.FocusManager;
        var modifiers = e.KeyModifiers;
        var key = e.Key;
        if (modifiers == KeyModifiers.None)
        {
            HotKey = MFAHotKey.NOTSET;
            focusManager?.ClearFocus();
            return;
        }
        // 过滤无效按键（适配 Avalonia 的键值系统）
        if (key == Key.LeftCtrl
            || key == Key.RightCtrl
            || key == Key.RightAlt
            || key == Key.LeftShift
            || key == Key.RightShift
            || key == Key.LWin
            || key == Key.RWin
            || key == Key.Clear
            || key == Key.OemClear
            || key == Key.Apps)
        {
            return;
        }
        
        var gesture = new KeyGesture(key, modifiers);
        HotKey = new MFAHotKey(gesture);
        
        focusManager?.ClearFocus();
    }

    private void OnPressed(object sender, EventArgs e)
    {
        GlobalHotkeyService.Unregister(HotKey.Gesture);
        HotKey = MFAHotKey.PRESSING;
    }
    
    private Button? _button;
    
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (_button != null)
        {
            _button.Click -= OnPressed;
            _button.KeyDown -= OnPreviewKeyDown;
        }
        base.OnApplyTemplate(e);
        _button = e.NameScope.Find<Button>(ElementButton);

        if (_button == null)
            throw new InvalidOperationException("Missing required template parts");

        if (_button != null)
        {
            _button.Click += OnPressed;
            _button.KeyDown += OnPreviewKeyDown;
        }
    }
}
