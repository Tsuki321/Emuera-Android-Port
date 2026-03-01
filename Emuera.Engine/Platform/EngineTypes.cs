namespace MinorShift.Emuera.Platform;

/// <summary>
/// Cross-platform color value (replaces System.Drawing.Color in engine data).
/// </summary>
public readonly record struct EngineColor(byte R, byte G, byte B, byte A = 255)
{
    public static readonly EngineColor Black   = new(0,   0,   0);
    public static readonly EngineColor White   = new(255, 255, 255);
    public static readonly EngineColor Yellow  = new(255, 255, 0);
    public static readonly EngineColor Gray    = new(128, 128, 128);
    public static readonly EngineColor LightGray = new(192, 192, 192);

    public static EngineColor FromArgb(int r, int g, int b)    => new((byte)r, (byte)g, (byte)b);
    public static EngineColor FromArgb(int a, int r, int g, int b) => new((byte)r, (byte)g, (byte)b, (byte)a);

    public int ToArgb() => (A << 24) | (R << 16) | (G << 8) | B;

    /// <summary>Parse a HTML color string: #RRGGBB or #AARRGGBB.</summary>
    public static EngineColor FromHtml(string html)
    {
        html = html.TrimStart('#');
        if (html.Length == 6)
            return new EngineColor(
                Convert.ToByte(html.Substring(0, 2), 16),
                Convert.ToByte(html.Substring(2, 2), 16),
                Convert.ToByte(html.Substring(4, 2), 16));
        if (html.Length == 8)
            return new EngineColor(
                Convert.ToByte(html.Substring(2, 2), 16),
                Convert.ToByte(html.Substring(4, 2), 16),
                Convert.ToByte(html.Substring(6, 2), 16),
                Convert.ToByte(html.Substring(0, 2), 16));
        return Black;
    }

    public override string ToString() => $"#{R:X2}{G:X2}{B:X2}";
}

/// <summary>
/// Cross-platform font style flags (replaces System.Drawing.FontStyle).
/// </summary>
[Flags]
public enum EngineFontStyle
{
    Regular   = 0,
    Bold      = 1,
    Italic    = 2,
    Underline = 4,
    Strikeout = 8,
}

/// <summary>
/// Cross-platform font description (replaces System.Drawing.Font in engine data).
/// The concrete font object (SKTypeface / etc.) is created on the platform side.
/// </summary>
public readonly record struct EngineFont(string FamilyName, float SizeInPixels, EngineFontStyle Style)
{
    public bool IsBold      => Style.HasFlag(EngineFontStyle.Bold);
    public bool IsItalic    => Style.HasFlag(EngineFontStyle.Italic);
    public bool IsUnderline => Style.HasFlag(EngineFontStyle.Underline);
    public bool IsStrikeout => Style.HasFlag(EngineFontStyle.Strikeout);
}
