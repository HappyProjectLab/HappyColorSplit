using HappyColorSplit.Models;

namespace HappyColorSplit.Services;

// A conservative raster search supplements the rectangular packer inside each source-file block.
// Entire cells touched by material, plus a rounded-up square clearance, are reserved. A candidate's
// whole rectangle must fit in the remaining cells, so even diagonal contours retain the minimum gap.
internal sealed class LaserCavityNesting(int cavityGap, int elementGap, CancellationToken token)
{
    private long _searchBudget = 8_000_000;
    private readonly HashSet<LaserPart> _moved = [];

    public void Compact(List<List<LaserPlacement>> bins)
    {
        for (int target = 0; target < bins.Count && _searchBudget > 0; target++)
        {
            var hosts = bins[target].OrderByDescending(p => (long)p.Width * p.Height).ThenBy(p => p.Part.Id).ToArray();
            foreach (var host in hosts)
            {
                token.ThrowIfCancellationRequested();
                if (_searchBudget <= 0) return;
                if (_moved.Contains(host.Part) || host.Width <= 2L * cavityGap || host.Height <= 2L * cavityGap
                    || host.Part.Runs.Sum(r => (long)r.Length) == (long)host.Part.NativeBounds.Width * host.Part.NativeBounds.Height) continue;
                var candidates = bins.SelectMany((bin, index) => index < target ? [] : bin.Select(p => (Placement: p, Bin: index)))
                    .Where(c => c.Placement.Part != host.Part && !_moved.Contains(c.Placement.Part)
                        && (long)c.Placement.Width * c.Placement.Height < (long)host.Width * host.Height
                        && (c.Bin != target || !Inside(c.Placement.Bounds, host.Bounds)))
                    .OrderBy(c => c.Placement.Part.Region == host.Part.Region ? 0 : 1)
                    .ThenByDescending(c => c.Bin).ThenByDescending(c => (long)c.Placement.Width * c.Placement.Height)
                    .ThenBy(c => c.Placement.Part.Id).ToArray();
                if (candidates.Length == 0) continue;
                var grid = new ClearanceGrid(host, cavityGap, elementGap, token);
                foreach (var placed in bins[target]) grid.Reserve(placed);
                grid.Inflate();
                foreach (var candidate in candidates)
                {
                    token.ThrowIfCancellationRequested();
                    if (_searchBudget <= 0) return;
                    if (!grid.Find(candidate.Placement, ref _searchBudget, out var nested)) continue;
                    bins[candidate.Bin].Remove(candidate.Placement);
                    bins[target].Add(nested);
                    _moved.Add(nested.Part);
                    grid.Reserve(nested); grid.Inflate();
                }
            }
        }
    }

    private static bool Inside(LaserRect a, LaserRect b) => a.X >= b.X && a.Y >= b.Y
        && a.X + a.Width <= b.X + b.Width && a.Y + a.Height <= b.Y + b.Height;

    private sealed class ClearanceGrid
    {
        private readonly LaserPlacement _host;
        private readonly CancellationToken _token;
        private readonly int _step, _halo, _width, _height, _originX, _originY;
        private readonly int _cavityRadius, _elementRadius;
        private readonly bool[] _material, _hostMaterial;
        private readonly int[] _prefix;
        public ClearanceGrid(LaserPlacement host, int cavityGap, int elementGap, CancellationToken token)
        {
            _host = host; _token = token;
            int largestGap = Math.Max(cavityGap, elementGap);
            _step = Math.Max(1, Math.Max((Math.Max(host.Width, host.Height) + 511) / 512, (largestGap + 255) / 256));
            _cavityRadius = (cavityGap + _step - 1) / _step;
            _elementRadius = (elementGap + _step - 1) / _step;
            _halo = Math.Max(_cavityRadius, _elementRadius);
            _originX = host.X - _halo * _step; _originY = host.Y - _halo * _step;
            _width = (host.Width + _step - 1) / _step + 2 * _halo;
            _height = (host.Height + _step - 1) / _step + 2 * _halo;
            _material = new bool[_width * _height]; _hostMaterial = new bool[_width * _height];
            _prefix = new int[(_width + 1) * (_height + 1)];
        }

