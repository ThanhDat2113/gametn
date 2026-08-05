using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Gắn động vào highlightTarget để detect click cho tutorial.
/// Tự được huỷ bởi CombatTutorialManager khi bước kết thúc hoặc tutorial dừng.
/// Không cần thêm vào GameObject nào thủ công.
///
/// YÊU CẦU: GameObject chứa component này phải có Raycast Target = true
/// (Image mặc định là true). Nếu có overlay che lên trên, overlay đó phải
/// có Raycast Target = false để click xuyên qua được.
/// </summary>
[DisallowMultipleComponent]
public class ClickDetector : MonoBehaviour, IPointerClickHandler
{
    public Action OnClicked;

    public void OnPointerClick(PointerEventData eventData)
    {
        OnClicked?.Invoke();
    }
}
