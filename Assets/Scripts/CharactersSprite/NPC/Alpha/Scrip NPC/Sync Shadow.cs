using UnityEngine;

public class SyncShadow : MonoBehaviour
{
    [Header("Animator bản gốc (sprite chính)")]
    [SerializeField] private Animator sourceAnimator;

    [Header("Tùy chọn")]
    [SerializeField] private bool syncSpeed = true;

    private Animator targetAnimator;

    private void Awake()
    {
        targetAnimator = GetComponent<Animator>();
    }

    private void LateUpdate()
    {
        if (sourceAnimator == null || targetAnimator == null)
            return;

        SyncAnimatorState(targetAnimator);

        if (syncSpeed)
            targetAnimator.speed = sourceAnimator.speed;
    }

    private void SyncAnimatorState(Animator target)
    {
        int layerCount = Mathf.Min(sourceAnimator.layerCount, target.layerCount);

        for (int layer = 0; layer < layerCount; layer++)
        {
            AnimatorStateInfo stateInfo = sourceAnimator.GetCurrentAnimatorStateInfo(layer);
            target.Play(stateInfo.fullPathHash, layer, stateInfo.normalizedTime % 1f);
        }
    }
}
