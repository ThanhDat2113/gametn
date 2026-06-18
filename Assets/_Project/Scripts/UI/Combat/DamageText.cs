using UnityEngine;
using TMPro;
using System.Collections;

public class DamageText : MonoBehaviour
{
    public TextMeshProUGUI textMesh;
    private float _lifeTime = 1.5f;
    private float _moveSpeed = 100f;
    private float _fadeSpeed = 1f;

    // Animation params
    private Vector3 _initialScale;
    private Vector3 _targetScale;

    public void Show(int damage, Vector3 worldPosition, bool isFinalHit, bool isCrit)
    {
        // Set giá trị và màu sắc
        textMesh.text = damage.ToString();
        textMesh.color = isCrit ? Color.yellow : Color.white;

        // Vị trí trên màn hình
        transform.position = Camera.main.WorldToScreenPoint(worldPosition);

        // Kích thước
        float scaleMultiplier = isFinalHit ? 1.8f : (isCrit ? 1.4f : 1.0f);
        _initialScale = Vector3.one * scaleMultiplier;
        _targetScale = _initialScale * 0.5f;
        transform.localScale = _initialScale;

        // Bắt đầu animation
        StartCoroutine(Animate());
    }

    private IEnumerator Animate()
    {
        float timer = 0f;
        Color startColor = textMesh.color;

        while (timer < _lifeTime)
        {
            // Di chuyển lên trên
            transform.position += Vector3.up * _moveSpeed * Time.deltaTime;

            // Co nhỏ lại
            transform.localScale = Vector3.Lerp(_initialScale, _targetScale, timer / _lifeTime);

            // Mờ dần
            float alpha = Mathf.Lerp(1f, 0f, timer / _fadeSpeed);
            textMesh.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            
            timer += Time.deltaTime;
            yield return null;
        }

        // Trả về pool
        gameObject.SetActive(false);
    }
}