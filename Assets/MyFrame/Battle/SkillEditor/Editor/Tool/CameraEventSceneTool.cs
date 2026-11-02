using UnityEditor;
using UnityEngine;

public static class CameraEventSceneTool
{
    public static void DrawCameraStatus(Transform skillCamera, float fieldOfView)
    {
        if (skillCamera == null) return;

        var size = HandleUtility.GetHandleSize(skillCamera.position);
        Handles.color = Color.white;
        Handles.SphereHandleCap(0, skillCamera.position, skillCamera.rotation, size * 0.08f, EventType.Repaint);
        Handles.Label(skillCamera.position + Vector3.up * size * 0.12f, $"SkillCamera\nFOV: {fieldOfView:F1}");
        DrawCameraFrustum(skillCamera, fieldOfView, size * 3f);
    }

    private static void DrawCameraFrustum(Transform skillCamera, float fieldOfView, float distance)
    {
        if (fieldOfView <= 0) return;

        var halfHeight = Mathf.Tan(fieldOfView * Mathf.Deg2Rad * 0.5f) * distance;
        var halfWidth = halfHeight * GetSceneCameraAspectRatio();
        var farCenter = skillCamera.position + skillCamera.forward * distance;
        var topLeft = farCenter - skillCamera.right * halfWidth + skillCamera.up * halfHeight;
        var topRight = farCenter + skillCamera.right * halfWidth + skillCamera.up * halfHeight;
        var bottomLeft = farCenter - skillCamera.right * halfWidth - skillCamera.up * halfHeight;
        var bottomRight = farCenter + skillCamera.right * halfWidth - skillCamera.up * halfHeight;

        Handles.color = new Color(0.3f, 0.8f, 1f, 0.8f);
        Handles.DrawLine(skillCamera.position, topLeft);
        Handles.DrawLine(skillCamera.position, topRight);
        Handles.DrawLine(skillCamera.position, bottomLeft);
        Handles.DrawLine(skillCamera.position, bottomRight);
        Handles.DrawLine(topLeft, topRight);
        Handles.DrawLine(topRight, bottomRight);
        Handles.DrawLine(bottomRight, bottomLeft);
        Handles.DrawLine(bottomLeft, topLeft);
    }

    private static float GetSceneCameraAspectRatio()
    {
        return SceneView.currentDrawingSceneView == null
            ? 16f / 9f
            : SceneView.currentDrawingSceneView.camera.aspect;
    }

}
