using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public sealed class InspectablePart : MonoBehaviour, IInteractable
{
    [Header("Interaction Settings")]
    [SerializeField] private UnityEvent onInteractEvent;

    // 🚨 수정: 상호작용 권한을 현재 상태 머신(InteractionStateManager)에 맞춤
    public bool CanInteract => InteractionStateManager.Instance != null && 
                               InteractionStateManager.Instance.CurrentState == GameState.Inspect;

    public void Interact()
    {
        if (!CanInteract) return;
        
        // 여기에 유니티 이벤트(UnityEvent)로 연결해둔 뚜껑 열기 애니메이션이나 소리 등이 실행됩니다.
        onInteractEvent?.Invoke(); 
    }
}