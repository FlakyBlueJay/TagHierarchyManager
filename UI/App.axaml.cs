using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using TagHierarchyManager.UI.ViewModels;
using TagHierarchyManager.UI.Views;

namespace TagHierarchyManager.UI;

public class App : Application
{
    private TagDatabaseService? TagDatabaseService { get; set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (this.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            this.TagDatabaseService = new TagDatabaseService();

            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(this.TagDatabaseService)
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}