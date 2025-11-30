using System.Windows.Media;
using ICSharpCode.AvalonEdit.Highlighting;

namespace BasicToMips.Editor.Highlighting;

public static class BasicHighlighting
{
    private static SyntaxColorSettings _colors = new();

    public static void SetColors(SyntaxColorSettings colors)
    {
        _colors = colors;
    }

    public static IHighlightingDefinition Create()
    {
        return Create(_colors);
    }

    public static IHighlightingDefinition Create(SyntaxColorSettings colors)
    {
        var definition = new CustomHighlightingDefinition();

        var keywordColor = colors.GetKeywordsColor();
        var typeColor = colors.GetDeclarationsColor();
        var stringColor = colors.GetStringsColor();
        var commentColor = colors.GetCommentsColor();
        var numberColor = colors.GetNumbersColor();
        var operatorColor = colors.GetOperatorsColor();
        var functionColor = colors.GetFunctionsColor();
        var propertyColor = colors.GetPropertiesColor();
        var labelColor = colors.GetLabelsColor();
        var deviceRefColor = colors.GetDeviceRefsColor();
        var booleanColor = colors.GetBooleansColor();
        var bracketColor = colors.GetBracketsColor();

        // Keywords
        definition.AddRule(new HighlightingRule
        {
            Pattern = @"\b(IF|THEN|ELSE|ELSEIF|ENDIF|END IF|FOR|TO|STEP|NEXT|WHILE|WEND|ENDWHILE|DO|LOOP|UNTIL|GOTO|GOSUB|RETURN|END|SUB|ENDSUB|END SUB|FUNCTION|ENDFUNCTION|CALL|EXIT|BREAK|CONTINUE)\b",
            Color = new HighlightingColor { Foreground = new SimpleHighlightingBrush(keywordColor) },
            IgnoreCase = true
        });

        // Declaration keywords
        definition.AddRule(new HighlightingRule
        {
            Pattern = @"\b(VAR|CONST|DIM|ARRAY|LET|ALIAS|DEFINE|AS|INTEGER|SINGLE|BOOLEAN)\b",
            Color = new HighlightingColor { Foreground = new SimpleHighlightingBrush(typeColor) },
            IgnoreCase = true
        });

        // Control keywords
        definition.AddRule(new HighlightingRule
        {
            Pattern = @"\b(YIELD|SLEEP|PRINT|INPUT|REM)\b",
            Color = new HighlightingColor { Foreground = new SimpleHighlightingBrush(keywordColor) },
            IgnoreCase = true
        });

        // Boolean literals
        definition.AddRule(new HighlightingRule
        {
            Pattern = @"\b(TRUE|FALSE)\b",
            Color = new HighlightingColor { Foreground = new SimpleHighlightingBrush(booleanColor) },
            IgnoreCase = true
        });

        // Logical operators
        definition.AddRule(new HighlightingRule
        {
            Pattern = @"\b(AND|OR|NOT|MOD)\b",
            Color = new HighlightingColor { Foreground = new SimpleHighlightingBrush(keywordColor) },
            IgnoreCase = true
        });

        // Built-in functions
        definition.AddRule(new HighlightingRule
        {
            Pattern = @"\b(ABS|SIN|COS|TAN|ASIN|ACOS|ATAN|ATAN2|SQRT|SQR|EXP|LOG|LOG10|CEIL|FLOOR|ROUND|TRUNC|INT|FIX|MIN|MAX|RND|RAND|SGN|POW|IIF|INRANGE|LERP)\b",
            Color = new HighlightingColor { Foreground = new SimpleHighlightingBrush(functionColor) },
            IgnoreCase = true
        });

        // Device references
        definition.AddRule(new HighlightingRule
        {
            Pattern = @"\b(d[0-5]|db)\b",
            Color = new HighlightingColor { Foreground = new SimpleHighlightingBrush(deviceRefColor) },
            IgnoreCase = true
        });

        // Common device properties
        definition.AddRule(new HighlightingRule
        {
            Pattern = @"\.(Temperature|Pressure|Power|On|Off|Open|Lock|Setting|Ratio|Quantity|Occupied|Mode|Error|Charge|SolarAngle|Activate|Vertical|Horizontal)\b",
            Color = new HighlightingColor { Foreground = new SimpleHighlightingBrush(propertyColor) },
            IgnoreCase = true
        });

        // Labels (word followed by colon at line start)
        definition.AddRule(new HighlightingRule
        {
            Pattern = @"^\s*\w+:",
            Color = new HighlightingColor { Foreground = new SimpleHighlightingBrush(labelColor) }
        });

        // Strings
        definition.AddRule(new HighlightingRule
        {
            Pattern = "\"[^\"]*\"",
            Color = new HighlightingColor { Foreground = new SimpleHighlightingBrush(stringColor) }
        });

        // Numbers
        definition.AddRule(new HighlightingRule
        {
            Pattern = @"\b\d+\.?\d*\b",
            Color = new HighlightingColor { Foreground = new SimpleHighlightingBrush(numberColor) }
        });

        // Brackets [ ] for array/batch access
        definition.AddRule(new HighlightingRule
        {
            Pattern = @"[\[\]]",
            Color = new HighlightingColor { Foreground = new SimpleHighlightingBrush(bracketColor) }
        });

        // Comments (BASIC style: ' and REM)
        definition.AddRule(new HighlightingRule
        {
            Pattern = @"'.*$",
            Color = new HighlightingColor { Foreground = new SimpleHighlightingBrush(commentColor) }
        });

        definition.AddRule(new HighlightingRule
        {
            Pattern = @"\bREM\b.*$",
            Color = new HighlightingColor { Foreground = new SimpleHighlightingBrush(commentColor) },
            IgnoreCase = true
        });

        // IC10 style comments (# comment) - for hybrid mode
        definition.AddRule(new HighlightingRule
        {
            Pattern = @"#.*$",
            Color = new HighlightingColor { Foreground = new SimpleHighlightingBrush(commentColor) }
        });

        // IC10 instructions (for hybrid mode)
        definition.AddRule(new HighlightingRule
        {
            Pattern = @"\b(move|add|sub|mul|div|mod|and|or|xor|nor|slt|sgt|seq|sne|sle|sge|beq|bne|blt|bgt|ble|bge|beqz|bnez|bgtz|bltz|bgez|blez|j|jal|jr|push|pop|peek|yield|sleep|hcf|select)\b",
            Color = new HighlightingColor { Foreground = new SimpleHighlightingBrush(keywordColor) }
        });

        // IC10 registers (for hybrid mode)
        definition.AddRule(new HighlightingRule
        {
            Pattern = @"\br([0-9]|1[0-5])\b",
            Color = new HighlightingColor { Foreground = new SimpleHighlightingBrush(deviceRefColor) }
        });

        return definition;
    }
}

