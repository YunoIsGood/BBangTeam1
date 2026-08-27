using UnityEngine;

[DisallowMultipleComponent]
public sealed class InventoryUIManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField, Tooltip("아까 만든 인벤토리 그리드(컨테이너)")] 
    private Transform gridContainer;
    
    [SerializeField, Tooltip("아까 만든 슬롯 프리팹")] 
    private InventorySlotUI slotPrefab;

    // 🚨 수정됨: OnEnable 대신 Start를 사용하여, InventoryManager가 Awake에서 완벽히 생성된 '이후'에 안전하게 연결합니다.
    private void Start()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnItemAdded += AddSlotUI;
            InventoryManager.Instance.OnItemConsumed += RemoveSlotUI;
        }
        else
        {
            Debug.LogError("[InventoryUIManager] InventoryManager를 찾을 수 없습니다! 매니저 오브젝트가 씬에 있는지 확인하세요.");
        }
    }

    // 🚨 수정됨: 이벤트 해제는 OnDestroy에서 안전하게 처리합니다.
    private void OnDestroy()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnItemAdded -= AddSlotUI;
            InventoryManager.Instance.OnItemConsumed -= RemoveSlotUI;
        }
    }

    // 아이템을 주웠을 때 UI 슬롯 생성
    private void AddSlotUI(InventoryManager.InventoryItem newItem)
    {
        InventorySlotUI spawnedSlot = Instantiate(slotPrefab, gridContainer);
        spawnedSlot.Setup(newItem); // 아이콘 및 데이터 세팅
    }

    // 아이템을 사용(소모)했을 때 UI 슬롯 삭제
    private void RemoveSlotUI(ItemData consumedData)
    {
        foreach (Transform child in gridContainer)
        {
            if (child.TryGetComponent(out InventorySlotUI slot))
            {
                if (slot.CurrentData == consumedData) 
                {
                    Destroy(child.gameObject);
                    break;
                }
            }
        }
    }
}