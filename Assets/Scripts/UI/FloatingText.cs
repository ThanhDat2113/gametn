using UnityEngine;
using TMPro;

public class FloatingText : MonoBehaviour
{
    public TextMeshProUGUI textMesh;
    public float lifetime = 1.5f;
    public Vector3 floatDirection = new Vector3(0, 1, 0);
    public float floatSpeed = 1f;

    private float timer;

    void Start()
    {
        textMesh = GetComponentInChildren<TextMeshProUGUI>();
        timer = lifetime;
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            Destroy(gameObject);
        }

        transform.position += floatDirection * floatSpeed * Time.deltaTime;

        // Fade out
        Color color = textMesh.color;
        color.a = timer / lifetime;
        textMesh.color = color;
    }

    public void SetText(string text, Color color)
    {
        if (textMesh == null)
        {
            textMesh = GetComponentInChildren<TextMeshProUGUI>();
        }
        textMesh.text = text;
        textMesh.color = color;
    }
}