using UnityEngine;

/// <summary>
/// Đặt component này trên Player GameObject.
/// Trong Block()/Unblock() hãy disable/enable script movement của bạn.
/// </summary>
public class InputBlocker : MonoBehaviour
{
    public static InputBlocker Instance { get; private set; }

    private int _blockCount = 0;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void Block()
    {
        _blockCount++;
        // Thêm dòng này, thay YourPlayerMovement bằng script của bạn:
        // GetComponent<YourPlayerMovement>().enabled = false;
    }

    public void Unblock()
    {
        _blockCount = Mathf.Max(0, _blockCount - 1);
        if (_blockCount == 0)
        {
            // GetComponent<YourPlayerMovement>().enabled = true;
        }
    }

    public bool IsBlocked => _blockCount > 0;
}