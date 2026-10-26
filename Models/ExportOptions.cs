namespace HappyColorSplit.Models;

public enum ExportBackground { Black = 0, Gray = 1, White = 2, Transparent = 3 }
public enum ExportArea { Black = 0, White = 1, Transparent = 2, GroupColor = 3 }

public sealed record ExportOptions(ExportArea Area = ExportArea.White, ExportBackground Background = ExportBackground.Black, bool SmoothContours = false);