        public void Reserve(LaserPlacement placement)
        {
            var part = placement.Part;
            var mask = part == _host.Part ? _hostMaterial : _material;
            if (placement.X + placement.Width <= _originX || placement.Y + placement.Height <= _originY
                || placement.X >= _originX + _width * _step || placement.Y >= _originY + _height * _step) return;
            foreach (var run in part.Runs)
            {
                _token.ThrowIfCancellationRequested();
                int x = Map(run.X, part.Width, part.NativeBounds.Width), right = Map(run.X + run.Length, part.Width, part.NativeBounds.Width);
                int y = Map(run.Y, part.Height, part.NativeBounds.Height), bottom = Map(run.Y + 1, part.Height, part.NativeBounds.Height);
                if (x == right || y == bottom) continue;
                var rect = placement.Turns switch
                {
                    0 => new LaserRect(x, y, right - x, bottom - y),
                    1 => new LaserRect(part.Height - bottom, x, bottom - y, right - x),
                    2 => new LaserRect(part.Width - right, part.Height - bottom, right - x, bottom - y),
                    _ => new LaserRect(y, part.Width - right, bottom - y, right - x)
                };
                int leftCell = Math.Max(0, Floor(rect.X + placement.X - _originX, _step));
                int topCell = Math.Max(0, Floor(rect.Y + placement.Y - _originY, _step));
                int rightCell = Math.Min(_width - 1, Floor(rect.X + placement.X + rect.Width - 1 - _originX, _step));
                int bottomCell = Math.Min(_height - 1, Floor(rect.Y + placement.Y + rect.Height - 1 - _originY, _step));
                for (int cy = topCell; cy <= bottomCell; cy++)
                    for (int cx = leftCell; cx <= rightCell; cx++) mask[cy * _width + cx] = true;
            }
        }

        public void Inflate()
        {
            // Summed-area tables avoid a per-pixel loop over the potentially large clearance radius.
            var blocked = new bool[_material.Length];
            AddClearance(_hostMaterial, _cavityRadius, blocked);
            AddClearance(_material, _elementRadius, blocked);
            BuildPrefix((x, y) => blocked[y * _width + x]);
        }

        private void AddClearance(bool[] material, int radius, bool[] blocked)
        {
            BuildPrefix((x, y) => material[y * _width + x]);
            for (int y = 0; y < _height; y++)
            {
                _token.ThrowIfCancellationRequested();
                for (int x = 0; x < _width; x++) blocked[y * _width + x] |= Sum(Math.Max(0, x - radius), Math.Max(0, y - radius),
                    Math.Min(_width, x + radius + 1), Math.Min(_height, y + radius + 1)) > 0;
            }
        }

        public bool Find(LaserPlacement candidate, ref long budget, out LaserPlacement nested)
        {
            for (int rotation = 0; rotation < 2; rotation++)
            {
                int turns = (candidate.Turns + rotation) % 4;
                int w = turns % 2 == 0 ? candidate.Part.Width : candidate.Part.Height;
                int h = turns % 2 == 0 ? candidate.Part.Height : candidate.Part.Width;
                int cellsW = (w + _step - 1) / _step, cellsH = (h + _step - 1) / _step;
                for (int y = 0; y * _step + h <= _host.Height; y++)
                {
                    _token.ThrowIfCancellationRequested();
                    for (int x = 0; x * _step + w <= _host.Width; x++)
                    {
                        if (--budget < 0) { nested = candidate; return false; }
                        if (Sum(x + _halo, y + _halo, x + _halo + cellsW, y + _halo + cellsH) != 0) continue;
                        nested = candidate with { X = _host.X + x * _step, Y = _host.Y + y * _step, Turns = turns, NestedInPartId = _host.Part.Id };
                        return true;
                    }
                }
            }
            nested = candidate; return false;
        }

        private void BuildPrefix(Func<int, int, bool> filled)
        {
            Array.Clear(_prefix);
            for (int y = 0; y < _height; y++)
            {
                _token.ThrowIfCancellationRequested(); int row = 0;
                for (int x = 0; x < _width; x++)
                { if (filled(x, y)) row++; _prefix[(y + 1) * (_width + 1) + x + 1] = _prefix[y * (_width + 1) + x + 1] + row; }
            }
        }
        private int Sum(int x, int y, int right, int bottom) => _prefix[bottom * (_width + 1) + right]
            - _prefix[y * (_width + 1) + right] - _prefix[bottom * (_width + 1) + x] + _prefix[y * (_width + 1) + x];
        private static int Map(int value, int target, int native) => (int)((value * (long)target + native - 1) / native);
        private static int Floor(int value, int divisor) => (int)Math.Floor((double)value / divisor);
    }
}
