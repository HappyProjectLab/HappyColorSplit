using HappyColorSplit.Models;

namespace HappyColorSplit.Services;

public sealed partial class LaserManualPlacementService
{
    public IReadOnlyList<LaserPlacement> SelectPolygon(LaserLassoRequest request, CancellationToken token)
    {
        token.ThrowIfCancellationRequested(); var points = request.Points;
        if (points.Count is < 3 or > 2048 || points.Any(p => !double.IsFinite(p.X) || !double.IsFinite(p.Y))) return [];
        double minX = points.Min(p => p.X), minY = points.Min(p => p.Y), maxX = points.Max(p => p.X), maxY = points.Max(p => p.Y);
        if (maxX - minX < 1 || maxY - minY < 1) return [];
        var chosen = new HashSet<LaserPart>();
        foreach (var p in request.Page.Placements)
        {
            token.ThrowIfCancellationRequested();
            if (p.X + p.Width < minX || p.Y + p.Height < minY || p.X > maxX || p.Y > maxY) continue;
            if (Covers(points, p.Bounds)) { chosen.Add(p.Part); continue; }
            // A lasso may closely follow a curved contour; empty bounding-box corners must not prevent selection.
            var runs = Rectangles(p, token).Rectangles;
            bool enclosed = runs.Length > 0;
            foreach (var run in runs)
            {
                token.ThrowIfCancellationRequested();
                if (!Covers(points, run)) { enclosed = false; break; }
            }
            if (enclosed) chosen.Add(p.Part);
        }
        return Expand(request.Page, chosen);
    }

    private static bool Covers(IReadOnlyList<LaserPoint> polygon, LaserRect rect)
    {
        if (!InPolygon(polygon, rect.X, rect.Y) || !InPolygon(polygon, rect.X + rect.Width, rect.Y)
            || !InPolygon(polygon, rect.X, rect.Y + rect.Height) || !InPolygon(polygon, rect.X + rect.Width, rect.Y + rect.Height)) return false;
        for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            if (CrossesInterior(polygon[j], polygon[i], rect)) return false;
        return true;
    }
    private static bool InPolygon(IReadOnlyList<LaserPoint> points, double x, double y)
    {
        bool inside = false;
        for (int i = 0, j = points.Count - 1; i < points.Count; j = i++)
        {
            var a = points[j]; var b = points[i];
            double cross = (b.X - a.X) * (y - a.Y) - (b.Y - a.Y) * (x - a.X);
            if (Math.Abs(cross) <= 1e-8 * Math.Max(1, Math.Abs(b.X - a.X) + Math.Abs(b.Y - a.Y))
                && x >= Math.Min(a.X, b.X) - 1e-8 && x <= Math.Max(a.X, b.X) + 1e-8
                && y >= Math.Min(a.Y, b.Y) - 1e-8 && y <= Math.Max(a.Y, b.Y) + 1e-8) return true;
            if ((a.Y > y) != (b.Y > y) && x < (b.X - a.X) * (y - a.Y) / (b.Y - a.Y) + a.X) inside = !inside;
        }
        return inside;
    }
    private static bool CrossesInterior(LaserPoint a, LaserPoint b, LaserRect rect)
    {
        const double epsilon = 1e-8;
        double dx = b.X - a.X, dy = b.Y - a.Y, first = 0, last = 1;
        return Clip(-dx, a.X - rect.X - epsilon, ref first, ref last)
            && Clip(dx, rect.X + rect.Width - epsilon - a.X, ref first, ref last)
            && Clip(-dy, a.Y - rect.Y - epsilon, ref first, ref last)
            && Clip(dy, rect.Y + rect.Height - epsilon - a.Y, ref first, ref last);
    }
    private static bool Clip(double direction, double distance, ref double first, ref double last)
    {
        if (Math.Abs(direction) < 1e-12) return distance >= 0;
        double t = distance / direction;
        if (direction < 0) { if (t > last) return false; first = Math.Max(first, t); }
        else { if (t < first) return false; last = Math.Min(last, t); }
        return first <= last;
    }
}
