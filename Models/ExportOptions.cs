namespace HappyColorSplit.Models;

public enum ExportBackground { Black = 0, Gray = 1, White = 2, Transparent = 3 }

public sealed record ExportOptions(bool UseGroupColor = false, ExportBackground Background = ExportBackground.Black);
