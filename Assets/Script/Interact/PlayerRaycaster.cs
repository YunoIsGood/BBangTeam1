using System;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class PlayerRaycaster : MonoBehaviour
{
    public event Action<IInteractable> OnTargetChanged;
    public IInteractable CurrentTarget { get; private set; }

    [Header("Settings")]
    [SerializeField] private float fpsInteractDistance = 15f;
    [SerializeField] private float focusInteractDistance = 10f;
    [SerializeField] private LayerMask interactLayer;

    private Camera _cachedCam;
    private readonly RaycastHit[] _hits = new RaycastHit[10];
    private InputAction _interactAction;

    private void Awake()
    {
        _cachedCam = Camera.main;
        _interactAction = new InputAction(name: "Interact", type: InputActionType.Button, binding: "<Mouse>/leftButton");
    }

    private void OnEnable()
    {
        _interactAction.performed += OnInteractPerformed;
        _interactAction.Enable();
        if (InteractionStateManager.Instance != null) InteractionStateManager.Instance.OnStateChanged += HandleStateChanged;
    }

    private void OnDisable()
    {
        _interactAction.performed -= OnInteractPerformed;
        _interactAction.Disable();
        if (InteractionStateManager.Instance != null) InteractionStateManager.Instance.OnStateChanged -= HandleStateChanged;
    }

    private void OnDestroy() => _interactAction?.Dispose();

    private void HandleStateChanged(GameState state) => UpdateTarget(null);

    private void Update()
    {
        if (_cachedCam == null || InteractionStateManager.Instance == null) return;

        var currentState = InteractionStateManager.Instance.CurrentState;
        if (currentState == GameState.UI)
        {
            UpdateTarget(null);
            return;
        }

        bool isMousePointerMode = (currentState == GameState.Focused || currentState == GameState.Inspect);
        Vector2 pointerPos = isMousePointerMode
            ? (Mouse.current?.position.ReadValue() ?? new Vector2(Screen.width / 2f, Screen.height / 2f))
            : new Vector2(Screen.width / 2f, Screen.height / 2f);

        Ray ray = isMousePointerMode ? _cachedCam.ScreenPointToRay(pointerPos) : new Ray(_cachedCam.transform.position, _cachedCam.transform.forward);
        float maxDistance = isMousePointerMode ? focusInteractDistance : fpsInteractDistance;
        int hitCount = Physics.RaycastNonAlloc(ray, _hits, maxDistance, interactLayer);

        Transform currentInspectedT = InspectViewer.Instance?.CurrentInspectable?.ObjectTransform;
        Transform currentFocusZoneT = InteractionStateManager.Instance.CurrentFocusZone;

        IInteractable validTarget = null;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            if (Vector3.Dot(ray.direction, _hits[i].normal) > 0f) continue;

            if (_hits[i].collider.TryGetComponent(out IInteractable interactable))
            {
                if (!interactable.CanInteract) continue;

                // 🚨 1. [Inspect 상태 (뷰어 모드)]: 눈앞에 띄운 아이템의 자식 부품(InspectablePart)만 무조건 감지!
                if (currentState == GameState.Inspect && currentInspectedT != null)
                {
                    // 클릭된 물체가 현재 관찰 중인 아이템의 부품(자식)인지 확인
                    if (_hits[i].collider.transform.IsChildOf(currentInspectedT))
                    {
                        if (interactable is InspectablePart && _hits[i].distance < closestDistance)
                        {
                            validTarget = interactable;
                            closestDistance = _hits[i].distance;
                        }
                    }
                }
                // 2. [Focused 상태 (줌인 모드)]: 바닥이나 선반에 있는 물체 감지
                else if (currentState == GameState.Focused && currentFocusZoneT != null)
                {
                    if (_hits[i].collider.transform.IsChildOf(currentFocusZoneT) && _hits[i].distance < closestDistance)
                    {
                        validTarget = interactable;
                        closestDistance = _hits[i].distance;
                    }
                }
                // 3. [FPS 상태 (기본 이동 모드)]
                else if (currentState == GameState.FPS && _hits[i].distance < closestDistance)
                {
                    validTarget = interactable;
                    closestDistance = _hits[i].distance;
                }
            }
        }

        UpdateTarget(validTarget);
    }

    private void UpdateTarget(IInteractable newTarget)
    {
        if (CurrentTarget != newTarget)
        {
            CurrentTarget = newTarget;
            OnTargetChanged?.Invoke(CurrentTarget);
        }
    }

    private void OnInteractPerformed(InputAction.CallbackContext ctx)
    {
        if (CurrentTarget == null) return;
        if (CurrentTarget is UnityEngine.Object unityObj && unityObj == null) { UpdateTarget(null); return; }

        var currentState = InteractionStateManager.Instance != null ? InteractionStateManager.Instance.CurrentState : GameState.FPS;

        // 🚨 뷰어 모드일 때는 오직 부품(InspectablePart)만 좌클릭 상호작용 허용
        if (currentState == GameState.Inspect && !(CurrentTarget is InspectablePart)) return;

        IInteractable targetToInteract = CurrentTarget;
        UpdateTarget(null);
        targetToInteract.Interact();
    }
}