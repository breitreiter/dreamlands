namespace MapGen;

public class Noise
{
    private readonly int[] _perm;

    public Noise(int seed)
    {
        var rng = new Random(seed);
        _perm = new int[512];

        // Generate permutation table
        var p = Enumerable.Range(0, 256).ToArray();
        for (int i = 255; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (p[i], p[j]) = (p[j], p[i]);
        }

        for (int i = 0; i < 512; i++)
            _perm[i] = p[i & 255];
    }

    public float Sample(float x, float y)
    {
        // Simple value noise with bilinear interpolation
        int ix = (int)MathF.Floor(x);
        int iy = (int)MathF.Floor(y);

        float fx = x - ix;
        float fy = y - iy;

        // Smooth interpolation
        float u = fx * fx * (3 - 2 * fx);
        float v = fy * fy * (3 - 2 * fy);

        float n00 = Hash(ix, iy);
        float n10 = Hash(ix + 1, iy);
        float n01 = Hash(ix, iy + 1);
        float n11 = Hash(ix + 1, iy + 1);

        float nx0 = Lerp(n00, n10, u);
        float nx1 = Lerp(n01, n11, u);

        return Lerp(nx0, nx1, v);
    }

    public float Octaves(float x, float y, int octaves, float persistence = 0.5f)
    {
        float total = 0;
        float amplitude = 1;
        float frequency = 1;
        float maxValue = 0;

        for (int i = 0; i < octaves; i++)
        {
            total += Sample(x * frequency, y * frequency) * amplitude;
            maxValue += amplitude;
            amplitude *= persistence;
            frequency *= 2;
        }

        return total / maxValue;
    }

    private float Hash(int x, int y)
    {
        int n = _perm[(_perm[x & 255] + y) & 255];
        return n / 255f;
    }

    private static float Lerp(float a, float b, float t) => a + t * (b - a);
}
