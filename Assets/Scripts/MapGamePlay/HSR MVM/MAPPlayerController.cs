using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class HSRPlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float sprintSpeed = 9f;
    public float gravityForce = 15f;
    public Transform cameraTransform;

    [Header("Visual References")]
    public Animator animator;
    public Transform spriteContainer;

    [Header("Audio")]
    public AudioClip[] footstepSounds;
    public float footstepInterval = 0.45f;
    [Range(0f, 1f)]
    public float footstepVolume = 0.3f;

    private CharacterController _controller;
    private Vector3 _moveDir;
    private Vector3 _initialScale;
    private float footstepTimer = 0f;
    private int lastFootstepIndex = -1;
    private bool _wasMoving = false;
    private AudioSource _footstepSource;

    void Awake()
    {
        _controller = GetComponent<CharacterController>();
        if (spriteContainer != null)
            _initialScale = spriteContainer.localScale;
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;

        _footstepSource = gameObject.AddComponent<AudioSource>();
        _footstepSource.playOnAwake = false;
        _footstepSource.spatialBlend = 1f;
        _footstepSource.volume = footstepVolume;
    }

    void Update()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;
        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 move = (camForward * v + camRight * h).normalized;
        _moveDir = move;

        if (_moveDir.magnitude >= 0.1f)
        {
            float speed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : moveSpeed;
            _controller.Move(_moveDir * speed * Time.deltaTime);

            if (spriteContainer != null)
            {
                float moveX = _moveDir.x;
                if (moveX != 0)
                {
                    float flipX = Mathf.Sign(moveX) * Mathf.Abs(_initialScale.x);
                    spriteContainer.localScale = new Vector3(flipX, _initialScale.y, _initialScale.z);
                }
            }

            footstepTimer -= Time.deltaTime;
            if (!_wasMoving)
            {
                _wasMoving = true;
                footstepTimer = 0f;
            }

            if (footstepTimer <= 0f)
            {
                AudioClip clip = GetFootstepClip();
                if (clip != null)
                {
                    _footstepSource.Stop();
                    _footstepSource.clip = clip;
                    _footstepSource.Play();
                }
                footstepTimer = footstepInterval;
            }

            UpdateAnimation(true, h, v);
        }
        else
        {
            if (_wasMoving)
            {
                _wasMoving = false;
                _footstepSource.Stop();
            }
            footstepTimer = 0f;
            UpdateAnimation(false, 0, 0);
        }

        if (!_controller.isGrounded)
            _controller.Move(Vector3.down * gravityForce * Time.deltaTime);

        if (spriteContainer != null)
        {
            spriteContainer.LookAt(cameraTransform.position);
            spriteContainer.rotation = Quaternion.Euler(0, spriteContainer.eulerAngles.y, 0);
        }
    }

    /// <summary>
    /// Khi script bị disable (VD: do DialogueTrigger tắt để dừng player),
    /// reset animation về Idle ngay lập tức để tránh bị kẹt ở trạng thái Run.
    /// </summary>
    private void OnDisable()
    {
        ResetToIdle();
    }

    /// <summary>
    /// Reset tất cả animation state về Idle và dừng footstep.
    /// Gọi được từ bên ngoài (VD: DialogueTrigger) nếu cần.
    /// </summary>
    public void ResetToIdle()
    {
        // Dừng footstep sound
        if (_footstepSource != null && _footstepSource.isPlaying)
            _footstepSource.Stop();

        _wasMoving = false;
        footstepTimer = 0f;

        // Reset animation về idle
        UpdateAnimation(false, 0, 0);
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
        else
        {
            // Khi về idle, reset MoveX/MoveY về 0 tránh bị blend tree giữ animation chạy
            animator.SetFloat("MoveX", 0f);
            animator.SetFloat("MoveY", 0f);
        }
    }

    AudioClip GetFootstepClip()
    {
        if (footstepSounds == null || footstepSounds.Length == 0)
            return null;

        int index;
        do
        {
            index = Random.Range(0, footstepSounds.Length);
        } while (index == lastFootstepIndex && footstepSounds.Length > 1);

        lastFootstepIndex = index;
        return footstepSounds[index];
    }

    // ==================== DIALOGUE FLIP API ====================

    /// <summary>
    /// Trả về true nếu spriteContainer đang nhìn sang phải (scale.x dương).
    /// </summary>
    public bool IsFacingRight()
    {
        if (spriteContainer == null) return true;
        return spriteContainer.localScale.x > 0;
    }

    /// <summary>
    /// Đặt hướng nhìn của spriteContainer.
    /// facingRight=true → scale.x dương; facingRight=false → scale.x âm.
    /// </summary>
    public void SetFacingDirection(bool facingRight)
    {
        if (spriteContainer == null) return;
        float absX = Mathf.Abs(_initialScale.x);
        spriteContainer.localScale = new Vector3(
            facingRight ? absX : -absX,
            _initialScale.y,
            _initialScale.z
        );
    }
}