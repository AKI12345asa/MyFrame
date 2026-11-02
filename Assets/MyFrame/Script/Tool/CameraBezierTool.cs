using UnityEngine;

public static class CameraBezierTool
{
    /// <summary>
    /// Evaluates a cubic Bezier position; each control-point offset is relative to its adjacent endpoint.
    /// </summary>
    public static Vector3 Evaluate(Vector3 startPosition, Vector3 endPosition,
        Vector3 firstControlPointOffset, Vector3 secondControlPointOffset, float t)
    {
        t = Mathf.Clamp01(t);
        var firstControlPoint = startPosition + firstControlPointOffset;
        var secondControlPoint = endPosition + secondControlPointOffset;
        var inverseT = 1f - t;
        var inverseTSquared = inverseT * inverseT;
        var tSquared = t * t;
        return inverseTSquared * inverseT * startPosition +
               3f * inverseTSquared * t * firstControlPoint +
               3f * inverseT * tSquared * secondControlPoint +
               tSquared * t * endPosition;
    }
}
