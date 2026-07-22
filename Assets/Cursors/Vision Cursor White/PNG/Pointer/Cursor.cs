using UnityEngine;

public class CursorManager : MonoBehaviour
{
    [SerializeField] private Texture2D cursorTexture;

    void Start()
    {
        // Change these values depending on where you want the click point.
        Vector2 hotspot = new Vector2(2, 2);

        Cursor.SetCursor(cursorTexture, hotspot, CursorMode.Auto);
    }
}