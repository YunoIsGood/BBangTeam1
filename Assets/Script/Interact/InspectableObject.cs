using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class InspectableObject : MonoBehaviour, IInspectable, IInteractable // 🚨 클릭 감지(IInteractable) 다시 복구!
{
    [field: Header("Inspect Settings")]
    [field: SerializeField, Tooltip("관찰 시 모델링 방향 보정 값")]
    public Vector3 InspectRotationOffset { get; private set; } = Vector3.zero;

    [field: SerializeField, Tooltip("카메라와 아이템 사이의 거리 (기본값: 0.45m / 작은 물건: 0.35m)")]
    public float InspectDistance { get; private set; } = 0.45f; 

    public Transform ObjectTransform => transform;

    // 🚨 줌인(Focus) 모드든, 그냥 돌아다니는(FPS) 모드든 다 클릭할 수 있게 허용!
    public bool CanInteract => InteractionStateManager.Instance != null && 
                               InteractionStateManager.Instance.CurrentState != GameState.Inspect;

    public void Interact()
    {
        if (!CanInteract) return;

        // 1. 똑똑한 라우팅: 이 오브젝트에 PickupItem(인벤토리용) 스크립트가 있다면, 인벤토리 줍기로 알아서 토스!
        if (TryGetComponent(out PickupItem pickup))
        {
            pickup.Interact();
            return;
        }

        // 2. 주울 수 없는 기본 오브젝트라면? ➔ 클릭 즉시 360도 뷰어 모드로 직행!
        if (InspectViewer.Instance != null)
        {
            InspectViewer.Instance.StartInspect(this);
        }
    }
}