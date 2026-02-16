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
