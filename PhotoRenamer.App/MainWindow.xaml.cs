using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using PhotoRenamer.App.Models;
using PhotoRenamer.App.Services;

namespace PhotoRenamer.App;

public partial class MainWindow : Window
{
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".webp", ".tif", ".tiff", ".heic"
    };

    private readonly PeopleStore _peopleStore = new();
    private readonly ObservableCollection<PhotoFile> _files = [];
    private readonly ObservableCollection<FolderNode> _folderTree = [];
    private readonly List<PhotoFile> _scopeFiles = [];
    private readonly List<string> _people = [];

    private string? _currentFolder;
    private string? _selectedFolderPath;

    public MainWindow()
    {
        InitializeComponent();
        FileListBox.ItemsSource = _files;
        FolderTreeView.ItemsSource = _folderTree;
        FileSearchTextBox.Text = string.Empty;
        LoadPeople();
    }

    private void OpenFolder_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog();
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        _currentFolder = dialog.FolderName;
        FolderPathText.Text = _currentFolder;
        BuildFolderTree(_currentFolder);
    }

    private void BuildFolderTree(string rootFolder)
    {
        _folderTree.Clear();

        var rootNode = CreateFolderNode(rootFolder);
        _folderTree.Add(rootNode);

        SelectFolderNode(rootFolder);
    }

    private static FolderNode CreateFolderNode(string folder)
    {
        var node = new FolderNode { FullPath = folder };

        try
        {
            var subDirectories = Directory.EnumerateDirectories(folder)
                .OrderBy(static path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase);

            foreach (var subDirectory in subDirectories)
            {
                node.Children.Add(CreateFolderNode(subDirectory));
            }
        }
        catch
        {
            // Ignore folders we cannot enumerate.
        }

        return node;
    }

    private void SelectFolderNode(string folderPath)
    {
        var node = FindNodeByPath(_folderTree, folderPath);
        if (node is null)
        {
            return;
        }

        FolderTreeView.SelectedItemChanged -= FolderTreeView_OnSelectedItemChanged;
        var container = GetTreeViewItem(FolderTreeView, node);
        if (container is not null)
        {
            ExpandAncestors(container);
            container.IsSelected = true;
            container.BringIntoView();
        }

        FolderTreeView.SelectedItemChanged += FolderTreeView_OnSelectedItemChanged;
        _selectedFolderPath = node.FullPath;
        DirectoryRenameTextBox.Text = node.Name;
        LoadFilesForFolder(node.FullPath);
    }

    private static FolderNode? FindNodeByPath(IEnumerable<FolderNode> nodes, string fullPath)
    {
        foreach (var node in nodes)
        {
            if (string.Equals(node.FullPath, fullPath, StringComparison.OrdinalIgnoreCase))
            {
                return node;
            }

            var match = FindNodeByPath(node.Children, fullPath);
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }

    private static TreeViewItem? GetTreeViewItem(ItemsControl container, object item)
    {
        if (container.ItemContainerGenerator.ContainerFromItem(item) is TreeViewItem treeViewItem)
        {
            return treeViewItem;
        }

        foreach (var child in container.Items)
        {
            if (container.ItemContainerGenerator.ContainerFromItem(child) is not TreeViewItem childContainer)
            {
                continue;
            }

            childContainer.IsExpanded = true;
            var result = GetTreeViewItem(childContainer, item);
            if (result is not null)
            {
                return result;
            }
        }

        return null;
    }

    private static void ExpandAncestors(TreeViewItem item)
    {
        var parent = ItemsControl.ItemsControlFromItemContainer(item) as TreeViewItem;
        while (parent is not null)
        {
            parent.IsExpanded = true;
            parent = ItemsControl.ItemsControlFromItemContainer(parent) as TreeViewItem;
        }
    }

    private void FolderTreeView_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (FolderTreeView.SelectedItem is not FolderNode selectedFolder)
        {
            return;
        }

        _selectedFolderPath = selectedFolder.FullPath;
        DirectoryRenameTextBox.Text = selectedFolder.Name;
        LoadFilesForFolder(selectedFolder.FullPath);
    }

    private void IncludeSubdirectoriesCheckBox_OnChanged(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_selectedFolderPath))
        {
            return;
        }

        LoadFilesForFolder(_selectedFolderPath);
    }

    private void FileSearchTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyFileFilter();
    }

    private void LoadFilesForFolder(string folder)
    {
        _scopeFiles.Clear();

        IEnumerable<string> files;
        try
        {
            files = Directory.EnumerateFiles(
                folder,
                "*",
                IncludeSubdirectoriesCheckBox.IsChecked == true ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Unable to load files: {ex.Message}";
            _files.Clear();
            return;
        }

        _scopeFiles.AddRange(files
            .Where(static file => ImageExtensions.Contains(Path.GetExtension(file)))
            .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase)
            .Select(file => new PhotoFile { FullPath = file }));

        ApplyFileFilter();

        StatusText.Text = _scopeFiles.Count == 0
            ? "No image files found in selected folder scope."
            : $"Loaded {_scopeFiles.Count} image file(s).";

        if (_files.Count > 0)
        {
            FileListBox.SelectedIndex = 0;
        }
        else
        {
            CurrentFileText.Text = "Select a file to rename";
            RenameTextBox.Text = string.Empty;
            ClearPhotoPreview("Preview unavailable");
        }
    }

    private void ApplyFileFilter()
    {
        var query = FileSearchTextBox.Text.Trim();

        var filtered = string.IsNullOrWhiteSpace(query)
            ? _scopeFiles
            : _scopeFiles.Where(file => file.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();

        _files.Clear();
        foreach (var file in filtered)
        {
            _files.Add(file);
        }
    }

    private void FileListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FileListBox.SelectedItem is not PhotoFile selected)
        {
            CurrentFileText.Text = "Select a file to rename";
            RenameTextBox.Text = string.Empty;
            ClearPhotoPreview("Preview unavailable");
            return;
        }

        CurrentFileText.Text = $"Current file: {selected.FullPath}";
        RenameTextBox.Text = Path.GetFileNameWithoutExtension(selected.DisplayName);
        RenameTextBox.Focus();
        SelectEditablePortionForRename(selected);
        ShowPhotoPreview(selected.FullPath);
    }

    private void SelectEditablePortionForRename(PhotoFile selected)
    {
        var fullName = RenameTextBox.Text;
        if (string.IsNullOrWhiteSpace(fullName))
        {
            RenameTextBox.SelectAll();
            return;
        }

        var directoryName = Path.GetFileName(selected.DirectoryPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        if (string.IsNullOrWhiteSpace(directoryName))
        {
            RenameTextBox.SelectAll();
            return;
        }

        var prefix = directoryName + " ";
        if (!fullName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || fullName.Length == prefix.Length)
        {
            RenameTextBox.SelectAll();
            return;
        }

        RenameTextBox.Select(prefix.Length, fullName.Length - prefix.Length);
    }

    private void ShowPhotoPreview(string path)
    {
        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(path);
            image.EndInit();
            image.Freeze();

            PhotoPreviewImage.Source = image;
            PhotoPreviewImage.Visibility = Visibility.Visible;
            PhotoPreviewPlaceholder.Visibility = Visibility.Collapsed;
        }
        catch
        {
            ClearPhotoPreview("Preview unavailable");
        }
    }

    private void ClearPhotoPreview(string message)
    {
        PhotoPreviewImage.Source = null;
        PhotoPreviewImage.Visibility = Visibility.Collapsed;
        PhotoPreviewPlaceholder.Text = message;
        PhotoPreviewPlaceholder.Visibility = Visibility.Visible;
    }

    private void RenameButton_OnClick(object sender, RoutedEventArgs e)
    {
        RenameCurrentAndMoveNext();
    }

    private void RenameTextBox_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
        {
            return;
        }

        RenameCurrentAndMoveNext();
        e.Handled = true;
    }

    private void RenameCurrentAndMoveNext()
    {
        if (FileListBox.SelectedItem is not PhotoFile selected)
        {
            StatusText.Text = "Pick a file first.";
            return;
        }

        var candidateName = RenameTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(candidateName))
        {
            StatusText.Text = "File name cannot be empty.";
            return;
        }

        var safeName = string.Join("_", candidateName.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)).Trim();
        if (string.IsNullOrWhiteSpace(safeName))
        {
            StatusText.Text = "Name only had invalid characters.";
            return;
        }

        var destinationPath = Path.Combine(selected.DirectoryPath, safeName + selected.Extension);
        if (string.Equals(destinationPath, selected.FullPath, StringComparison.OrdinalIgnoreCase))
        {
            MoveSelectionNext();
            return;
        }

        if (File.Exists(destinationPath))
        {
            StatusText.Text = "A file with that name already exists.";
            return;
        }

        File.Move(selected.FullPath, destinationPath);

        var updated = new PhotoFile { FullPath = destinationPath };
        ReplacePhotoInCollections(selected.FullPath, updated);

        var index = FileListBox.SelectedIndex;
        _files[index] = updated;
        FileListBox.SelectedIndex = index;

        StatusText.Text = $"Renamed to {updated.DisplayName}";
        MoveSelectionNext();
    }

    private void ReplacePhotoInCollections(string oldPath, PhotoFile updated)
    {
        var scopeIndex = _scopeFiles.FindIndex(file => string.Equals(file.FullPath, oldPath, StringComparison.OrdinalIgnoreCase));
        if (scopeIndex >= 0)
        {
            _scopeFiles[scopeIndex] = updated;
        }
    }

    private void MoveSelectionNext()
    {
        if (_files.Count == 0)
        {
            return;
        }

        if (FileListBox.SelectedIndex < _files.Count - 1)
        {
            FileListBox.SelectedIndex += 1;
            return;
        }

        FileListBox.SelectedIndex = _files.Count - 1;
        RenameTextBox.Focus();
        RenameTextBox.SelectAll();
        StatusText.Text = "Reached last file.";
    }

    private void RenameDirectoryButton_OnClick(object sender, RoutedEventArgs e)
    {
        RenameSelectedDirectory();
    }

    private void DirectoryRenameTextBox_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
        {
            return;
        }

        RenameSelectedDirectory();
        e.Handled = true;
    }

    private void RenameSelectedDirectory()
    {
        if (string.IsNullOrWhiteSpace(_selectedFolderPath) || !Directory.Exists(_selectedFolderPath))
        {
            StatusText.Text = "Select a folder first.";
            return;
        }

        var parentDirectory = Path.GetDirectoryName(_selectedFolderPath);
        if (string.IsNullOrWhiteSpace(parentDirectory))
        {
            StatusText.Text = "Cannot rename this folder.";
            return;
        }

        var requestedName = DirectoryRenameTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(requestedName))
        {
            StatusText.Text = "Folder name cannot be empty.";
            return;
        }

        var safeName = string.Join("_", requestedName.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)).Trim();
        if (string.IsNullOrWhiteSpace(safeName))
        {
            StatusText.Text = "Folder name only had invalid characters.";
            return;
        }

        var destinationPath = Path.Combine(parentDirectory, safeName);
        if (string.Equals(destinationPath, _selectedFolderPath, StringComparison.OrdinalIgnoreCase))
        {
            StatusText.Text = "Folder name is unchanged.";
            return;
        }

        if (Directory.Exists(destinationPath))
        {
            StatusText.Text = "A folder with that name already exists.";
            return;
        }

        Directory.Move(_selectedFolderPath, destinationPath);

        var prependPrefix = Path.GetFileName(destinationPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var directFiles = Directory.EnumerateFiles(destinationPath, "*", SearchOption.TopDirectoryOnly)
            .Where(static file => ImageExtensions.Contains(Path.GetExtension(file)));

        var renamedCount = 0;
        var skippedCount = 0;

        foreach (var file in directFiles)
        {
            var currentNameWithoutExtension = Path.GetFileNameWithoutExtension(file);
            var newNameWithoutExtension = currentNameWithoutExtension.StartsWith(prependPrefix + " ", StringComparison.OrdinalIgnoreCase)
                ? currentNameWithoutExtension
                : $"{prependPrefix} {currentNameWithoutExtension}";

            var targetPath = Path.Combine(destinationPath, newNameWithoutExtension + Path.GetExtension(file));
            if (string.Equals(targetPath, file, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (File.Exists(targetPath))
            {
                skippedCount += 1;
                continue;
            }

            File.Move(file, targetPath);
            renamedCount += 1;
        }

        if (string.Equals(_currentFolder, _selectedFolderPath, StringComparison.OrdinalIgnoreCase))
        {
            _currentFolder = destinationPath;
            FolderPathText.Text = destinationPath;
        }

        BuildFolderTree(_currentFolder ?? destinationPath);
        SelectFolderNode(destinationPath);

        StatusText.Text = $"Renamed folder. Updated {renamedCount} direct file(s); skipped {skippedCount}.";
    }

    private void LoadPeople()
    {
        _people.Clear();
        _people.AddRange(_peopleStore.Load());
        RenderPeopleButtons();
    }

    private void AddPersonButton_OnClick(object sender, RoutedEventArgs e)
    {
        AddPersonFromInput();
    }

    private void NewPersonTextBox_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
        {
            return;
        }

        AddPersonFromInput();
        e.Handled = true;
    }

    private void AddPersonFromInput()
    {
        var person = NewPersonTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(person))
        {
            return;
        }

        if (_people.Contains(person, StringComparer.OrdinalIgnoreCase))
        {
            StatusText.Text = $"'{person}' already exists.";
            return;
        }

        _people.Add(person);
        _peopleStore.Save(_people);
        NewPersonTextBox.Text = string.Empty;
        RenderPeopleButtons();
        StatusText.Text = $"Added '{person}' to quick list.";
    }

    private void RenderPeopleButtons()
    {
        PeopleButtonsPanel.Children.Clear();

        foreach (var person in _people.OrderBy(static p => p, StringComparer.OrdinalIgnoreCase))
        {
            var button = new Button
            {
                Content = person,
                Margin = new Thickness(0, 0, 8, 8),
                Padding = new Thickness(10, 6, 10, 6),
                MinWidth = 90
            };

            button.Click += (_, _) =>
            {
                if (RenameTextBox.Text.Length > 0)
                {
                    RenameTextBox.Text += " ";
                }

                RenameTextBox.Text += person;
                RenameTextBox.CaretIndex = RenameTextBox.Text.Length;
                RenameTextBox.Focus();
            };

            PeopleButtonsPanel.Children.Add(button);
        }
    }
}
