using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class FocusZone : MonoBehaviour, IInteractable
{
    [SerializeField, Tooltip("줌인될 카메라의 목표 위치/회전값 (자식 빈 오브젝트)")] 
    private Transform targetCameraView;

    public bool CanInteract => InteractionStateManager.Instance != null && 
                               InteractionStateManager.Instance.CurrentState == GameState.FPS;

    public void Interact()
    {
        if (!CanInteract) return;

        // 🚨 줌인 실행 전, 이 FocusZone을 현재 활성 구역으로 시스템에 등록
        InteractionStateManager.Instance.SetFocusZone(transform);
        
        if (CameraController.Instance != null && targetCameraView != null)
        {
            CameraController.Instance.MoveToZone(targetCameraView);
        }
    }
}