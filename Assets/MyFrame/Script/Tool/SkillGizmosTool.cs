#if UNITY_EDITOR
using UnityEngine;
using System;
public static class SkillGizmosTool
{
    public static void DrawDetection(AttackDetectionEvent attackDetectionEvent,Skill_Player skillPlayer)
    {
        if(skillPlayer == null) return;
        Gizmos.color = new Color(0, 1, 0, 0.5f);
        var modelTransform = skillPlayer.ModelTransform== null ? skillPlayer.transform : skillPlayer.ModelTransform;
        var rotateAndPositionMatrix = new Matrix4x4();
        switch (attackDetectionEvent.AttackDetectionType)
        {
            case AttackDetectionType.Weapon:
                var weaponDetectionData = (AttackWeaponDetectionData)attackDetectionEvent.AttackDetectionData;
                if (string.IsNullOrEmpty(weaponDetectionData.WeaponName) || !skillPlayer.checkerDic.TryGetValue(weaponDetectionData.WeaponName,out var weapon)) return;
                var collider = weapon.GetComponent<Collider>();
                rotateAndPositionMatrix.SetTRS(collider.transform.position,collider.transform.rotation,collider.transform.localScale);
                Gizmos.matrix = rotateAndPositionMatrix;
                switch (collider)
                {
                    case BoxCollider boxCollider:
                        Gizmos.DrawWireCube(boxCollider.center,boxCollider.size);//当前的centre是在物体的本地坐标系下绘制出来的所以要基于其自身的位置矩阵来绘制
                        break;
                    case SphereCollider sphereCollider:         ;
                        Gizmos.DrawWireSphere(sphereCollider.center, sphereCollider.radius);
                        break;
                }
                break;
            case AttackDetectionType.Box:
                var boxDetectionData = attackDetectionEvent.AttackDetectionData as AttackBoxDetectionData;
                rotateAndPositionMatrix.SetTRS(modelTransform.TransformPoint(boxDetectionData.Position),
                    modelTransform.rotation * Quaternion.Euler(boxDetectionData.Rotation), Vector3.one);
                Gizmos.matrix = rotateAndPositionMatrix;
                Gizmos.DrawCube(Vector3.zero, boxDetectionData.Scale);
                break;
            case AttackDetectionType.Sphere:
                var sphereDetectionData = attackDetectionEvent.AttackDetectionData as AttackSphereDetectionData;
                Gizmos.DrawSphere(modelTransform.TransformPoint(sphereDetectionData.Position),
                    sphereDetectionData.Radius);
                break;
            case AttackDetectionType.Fan:
                var fanDetectionData = attackDetectionEvent.AttackDetectionData as AttackFanDetectionData;
                var fanPosition = modelTransform.TransformPoint(fanDetectionData.Position);
                var fanRotation = modelTransform.rotation * Quaternion.Euler(fanDetectionData.Rotation);
                var mesh = MeshGenerator.GenerateFanMesh(fanDetectionData.InsideRadius, fanDetectionData.Radius,
                    fanDetectionData.Height, fanDetectionData.Angle);
                Gizmos.DrawMesh(mesh,fanPosition,fanRotation);
                break;
        }
        Gizmos.color = Color.white;
        Gizmos.matrix = Matrix4x4.identity;
    }
}

#endif