using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class SkillAttackDetectionEventInspector:SkillEventDataInspectorBase<AttackDetectionTrackItem,AttackDetectionTrack>
{
    public override void OnDraw()
    {   
        DrawDetection();
        DrawDetectionEvents();
    }

    #region 检测部分
    
    private List<string> _detectionChoiceList;
    private IntegerField _durationFrameField;
    private void DrawDetection()
    {
        _durationFrameField = new IntegerField("持续帧数");
        _durationFrameField.value = trackItem.AttackDetectionEvent.DurationFrame;
        _durationFrameField.RegisterValueChangedCallback(DurationFrameFieldValueChanged);
        Root.Add(_durationFrameField);
        
       _detectionChoiceList = new(Enum.GetNames(typeof(AttackDetectionType)));
       var detectionDropDownField = new DropdownField("检测类型",_detectionChoiceList,(int)trackItem.AttackDetectionEvent.AttackDetectionType);
       detectionDropDownField.RegisterValueChangedCallback(OnDetectionDropDownFieldValue);
       Root.Add(detectionDropDownField);
       
       var comboCountField = new IntegerField("连击数");
       comboCountField.value = trackItem.AttackDetectionEvent.DetectionCount;
       comboCountField.RegisterValueChangedCallback(evt =>
       {
            trackItem.AttackDetectionEvent.DetectionCount = evt.newValue;
            RequestConfigSave();
       });
       Root.Add(comboCountField);
       
       var comboCheckDeltaTimeField = new FloatField("连击检测时间窗口");
       comboCheckDeltaTimeField.value = trackItem.AttackDetectionEvent.ComboCheckDeltaTime;
       comboCheckDeltaTimeField.RegisterValueChangedCallback(evt =>
       {
            trackItem.AttackDetectionEvent.ComboCheckDeltaTime = evt.newValue;
            RequestConfigSave();
       });
       Root.Add(comboCheckDeltaTimeField);

       var checkLayerMaskField = new LayerMaskField("检测层");
       checkLayerMaskField.value = trackItem.AttackDetectionEvent.CheckLayerMask;
       checkLayerMaskField.RegisterValueChangedCallback(evt =>
       {
           trackItem.AttackDetectionEvent.CheckLayerMask = evt.newValue;
           RequestConfigSave();
       });
       Root.Add(checkLayerMaskField);
       
       //根据检测类型进行绘制
       switch (trackItem.AttackDetectionEvent.AttackDetectionType)
       {
           case AttackDetectionType.Weapon:
               var weaponDetectionData = (AttackWeaponDetectionData)trackItem.AttackDetectionEvent.AttackDetectionData;
               var weaponDetectionDropDownField = new DropdownField("武器选择");
               if (SkillEditorWindow.Instance.CurrentPreviewCharacterObj != null)
               {
                   var skillPlayer = SkillEditorWindow.Instance.CurrentPreviewCharacterObj
                       .GetComponentInChildren<Skill_Player>();
                   weaponDetectionDropDownField.choices = skillPlayer.checkerDic.Keys.ToList();
               }
               if (!string.IsNullOrEmpty(weaponDetectionData.WeaponName))
               {
                   weaponDetectionDropDownField.value = weaponDetectionData.WeaponName;
               }

               weaponDetectionDropDownField.RegisterValueChangedCallback(evt =>
               {
                    var detectionData = (AttackWeaponDetectionData)trackItem.AttackDetectionEvent.AttackDetectionData;
                    detectionData.WeaponName = evt.newValue;
                    RequestConfigSave();
               });
               Root.Add(weaponDetectionDropDownField);
               break;
           case AttackDetectionType.Box:
              var boxDetectionData = (AttackBoxDetectionData)trackItem.AttackDetectionEvent.AttackDetectionData;
              var boxDetectionPositionField = new Vector3Field("坐标");
              var boxDetectionScaleField = new Vector3Field("缩放");
              var boxDetectionRotationField = new Vector3Field("旋转");
              boxDetectionPositionField.value = boxDetectionData.Position;
              boxDetectionRotationField.value = boxDetectionData.Rotation;
              boxDetectionScaleField.value = boxDetectionData.Scale;

              boxDetectionPositionField.RegisterValueChangedCallback(ShapeDetectionPositionFieldValueChanged);
              boxDetectionRotationField.RegisterValueChangedCallback(BoxDetectionRotationFieldValueChanged);
              boxDetectionScaleField.RegisterValueChangedCallback(BoxDetectionScaleFieldValueChanged);
              
              Root.Add(boxDetectionPositionField);
              Root.Add(boxDetectionRotationField);
              Root.Add(boxDetectionScaleField);
               break;
           case AttackDetectionType.Sphere:
               var sphereDetectionData = (AttackSphereDetectionData)trackItem.AttackDetectionEvent.AttackDetectionData;
               var sphereDetectionPositionField = new Vector3Field("坐标");
               var sphereRadiusField = new FloatField("半径");
               sphereDetectionPositionField.value = sphereDetectionData.Position;
               sphereRadiusField.value = sphereDetectionData.Radius;
               
               sphereDetectionPositionField.RegisterValueChangedCallback(ShapeDetectionPositionFieldValueChanged);
               sphereRadiusField.RegisterValueChangedCallback(SphereRadiusFieldFieldValueChanged);
               
               Root.Add(sphereDetectionPositionField);
               Root.Add(sphereRadiusField);
               break;
           case AttackDetectionType.Fan:
               var fanDetectionData = (AttackFanDetectionData)trackItem.AttackDetectionEvent.AttackDetectionData;
               var fanDetectionPositionField = new Vector3Field("坐标");
               var fanDetectionRotationField = new Vector3Field("旋转");
               var fanDetectionInsideRadiusField = new FloatField("内半径");
               var fanDetectionRadiusField = new FloatField("外半径");
               var fanDetectionHeightField = new FloatField("厚度");
               var fanDetectionAngleField = new FloatField("角度");
               
               
               fanDetectionPositionField.value = fanDetectionData.Position;
               fanDetectionRotationField.value = fanDetectionData.Rotation;
               fanDetectionInsideRadiusField.value = fanDetectionData.InsideRadius;
               fanDetectionRadiusField.value = fanDetectionData.Radius;
               fanDetectionHeightField.value = fanDetectionData.Height;
               fanDetectionAngleField.value = fanDetectionData.Angle;

               fanDetectionPositionField.RegisterValueChangedCallback(ShapeDetectionPositionFieldValueChanged);
               fanDetectionRotationField.RegisterValueChangedCallback(FanDetectionRotationFieldValueChanged);
               fanDetectionInsideRadiusField.RegisterValueChangedCallback(FanInsideRadiusFieldFieldValueChanged);
               fanDetectionRadiusField.RegisterValueChangedCallback(FanRadiusFieldFieldValueChanged);
               fanDetectionHeightField.RegisterValueChangedCallback(FanHeightFieldFieldValueChanged);
               fanDetectionAngleField.RegisterValueChangedCallback(FanAngleFieldFieldValueChanged);
               Root.Add(fanDetectionPositionField);
               Root.Add(fanDetectionRotationField);
               Root.Add(fanDetectionInsideRadiusField);
               Root.Add(fanDetectionRadiusField);
               Root.Add(fanDetectionHeightField);
               Root.Add(fanDetectionAngleField);
               break;
       }
       
       
       var setFrameButton = new Button(SetDetectionDurationFrameButtonClick);
       setFrameButton.text = "设置帧数到当前选中帧";
       Root.Add(setFrameButton);
    }
    
    private void ShapeDetectionPositionFieldValueChanged(ChangeEvent<Vector3> evt)
    {
        var shapeDetectionData = (AttackShapeDetectionDataBase)trackItem.AttackDetectionEvent.AttackDetectionData;
        shapeDetectionData.Position = evt.newValue;
        SceneView.RepaintAll();
        RequestConfigSave();
    }
    
    private void BoxDetectionRotationFieldValueChanged(ChangeEvent<Vector3> evt)
    {
        var shapeDetectionData = (AttackShapeDetectionDataBase)trackItem.AttackDetectionEvent.AttackDetectionData;
        ((AttackBoxDetectionData)shapeDetectionData).Rotation = evt.newValue;
        SceneView.RepaintAll();
        RequestConfigSave();
    }
    private void BoxDetectionScaleFieldValueChanged(ChangeEvent<Vector3> evt)
    {
        var shapeDetectionData = (AttackShapeDetectionDataBase)trackItem.AttackDetectionEvent.AttackDetectionData;
        ((AttackBoxDetectionData)shapeDetectionData).Scale= evt.newValue;
        SceneView.RepaintAll();
        RequestConfigSave();
    }
    
    private void SphereRadiusFieldFieldValueChanged(ChangeEvent<float> evt)
    {
        var shapeDetectionData = (AttackShapeDetectionDataBase)trackItem.AttackDetectionEvent.AttackDetectionData;
        ((AttackSphereDetectionData)shapeDetectionData).Radius = evt.newValue;
        SceneView.RepaintAll();
        RequestConfigSave();
    }
    
    private void DurationFrameFieldValueChanged(ChangeEvent<int> evt)
    {
        var attackDetectionTrackItem = trackItem;
        attackDetectionTrackItem.AttackDetectionEvent.DurationFrame = evt.newValue;
        SceneView.RepaintAll();
        trackItem.ResetView();
        RequestConfigSave();
    }

    private void OnDetectionDropDownFieldValue(ChangeEvent<string> evt)
    {
        var attackDetectionTrackItem = trackItem;
        attackDetectionTrackItem.AttackDetectionEvent.AttackDetectionType = (AttackDetectionType)_detectionChoiceList.IndexOf(evt.newValue);
        SceneView.RepaintAll();
        SkillEditorInspector.Instance.Show();
        RequestConfigSave();
    }
    
    
    private void FanDetectionRotationFieldValueChanged(ChangeEvent<Vector3> evt)
    {
        var fanDetectionData = ((AttackFanDetectionData)trackItem.AttackDetectionEvent.AttackDetectionData);
        fanDetectionData.Rotation = evt.newValue;
        SceneView.RepaintAll();
        RequestConfigSave();
    }
    
  
    private void FanInsideRadiusFieldFieldValueChanged(ChangeEvent<float> evt)
    {
        var fanDetectionData = (AttackFanDetectionData)trackItem.AttackDetectionEvent.AttackDetectionData;
        fanDetectionData.InsideRadius = evt.newValue;
        if (fanDetectionData.Radius < fanDetectionData.InsideRadius)
        {
            fanDetectionData.InsideRadius = fanDetectionData.Radius - 0.01f;
            SkillEditorInspector.Instance.Show();
        }
        SceneView.RepaintAll();
        RequestConfigSave();
    }
    
    private void FanRadiusFieldFieldValueChanged(ChangeEvent<float> evt)
    {
        var fanDetectionData = (AttackFanDetectionData)trackItem.AttackDetectionEvent.AttackDetectionData;
        fanDetectionData.Radius = evt.newValue;
        if (fanDetectionData.Radius < fanDetectionData.InsideRadius)
        {
            fanDetectionData.InsideRadius = fanDetectionData.Radius - 0.01f;
            SkillEditorInspector.Instance.Show();
        }
        SceneView.RepaintAll();
        RequestConfigSave();
    }
    
    private void FanHeightFieldFieldValueChanged(ChangeEvent<float> evt)
    {
        var fanDetectionData = (AttackFanDetectionData)trackItem.AttackDetectionEvent.AttackDetectionData;
        fanDetectionData.Height = evt.newValue;
        if (fanDetectionData.Height <= 0)
        {
            fanDetectionData.Height = 0.01f;
            SkillEditorInspector.Instance.Show();
        }
        SceneView.RepaintAll();
        RequestConfigSave();
    }
    
    private void FanAngleFieldFieldValueChanged(ChangeEvent<float> evt)
    {
        var fanDetectionData = (AttackFanDetectionData)trackItem.AttackDetectionEvent.AttackDetectionData;
        fanDetectionData.Angle = evt.newValue;
        if (fanDetectionData.Angle < 0)
        {
            fanDetectionData.Angle = 1;
            SkillEditorInspector.Instance.Show();
        }
        else if(fanDetectionData.Angle > 360)
        {
            fanDetectionData.Angle = 360;
            SkillEditorInspector.Instance.Show();
        }
        SceneView.RepaintAll();
        RequestConfigSave();
    }
    
    private void SetDetectionDurationFrameButtonClick()
    {
        var frame = SkillEditorWindow.Instance.CurrentSelectFrameIndex - trackItem.FrameIndex;
        _durationFrameField.value = frame > 0 ? frame : trackItem.AttackDetectionEvent.DurationFrame;
    }

    #endregion

    #region 检测事件

    private void DrawDetectionEvents()
    {
        var detectionEvents = trackItem.AttackDetectionEvent.TriggerEvents;
        if (detectionEvents == null)
        {
            detectionEvents = new List<DetectionTriggerEventBase>();
            trackItem.AttackDetectionEvent.TriggerEvents = detectionEvents;
        }

        var detectionEventsProperty = new DetectionEventsProperty
        {
            Events = detectionEvents
        };
        var propertyTree = PropertyTree.Create(detectionEventsProperty);
        Root.Add(new Label("检测事件"));
        Root.Add(new IMGUIContainer(() =>
        {
            EditorGUI.BeginChangeCheck();
            propertyTree.Draw(false);
            trackItem.AttackDetectionEvent.TriggerEvents = detectionEventsProperty.Events;
            if (EditorGUI.EndChangeCheck())
            {
                RequestConfigSave();
                SceneView.RepaintAll();
            }
        }));
    }

    private sealed class DetectionEventsProperty
    {
        [ShowInInspector]
        public List<DetectionTriggerEventBase> Events;
    }

    private static void RequestConfigSave()
    {
        SkillEditorWindow.Instance.RequestSaveConfig();
    }

    #endregion
}
