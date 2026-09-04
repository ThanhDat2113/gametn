using UnityEngine;

public class SyncShadow : MonoBehaviour
{
    [Header("Animator bản gốc (sprite chính)")]
    [SerializeField] private Animator sourceAnimator;

    [Header("Tùy chọn")]
    [SerializeField] private bool syncSpeed = true;
    [SerializeField] private bool syncFacing = true;

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

        if (syncFacing)
            SyncFacing();
    }

    private void SyncFacing()
    {
        Vector3 sourceScale = sourceAnimator.transform.localScale;
        Vector3 targetScale = targetAnimator.transform.localScale;

        if (Mathf.Abs(sourceScale.x) > 0.01f)
            targetScale.x = Mathf.Sign(sourceScale.x) * Mathf.Abs(targetScale.x);

        targetAnimator.transform.localScale = targetScale;
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
