namespace HappyColorSplit.Models;

public sealed record LaserMoveRequest(LaserPage Page, int SourceIndex, int PartId, bool WholeRegion, int DeltaX, int DeltaY);
public readonly record struct LaserPartKey(int SourceIndex, int PartId);
public readonly record struct LaserPoint(double X, double Y);
public sealed record LaserSelection(LaserPage Page, IReadOnlyList<LaserPartKey> Parts, bool IsMultiSelection = false);
public sealed record LaserTransformRequest(LaserPage Page, IReadOnlyList<LaserPartKey> Parts, int DeltaX, int DeltaY, int QuarterTurns = 0);
public sealed record LaserLassoRequest(LaserPage Page, IReadOnlyList<LaserPoint> Points);
public enum LaserMoveFailure { None, Selection, Edge, Clearance, Complexity }
public sealed record LaserMoveResult(LaserPage Page, LaserMoveFailure Failure)
{ public bool Success => Failure == LaserMoveFailure.None; }
