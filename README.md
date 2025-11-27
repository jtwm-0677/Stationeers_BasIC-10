# BASIC to IC10 Compiler for Stationeers

A professional Windows application that compiles BASIC programming language code into optimized IC10 MIPS assembly for [Stationeers](https://store.steampowered.com/app/544550/Stationeers/).

![Screenshot](docs/screenshot.png)

## Features

### Modern IDE Experience
- **Professional dark-themed interface** - Easy on the eyes during long coding sessions
- **Split-pane editor** - BASIC source on top, IC10 output below
- **Real-time line counts** - Track both BASIC lines and IC10 output (with 128-line limit warnings)
- **Syntax highlighting** - Full color coding for BASIC and MIPS code
- **IntelliSense auto-complete** - Press Ctrl+Space for smart suggestions
- **Code snippets** - Insert common patterns quickly

### Powerful Compiler
- **Full BASIC language support** - Variables, expressions, control flow, functions
- **Automatic register allocation** - No manual memory management needed
- **Multiple optimization levels** - From fast compile to aggressive size optimization
- **Real-time error reporting** - Catch mistakes as you type

### Stationeers Integration
- **Auto-detect game directory** - Finds your Stationeers installation automatically
- **One-click deployment** - Save & Deploy sends code directly to the game
- **Auto-compile on save** - IC10 output updates automatically when you save

### Comprehensive Documentation
- **Built-in quick reference** - Always-visible documentation panel
- **Language reference** - Complete BASIC-IC10 language guide
- **Example programs** - Learn from working code samples
- **Context-sensitive help** - Press F1 anytime

## Installation

### Prerequisites
- Windows 10/11 (64-bit)
- [.NET 8.0 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) (or use self-contained build)

### Download
Download the latest release from the [Releases](../../releases) page.

### Building from Source

```bash
# Clone the repository
git clone https://github.com/your-repo/BasicToMips.git
cd BasicToMips

# Build for development
dotnet build

# Build release version
dotnet build -c Release

# Create standalone Windows executable (no .NET required)
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

The standalone executable will be in `bin/Release/net8.0-windows/win-x64/publish/`.

## Quick Start

1. **Launch the application**
2. **Write your BASIC code** in the top editor
3. **Press F5** or click **Compile** to generate IC10 code
4. **Click Save & Deploy** to send the code to Stationeers

### Your First Program

```basic
' Temperature Controller
ALIAS sensor d0
ALIAS heater d1

CONST TARGET = 20

main:
    VAR temp = sensor.Temperature

    IF temp < TARGET THEN
        heater.On = 1
    ELSE
        heater.On = 0
    ENDIF

    YIELD
    GOTO main
END
```

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| Ctrl+N | New file |
| Ctrl+O | Open file |
| Ctrl+S | Save file |
| Ctrl+Shift+S | Save as |
| F5 | Compile |
| F6 | Compile and copy to clipboard |
| Ctrl+Space | Show auto-complete |
| F1 | Show documentation |
| Ctrl+Z/Y | Undo/Redo |
| Ctrl+F | Find |
| Ctrl+H | Replace |

## BASIC-IC10 Language

### Variables & Constants

```basic
VAR temperature = 0      ' Variable declaration
CONST MAX_TEMP = 100     ' Compile-time constant
ALIAS sensor d0          ' Device alias
```

### Control Flow

```basic
' Conditionals
IF condition THEN
    ' code
ELSEIF other_condition THEN
    ' code
ELSE
    ' code
ENDIF

' Loops
FOR i = 1 TO 10 STEP 2
    ' code
NEXT i

WHILE condition
    ' code
    YIELD
ENDWHILE

' Subroutines
GOSUB my_routine
END

my_routine:
    ' code
    RETURN
```

### Device Access

```basic
' Read device property
VAR temp = sensor.Temperature

' Write device property
heater.On = 1

' Common properties
' Temperature, Pressure, Power, On, Open, Lock
' Setting, Ratio, Quantity, Occupied, Mode, Charge
```

### Built-in Functions

| Category | Functions |
|----------|-----------|
| Math | ABS, SQRT, SIN, COS, TAN, ASIN, ACOS, ATAN, ATAN2 |
| Rounding | CEIL, FLOOR, ROUND, TRUNC, INT |
| Comparison | MIN, MAX, SGN |
| Other | EXP, LOG, RND, POW |
| Control | YIELD, SLEEP, END |

## Project Structure

```
BasicToMips/
├── src/
│   ├── Lexer/          # Tokenization
│   ├── Parser/         # AST generation
│   ├── AST/            # Abstract syntax tree
│   └── CodeGen/        # IC10 code generation
├── Editor/
│   ├── Highlighting/   # Syntax highlighting
│   └── Completion/     # Auto-complete
├── UI/
│   ├── Themes/         # Dark theme resources
│   ├── Services/       # App services
│   └── *.xaml          # Window definitions
└── examples/           # Sample BASIC programs
```

## Contributing

Contributions are welcome! Please feel free to submit issues and pull requests.

## License

MIT License - See [LICENSE](LICENSE) for details.

## Acknowledgments

- Built with [AvalonEdit](https://github.com/icsharpcode/AvalonEdit) for the code editor
- Inspired by the Stationeers community's automation needs
