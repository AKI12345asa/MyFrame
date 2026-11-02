using System;
using System.Collections.Generic;
using Cinemachine;
using UnityEditor;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class CameraEventTrack : SkillTrackBase
{
    private SkillMutilineTrackStyle _trackStyle;
    private readonly List<CameraEventTrackItem> _trackItems = new();
    private readonly HashSet<CameraImpulseEvent> _triggeredImpulseEvents = new();
    private CameraPreviewState _previewState;
    private int _lastPreviewFrame;

    private CameraOrbitData CameraEventData => SkillEditorWindow.Instance.SkillClip.CameraOrbitData;

    public override void Init(VisualElement menuParent, VisualElement trackParent, float frameWidth)
    {
        base.Init(menuParent, trackParent, frameWidth);
        _trackStyle = new SkillMutilineTrackStyle();
        _trackStyle.Init(menuParent, trackParent, "相机轨道", null, CheckDeleteChildTrack, SwapChildTrack,
            UpdateChildTrackName);
        _trackStyle.ConfigureAddTypeDropdown(GetCameraEventTypes().Select(type => GetMenuPath(type)).ToList(), AddChildTrack);
        CameraEventData.FrameData.ForEach(CreateItem);
    }

    public override void ResetView(float frameWidth)
    {
        base.ResetView(frameWidth);
        _trackItems.ForEach(item => item.Destroy());
        _trackItems.Clear();
        if (SkillEditorWindow.Instance.SkillClip == null) return;
        CameraEventData.FrameData.ForEach(CreateItem);
    }

    public override void Destroy()
    {
        ResetPreviewCamera();
        _trackStyle.Destroy();
    }

    public override void OnPlay(int startFrameIndex)
    {
        ResetPreviewCamera();
        CapturePreviewCameraState();
        _triggeredImpulseEvents.Clear();
        _lastPreviewFrame = startFrameIndex - 1;
        TickView(startFrameIndex);
    }

    public override void TickView(int frameIndex)
    {
        if (!CapturePreviewCameraState()) return;

        RestorePreviewCameraState();
        foreach (var cameraEvent in CameraEventData.FrameData)
        {
            ApplyPreviewEvent(cameraEvent, frameIndex);
        }

        if (SkillEditorWindow.Instance.IsPlaying)
        {
            _lastPreviewFrame = frameIndex;
        }
    }

    public override void OnStop()
    {
        _triggeredImpulseEvents.Clear();
        _lastPreviewFrame = 0;
    }

    public override void OnSceneGUI()
    {
        var selectedCameraTrackItem = SkillEditorInspector.CurrentTrackItem as CameraEventTrackItem;
        if (selectedCameraTrackItem == null || !_trackItems.Contains(selectedCameraTrackItem)) return;

        TickView(SkillEditorWindow.Instance.CurrentSelectFrameIndex);
        var skillCamera = SkillEditorWindow.Instance.CurrentSkillCameraTransform;
        var model = GetPreviewModelTransform();
        if (model == null) return;
        CameraEventSceneTool.DrawCameraStatus(skillCamera, GetFieldOfView(skillCamera?.GetComponent<CinemachineVirtualCameraBase>()));

        foreach (var item in _trackItems)
        {
            if (SkillEditorInspector.CurrentTrackItem != item) continue;
            CameraEventInspector.DrawSceneGUI(
                item.CameraEvent,
                model,
                skillCamera,
                _previewState == null ? Quaternion.identity : _previewState.ModelRotation);
        }
    }

    public void ClearPreviewState()
    {
        _previewState = null;
    }

    private void AddChildTrack(string eventTypeName)
    {
        var eventType = GetCameraEventTypes().FirstOrDefault(type => GetMenuPath(type) == eventTypeName);
        if (eventType == null) return;
        var cameraEvent = (CameraEventBase)Activator.CreateInstance(eventType);
        cameraEvent.FrameIndex = 0;
        cameraEvent.TrackName = cameraEvent.DisplayName;
        CameraEventData.FrameData.Add(cameraEvent);
        CreateItem(cameraEvent);
        SkillEditorWindow.Instance.SaveConfig();
    }

    private void CreateItem(CameraEventBase cameraEvent)
    {
        var item = new CameraEventTrackItem();
        item.Init(this, FrameUnitWidth, cameraEvent, _trackStyle.AddChildTrack());
        item.SetTrackName(string.IsNullOrEmpty(cameraEvent.TrackName) ? cameraEvent.DisplayName : cameraEvent.TrackName);
        _trackItems.Add(item);
    }

    private static Type[] GetCameraEventTypes()
    {
        return TypeCache.GetTypesDerivedFrom<CameraEventBase>()
            .Where(type => !type.IsAbstract && type.GetConstructor(Type.EmptyTypes) != null)
            .Where(type => !string.IsNullOrEmpty(GetMenuPath(type)))
            .OrderBy(GetMenuPath)
            .ToArray();
    }

    private static string GetMenuPath(Type type)
    {
        return type.GetCustomAttributes(typeof(CameraEventAttribute), false)
            .Cast<CameraEventAttribute>()
            .Select(attribute => attribute.MenuPath)
            .FirstOrDefault();
    }

    private bool CheckDeleteChildTrack(int index)
    {
        if (index < 0 || index >= CameraEventData.FrameData.Count) return false;
        CameraEventData.FrameData.RemoveAt(index);
        _trackItems.RemoveAt(index);
        SkillEditorWindow.Instance.SaveConfig();
        return true;
    }

    private void SwapChildTrack(int index1, int index2)
    {
        (CameraEventData.FrameData[index1], CameraEventData.FrameData[index2]) = (CameraEventData.FrameData[index2], CameraEventData.FrameData[index1]);
    }

    private void UpdateChildTrackName(SkillMutilineTrackStyle.ChildTrack childTrack, string newName)
    {
        CameraEventData.FrameData[childTrack.GetIndex()].TrackName = newName;
        SkillEditorWindow.Instance.SaveConfig();
    }

    private bool CapturePreviewCameraState()
    {
        var camera = SkillEditorWindow.Instance.CurrentSkillCameraTransform;
        var model = GetPreviewModelTransform();
        if (camera == null || model == null) return false;
        if (_previewState != null && _previewState.CameraTransform == camera &&
            _previewState.ModelTransform == model) return true;
        if (_previewState != null)
        {
            ResetPreviewCamera();
        }

        _previewState = new CameraPreviewState
        {
            CameraTransform = camera,
            Position = camera.position,
            Rotation = camera.rotation,
            ModelRotation = model.rotation,
            FieldOfView = GetFieldOfView(camera.GetComponent<CinemachineVirtualCameraBase>()),
            Camera = camera.GetComponent<CinemachineVirtualCameraBase>(),
            ModelTransform = model
        };
        if (_previewState.Camera != null)
        {
            _previewState.Priority = _previewState.Camera.Priority;
            _previewState.Camera.Priority = 10000;
        }
        return true;
    }

    private void ResetPreviewCamera()
    {
        if (_previewState == null) return;

        RestorePreviewCameraState();
        if (_previewState.Camera != null)
        {
            _previewState.Camera.Priority = _previewState.Priority;
        }
        _previewState = null;
    }

    private void RestorePreviewCameraState()
    {
        if (_previewState == null) return;

        _previewState.CameraTransform.SetPositionAndRotation(_previewState.Position, _previewState.Rotation);
        SetFieldOfView(_previewState.CameraTransform.GetComponent<CinemachineVirtualCameraBase>(), _previewState.FieldOfView);
    }

    private void ApplyPreviewEvent(CameraEventBase cameraEvent, int currentFrame)
    {
        var context = new CameraEventContext
        {
            CameraTransform = _previewState.CameraTransform,
            VirtualCamera = _previewState.Camera,
            ModelTransform = _previewState.ModelTransform,
            ModelRotation = _previewState.ModelRotation,
            InitialPosition = _previewState.Position,
            InitialRotation = _previewState.Rotation,
            InitialFieldOfView = _previewState.FieldOfView,
            CurrentFrame = currentFrame,
            LastFrame = _lastPreviewFrame,
            FrameRate = SkillEditorWindow.Instance.SkillClip.FrameRate,
            IsPlaying = SkillEditorWindow.Instance.IsPlaying,
            TryMarkTriggered = TryMarkTriggered
        };
        CameraEventInspector.ApplyFrameEvent(cameraEvent, context);
    }

    private bool TryMarkTriggered(CameraEventBase cameraEvent)
    {
        return cameraEvent is CameraImpulseEvent impulseEvent && _triggeredImpulseEvents.Add(impulseEvent);
    }

    private Transform GetPreviewModelTransform()
    {
        return SkillEditorWindow.Instance.PreviewModel == null
            ? null
            : SkillEditorWindow.Instance.PreviewModel.transform;
    }

    private static float GetFieldOfView(CinemachineVirtualCameraBase camera)
    {
        switch (camera)
        {
            case CinemachineVirtualCamera virtualCamera:
                return virtualCamera.m_Lens.FieldOfView;
            case CinemachineFreeLook freeLook:
                return freeLook.m_Lens.FieldOfView;
            default:
                return 0;
        }
    }

    private static void SetFieldOfView(CinemachineVirtualCameraBase camera, float fieldOfView)
    {
        switch (camera)
        {
            case CinemachineVirtualCamera virtualCamera:
                var virtualCameraLens = virtualCamera.m_Lens;
                virtualCameraLens.FieldOfView = fieldOfView;
                virtualCamera.m_Lens = virtualCameraLens;
                break;
            case CinemachineFreeLook freeLook:
                var freeLookLens = freeLook.m_Lens;
                freeLookLens.FieldOfView = fieldOfView;
                freeLook.m_Lens = freeLookLens;
                break;
        }
    }

    private class CameraPreviewState
    {
        public Transform CameraTransform;
        public Vector3 Position;
        public Quaternion Rotation;
        public Quaternion ModelRotation;
        public float FieldOfView;
        public CinemachineVirtualCameraBase Camera;
        public int Priority;
        public Transform ModelTransform;
    }
}
