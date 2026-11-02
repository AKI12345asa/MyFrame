using System;
using Sirenix.Utilities;
using UnityEngine;

public static class SkillAttackDetectionTool
{
    private static Collider[] _detectionResults = new Collider[20];
    public static Collider[] ShapeDetection(Transform modelTransform, AttackDetectionDataBase data,AttackDetectionType attackDetectionType,LayerMask layerMask)
    {
        CleanDetectionResults();
        switch (attackDetectionType)
        {
            case AttackDetectionType.Box:
                 return BoxDetection(modelTransform, (AttackBoxDetectionData)data,layerMask);
            case AttackDetectionType.Sphere:
                return SphereDetection(modelTransform, (AttackSphereDetectionData)data,layerMask);
            case AttackDetectionType.Fan:
                return FanDetection(modelTransform,(AttackFanDetectionData)data,layerMask);
        }

        return null;
    }

    public static Collider[] BoxDetection(Transform modelTransform, AttackBoxDetectionData data, LayerMask layerMask)
    {
        CleanDetectionResults();
        
        Physics.OverlapBoxNonAlloc(modelTransform.TransformPoint(data.Position),data.Scale / 2,_detectionResults,modelTransform.rotation *Quaternion.Euler(data.Rotation),layerMask);
        
        return _detectionResults;
    }
    
    public static Collider[] SphereDetection(Transform modelTransform, AttackSphereDetectionData data, LayerMask layerMask)
    {
        CleanDetectionResults();
        
        Physics.OverlapSphereNonAlloc(modelTransform.TransformPoint(data.Position), data.Radius, _detectionResults, layerMask);
        return _detectionResults;
    }
    
    public static Collider[] FanDetection(Transform modelTransform, AttackFanDetectionData data, LayerMask layerMask)
    {
        CleanDetectionResults();
        var size = new Vector3();
        size.x = data.Radius * 2;
        size.y = data.Height;
        size.z = size.x;
        var fanPosition = modelTransform.TransformPoint(data.Position);
        Physics.OverlapBoxNonAlloc(fanPosition, size / 2, _detectionResults, modelTransform.rotation * Quaternion.Euler(data.Rotation),layerMask);
        var fanForward = modelTransform.rotation * Quaternion.Euler(data.Rotation) * Vector3.forward;
        for (var i = 0; i < _detectionResults.Length; i++)
        {
            if (_detectionResults[i] == null) break;
            //根据距离过滤不在内外半径内的碰撞体
            var point = _detectionResults[i].ClosestPoint(fanPosition);
            var distance = Vector3.Distance(point,fanPosition);
            var remove = distance < data.InsideRadius || distance > data.Radius;
            if (!remove)
            {
                var dir = point - fanPosition;
                //继续计算是否在角度范围内
                var angle = Vector3.Angle(fanForward, dir);
                remove = angle > data.Angle * 0.5f;
            }
            if(remove)
            {
                if (i < _detectionResults.Length - 1)
                {
                    _detectionResults[i] = _detectionResults[^1];
                    _detectionResults[^1] = null;
                    i--;
                }
            }
        }
        return _detectionResults;
    }

    private static void CleanDetectionResults()
    {
        for (var i = 0; i < _detectionResults.Length; i++)
        {
            _detectionResults[i] = null;    
        }
    }
}
