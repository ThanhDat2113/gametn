using UnityEngine;
using System.Collections.Generic;

public class FloatingTextController : MonoBehaviour
{
    public static FloatingTextController Instance { get; private set; }

    public GameObject floatingTextPrefab;
    private List<FloatingText> pool = new List<FloatingText>();

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    public void ShowFloatingText(string text, Vector3 position, Color color)
    {
        FloatingText textObject = GetPooledText();
        if (textObject == null) return;

        textObject.transform.position = position;
        textObject.SetText(text, color);
        textObject.gameObject.SetActive(true);
    }

    private FloatingText GetPooledText()
    {
        foreach (var text in pool)
        {
            if (!text.gameObject.activeInHierarchy)
            {
                return text;
            }
        }

        if (floatingTextPrefab != null)
        {
            GameObject newTextGO = Instantiate(floatingTextPrefab, transform);
            FloatingText newText = newTextGO.GetComponent<FloatingText>();
            pool.Add(newText);
            return newText;
        }

        return null;
    }
}