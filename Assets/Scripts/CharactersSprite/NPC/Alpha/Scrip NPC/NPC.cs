using UnityEngine;
using System.Collections;

public class PatrolNPC2D : MonoBehaviour
{
    [Header("Điểm tuần tra")]
    public Transform[] waypoints;

    [Header("Tốc độ di chuyển")]
    public float moveSpeed = 2f;

    [Header("Thời gian nghỉ (giây)")]
    public float idleTime = 2f;

    [Header("Sprite con (để lật mặt)")]
    public Transform spriteChild;

    [Header("Animator (để chạy animation)")]
    public Animator animator;      // Kéo Animator của Sprite vào đây
    public string walkBoolParam = "IsWalking"; // Tên tham số bool trong Animator

    private int currentIndex = 0;
    private bool isIdle = false;

    void Start()
    {
        if (waypoints.Length == 0)
        {
            Debug.LogError("Hãy kéo các Waypoint vào mảng waypoints!");
            return;
        }
        if (spriteChild == null)
            Debug.LogWarning("Chưa gán spriteChild, sẽ không lật mặt!");
        if (animator == null)
            animator = GetComponentInChildren<Animator>(); // Tự tìm nếu quên gán

        StartCoroutine(Patrol());
    }

    IEnumerator Patrol()
    {
        while (true)
        {
            Transform target = waypoints[currentIndex];

            // Bắt đầu di chuyển -> bật animation Walk
            isIdle = false;
            if (animator != null)
                animator.SetBool(walkBoolParam, true);

            // Di chuyển đến khi gần điểm đích
            while (Vector3.Distance(transform.position, target.position) > 0.1f)
            {
                Vector3 newPos = Vector3.MoveTowards(transform.position, target.position, moveSpeed * Time.deltaTime);
                transform.position = newPos;

                // Lật sprite theo hướng di chuyển
                if (spriteChild != null)
                {
                    Vector3 moveDir = (target.position - transform.position).normalized;
                    if (Mathf.Abs(moveDir.x) > 0.01f)
                    {
                        Vector3 scale = spriteChild.localScale;
                        scale.x = Mathf.Sign(moveDir.x) * Mathf.Abs(scale.x);
                        spriteChild.localScale = scale;
                    }
                }

                yield return null;
            }

            // Đã đến điểm, bắt đầu nghỉ -> bật animation Idle
            isIdle = true;
            if (animator != null)
                animator.SetBool(walkBoolParam, false);

            Debug.Log($"Đã tới {target.name}, nghỉ {idleTime} giây.");
            yield return new WaitForSeconds(idleTime);

            // Chuyển sang điểm tiếp theo (vòng tròn)
            currentIndex = (currentIndex + 1) % waypoints.Length;
        }
    }

    // Vẽ đường nối giữa các waypoint trong Scene view
    void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Length == 0)
            return;
        Gizmos.color = Color.blue;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] != null)
            {
                Gizmos.DrawSphere(waypoints[i].position, 0.2f);
                int next = (i + 1) % waypoints.Length;
                if (waypoints[next] != null)
                    Gizmos.DrawLine(waypoints[i].position, waypoints[next].position);
            }
        }
    }
}