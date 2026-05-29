using UnityEngine;
using System.Collections;

public class ElevatorController : MonoBehaviour
{
    public Transform[] stopPoints; // Kéo các điểm dừng vào đây
    public float speed = 3f;       // Tốc độ di chuyển

    private int currentIndex = 0;
    private bool isMoving = false;

    // Gọi hàm này từ nút bấm với floorIndex (0,1,2...)
    public void GoToFloor(int floorIndex)
    {
        if (floorIndex < 0 || floorIndex >= stopPoints.Length) return;
        if (floorIndex == currentIndex || isMoving) return;

        StopAllCoroutines();
        StartCoroutine(MoveTo(floorIndex));
    }

    IEnumerator MoveTo(int index)
    {
        isMoving = true;
        Vector3 target = stopPoints[index].position;
        while (Vector3.Distance(transform.position, target) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
            yield return null;
        }
        transform.position = target;
        currentIndex = index;
        isMoving = false;
    }
}