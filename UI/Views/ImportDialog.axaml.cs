using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using TagHierarchyManager.UI.ViewModels;

namespace TagHierarchyManager.UI.Views;

public partial class ImportDialog : Window
{
    public ImportDialog()
    {
        InitializeComponent();
        this.DataContextChanged += (s, e) =>
        {
            if (this.ViewModel != null)
            {
                this.ViewModel.RequestClose += this.Close;
            }
        };
    }
    
    private ImportDialogViewModel? ViewModel => this.DataContext as ImportDialogViewModel;
    
    public void ButtonCancel_Click(object? sender, RoutedEventArgs e) => this.Close();

    public async void ButtonBrowseTemplate_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var files = await this.StorageProvider.OpenFilePickerAsync(
                new FilePickerOpenOptions
                {
                    AllowMultiple = false,
                    Title = Assets.Resources.DialogTitleChooseImportTemplate,
                    FileTypeFilter = [Common.MusicBeeTagHierarchy]
                });
            if (files.Count == 0) return;
            var path = files[0].TryGetLocalPath();
            if (path == null) return;
            this.ViewModel?.TemplateFilePath = path;
        }
        catch (Exception ex)
        {
            this.ViewModel?.TemplateFilePath = string.Empty;
            var error = new ErrorDialogViewModel(ex.Message);
            error.ShowDialog();
        }
    }
    
    public async void ButtonBrowseDatabase_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var file = await this.StorageProvider.SaveFilePickerAsync(
                new FilePickerSaveOptions
                {
                    Title = Assets.Resources.DialogTitleSaveDatabaseAs,
                    FileTypeChoices = [Common.TagDatabaseFileType]
                });
            var path = file?.TryGetLocalPath();
            if (path == null) return;
            this.ViewModel?.DatabaseFilePath = path;
        }
        catch (Exception ex)
        {
            this.ViewModel?.TemplateFilePath = string.Empty;
            var error = new ErrorDialogViewModel(ex.Message);
            error.ShowDialog();
        }
    }

    public async void ButtonImport_Click(object? sender, RoutedEventArgs e)
    {
        await this.ViewModel.InitiateImport();
    }
}