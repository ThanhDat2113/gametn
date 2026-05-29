using UnityEngine;

public class ElevatorButton : MonoBehaviour
{
    public ElevatorController elevator;
    public int targetFloor = 1; // 0 là tầng 1, 1 là tầng 2...

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            elevator.GoToFloor(targetFloor);
        }
    }
}