public static class MipsHighlighting
{
    private static SyntaxColorSettings _colors = new();

    public static void SetColors(SyntaxColorSettings colors)
    {
        _colors = colors;
    }

    public static IHighlightingDefinition Create()
    {
        return Create(_colors);
    }

    public static IHighlightingDefinition Create(SyntaxColorSettings colors)
    {
        var definition = new CustomHighlightingDefinition();

        var instructionColor = colors.GetKeywordsColor();
        var registerColor = colors.GetDeviceRefsColor();
        var labelColor = colors.GetLabelsColor();
        var numberColor = colors.GetNumbersColor();
        var commentColor = colors.GetCommentsColor();
        var directiveColor = colors.GetDeclarationsColor();

        // Instructions
        definition.AddRule(new HighlightingRule
        {
            Pattern = @"\b(move|add|sub|mul|div|mod|and|or|xor|nor|slt|sgt|seq|sne|sle|sge|beq|bne|blt|bgt|ble|bge|bna|bnan|beqz|bnez|bgtz|bltz|bgez|blez|j|jal|jr|l|s|ls|ss|lb|sb|lr|sr|push|pop|peek|poke|get|put|getd|putd|alias|define|yield|sleep|hcf|abs|ceil|floor|round|trunc|sqrt|exp|log|sin|cos|tan|asin|acos|atan|atan2|min|max|rand|select|sap|sdb|sdns|sdse)\b",
            Color = new HighlightingColor { Foreground = new SimpleHighlightingBrush(instructionColor) }
        });

        // Registers (r0-r15 only, plus sp and ra)
        definition.AddRule(new HighlightingRule
        {
            Pattern = @"\b(r[0-9]|r1[0-5]|sp|ra)\b",
            Color = new HighlightingColor { Foreground = new SimpleHighlightingBrush(registerColor) }
        });

        // Device references
        definition.AddRule(new HighlightingRule
        {
            Pattern = @"\b(d[0-5]|db)\b",
            Color = new HighlightingColor { Foreground = new SimpleHighlightingBrush(registerColor) }
        });

        // Labels
        definition.AddRule(new HighlightingRule
        {
            Pattern = @"^\w+:",
            Color = new HighlightingColor { Foreground = new SimpleHighlightingBrush(labelColor) }
        });

        // Directives
        definition.AddRule(new HighlightingRule
        {
            Pattern = @"^\s*(alias|define)\b",
            Color = new HighlightingColor { Foreground = new SimpleHighlightingBrush(directiveColor) }
        });

        // Numbers
        definition.AddRule(new HighlightingRule
        {
            Pattern = @"\b-?\d+\.?\d*\b",
            Color = new HighlightingColor { Foreground = new SimpleHighlightingBrush(numberColor) }
        });

        // Comments
        definition.AddRule(new HighlightingRule
        {
            Pattern = @"#.*$",
            Color = new HighlightingColor { Foreground = new SimpleHighlightingBrush(commentColor) }
        });

        return definition;
    }
}

// Custom highlighting infrastructure
public class CustomHighlightingDefinition : IHighlightingDefinition
{
    private readonly List<HighlightingRule> _rules = new();

    public string Name => "Custom";
    public HighlightingRuleSet MainRuleSet { get; } = new();

    public void AddRule(HighlightingRule rule)
    {
        _rules.Add(rule);
        MainRuleSet.Rules.Add(new ICSharpCode.AvalonEdit.Highlighting.HighlightingRule
        {
            Regex = new System.Text.RegularExpressions.Regex(
                rule.Pattern,
                rule.IgnoreCase
                    ? System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Multiline
                    : System.Text.RegularExpressions.RegexOptions.Multiline),
            Color = rule.Color
        });
    }

    public HighlightingRuleSet? GetNamedRuleSet(string name) => null;
    public HighlightingColor? GetNamedColor(string name) => null;
    public IEnumerable<HighlightingColor> NamedHighlightingColors => Enumerable.Empty<HighlightingColor>();

    public IDictionary<string, string> Properties { get; } = new Dictionary<string, string>();
}

public class HighlightingRule
{
    public string Pattern { get; set; } = "";
    public HighlightingColor Color { get; set; } = new();
    public bool IgnoreCase { get; set; }
}

public class SimpleHighlightingBrush : HighlightingBrush
{
    private readonly SolidColorBrush _brush;

    public SimpleHighlightingBrush(Color color)
    {
        _brush = new SolidColorBrush(color);
        _brush.Freeze();
    }

    public override Brush GetBrush(ICSharpCode.AvalonEdit.Rendering.ITextRunConstructionContext context) => _brush;
}
