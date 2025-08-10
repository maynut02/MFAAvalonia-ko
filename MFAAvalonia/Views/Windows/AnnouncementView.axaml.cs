using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using SukiUI.Controls;

namespace MFAAvalonia.Views.Windows;

public partial class AnnouncementView : SukiWindow
{
    public AnnouncementView()
    {
        InitializeComponent();
    }
    
    private void Close(object sender, RoutedEventArgs e) => Close();
}

