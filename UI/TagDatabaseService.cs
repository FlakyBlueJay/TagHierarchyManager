using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using TagHierarchyManager.Common;
using TagHierarchyManager.Exporters;
using TagHierarchyManager.Importers;
using TagHierarchyManager.Models;
using TagHierarchyManager.UI.Assets;
using TagHierarchyManager.UI.ViewModels;
using TagHierarchyManager.UI.Views;

namespace TagHierarchyManager.UI;

public class TagDatabaseService : ObservableObject
{
    public event EventHandler? InitialisationComplete;


    public event EventHandler<TagWriteResult>? TagsWritten;

    public string DatabaseName => this.Database?.Name ?? string.Empty;
    public int DatabaseVersion => this.Database?.Version ?? 0;
    public List<string> DefaultTagBindings => this.Database?.DefaultTagBindings ?? [];

    public bool IsDatabaseOpen => this.Database != null;

    public int TagCount => this.Database?.Tags.Count ?? 0;
    public async Task<int> GetTagRelationshipCountAsync() =>
        await (this.Database?.GetTagRelationshipCountAsync() ?? Task.FromResult(0));

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
        catch
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

    public Dictionary<int, List<Tag>> GetChildLookup()
    {
        if (this.Database is null) return new Dictionary<int, List<Tag>>();
        var lookup = new Dictionary<int, List<Tag>>();
        foreach (var tag in this.Database.Tags)
        foreach (var parentId in tag.ParentIds)
        {
            if (!lookup.TryGetValue(parentId, out var children))
            {
                children = [];
                lookup[parentId] = children;
            }

            children.Add(tag);
        }

        foreach (var children in lookup.Values)
            children.Sort((a, b) => a.Name.CompareTo(b.Name, StringComparison.CurrentCultureIgnoreCase));
        return lookup;
    }

    public List<string> GetParentNamesByIds(List<int> ids)
    {
        return ids.Select(id => this.Database?.Tags.FirstOrDefault(t => t.Id == id))
            .Where(tag => tag is not null)
            .Select(tag => tag!.Name)
            .ToList();
    }

    public List<Tag> GetRecentTags(bool recentlyAdded)
    {
        if (this.Database is null) return [];
        return recentlyAdded
            ? this.Database.Tags.Where(t => t.CreatedAt is not null).OrderByDescending(t => t.CreatedAt).Take(50)
                .ToList()
            : this.Database.Tags.OrderByDescending(t => t.UpdatedAt).Take(50).ToList();
    }

    public Tag? GetTagById(int id)
    {
        return this.Database?.Tags.FirstOrDefault(t => t.Id == id);
    }

