using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class HSRPlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float sprintSpeed = 9f;
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

        // Tạo AudioSource riêng cho footstep để có thể stop ngay khi dừng
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
                    float flipX = Mathf.Sign(moveX) * _initialScale.x;
                    spriteContainer.localScale = new Vector3(flipX, _initialScale.y, _initialScale.z);
                }
            }

            // Footstep — không delay, play footstep lần đầu ngay
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
                // Dừng footstep ngay lập tức
                _footstepSource.Stop();
            }
            footstepTimer = 0f;
            UpdateAnimation(false, 0, 0);
        }

        if (!_controller.isGrounded)
            _controller.Move(Vector3.down * 5f * Time.deltaTime);

        if (spriteContainer != null)
        {
            spriteContainer.LookAt(cameraTransform.position);
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
}