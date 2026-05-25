using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class HSRPlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float sprintSpeed = 9f;
    public Transform cameraTransform;   // Kéo Camera chính vào đây

    [Header("Visual References")]
    public Animator animator;
    public Transform spriteContainer; 

    private CharacterController _controller;
    private Vector3 _moveDir;
    private Vector3 _initialScale;

    void Awake()
    {
        _controller = GetComponent<CharacterController>();
        if (spriteContainer != null)
            _initialScale = spriteContainer.localScale;
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        
        // Lấy hướng forward và right từ camera, bỏ qua độ nghiêng (chỉ lấy ngang)
        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;
        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();

        // Hướng di chuyển tương đối theo camera
        Vector3 move = (camForward * v + camRight * h).normalized;
        _moveDir = move;

        if (_moveDir.magnitude >= 0.1f)
        {
            float speed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : moveSpeed;
            _controller.Move(_moveDir * speed * Time.deltaTime);

            // Lật sprite dựa trên hướng di chuyển so với camera
            if (spriteContainer != null)
            {
                // Tính hướng di chuyển trong không gian world, lấy thành phần X
                float moveX = _moveDir.x;
                if (moveX != 0)
                {
                    float flipX = Mathf.Sign(moveX) * _initialScale.x;
                    spriteContainer.localScale = new Vector3(flipX, _initialScale.y, _initialScale.z);
                }
            }

            UpdateAnimation(true, h, v);
        }
        else
        {
            UpdateAnimation(false, 0, 0);
        }

        // Trọng lực
        if (!_controller.isGrounded)
            _controller.Move(Vector3.down * 5f * Time.deltaTime);

        // Billboard cho sprite (luôn hướng về camera) – nếu bạn chưa có script Billboard
        if (spriteContainer != null)
        {
            spriteContainer.LookAt(cameraTransform.position);
            // Chỉ xoay theo trục Y (nếu sprite bị nghiêng)
            spriteContainer.rotation = Quaternion.Euler(0, spriteContainer.eulerAngles.y, 0);
        }
    }

    void UpdateAnimation(bool isMoving, float x, float y)
    {
        if (animator == null) return;
        animator.SetBool("IsMoving", isMoving);
        if (isMoving)
        {
            animator.SetFloat("MoveX", x);
            animator.SetFloat("MoveY", y);
        }
    }
}