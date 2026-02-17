# redesigned-journey

Photo Renamer is a Windows WPF desktop tool for quickly renaming picture files.

## What it currently does

- Open a folder and load image files in alphabetical order.
- Step through files and rename with keyboard-first flow (`Enter` to rename + move next).
- Store a reusable quick list of people names.
- Click person buttons to append names to the new filename text.
- Persist quick-name list across app restarts in `%LocalAppData%/PhotoRenamer/people.json`.

## Project layout

- `PhotoRenamer.sln` - solution file
- `PhotoRenamer.App` - WPF application

## Run

```bash
dotnet run --project PhotoRenamer.App/PhotoRenamer.App.csproj
```

> Note: This is a Windows-targeted WPF app (`net8.0-windows`).

## Versioning and releases

- The project uses semantic version tags in the format `vMAJOR.MINOR.PATCH`.
- Every push to `main` runs tests, calculates the next patch version, publishes the app, and creates a GitHub Release with a zipped `win-x64` build artifact.
- Pull requests to `main` run tests only.

- The app displays its current version in the bottom-right corner.
- The app can check GitHub for newer releases (startup check + **Check for updates** button). To enable this, set `GitHubRepository` in `PhotoRenamer.App/MainWindow.xaml.cs` to your `owner/repo`.
