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
    public Transform spriteContainer; // GameObject chứa sprite (có SpriteBillboard)

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

    // 🔥 Tham chiếu đến SpriteBillboard
    private SpriteBillboard _billboard;

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

        // 🔥 Tìm hoặc thêm SpriteBillboard vào spriteContainer
        if (spriteContainer != null)
        {
            _billboard = spriteContainer.GetComponent<SpriteBillboard>();
            if (_billboard == null)
                _billboard = spriteContainer.gameObject.AddComponent<SpriteBillboard>();
            _billboard.targetCamera = cameraTransform;
        }
    }

    void Update()
    {
        // 🔥 Chặn di chuyển khi Timeline đang chạy
        if (TimelinePlaybackManager.IsTimelinePlaying)
        {
            ResetToIdle();
            return;
        }

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

            // 🔥 Flip sprite dựa trên hướng di chuyển (dùng SpriteBillboard)
            if (_billboard != null)
                _billboard.FlipBasedOnMovement(_moveDir);

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

        // 🔥 KHÔNG cần Billboard ở đây nữa vì SpriteBillboard đã xử lý trong LateUpdate
        // Nếu muốn bật/tắt Billboard, dùng _billboard.enableBillboard
    }

    private void OnDisable()
    {
        ResetToIdle();
    }

    public void ResetToIdle()
    {
        if (_footstepSource != null && _footstepSource.isPlaying)
            _footstepSource.Stop();

        _wasMoving = false;
        footstepTimer = 0f;
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

    public bool IsFacingRight()
    {
        if (_billboard != null)
            return _billboard.IsFacingRight();
        return spriteContainer != null && spriteContainer.localScale.x > 0;
    }

    public void SetFacingDirection(bool facingRight)
    {
        if (_billboard != null)
            _billboard.SetFacingDirection(facingRight);
        else if (spriteContainer != null)
        {
            float absX = Mathf.Abs(_initialScale.x);
            spriteContainer.localScale = new Vector3(
                facingRight ? absX : -absX,
                _initialScale.y,
                _initialScale.z
            );
        }
    }
}