    public async Task LoadDatabase(string filePath)
    {
        TagDatabase db = new();
        try
        {
            if (this.Database != null)
                this.CloseDatabase();

            db.InitialisationComplete += this.TagDatabase_OnInitialisationComplete;
            await Task.Run(() => db.LoadAsync(filePath));
        }
        catch
        {
            db.InitialisationComplete -= this.TagDatabase_OnInitialisationComplete;
            throw;
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

    public async Task WriteTagsToDatabase(List<TagItemViewModel> tags)
    {
        if (this.Database is null) return;
        var tagsToSave = new List<(TagItemViewModel vm, Tag tag)>();
        foreach (var vm in tags)
        {
            var tag = new Tag
            {
                Id = vm.Id,
                Name = vm.EditingName,
                IsTopLevel = vm.EditingIsTopLevel,
                Aliases = vm.EditingAliases
                    .Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList(),
                TagBindings = vm.EditingTagBindings
                    .Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList(),
                Notes = vm.EditingNotes,
                Parents = vm.EditingParents
                    .Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList()
            };

            if (tags.Count == 1)
            {
                var success = await this.GetSavingTagParents(tag);
                if (!success) return;
            }

            tagsToSave.Add((vm, tag));
        }

        if (tags.Count > 1)
        {
            var sortedPairs = this.SortTagsTopologically(tagsToSave.Select(x => x.tag).ToList())
                .Select(t => tagsToSave.First(x => x.tag == t))
                .ToList();

            var transaction = await this.Database.BeginExternalTransactionAsync();

            try
            {
                foreach (var (_, tag) in sortedPairs)
                {
                    var success = await this.GetSavingTagParents(tag, tagsToSave.Select(x => x.tag).ToList());
                    if (!success)
                    {
                        await transaction.RollbackAsync();
                        return;
                    }

                    tag.Validate();
                    await this.Database.WriteTagToDatabase(tag, transaction);
                }

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
            finally
            {
                await transaction.DisposeAsync();
            }
        }
        else
        {
            await this.Database.WriteTagToDatabase(tagsToSave[0].tag);
        }

        tagsToSave.ForEach(pair => pair.vm.CommitEdit(pair.tag));
    }

    private static IExporter PickExporterFromFileExt(string path)
    {
        var fileExt = Path.GetExtension(path);

        // ReSharper disable once ConvertIfStatementToReturnStatement
        if (fileExt == FileTypes.MusicBeeTagHierarchyTemplate.FileExtension) return new MusicBeeTagHierarchyExporter();

        throw new NotSupportedException($"File extension '{fileExt}' is not supported.");
    }

    private async Task<bool> GetSavingTagParents(Tag tag, List<Tag>? batch = null)
    {
        if (this.Database is null) return false;
        if (tag.Parents.Count == 0) return true;
        foreach (var parentName in tag.Parents)
        {
            var parentTags = this.Database.Tags.Where(t => t.Name == parentName).ToList();
            switch (parentTags.Count)
            {
                case 0:
                    var match = batch?.FirstOrDefault(t => t.Name == parentName);
                    if (match is null) throw new Exception($"Parent tag '{parentName}' not found.");
                    tag.ParentIds.Add(match.Id);
                    continue;
                case 1:
                    tag.ParentIds.Add(parentTags[0].Id);
                    continue;
                case > 1:
                    var ambiguousVm = new SaveAmbiguousViewModel(this, tag, parentTags);

                    var dialog = new SaveAmbiguousDialog();
                    var dialogOwner =
                        (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)
                        ?.Windows
                        .FirstOrDefault(w => w.IsActive);
                    dialog.DataContext = ambiguousVm;
                    var result = await dialog.ShowDialog<TagItemViewModel?>(dialogOwner!);

                    if (result == null) return false;
                    tag.ParentIds.Add(result.Id);
                    break;
            }
        }

        return true;
    }

    private void NotifyDatabasePropertiesChanged()
    {
        Dispatcher.UIThread.Post(() =>
        {
            this.OnPropertyChanged(nameof(this.TagCount));
            this.OnPropertyChanged(nameof(this.DefaultTagBindings));
            this.OnPropertyChanged(nameof(this.DatabaseVersion));

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

    private List<Tag> SortTagsTopologically(List<Tag> tags)
    {
        var sorted = new List<Tag>();
        var checkedNames = new HashSet<Tag>();
        var checking = new HashSet<Tag>();

        void CheckParents(Tag tag)
        {
            if (checking.Contains(tag))
                throw new InvalidOperationException($"Circular parent depedency detected involving {tag.Name}");

            if (!checkedNames.Add(tag)) return;

            checking.Add(tag);

            foreach (var parentName in tag.Parents)
            {
                var parents = tags.Where(t => t.Name == parentName).ToList();
                if (parents.Count == 1)
                    CheckParents(parents[0]);
            }

            checking.Remove(tag);
            sorted.Add(tag);
        }

        foreach (var tag in tags)
            CheckParents(tag);

        return sorted;
    }

    private void SubscribeToEvents()
    {
        if (this.Database is null) return;
        this.Database.TagsWritten += this.TagDatabase_OnTagsWritten;
    }

    private void TagDatabase_OnInitialisationComplete(object? sender, EventArgs e)
    {
        this.Database = sender as TagDatabase;
        this.Database!.InitialisationComplete -= this.TagDatabase_OnInitialisationComplete;
        this.SubscribeToEvents();
        this.NotifyDatabasePropertiesChanged();
        this.InitialisationComplete?.Invoke(this, e);
    }

    private void TagDatabase_OnTagsWritten(object? sender, TagDatabase.DatabaseEditResult editResult)
    {
        var resultConverted = new TagWriteResult(editResult.Added, editResult.Updated, editResult.Deleted);
        this.TagsWritten?.Invoke(this, resultConverted);
    }

    private void UnsubscribeFromEvents()
    {
        if (this.Database is null) return;
        this.Database.TagsWritten -= this.TagDatabase_OnTagsWritten;
    }

    public bool ValidateUnique(TagItemViewModel tag)
    {
        if (this.Database is null) return false;
        return tag.EditingName == tag.CurrentName
               || this.Database.Tags.All(t => t.Name != tag.EditingName);
    }

    public sealed record TagWriteResult(
        IReadOnlyList<Tag> Added,
        IReadOnlyList<Tag> Updated,
        IReadOnlyList<(int id, string name)> Deleted
    );
}