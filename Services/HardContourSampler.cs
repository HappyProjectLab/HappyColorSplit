using HappyColorSplit.Models;

namespace HappyColorSplit.Services;

// Reconstruct categorical masks between source pixel centres, then threshold the selected union.
// Only final opaque/transparent mask decisions escape this sampler; no soft edge values are exported.
internal sealed class HardContourSampler(ColorAnalysis analysis, IReadOnlyDictionary<int, int> buckets)
{
    private readonly int[] _buckets = analysis.Groups.Select(g => buckets.GetValueOrDefault(g.Index, -1)).ToArray();

    public int Sample(int x, int y, int width, int height)
    {
        double sx = (x + .5) * analysis.Width / width - .5, sy = (y + .5) * analysis.Height / height - .5;
        int ix = (int)Math.Floor(sx), iy = (int)Math.Floor(sy);
        int central = Bucket(ix, iy);
        // The four central Catmull-Rom samples dominate the 0.5 decision when they share a bucket.
        if (Bucket(ix + 1, iy) == central && Bucket(ix, iy + 1) == central && Bucket(ix + 1, iy + 1) == central) return central;
        Span<double> wx = stackalloc double[4], wy = stackalloc double[4], scores = stackalloc double[16];
        Span<int> ids = stackalloc int[16]; int count = 0; double coverage = 0;
        for (int i = 0; i < 4; i++) { wx[i] = Cubic(sx - (ix + i - 1)); wy[i] = Cubic(sy - (iy + i - 1)); }
        for (int dy = 0; dy < 4; dy++)
        {
            int row = Math.Clamp(iy + dy - 1, 0, analysis.Height - 1) * analysis.Width;
            for (int dx = 0; dx < 4; dx++)
            {
                double weight = wx[dx] * wy[dy]; if (weight == 0) continue;
                int label = analysis.Labels[row + Math.Clamp(ix + dx - 1, 0, analysis.Width - 1)];
                int bucket = label < 0 ? -1 : _buckets[label]; if (bucket < 0) continue;
                coverage += weight;
                int index = 0; while (index < count && ids[index] != bucket) index++;
                if (index == count) { ids[count] = bucket; scores[count++] = 0; }
                scores[index] += weight;
            }
        }
        int nearest = Bucket((int)Math.Floor(sx + .5), (int)Math.Floor(sy + .5));
        if (coverage < .5 - 1e-12 || count == 0 || (Math.Abs(coverage - .5) <= 1e-12 && nearest < 0)) return -1;
        int best = 0;
        for (int i = 1; i < count; i++)
            if (scores[i] > scores[best] + 1e-12 || (Math.Abs(scores[i] - scores[best]) <= 1e-12
                && (ids[i] == nearest || (ids[best] != nearest && ids[i] < ids[best])))) best = i;
        return ids[best];
    }

    private int Bucket(int x, int y)
    {
        int label = analysis.Labels[Math.Clamp(y, 0, analysis.Height - 1) * analysis.Width + Math.Clamp(x, 0, analysis.Width - 1)];
        return label < 0 ? -1 : _buckets[label];
    }

    private static double Cubic(double value)
    {
        double t = Math.Abs(value);
        return t <= 1 ? ((1.5 * t - 2.5) * t) * t + 1 : t < 2 ? ((-.5 * t + 2.5) * t - 4) * t + 2 : 0;
    }
}
