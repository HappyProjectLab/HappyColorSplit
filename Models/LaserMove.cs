namespace HappyColorSplit.Models;

public sealed record LaserMoveRequest(LaserPage Page, int SourceIndex, int PartId, bool WholeRegion, int DeltaX, int DeltaY);
public enum LaserMoveFailure { None, Selection, Edge, Clearance, Complexity }
public sealed record LaserMoveResult(LaserPage Page, LaserMoveFailure Failure)
{ public bool Success => Failure == LaserMoveFailure.None; }
