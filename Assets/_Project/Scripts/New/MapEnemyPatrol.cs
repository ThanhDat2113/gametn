using UnityEngine;
using System.Collections;

public class MapEnemyPatrol : MonoBehaviour
{
    [Header("Movement")]
    public float moveDistance = 2f;
    public float moveSpeed = 1f;
    public float waitTime = 2f;

    [Header("Animation")]
    public Animator animator;

    private Vector3 startPos;
    private Coroutine patrolRoutine;
    private bool isMoving = false;

    void Start()
    {
        startPos = transform.position;
        patrolRoutine = StartCoroutine(PatrolLoop());
    }

    IEnumerator PatrolLoop()
    {
        while (true)
        {
            // Di chuyển đến điểm bên phải
            Vector3 targetRight = startPos + Vector3.right * moveDistance;
            yield return MoveTo(targetRight);

            // Đứng yên tại điểm bên phải
            yield return WaitAndIdle(waitTime);

            // Di chuyển về vị trí cũ
            yield return MoveTo(startPos);

            // Đứng yên tại điểm xuất phát
            yield return WaitAndIdle(waitTime);
        }
    }

    IEnumerator MoveTo(Vector3 target)
    {
        isMoving = true;
        SetAnimMoving(true);

        // Flip sprite theo hướng di chuyển
        Vector3 dir = target - transform.position;
        if (dir.x > 0.01f)
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        else if (dir.x < -0.01f)
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);

        while (Vector3.Distance(transform.position, target) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = target;

        isMoving = false;
        SetAnimMoving(false);
    }

    IEnumerator WaitAndIdle(float duration)
    {
        isMoving = false;
        SetAnimMoving(false);
        yield return new WaitForSeconds(duration);
    }

    void SetAnimMoving(bool moving)
    {
        if (animator != null)
            animator.SetBool("IsMoving", moving);
    }

    public void StopPatrol()
    {
        if (patrolRoutine != null)
        {
            StopCoroutine(patrolRoutine);
            patrolRoutine = null;
        }
        SetAnimMoving(false);
        isMoving = false;
    }

    public void ResumePatrol()
    {
        if (patrolRoutine == null)
        {
            patrolRoutine = StartCoroutine(PatrolLoop());
        }
    }

    public bool IsMoving() => isMoving;

    public void ResetStartPosition()
    {
        startPos = transform.position;
    }
}