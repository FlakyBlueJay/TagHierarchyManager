using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using TagHierarchyManager.Common;
using TagHierarchyManager.Exporters;
using TagHierarchyManager.Importers;
using TagHierarchyManager.Models;
using TagHierarchyManager.UI.Assets;

namespace TagHierarchyManager.UI;

public class TagDatabaseService : ObservableObject
{
    public event EventHandler InitialisationComplete;

    public event EventHandler<Tag> TagAdded;
    public event EventHandler<(int id, string name)> TagDeleted;
    public event EventHandler<List<Tag>> TagsAdded;
    public event EventHandler<Tag> TagUpdated;

    public string DatabaseName => this.Database?.Name ?? string.Empty;
    public int DatabaseVersion => this.Database?.Version ?? 0;
    public List<string> DefaultTagBindings => this.Database.DefaultTagBindings;

    public bool IsDatabaseOpen => this.Database != null;

    public int TagCount => this.Database?.Tags.Count ?? 0;
    public int TagRelationshipCount => this.Database?.GetTagRelationshipCount() ?? 0;

    private TagDatabase? Database { get; set; }

    public void CloseDatabase()
    {
        if (this.Database == null) return;
        this.UnsubscribeFromEvents();
        this.Database = null;
        this.NotifyDatabasePropertiesChanged();
    }

    public async Task CreateNewDatabase(string filePath, string? templateFilePath = null)
    {
        if (this.Database != null)
            this.CloseDatabase();

        Dictionary<string, ImportedTag>? tagsToImport = null;
        TagDatabase db = new();
        db.InitialisationComplete += this.TagDatabase_OnInitialisationComplete;

        try
        {
            if (string.IsNullOrWhiteSpace(filePath)) return;

            if (templateFilePath is not null && File.Exists(templateFilePath))
            {
                var importer = this.PickImporterFromFileExt(templateFilePath);
                tagsToImport = await importer.ImportFromFileAsync(templateFilePath);
            }

            // overwrite is set to true here for now since the OS should handle the overwrite request.
            // will need to remove once Terminal.Gui is fully replaced.
            await Task.Run(() => db.CreateAsync(filePath, true, tagsToImport));
        }
        catch (Exception ex)
        {
            db.InitialisationComplete -= this.TagDatabase_OnInitialisationComplete;
            throw;
        }
    }

    public async Task DeleteTag(int id)
    {
        if (this.Database is null) return;
        await Task.Run(() => this.Database.DeleteTag(id));
    }

    public async Task ExportAsync(string path)
    {
        if (!this.IsDatabaseOpen || string.IsNullOrWhiteSpace(path)) return;
        var exporter = PickExporterFromFileExt(path);
        var exportContent = await Task.Run(() => exporter.ExportDatabase(this.Database!));
        await File.WriteAllTextAsync(path, exportContent);
    }

    public List<Tag> GetAllTagChildren(int id)
    {
        return this.Database is not null
            ? this.Database.Tags.Where(t => t.ParentIds.Contains(id)).OrderBy(t => t.Name).ToList()
            : [];
    }

    public List<Tag> GetAllTags(bool topLevelOnly = false)
    {
        if (this.Database is null) return [];

        return topLevelOnly
            ? this.Database.Tags.Where(t => t.IsTopLevel).OrderBy(t => t.Name).ToList()
            : this.Database.Tags.OrderBy(t => t.Name).ToList();
    }

    public List<string> GetParentNamesByIds(List<int> ids)
    {
        return ids.Select(id => this.Database?.Tags.FirstOrDefault(t => t.Id == id))
            .Where(tag => tag is not null)
            .Select(tag => tag!.Name)
            .ToList();
    }

    public async Task LoadDatabase(string filePath)
    {
        try
        {
            if (this.Database != null)
                this.CloseDatabase();

            TagDatabase db = new();
            db.InitialisationComplete += this.TagDatabase_OnInitialisationComplete;
            await Task.Run(() => db.LoadAsync(filePath));
        }
        catch (Exception ex)
        {
            // this.ShowErrorDialog(ex.Message);
        }
    }

