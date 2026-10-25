using HappyColorSplit.Models;

namespace HappyColorSplit.Services;

public sealed partial class LaserManualPlacementService
{
    public LaserPlacement? HitTest(LaserPage page, int x, int y) => page.Placements.Reverse().FirstOrDefault(p => Contains(p, x, y));

    public IReadOnlyList<LaserPlacement> Select(LaserPage page, int source, int part, bool wholeRegion)
    {
        var hit = page.Placements.FirstOrDefault(p => p.Part.SourceIndex == source && p.Part.Id == part);
        if (hit is null) return [];
        var chosen = page.Placements.Where(p => p.Part.SourceIndex == source
            && (wholeRegion ? p.Part.Region == hit.Part.Region : p.Part.Id == part)).Select(p => p.Part).ToHashSet();
        return Expand(page, chosen);
    }

    public IReadOnlyList<LaserPlacement> SelectMany(LaserPage page, IReadOnlyList<LaserPartKey> keys)
    {
        var wanted = keys.ToHashSet();
        var chosen = page.Placements.Where(p => wanted.Contains(new(p.Part.SourceIndex, p.Part.Id))).Select(p => p.Part).ToHashSet();
        if (chosen.Count != wanted.Count) return [];
        return Expand(page, chosen);
    }

    private static IReadOnlyList<LaserPlacement> Expand(LaserPage page, HashSet<LaserPart> chosen)
    {
        // A surrounding piece carries all its nested descendants, even across original source regions.
        var children = page.Placements.Where(p => p.NestedInPartId is not null)
            .GroupBy(p => new LaserPartKey(p.Part.SourceIndex, p.NestedInPartId!.Value)).ToDictionary(g => g.Key, g => g.Select(p => p.Part).ToArray());
        var pending = new Queue<LaserPartKey>(chosen.Select(p => new LaserPartKey(p.SourceIndex, p.Id)));
        while (pending.TryDequeue(out var parent))
        {
            if (!children.TryGetValue(parent, out var nested)) continue;
            foreach (var child in nested) if (chosen.Add(child)) pending.Enqueue(new(child.SourceIndex, child.Id));
        }
        return page.Placements.Where(p => chosen.Contains(p.Part)).ToArray();
    }

    public LaserMoveResult Move(LaserMoveRequest request, CancellationToken token)
        => Transform(new(request.Page, Select(request.Page, request.SourceIndex, request.PartId, request.WholeRegion)
            .Select(p => new LaserPartKey(p.Part.SourceIndex, p.Part.Id)).ToArray(), request.DeltaX, request.DeltaY), token);

    public LaserMoveResult Transform(LaserTransformRequest request, CancellationToken token)
    {
        token.ThrowIfCancellationRequested(); var page = request.Page;
        var selected = SelectMany(page, request.Parts).Select(p => p.Part).ToHashSet();
        if (selected.Count == 0) return new(page, LaserMoveFailure.Selection);
        if (request.DeltaX == 0 && request.DeltaY == 0) return new(page, LaserMoveFailure.None);
        int margin = page.Settings.MarginPixels;
        foreach (var p in page.Placements.Where(p => selected.Contains(p.Part)))
        {
            long x = (long)p.X + request.DeltaX, y = (long)p.Y + request.DeltaY;
            if (x < margin || y < margin || x + p.Width > page.Size.Width - margin || y + p.Height > page.Size.Height - margin)
                return new(page, LaserMoveFailure.Edge);
        }
        var moved = page.Placements.Select(p => selected.Contains(p.Part)
            ? p with { X = p.X + request.DeltaX, Y = p.Y + request.DeltaY } : p).ToArray();
        if (selected.Count == page.Placements.Count) return new(page with { Placements = moved }, LaserMoveFailure.None);
        for (int i = 0; i < moved.Length; i++)
        {
            token.ThrowIfCancellationRequested(); var p = moved[i];
            var containers = moved.Where(q => q.Part != p.Part && q.Part.SourceIndex == p.Part.SourceIndex
                && (long)q.Width * q.Height > (long)p.Width * p.Height && Inside(p.Bounds, q.Bounds));
            var host = containers.OrderBy(q => q.Part.Id == p.NestedInPartId ? 0 : 1)
                .ThenBy(q => (long)q.Width * q.Height).ThenBy(q => q.Part.Id).FirstOrDefault();
            moved[i] = p with { NestedInPartId = host?.Part.Id };
        }
        var affected = moved.Where((p, i) => selected.Contains(p.Part) || p.NestedInPartId != page.Placements[i].NestedInPartId)
            .Select(p => p.Part).ToHashSet();
        var masks = new Dictionary<LaserPart, Mask>(); long budget = 4_000_000;
        for (int i = 0; i < moved.Length; i++) for (int j = 0; j < i; j++)
        {
            token.ThrowIfCancellationRequested(); var a = moved[i]; var b = moved[j];
            if (!affected.Contains(a.Part) && !affected.Contains(b.Part)) continue;
            if (selected.Contains(a.Part) && selected.Contains(b.Part)
                && a.NestedInPartId == page.Placements[i].NestedInPartId && b.NestedInPartId == page.Placements[j].NestedInPartId) continue;
            bool nested = a.Part.SourceIndex == b.Part.SourceIndex && (a.NestedInPartId == b.Part.Id || b.NestedInPartId == a.Part.Id);
            int gap = nested ? page.Settings.CavityGapPixels : page.Settings.ElementGapPixels;
            if (Clear(a.Bounds, b.Bounds, gap)) continue;
            if (!masks.TryGetValue(a.Part, out var aMask)) masks.Add(a.Part, aMask = Rectangles(a, token));
            if (!masks.TryGetValue(b.Part, out var bMask)) masks.Add(b.Part, bMask = Rectangles(b, token));
            foreach (var ra in aMask.Rectangles)
            {
                token.ThrowIfCancellationRequested();
                for (int k = bMask.FirstRelevant(ra.Y - gap); k < bMask.Rectangles.Length; k++)
                {
                    var rb = bMask.Rectangles[k];
                    if (--budget < 0) return new(page, LaserMoveFailure.Complexity);
                    if (rb.Y > (long)ra.Y + ra.Height + gap) break;
                    if ((long)rb.Y + rb.Height + gap < ra.Y) continue;
                    if (!Clear(ra, rb, gap)) return new(page, LaserMoveFailure.Clearance);
                }
            }
        }
        return new(page with { Placements = moved }, LaserMoveFailure.None);
    }

