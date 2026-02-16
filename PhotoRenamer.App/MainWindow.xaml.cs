using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
    private readonly List<string> _people = [];

    private string? _currentFolder;

    public MainWindow()
    {
        InitializeComponent();
        FileListBox.ItemsSource = _files;
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
        LoadFiles(_currentFolder);
    }

    private void LoadFiles(string folder)
    {
        _files.Clear();

        var files = Directory.EnumerateFiles(folder)
            .Where(static file => ImageExtensions.Contains(Path.GetExtension(file)))
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .Select(file => new PhotoFile { FullPath = file });

        foreach (var file in files)
        {
            _files.Add(file);
        }

        StatusText.Text = _files.Count == 0
            ? "No image files found in folder."
            : $"Loaded {_files.Count} image file(s).";

        if (_files.Count > 0)
        {
            FileListBox.SelectedIndex = 0;
        }
        else
        {
            CurrentFileText.Text = "Select a file to rename";
            RenameTextBox.Text = string.Empty;
        }
    }

    private void FileListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FileListBox.SelectedItem is not PhotoFile selected)
        {
            return;
        }

        CurrentFileText.Text = $"Current file: {selected.DisplayName}";
        RenameTextBox.Text = Path.GetFileNameWithoutExtension(selected.DisplayName);
        RenameTextBox.Focus();
        RenameTextBox.SelectAll();
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
        var index = FileListBox.SelectedIndex;
        _files[index] = updated;

        StatusText.Text = $"Renamed to {updated.DisplayName}";
        MoveSelectionNext();
    }

    private void MoveSelectionNext()
    {
        if (FileListBox.SelectedIndex < _files.Count - 1)
        {
            FileListBox.SelectedIndex += 1;
        }
        else
        {
            StatusText.Text = "Reached last file.";
        }
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
