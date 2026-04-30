using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using TagHierarchyManager.UI.Services;
using TagHierarchyManager.UI.ViewModels;
using TagHierarchyManager.UI.Views;

namespace TagHierarchyManager.UI;

public class App : Application
{
    private TagDatabaseService? TagDatabaseService { get; set; }
    private DialogService? DialogService { get; set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (this.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            this.DisableAvaloniaDataAnnotationValidation();

            this.TagDatabaseService = new TagDatabaseService();
            this.DialogService = new DialogService();

            desktop.MainWindow = new MainWindow(this.DialogService)
            {
                DataContext = new MainWindowViewModel(this.TagDatabaseService, this.DialogService)
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove) BindingPlugins.DataValidators.Remove(plugin);
    }
}