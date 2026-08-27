using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using PrimeTween;

[DisallowMultipleComponent]
public sealed class InspectViewer : MonoBehaviour
{
    public static InspectViewer Instance { get; private set; }
    
    public IInspectable CurrentInspectable { get; private set; }
    public bool IsRotating { get; private set; }
    public bool IsTransitioning => _activeSequence.isAlive;

    [Header("References")]
    [SerializeField] private Transform inspectPoint;
    [SerializeField] private Light inspectLight;

    [Header("Settings")]
    [SerializeField] private float moveDuration = 0.3f;
    [SerializeField] private float rotationSpeed = 0.5f;

    private Transform _cachedCamTransform;
    private CancellationTokenSource _cts;
    private Sequence _activeSequence;

    // 원래 있던 자리로 돌려보내기 위한 기억 장치
    private Vector3 _originalPos;
    private Quaternion _originalRot;

    private InputAction _rotateAction;
    private InputAction _rotateClickAction;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

        if (Camera.main is { } mainCam) _cachedCamTransform = mainCam.transform;
        if (inspectLight) inspectLight.enabled = false;

        _rotateAction = new InputAction(type: InputActionType.Value, binding: "<Mouse>/delta");
        _rotateClickAction = new InputAction(type: InputActionType.Button, binding: "<Mouse>/leftButton");
    }

    private void OnEnable()
    {
        _rotateClickAction.started += OnRotateStarted;
        _rotateClickAction.canceled += OnRotateCanceled;
        _rotateAction.Enable(); 
        _rotateClickAction.Enable();
    }

    private void OnDisable()
    {
        _rotateClickAction.started -= OnRotateStarted;
        _rotateClickAction.canceled -= OnRotateCanceled;
        _rotateAction.Disable(); 
        _rotateClickAction.Disable();
        ResetToken();
    }

    private void OnDestroy()
    {
        _rotateAction?.Dispose();
        _rotateClickAction?.Dispose();
        if (Instance == this) Instance = null;
    }

    private void OnRotateStarted(InputAction.CallbackContext ctx)
    {
        if (InteractionStateManager.Instance != null && InteractionStateManager.Instance.CurrentState == GameState.Inspect)
        {
            IsRotating = true;
        }
    }

    private void OnRotateCanceled(InputAction.CallbackContext ctx) => IsRotating = false;

    private void Update()
    {
        if (InteractionStateManager.Instance == null ||
            InteractionStateManager.Instance.CurrentState != GameState.Inspect || 
            CurrentInspectable == null || 
            !IsRotating || 
            _cachedCamTransform == null) return;

        Vector2 mouseDelta = _rotateAction.ReadValue<Vector2>();
        
        if (mouseDelta.sqrMagnitude > 0.01f && mouseDelta.sqrMagnitude < 5000f)
        {
            Transform targetT = CurrentInspectable.ObjectTransform;
            targetT.RotateAround(targetT.position, _cachedCamTransform.up, -mouseDelta.x * rotationSpeed);
            targetT.RotateAround(targetT.position, _cachedCamTransform.right, mouseDelta.y * rotationSpeed);
        }
    }

    public void StartInspect(IInspectable target)
    {
        if (target == null || Camera.main == null) return;

        CurrentInspectable = target;
        Transform targetT = target.ObjectTransform;

        // 🚨 방금 고친 핵심 버그: 아이템이 카메라 앞(뷰어)으로 날아오기 전에, 원래 있던 위치를 완벽하게 기억해둠!
        _originalPos = targetT.position;
        _originalRot = targetT.rotation;

        Vector3 targetInspectPos = Camera.main.transform.position + (Camera.main.transform.forward * target.InspectDistance);
        Quaternion targetInspectRot = Camera.main.transform.rotation * Quaternion.Euler(target.InspectRotationOffset);

        InteractionStateManager.Instance.ChangeState(GameState.Inspect);

        Sequence.Create()
            .Group(Tween.Position(targetT, targetInspectPos, 0.4f, Ease.OutQuad))
            .Group(Tween.Rotation(targetT, targetInspectRot, 0.4f, Ease.OutQuad));
    }

    public void StopInspect()
    {
        if (InteractionStateManager.Instance.CurrentState != GameState.Inspect || CurrentInspectable == null) return;

        InteractionStateManager.Instance.ChangeState(GameState.Focused);
        if (inspectLight) inspectLight.enabled = false;

        ResetToken();
        Transform targetT = CurrentInspectable.ObjectTransform;

        // 🚨 기억해둔 원래 자리(테이블 위, 선반 위 등)로 아름답게 날아서 복귀함
        _activeSequence = Sequence.Create()
            .Group(Tween.Position(targetT, _originalPos, moveDuration, Ease.InOutSine))
            .Group(Tween.Rotation(targetT, _originalRot, moveDuration, Ease.InOutSine))
            .OnComplete(() => CurrentInspectable = null);
    }

    private void ResetToken()
    {
        if (_activeSequence.isAlive) _activeSequence.Stop();
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();
    }
}