    private static bool Inside(LaserRect a, LaserRect b) => a.X >= b.X && a.Y >= b.Y
        && (long)a.X + a.Width <= (long)b.X + b.Width && (long)a.Y + a.Height <= (long)b.Y + b.Height;
    private static bool Clear(LaserRect a, LaserRect b, int gap)
    {
        bool overlap = a.X < (long)b.X + b.Width && b.X < (long)a.X + a.Width
            && a.Y < (long)b.Y + b.Height && b.Y < (long)a.Y + a.Height;
        long dx = Math.Max(0L, Math.Max((long)a.X - b.X - b.Width, (long)b.X - a.X - a.Width));
        long dy = Math.Max(0L, Math.Max((long)a.Y - b.Y - b.Height, (long)b.Y - a.Y - a.Height));
        return !overlap && dx * dx + dy * dy >= (long)gap * gap;
    }

    public static bool Contains(LaserPlacement placement, int x, int y)
    {
        int lx = x - placement.X, ly = y - placement.Y; var p = placement.Part;
        if (lx < 0 || ly < 0 || lx >= placement.Width || ly >= placement.Height) return false;
        var (px, py) = placement.Turns switch
        {
            0 => (lx, ly), 1 => (ly, p.Height - 1 - lx),
            2 => (p.Width - 1 - lx, p.Height - 1 - ly), _ => (p.Width - 1 - ly, lx)
        };
        int nx = (int)(px * (long)p.NativeBounds.Width / p.Width), ny = (int)(py * (long)p.NativeBounds.Height / p.Height);
        int lo = 0, hi = p.Runs.Count - 1;
        while (lo <= hi)
        {
            int mid = lo + (hi - lo) / 2; var r = p.Runs[mid];
            if (ny < r.Y || (ny == r.Y && nx < r.X)) hi = mid - 1;
            else if (ny > r.Y || nx >= r.X + r.Length) lo = mid + 1;
            else return true;
        }
        return false;
    }

    private sealed class Mask(LaserRect[] rectangles)
    {
        public LaserRect[] Rectangles { get; } = rectangles;
        private readonly int[] _maxBottom = Bottoms(rectangles);
        private static int[] Bottoms(LaserRect[] values)
        {
            var result = new int[values.Length]; int bottom = 0;
            for (int i = 0; i < values.Length; i++) result[i] = bottom = Math.Max(bottom, values[i].Y + values[i].Height);
            return result;
        }
        public int FirstRelevant(int top)
        {
            int lo = 0, hi = _maxBottom.Length;
            while (lo < hi) { int mid = lo + (hi - lo) / 2; if (_maxBottom[mid] < top) lo = mid + 1; else hi = mid; }
            return lo;
        }
    }

    private static Mask Rectangles(LaserPlacement placement, CancellationToken token)
    {
        var p = placement.Part; var rectangles = new List<LaserRect>();
        foreach (var run in p.Runs)
        {
            token.ThrowIfCancellationRequested();
            int x = Map(run.X, p.Width, p.NativeBounds.Width), right = Map(run.X + run.Length, p.Width, p.NativeBounds.Width);
            int y = Map(run.Y, p.Height, p.NativeBounds.Height), bottom = Map(run.Y + 1, p.Height, p.NativeBounds.Height);
            if (x == right || y == bottom) continue;
            var r = placement.Turns switch
            {
                0 => new LaserRect(x, y, right - x, bottom - y),
                1 => new LaserRect(p.Height - bottom, x, bottom - y, right - x),
                2 => new LaserRect(p.Width - right, p.Height - bottom, right - x, bottom - y),
                _ => new LaserRect(y, p.Width - right, bottom - y, right - x)
            };
            rectangles.Add(r with { X = r.X + placement.X, Y = r.Y + placement.Y });
        }
        return new(rectangles.OrderBy(r => r.Y).ThenBy(r => r.X).ToArray());
    }
    private static int Map(int value, int target, int native) => (int)((value * (long)target + native - 1) / native);
}
