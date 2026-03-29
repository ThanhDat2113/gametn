using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class HSRPlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float sprintSpeed = 9f;
    
    [Header("Visual References")]
    public Animator animator;
    public Transform spriteContainer; 

    private CharacterController _controller;
    private Vector3 _moveDir;
    private Vector3 _initialScale; // 1. Thêm biến lưu scale ban đầu

    void Awake() 
    {
        _controller = GetComponent<CharacterController>();
        
        // 2. Lưu lại scale bạn đã chỉnh trong Inspector lúc bắt đầu game
        if (spriteContainer != null)
        {
            _initialScale = spriteContainer.localScale;
        }
    }

    void Update()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        _moveDir = new Vector3(h, 0, v).normalized;

        if (_moveDir.magnitude >= 0.1f)
        {
            float speed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : moveSpeed;
            _controller.Move(_moveDir * speed * Time.deltaTime);

            // 3. SỬA TẠI ĐÂY: Lật sprite mà không làm mất kích thước thật
            if (h != 0 && spriteContainer != null)
            {
                // Chỉ đảo dấu trục X dựa trên hướng di chuyển h
                // Giữ nguyên Y và Z từ _initialScale
                float flipX = Mathf.Sign(h) * _initialScale.x;
                spriteContainer.localScale = new Vector3(flipX, _initialScale.y, _initialScale.z);
            }

            UpdateAnimation(true, h, v);
        }
        else
        {
            UpdateAnimation(false, 0, 0);
        }

        // Trọng lực
        if (!_controller.isGrounded) _controller.Move(Vector3.down * 5f * Time.deltaTime);
    }

    void UpdateAnimation(bool isMoving, float x, float y)
    {
        if (animator == null) return;
        animator.SetBool("IsMoving", isMoving);
        
        // Chỉ cập nhật hướng khi thực sự di chuyển 
        // để khi đứng yên nhân vật không bị quay về hướng mặc định
        if (isMoving)
        {
            animator.SetFloat("MoveX", x);
            animator.SetFloat("MoveY", y);
        }
    }
}