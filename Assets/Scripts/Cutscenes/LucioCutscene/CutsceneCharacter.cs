using UnityEngine;

/// <summary>
/// Represents a character in the cutscene
/// Simple component to track character position and state
/// </summary>
public class CutsceneCharacter : MonoBehaviour
{
    [SerializeField] private string characterName;
    [SerializeField] private int characterID; // 0 = Lucio, 1 = Cedric
    [SerializeField] private SpriteRenderer spriteRenderer;

    private Vector3 characterPosition;
    private bool isAlive = true;

    private void Start()
    {
        // Initialize character position
        characterPosition = transform.position;

        // Set square color for visual distinction
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            spriteRenderer.color = characterID == 0 ? Color.blue : Color.red;
        }
    }

    public Vector3 GetPosition()
    {
        return transform.position;
    }

    public void SetPosition(Vector3 newPosition)
    {
        transform.position = newPosition;
        characterPosition = newPosition;
    }

    public int GetCharacterID() => characterID;

    public string GetCharacterName() => characterName;

    public bool IsAlive() => isAlive;

    public void SetAlive(bool alive)
    {
        isAlive = alive;
        if (!alive && spriteRenderer != null)
        {
            spriteRenderer.color = new Color(0.5f, 0.5f, 0.5f); // Darken when defeated
        }
    }

    public Vector3 GetBeamOrigin()
    {
        // Beam originates from the right side of Lucio, left side of Cedric
        float offset = characterID == 0 ? 0.5f : -0.5f;
        return GetPosition() + Vector3.right * offset;
    }
}
