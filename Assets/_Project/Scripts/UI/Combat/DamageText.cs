using UnityEngine;
using TMPro;
using System.Collections;

public class DamageText : MonoBehaviour
{
    public TextMeshProUGUI textMesh;
    public float lifetime = 1.5f;

    [Header("Jump Physics (World Units)")]
    [Tooltip("Vận tốc ban đầu (world units/giây)")]
    public float initialVelocity = 3f;
    [Tooltip("Gia tốc trọng trường (world units/giây²)")]
    public float gravity = 9f;

    [Header("Random Spread (World Units)")]
    [Tooltip("Biên độ dịch ngang ngẫu nhiên (world units)")]
    public float horizontalSpread = 0.8f;

    [Header("Buff Float Settings")]
    [Tooltip("Vận tốc bay lên của buff (world units/giây)")]
    public float buffFloatSpeed = 1.5f;
    [Tooltip("Giảm tốc của buff (world units/giây²)")]
    public float buffDeceleration = 0.8f;

    private Vector3 _initialScale;
    private Vector3 _targetScale;
    private Vector3 _velocity;
    private float _elapsedTime;
    private bool _hasReachedPeak;
    private Color _startColor;
    private Coroutine _animCoroutine;
    private bool _isBuff;

    private void Awake()
    {
        if (textMesh == null)
            textMesh = GetComponentInChildren<TextMeshProUGUI>();
    }

    /// <summary>
    /// Hiển thị text chuỗi (status/buff/damage) tại vị trí world.
    /// </summary>
    /// <param name="isBuff">true = buff bay lên nhẹ nhàng, false = nhảy lên rồi rơi xuống</param>
    public void Show(string text, Vector3 worldPosition, Color textColor, Vector2 direction, bool isFinalHit = false, bool isStatusText = false, bool isBuff = false)
    {
        textMesh.text = text;
        textMesh.color = textColor;

        // Vị trí world trực tiếp (không convert)
        transform.position = worldPosition;

        // Scale: status text to hơn để dễ nhận biết
        float scaleMultiplier = isFinalHit ? 1.8f : (isStatusText ? 1.35f : 1.0f);
        _initialScale = Vector3.one * scaleMultiplier;
        _targetScale = _initialScale * 0.5f;
        transform.localScale = _initialScale;

        _isBuff = isBuff;

        if (isBuff)
        {
            // Buff text: bay lên nhẹ nhàng, không gravity
            _velocity = new Vector3(direction.x * buffFloatSpeed * 0.3f, buffFloatSpeed, 0);
        }
        else if (isStatusText)
        {
            // Status text: bay ngang nhiều hơn (ngược với damage), lên ít
            _velocity = new Vector3(direction.x * initialVelocity * 1.2f, initialVelocity * 0.5f, 0);
        }
        else
        {
            // Damage text: nhảy lên rồi rơi xuống (giữ nguyên)
            _velocity = new Vector3(direction.x, direction.y, 0) * initialVelocity;
            _velocity.x += Random.Range(-horizontalSpread * 0.5f, horizontalSpread * 0.5f);
        }

        _elapsedTime = 0f;
        _hasReachedPeak = false;
        _startColor = textMesh.color;

        // Dừng coroutine cũ để tránh chồng lên nhau khi object được pool tái sử dụng
        if (_animCoroutine != null)
            StopCoroutine(_animCoroutine);
        _animCoroutine = StartCoroutine(Animate());
    }

    /// <summary>
    /// Hiển thị text tại vị trí world (damage).
    /// </summary>
    public void Show(int damage, Vector3 worldPosition, Color textColor, Vector2 direction, bool isFinalHit = false)
    {
        Show(damage.ToString(), worldPosition, textColor, direction, isFinalHit, false, false);
    }

    // Overload không direction (dùng ngẫu nhiên)
    public void Show(int damage, Vector3 worldPosition, Color textColor, bool isFinalHit = false)
    {
        Vector2 randomDir = new Vector2(Random.Range(-1f, 1f), 0.5f).normalized;
        Show(damage, worldPosition, textColor, randomDir, isFinalHit);
    }

    // Overload cũ để tương thích
    public void Show(int damage, Vector3 worldPosition, bool isFinalHit, bool isCrit)
    {
        Color color = isCrit ? Color.yellow : Color.white;
        Show(damage, worldPosition, color, Vector2.up, isFinalHit);
    }

    private IEnumerator Animate()
    {
        while (_elapsedTime < lifetime)
        {
            _elapsedTime += Time.deltaTime;

            if (_isBuff)
            {
                // Buff: bay lên chậm dần, fade out sớm
                _velocity.y -= buffDeceleration * Time.deltaTime;
                if (_velocity.y < 0.1f) _velocity.y = 0.1f;
                transform.position += _velocity * Time.deltaTime;

                float alpha = Mathf.Lerp(1f, 0f, _elapsedTime / (lifetime * 0.7f));
                textMesh.color = new Color(_startColor.r, _startColor.g, _startColor.b, Mathf.Clamp01(alpha));

                float t = _elapsedTime / lifetime;
                transform.localScale = Vector3.Lerp(_initialScale, _targetScale, t);
            }
            else
            {
                // Damage & Status: nhảy lên rồi rơi xuống
                _velocity.y -= gravity * Time.deltaTime;
                transform.position += _velocity * Time.deltaTime;

                if (!_hasReachedPeak && _velocity.y < 0)
                    _hasReachedPeak = true;

                float alpha = 1f;
                if (_hasReachedPeak)
                {
                    float peakTime = initialVelocity / gravity;
                    float fadeElapsed = (_elapsedTime - peakTime) / (lifetime - peakTime);
                    alpha = Mathf.Lerp(1f, 0f, Mathf.Clamp01(fadeElapsed));
                }

                textMesh.color = new Color(_startColor.r, _startColor.g, _startColor.b, alpha);

                float t = _elapsedTime / lifetime;
                transform.localScale = Vector3.Lerp(_initialScale, _targetScale, t);
            }

            yield return null;
        }

        gameObject.SetActive(false);
    }
}