    public List<Tag> SearchTags(string searchQuery, TagDatabaseSearchMode mode, bool searchAliases)
    {
        if (this.Database is null) return [];
        return searchAliases
            ? this.Database.SearchWithAliases(searchQuery, mode)
            : this.Database.Search(searchQuery, mode);
    }

    public void SetDefaultTagBindings(string input)
    {
        this.Database?.DefaultTagBindings = input.Split(";",
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        this.NotifyDatabasePropertiesChanged();
    }

    public async Task WriteTagsToDatabase(List<Tag> tags)
    {
        if (this.Database is null) return;
        await this.Database.WriteTagsToDatabase(tags);
    }

    private static IExporter PickExporterFromFileExt(string path)
    {
        var fileExt = Path.GetExtension(path);

        // ReSharper disable once ConvertIfStatementToReturnStatement
        if (fileExt == FileTypes.MusicBeeTagHierarchyTemplate.FileExtension) return new MusicBeeTagHierarchyExporter();

        throw new NotSupportedException($"File extension '{fileExt}' is not supported.");
    }

    private void NotifyDatabasePropertiesChanged()
    {
        Dispatcher.UIThread.Post(() =>
        {
            this.OnPropertyChanged(nameof(this.TagCount));
            this.OnPropertyChanged(nameof(this.DefaultTagBindings));
            this.OnPropertyChanged(nameof(this.DatabaseVersion));
            this.OnPropertyChanged(nameof(this.TagRelationshipCount));

            this.OnPropertyChanged(nameof(this.IsDatabaseOpen));
            this.OnPropertyChanged(nameof(this.DatabaseName));
        });
    }

    private Importer PickImporterFromFileExt(string path)
    {
        var fileExt = Path.GetExtension(path);

        // if more importers are added, convert this to a switch statement based on file extension.
        // ReSharper disable once ConvertIfStatementToReturnStatement
        if (fileExt == FileTypes.MusicBeeTagHierarchyTemplate.FileExtension)
            return new MusicBeeTagHierarchyImporter();

        throw new NotSupportedException(string.Format(Resources.ErrorImportFileTypeNotSupported, fileExt));
    }

    private void SubscribeToEvents()
    {
        if (this.Database is null) return;
        this.Database.TagAdded += this.TagDatabase_OnTagAdded;
        this.Database.TagsAdded += this.TagDatabase_OnTagsAdded;
        this.Database.TagUpdated += this.TagDatabase_OnTagUpdated;
        this.Database.TagDeleted += this.TagDatabase_OnTagDeleted;
    }

    // TODO this isn't activating and new database results in NullReferenceException.
    private void TagDatabase_OnInitialisationComplete(object? sender, EventArgs e)
    {
        this.Database = sender as TagDatabase;
        this.Database.InitialisationComplete -= this.TagDatabase_OnInitialisationComplete;
        this.SubscribeToEvents();
        this.NotifyDatabasePropertiesChanged();
        this.InitialisationComplete.Invoke(this, e);
    }

    private void TagDatabase_OnTagAdded(object? sender, Tag newTag)
    {
        this.TagAdded.Invoke(this, newTag);
        this.NotifyDatabasePropertiesChanged();
    }

    private void TagDatabase_OnTagDeleted(object? sender, (int id, string name) deletedTag)
    {
        this.TagDeleted.Invoke(this, deletedTag);
        this.NotifyDatabasePropertiesChanged();
    }

    private void TagDatabase_OnTagsAdded(object? sender, List<Tag> newTags)
    {
        this.TagsAdded.Invoke(this, newTags);
        this.NotifyDatabasePropertiesChanged();
    }

    private void TagDatabase_OnTagUpdated(object? sender, Tag updatedTag)
    {
        this.TagUpdated.Invoke(this, updatedTag);
        this.NotifyDatabasePropertiesChanged();
    }

    private void UnsubscribeFromEvents()
    {
        if (this.Database is null) return;
        this.Database.TagAdded -= this.TagDatabase_OnTagAdded;
        this.Database.TagsAdded -= this.TagDatabase_OnTagsAdded;
        this.Database.TagUpdated -= this.TagDatabase_OnTagUpdated;
        this.Database.TagDeleted -= this.TagDatabase_OnTagDeleted;
    }
}