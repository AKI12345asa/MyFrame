using UnityEngine;

/// <summary>基于 Gradient Band 方法计算二维采样点的混合权重。</summary>
public static class GradientBand
{
    public const int POINT_COUNT = 4; // 按需修改

    /// <summary>在笛卡尔坐标空间中计算固定数量采样点的归一化权重。</summary>
    /// <param name="samplePoint">当前采样位置。</param>
    /// <param name="points">参与混合的采样点，数量至少为 <see cref="POINT_COUNT"/>。</param>
    /// <param name="weights">用于接收权重的数组，长度至少为 <see cref="POINT_COUNT"/>。</param>
    public static void SampleWeightsCartesian(Vector2 samplePoint, Vector2[] points, float[] weights)
    {
        float totalWeight = 0f;

        for (int i = 0; i < POINT_COUNT; ++i)
        {
            Vector2 pointI = points[i];
            Vector2 vecIS = samplePoint - pointI;

            float weight = 1f;

            for (int j = 0; j < POINT_COUNT; ++j)
            {
                if (j == i) continue;

                Vector2 pointJ = points[j];
                Vector2 vecIJ = pointJ - pointI;

                float lensqIJ = Vector2.Dot(vecIJ, vecIJ);
                float newWeight = Vector2.Dot(vecIS, vecIJ) / lensqIJ;
                newWeight = 1f - newWeight;
                newWeight = Mathf.Clamp01(newWeight);

                weight = Mathf.Min(weight, newWeight);
            }

            weights[i] = weight;
            totalWeight += weight;
        }

        // 归一化
        for (int i = 0; i < POINT_COUNT; ++i)
        {
            weights[i] /= totalWeight;
        }
    }

    private static float SignedAngle(Vector2 a, Vector2 b)
    {
        // 等价于 GLSL atan(y, x)
        return Mathf.Atan2(a.x * b.y - a.y * b.x, a.x * b.x + a.y * b.y);
    }

    /// <summary>在极坐标空间中计算固定数量采样点的归一化权重。</summary>
    /// <param name="samplePoint">当前采样位置。</param>
    /// <param name="points">参与混合的采样点，数量至少为 <see cref="POINT_COUNT"/>。</param>
    /// <param name="weights">用于接收权重的数组，长度至少为 <see cref="POINT_COUNT"/>。</param>
    public static void SampleWeightsPolar(Vector2 samplePoint, Vector2[] points, float[] weights)
    {
        const float kDirScale = 2f;
        float totalWeight = 0f;

        float sampleMag = samplePoint.magnitude;

        for (int i = 0; i < POINT_COUNT; ++i)
        {
            Vector2 pointI = points[i];
            float pointMagI = pointI.magnitude;

            float weight = 1f;

            for (int j = 0; j < POINT_COUNT; ++j)
            {
                if (j == i) continue;

                Vector2 pointJ = points[j];
                float pointMagJ = pointJ.magnitude;
    
                float ijAvgMag = (pointMagJ + pointMagI) * 0.5f;

                float magIS = (sampleMag - pointMagI) / ijAvgMag;
                float angleIS = SignedAngle(pointI, samplePoint);

                float magIJ = (pointMagJ - pointMagI) / ijAvgMag;
                float angleIJ = SignedAngle(pointI, pointJ);

                Vector2 vecIS = new Vector2(magIS, angleIS * kDirScale);
                Vector2 vecIJ = new Vector2(magIJ, angleIJ * kDirScale);

                float lensqIJ = Vector2.Dot(vecIJ, vecIJ);
                float newWeight = Vector2.Dot(vecIS, vecIJ) / lensqIJ;
                newWeight = 1f - newWeight;
                newWeight = Mathf.Clamp01(newWeight);

                weight = Mathf.Min(weight, newWeight);
            }

            weights[i] = weight;
            totalWeight += weight;
        }

        for (int i = 0; i < POINT_COUNT; ++i)
        {
            weights[i] /= totalWeight;
        }
    }
}
