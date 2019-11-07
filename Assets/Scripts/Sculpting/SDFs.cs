using UnityEngine;

sealed class OffsetSDF : ISdf
{
    private readonly Vector3 offset;
    private readonly ISdf sdf;

    public OffsetSDF(Vector3 offset, ISdf sdf)
    {
        this.offset = offset;
        this.sdf = sdf;
    }

    public double Eval(double x, double y, double z)
    {
        return sdf.Eval(x + offset.x, y + offset.y, z + offset.z);
    }

    public Vector3 Max()
    {
        return sdf.Max() - offset;
    }

    public Vector3 Min()
    {
        return sdf.Min() - offset;
    }
}

sealed class BoxSDF : ISdf
{
    private readonly float radius;

    public BoxSDF(float radius)
    {
        this.radius = radius;
    }

    public double Eval(double x, double y, double z)
    {
        float dx = Mathf.Abs((float)x) - radius;
        float dy = Mathf.Abs((float)y) - radius;
        float dz = Mathf.Abs((float)z) - radius;
        return new Vector3(Mathf.Max(dx, 0), Mathf.Max(dy, 0), Mathf.Max(dz, 0)).magnitude + Mathf.Min(Mathf.Max(dx, Mathf.Max(dy, dz)), 0);
    }

    public Vector3 Max()
    {
        return new Vector3(radius, radius, radius);
    }

    public Vector3 Min()
    {
        return new Vector3(-radius, -radius, -radius);
    }
}

sealed class SphereSDF : ISdf
{
    private readonly float radius;

    public SphereSDF(float radius)
    {
        this.radius = radius;
    }

    public double Eval(double x, double y, double z)
    {
        return Mathf.Sqrt((float)(x * x + y * y + z * z)) - radius;
    }

    public Vector3 Max()
    {
        return new Vector3(radius, radius, radius);
    }

    public Vector3 Min()
    {
        return new Vector3(-radius, -radius, -radius);
    }
}

sealed class TransformSDF : ISdf
{
    private readonly Matrix4x4 transform;
    private readonly Matrix4x4 invTransform;
    private readonly ISdf sdf;

    public TransformSDF(Matrix4x4 transform, ISdf sdf)
    {
        this.invTransform = transform.inverse;
        this.transform = transform;
        this.sdf = sdf;
    }

    public TransformSDF(Matrix4x4 transform, Matrix4x4 invTransform, ISdf sdf)
    {
        this.invTransform = invTransform;
        this.transform = transform;
        this.sdf = sdf;
    }

    public double Eval(double x, double y, double z)
    {
        Vector3 point = this.invTransform.MultiplyPoint(new Vector3((float)x, (float)y, (float)z));
        return sdf.Eval(point.x, point.y, point.z);
    }

    public Vector3 Max()
    {
        Vector3 min = this.sdf.Min();
        Vector3 max = this.sdf.Max();
        Vector3 worldMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        for (int mx = 0; mx < 2; mx++)
        {
            for (int my = 0; my < 2; my++)
            {
                for (int mz = 0; mz < 2; mz++)
                {
                    Vector3 corner = this.transform.MultiplyPoint(new Vector3(
                        mx == 0 ? min.x : max.x,
                        my == 0 ? min.y : max.y,
                        mz == 0 ? min.z : max.z
                        ));
                    if(corner.x > worldMax.x)
                    {
                        worldMax.x = corner.x;
                    }
                    if (corner.y > worldMax.y)
                    {
                        worldMax.y = corner.y;
                    }
                    if (corner.z > worldMax.z)
                    {
                        worldMax.z = corner.z;
                    }
                }
            }
        }
        return worldMax;
    }

    public Vector3 Min()
    {
        Vector3 min = this.sdf.Min();
        Vector3 max = this.sdf.Max();
        Vector3 worldMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        for (int mx = 0; mx < 2; mx++)
        {
            for (int my = 0; my < 2; my++)
            {
                for (int mz = 0; mz < 2; mz++)
                {
                    Vector3 corner = this.transform.MultiplyPoint(new Vector3(
                        mx == 0 ? min.x : max.x,
                        my == 0 ? min.y : max.y,
                        mz == 0 ? min.z : max.z
                        ));
                    if (corner.x < worldMin.x)
                    {
                        worldMin.x = corner.x;
                    }
                    if (corner.y < worldMin.y)
                    {
                        worldMin.y = corner.y;
                    }
                    if (corner.z < worldMin.z)
                    {
                        worldMin.z = corner.z;
                    }
                }
            }
        }
        return worldMin;
    }
}

sealed class ScaleSDF : ISdf
{
    private readonly float scale;
    private readonly ISdf sdf;

    public ScaleSDF(float scale, ISdf sdf)
    {
        this.scale = scale;
        this.sdf = sdf;
    }

    public double Eval(double x, double y, double z)
    {
        return sdf.Eval(x / scale, y / scale, z / scale) * scale;
    }

    public Vector3 Max()
    {
        return sdf.Max() * scale;
    }

    public Vector3 Min()
    {
        return sdf.Min() * scale;
    }
}

sealed class PerlinSDF : ISdf
{
    private readonly Vector2 sampleOffset;
    private readonly Vector3 min, max;
    private readonly Vector2 scale;
    private readonly float amplitude;
    private readonly int octaves;
    private readonly float octaveScale, octaveAmplitude;

    public PerlinSDF(Vector3 min, Vector3 max, Vector2 sampleOffset, Vector2 scale, float amplitude, int octaves, float octaveScale, float octaveAmplitude)
    {
        this.sampleOffset = sampleOffset;
        this.min = min;
        this.max = max;
        this.scale = scale;
        this.amplitude = amplitude;
        this.octaves = octaves;
        this.octaveScale = octaveScale;
        this.octaveAmplitude = octaveAmplitude;
    }

    private float CalculateNoise(float x, float y)
    {
        float noise = 0.0f;
        Vector2 scale = this.scale;
        float amplitude = this.amplitude;
        for (int i = 0; i < octaves; i++)
        {
            noise += Mathf.PerlinNoise((float)x * scale.x, (float)y * scale.y) * amplitude;
            scale *= this.octaveScale;
            amplitude *= this.octaveAmplitude;
        }
        return noise;
    }

    public double Eval(double x, double y, double z)
    {
        float dx = Mathf.Abs((float)x) - (max.x - min.x);
        float dy = Mathf.Abs((float)y) - (max.y - min.y);
        float dz = Mathf.Abs((float)z) - (max.z - min.z);
        float distFromBounds = new Vector3(Mathf.Max(dx, 0), Mathf.Max(dy, 0), Mathf.Max(dz, 0)).magnitude + Mathf.Min(Mathf.Max(dx, Mathf.Max(dy, dz)), 0);
        float distFromNoise = (float)y - CalculateNoise((float)x - min.x + sampleOffset.x, (float)z - min.z + sampleOffset.y);
        return Mathf.Max(distFromBounds, distFromNoise);
    }

    public Vector3 Max()
    {
        return max;
    }

    public Vector3 Min()
    {
        return min;
    }
}