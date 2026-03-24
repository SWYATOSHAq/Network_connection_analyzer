using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using Анализатор_сетевых_подключений.ViewModels;

namespace Анализатор_сетевых_подключений;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
        TryLoadBackground();
    }

    private void TryLoadBackground()
    {
        try
        {
            var uri = new Uri("avares://Анализатор_сетевых_подключений/Assets/background.png");
            var bitmap = new Bitmap(AssetLoader.Open(uri));
            BgImage.Source = bitmap;
        }
        catch
        {
            // Картинка не найдена — приложение работает без фона
        }
    }
}
