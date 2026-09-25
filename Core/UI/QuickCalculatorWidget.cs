using System.Data;
using System.Text.RegularExpressions;

namespace Swifter.Core.UI;

public sealed class QuickCalculatorWidget
{
    private static QuickCalculatorWidget? _instance;

    public static QuickCalculatorWidget Instance => _instance ??= new QuickCalculatorWidget();

    public bool CanHandle(string input)
    {
        input = input.Trim();
        if (Regex.IsMatch(input, @"^[\d\s\+\-\*\/\(\)\.\%\^]+$")) return true;
        if (Regex.IsMatch(input, @"^\d+\s*(to|in)\s*\w+", RegexOptions.IgnoreCase)) return true;
        if (Regex.IsMatch(input, @"^(time|date|now|utc)", RegexOptions.IgnoreCase)) return true;
        if (Regex.IsMatch(input, @"^(hex|bin|oct)\s+", RegexOptions.IgnoreCase)) return true;
        return false;
    }

    public CalculatorResult Evaluate(string input)
    {
        input = input.Trim();
        if (Regex.IsMatch(input, @"^(time|date|now|utc)", RegexOptions.OrdinalIgnoreCase))
        {
            return new CalculatorResult
            {
                Type = CalculatorResultType.Time,
                Display = DateTime.Now.ToString("dddd, MMMM d, yyyy  HH:mm:ss"),
                SubDisplay = $"UTC: {DateTime.UtcNow:HH:mm:ss}",
                Success = true
            };
        }

        if (input.StartsWith("hex ", StringComparison.OrdinalIgnoreCase) && int.TryParse(input[4..].Trim(), out int hexVal))
        {
            return new CalculatorResult
            {
                Type = CalculatorResultType.Conversion,
                Display = $"0x{hexVal:X}",
                SubDisplay = $"Binary: {Convert.ToString(hexVal, 2)}  |  Octal: {Convert.ToString(hexVal, 8)}",
                Success = true
            };
        }

        if (input.StartsWith("bin ", StringComparison.OrdinalIgnoreCase) && int.TryParse(input[4..].Trim(), System.Globalization.NumberStyles.HexNumber, null, out int binVal))
        {
            return new CalculatorResult
            {
                Type = CalculatorResultType.Conversion,
                Display = Convert.ToString(binVal, 2),
                SubDisplay = $"Decimal: {binVal}  |  Hex: 0x{binVal:X}",
                Success = true
            };
        }

        try
        {
            var sanitized = Regex.Replace(input, @"[^0-9\+\-\*\/\(\)\.\%\^\s]", "");
            if (string.IsNullOrWhiteSpace(sanitized))
                return new CalculatorResult { Success = false, Display = "Invalid expression" };

            sanitized = sanitized.Replace("^", ",");
            var table = new DataTable();
            var result = table.Compute(sanitized, "");
            var resultStr = result?.ToString() ?? "Error";
            return new CalculatorResult
            {
                Type = CalculatorResultType.Math,
                Display = $"= {resultStr}",
                SubDisplay = input,
                Success = true
            };
        }
        catch
        {
            return new CalculatorResult { Success = false, Display = "Invalid expression" };
        }
    }
}

public sealed class CalculatorResult
{
    public CalculatorResultType Type { get; set; }
    public string Display { get; set; } = "";
    public string SubDisplay { get; set; } = "";
    public bool Success { get; set; }
}

public enum CalculatorResultType
{
    Math,
    Conversion,
    Time,
    Error
}