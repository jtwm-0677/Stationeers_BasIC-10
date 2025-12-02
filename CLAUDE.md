# Basic-10 Compiler Project Instructions

## Project Overview
Basic-10 is a BASIC to IC10 (MIPS) compiler for the game Stationeers. It converts high-level BASIC code into optimized IC10 assembly for use in the game's IC chips.

## Technology Stack
- .NET 8.0 WPF application (Windows only)
- AvalonEdit for code editing
- WebView2 for documentation display
- ASP.NET Core for HTTP API (MCP integration)

## Key Files
- `BasicToMips.csproj` - Project configuration and version number
- `UI/MainWindow.xaml(.cs)` - Main application window
- `UI/SettingsWindow.xaml(.cs)` - Settings dialog
- `UI/Services/SettingsService.cs` - Settings persistence
- `src/Lexer/` - Tokenization
- `src/Parser/` - AST generation
- `src/CodeGen/MipsGenerator.cs` - IC10 code generation
- `docs/` - Documentation files (copied to output)
- `Data/` - Device databases and JSON files

## Context & Task Management
- Your context window will be automatically compacted as it approaches its limit, allowing you to continue working indefinitely from where you left off. Therefore, do not stop tasks early due to token budget concerns. As you approach your token budget limit, save your current progress and state to memory before the context window refreshes. Always be as persistent and autonomous as possible and complete tasks fully, even if the end of your budget is approaching. Never artificially stop any task early regardless of the context remaining.

## Critical Rules

### Version Management
- **ALWAYS increment the version number in BasicToMips.csproj when making changes or adding features**
- Version is in `<Version>` tag (currently 1.7.9)
- Package naming follows format: `BasicToMips_v{version}.zip`

### Running Compiler
- **NEVER kill the compiler process without asking the user first**
- The user may have unsaved work
- Always ask before terminating Basic_10.exe
- The packaged version runs separately from bin folder builds

### Build & Package Commands
```powershell
# Build
dotnet build -c Release

# Publish
dotnet publish -c Release -o publish

# Package (update version in filename)
Compress-Archive -Path './publish/*' -DestinationPath './BasicToMips_v1.7.9.zip' -Force
```

### Testing Changes
- User prefers to test from the packaged version, not bin folder
- Build changes won't affect running packaged instance
- Ask user to close and reopen from new package to test

## MCP Integration
- HTTP API runs on port 19410 by default
- Endpoints: `/api/code`, `/api/properties`
- Enabled/disabled via settings

## Stationeers Integration
- Scripts saved to Stationeers scripts folder
- Each script is a folder containing:
  - `script.ic10` - The compiled IC10 code
  - `instruction.xml` - Metadata (Title, Description, Author)
- Title in instruction.xml should match the folder name
- Description auto-appends "Built in Basic-10"

## Symbols Panel
Tracks and displays:
- Variables (VAR, LET)
- Constants (CONST, DEFINE)
- Labels (word:)
- Aliases (ALIAS, DEVICE)
- Arrays (DIM)

## Recent Features Added
- Arrays support (DIM statements)
- Script metadata settings (Author, Description)
- Auto-save functionality
- Dynamic device aliases
- Named device references
- Fixed theme switching (StaticResource → DynamicResource)
- Comprehensive MCP tools for simulator, debugging, editor state, settings, and code analysis

## Settings Location
Settings stored in: `%LOCALAPPDATA%\BasicToMips\settings.json`
