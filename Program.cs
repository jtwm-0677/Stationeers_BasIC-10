using BasicToMips.Lexer;
using BasicToMips.Parser;
using BasicToMips.CodeGen;

namespace BasicToMips;

class Program
{
    static int Main(string[] args)
    {
        Console.WriteLine("BASIC to Stationeers MIPS Compiler v1.0");
        Console.WriteLine("======================================");
        Console.WriteLine();

        if (args.Length == 0)
        {
            PrintUsage();
            return 1;
        }

        string? inputFile = null;
        string? outputFile = null;
        bool verbose = false;
        bool showAst = false;
        bool showTokens = false;

        // Parse command line arguments
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLowerInvariant())
            {
                case "-h":
                case "--help":
                    PrintUsage();
                    return 0;

                case "-o":
                case "--output":
                    if (i + 1 < args.Length)
                    {
                        outputFile = args[++i];
                    }
                    else
                    {
                        Console.Error.WriteLine("Error: -o requires an output file path");
                        return 1;
                    }
                    break;

                case "-v":
                case "--verbose":
                    verbose = true;
                    break;

                case "--tokens":
                    showTokens = true;
                    break;

                case "--ast":
                    showAst = true;
                    break;

                default:
                    if (args[i].StartsWith("-"))
                    {
                        Console.Error.WriteLine($"Error: Unknown option '{args[i]}'");
                        return 1;
                    }
                    inputFile = args[i];
                    break;
            }
        }

        if (inputFile == null)
        {
            Console.Error.WriteLine("Error: No input file specified");
            PrintUsage();
            return 1;
        }

        // Check if input file exists
        if (!File.Exists(inputFile))
        {
            Console.Error.WriteLine($"Error: Input file '{inputFile}' not found");
            return 1;
        }

        try
        {
            // Read source code
            var source = File.ReadAllText(inputFile);
            if (verbose)
            {
                Console.WriteLine($"Compiling: {inputFile}");
                Console.WriteLine($"Source size: {source.Length} bytes");
                Console.WriteLine();
            }

            // Lexical analysis
            if (verbose) Console.WriteLine("Tokenizing...");
            var lexer = new Lexer.Lexer(source);
            var tokens = lexer.Tokenize();

            if (showTokens)
            {
                Console.WriteLine("Tokens:");
                Console.WriteLine("-------");
                foreach (var token in tokens)
                {
                    Console.WriteLine($"  {token}");
                }
                Console.WriteLine();
            }

            if (verbose) Console.WriteLine($"  Found {tokens.Count} tokens");

            // Parsing
            if (verbose) Console.WriteLine("Parsing...");
            var parser = new Parser.Parser(tokens);
            var program = parser.Parse();

            if (showAst)
            {
                Console.WriteLine("AST:");
                Console.WriteLine("----");
                PrintAst(program);
                Console.WriteLine();
            }

            if (verbose) Console.WriteLine($"  Found {program.Statements.Count} statements");

            // Code generation
            if (verbose) Console.WriteLine("Generating MIPS code...");
            var generator = new MipsGenerator();
            var mipsCode = generator.Generate(program);

            if (verbose)
            {
                var lineCount = mipsCode.Split('\n').Length;
                Console.WriteLine($"  Generated {lineCount} lines of MIPS");
            }

            // Output
            if (outputFile != null)
            {
                File.WriteAllText(outputFile, mipsCode);
                Console.WriteLine($"Output written to: {outputFile}");
            }
            else
            {
                // Default output file name
                outputFile = Path.ChangeExtension(inputFile, ".ic10");
                File.WriteAllText(outputFile, mipsCode);
                Console.WriteLine($"Output written to: {outputFile}");
            }

            Console.WriteLine();
            Console.WriteLine("Compilation successful!");
            return 0;
        }
        catch (LexerException ex)
        {
            Console.Error.WriteLine($"Lexer error: {ex.Message}");
            return 1;
        }
        catch (ParserException ex)
        {
            Console.Error.WriteLine($"Parser error: {ex.Message}");
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            if (verbose)
            {
                Console.Error.WriteLine(ex.StackTrace);
            }
            return 1;
        }
    }

    static void PrintUsage()
    {
        Console.WriteLine("Usage: BasicToMips <input.bas> [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -o, --output <file>  Specify output file (default: input.ic10)");
        Console.WriteLine("  -v, --verbose        Show detailed compilation progress");
        Console.WriteLine("  --tokens             Show lexer tokens");
        Console.WriteLine("  --ast                Show abstract syntax tree");
        Console.WriteLine("  -h, --help           Show this help message");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  BasicToMips program.bas");
        Console.WriteLine("  BasicToMips program.bas -o output.ic10 -v");
        Console.WriteLine();
        Console.WriteLine("The output file contains Stationeers MIPS (IC10) assembly code");
        Console.WriteLine("that can be pasted into an IC10 chip in the game.");
    }

    static void PrintAst(AST.ProgramNode program, int indent = 0)
    {
        var prefix = new string(' ', indent * 2);
        Console.WriteLine($"{prefix}Program ({program.Statements.Count} statements)");

        foreach (var stmt in program.Statements)
        {
            PrintStatement(stmt, indent + 1);
        }
    }

    static void PrintStatement(AST.StatementNode stmt, int indent)
    {
        var prefix = new string(' ', indent * 2);
        var lineNum = stmt.BasicLineNumber.HasValue ? $"[{stmt.BasicLineNumber}] " : "";

        switch (stmt)
        {
            case AST.LetStatement let:
                Console.WriteLine($"{prefix}{lineNum}LET {let.VariableName} = ...");
                break;
            case AST.PrintStatement print:
                Console.WriteLine($"{prefix}{lineNum}PRINT ({print.Expressions.Count} expressions)");
                break;
            case AST.IfStatement ifStmt:
                Console.WriteLine($"{prefix}{lineNum}IF ... THEN ({ifStmt.ThenBranch.Count} statements)");
                if (ifStmt.ElseBranch.Count > 0)
                {
                    Console.WriteLine($"{prefix}  ELSE ({ifStmt.ElseBranch.Count} statements)");
                }
                break;
            case AST.ForStatement forStmt:
                Console.WriteLine($"{prefix}{lineNum}FOR {forStmt.VariableName} = ... TO ... ({forStmt.Body.Count} statements)");
                break;
            case AST.WhileStatement whileStmt:
                Console.WriteLine($"{prefix}{lineNum}WHILE ... ({whileStmt.Body.Count} statements)");
                break;
            case AST.GotoStatement gotoStmt:
                Console.WriteLine($"{prefix}{lineNum}GOTO {gotoStmt.TargetLine}");
                break;
            case AST.GosubStatement gosubStmt:
                Console.WriteLine($"{prefix}{lineNum}GOSUB {gosubStmt.TargetLine}");
                break;
            case AST.ReturnStatement:
                Console.WriteLine($"{prefix}{lineNum}RETURN");
                break;
            case AST.EndStatement:
                Console.WriteLine($"{prefix}{lineNum}END");
                break;
            default:
                Console.WriteLine($"{prefix}{lineNum}{stmt.GetType().Name}");
                break;
        }
    }
}
