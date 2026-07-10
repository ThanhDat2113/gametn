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

    private Vector3 _initialScale;
    private Vector3 _targetScale;
    private Vector3 _velocity;
    private float _elapsedTime;
    private bool _hasReachedPeak;
    private Color _startColor;

    private void Awake()
    {
        if (textMesh == null)
            textMesh = GetComponentInChildren<TextMeshProUGUI>();
    }

    /// <summary>
    /// Hiển thị text tại vị trí world.
    /// </summary>
    public void Show(int damage, Vector3 worldPosition, Color textColor, Vector2 direction, bool isFinalHit = false)
    {
        textMesh.text = damage.ToString();
        textMesh.color = textColor;

        // Vị trí world trực tiếp (không convert)
        transform.position = worldPosition;

        // Scale
        float scaleMultiplier = isFinalHit ? 1.8f : 1.0f;
        _initialScale = Vector3.one * scaleMultiplier;
        _targetScale = _initialScale * 0.5f;
        transform.localScale = _initialScale;

        // Hướng bay: direction đã được chuẩn hóa, nhân với vận tốc
        // direction = (dirX, 0.5) đã normalize
        _velocity = new Vector3(direction.x, direction.y, 0) * initialVelocity;

        // Thêm ngẫu nhiên nhẹ theo trục X để trông tự nhiên
        _velocity.x += Random.Range(-horizontalSpread * 0.5f, horizontalSpread * 0.5f);

        _elapsedTime = 0f;
        _hasReachedPeak = false;
        _startColor = textMesh.color;

        StartCoroutine(Animate());
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

            // Trọng lực
            _velocity.y -= gravity * Time.deltaTime;

            // Di chuyển
            transform.position += _velocity * Time.deltaTime;

            // Kiểm tra đỉnh
            if (!_hasReachedPeak && _velocity.y < 0)
                _hasReachedPeak = true;

            // Mờ dần sau khi đạt đỉnh
            float alpha = 1f;
            if (_hasReachedPeak)
            {
                float peakTime = initialVelocity / gravity; // thời gian lên đến đỉnh
                float fadeElapsed = (_elapsedTime - peakTime) / (lifetime - peakTime);
                alpha = Mathf.Lerp(1f, 0f, Mathf.Clamp01(fadeElapsed));
            }

            textMesh.color = new Color(_startColor.r, _startColor.g, _startColor.b, alpha);

            // Co nhỏ dần
            float t = _elapsedTime / lifetime;
            transform.localScale = Vector3.Lerp(_initialScale, _targetScale, t);

            yield return null;
        }

        gameObject.SetActive(false);
    }
}