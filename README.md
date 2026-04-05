# LocalPass

LocalPass is a local-first password manager for the terminal built with .NET 10.
It stores secrets in an encrypted vault on disk, opens them in a text UI, and keeps local snapshots of previous vault versions.

## What It Does

- Creates a new encrypted vault on first launch.
- Unlocks an existing vault with a master password.
- Lets you add, edit, delete, and inspect secrets in a terminal UI.
- Re-encrypts the vault when the master password changes.
- Shows vault metadata in the app, including the last write time and document revision.
- Creates snapshot files when replacing an existing vault file.

## Solution Layout

- `src/LocalPass`: application entry point and dependency wiring.
- `src/Logic`: top-level workflow that coordinates unlock and UI execution.
- `src/Infrastructure`: encrypted file storage, terminal screens, and TUI rendering.
- `src/Models`: immutable domain models for vaults and secrets.
- `src/Abstractions`: shared interfaces between layers.
- `src/*Tests`: unit tests for each layer.

## Requirements

- .NET SDK 10.0
- Windows terminal environment

## Run

From the repository root:

```powershell
dotnet run --project .\src\LocalPass\LocalPass.csproj
```

On first launch, LocalPass asks you to create a master password.
Requirements are enforced by the app: 16 or more characters, uppercase, lowercase, digit, symbol, and no whitespace.

## Test

Run the full test suite:

```powershell
dotnet test .\src\LocalPass.slnx --no-restore
```

## Storage

By default, the encrypted vault is stored in:

```text
%LOCALAPPDATA%\LocalPass
```

You can override the storage directory through `appsettings.json`:

```json
{
  "LocalPass": {
    "StorageDirectoryPath": "%USERPROFILE%\\Dropbox\\LocalPass"
  }
}
```

If the value is empty, LocalPass keeps using the default `%LOCALAPPDATA%\\LocalPass` location.
Environment variables are expanded automatically.

Important files:

- `vault.localpass`: current encrypted vault.
- `snapshots\*.localpass`: previous encrypted versions created during replacement saves.

The vault payload is encrypted locally with:

- PBKDF2-SHA512
- AES-256-GCM

## Using The App

After unlocking the vault, the terminal UI supports these actions:

- `N`: create a new secret
- `E`: edit the selected secret
- `D`: delete the selected secret
- `O`: open the storage folder
- `P`: reveal or hide passwords
- `R`: rotate the master password
- `Esc`: exit

The summary line shows:

- number of records in the vault
- last write time in UTC
- current document revision
- current selection

## Notes

- LocalPass is local-first. There is no sync service or remote backend in this repository.
- Existing vault payloads remain compatible when new metadata fields are introduced with defaults.
