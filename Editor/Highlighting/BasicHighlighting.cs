using System.Windows.Media;
using ICSharpCode.AvalonEdit.Highlighting;

namespace BasicToMips.Editor.Highlighting;

public static class BasicHighlighting
{
    public static IHighlightingDefinition Create()
    {
        var definition = new CustomHighlightingDefinition();

        // Colors matching VS Code dark theme
        var keywordColor = Color.FromRgb(86, 156, 214);      // Blue
        var typeColor = Color.FromRgb(78, 201, 176);         // Teal
        var stringColor = Color.FromRgb(206, 145, 120);      // Orange
        var commentColor = Color.FromRgb(106, 153, 85);      // Green
        var numberColor = Color.FromRgb(181, 206, 168);      // Light green
        var operatorColor = Color.FromRgb(212, 212, 212);    // White
        var functionColor = Color.FromRgb(220, 220, 170);    // Yellow
        var propertyColor = Color.FromRgb(156, 220, 254);    // Light blue
        var labelColor = Color.FromRgb(197, 134, 192);       // Purple

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
            Pattern = @"\b(VAR|CONST|DIM|LET|ALIAS|DEFINE|AS|INTEGER|SINGLE|BOOLEAN)\b",
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
            Color = new HighlightingColor { Foreground = new SimpleHighlightingBrush(keywordColor) },
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
            Color = new HighlightingColor { Foreground = new SimpleHighlightingBrush(propertyColor) },
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

        // Comments (both ' and REM)
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

        return definition;
    }
}

public static class MipsHighlighting
{
    public static IHighlightingDefinition Create()
    {
        var definition = new CustomHighlightingDefinition();

        var instructionColor = Color.FromRgb(86, 156, 214);   // Blue
        var registerColor = Color.FromRgb(156, 220, 254);     // Light blue
        var labelColor = Color.FromRgb(197, 134, 192);        // Purple
        var numberColor = Color.FromRgb(181, 206, 168);       // Light green
        var commentColor = Color.FromRgb(106, 153, 85);       // Green
        var directiveColor = Color.FromRgb(78, 201, 176);     // Teal

        // Instructions
        definition.AddRule(new HighlightingRule
        {
            Pattern = @"\b(move|add|sub|mul|div|mod|and|or|xor|nor|slt|sgt|seq|sne|sle|sge|beq|bne|blt|bgt|ble|bge|bna|bnan|beqz|bnez|bgtz|bltz|bgez|blez|j|jal|jr|l|s|ls|ss|lb|sb|lr|sr|push|pop|peek|poke|get|put|getd|putd|alias|define|yield|sleep|hcf|abs|ceil|floor|round|trunc|sqrt|exp|log|sin|cos|tan|asin|acos|atan|atan2|min|max|rand|select|sap|sdb|sdns|sdse)\b",
            Color = new HighlightingColor { Foreground = new SimpleHighlightingBrush(instructionColor) }
        });

        // Registers
        definition.AddRule(new HighlightingRule
        {
            Pattern = @"\b(r[0-9]|r1[0-7]|sp|ra)\b",
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

    public override Brush GetBrush(ITextRunConstructionContext context) => _brush;
}
