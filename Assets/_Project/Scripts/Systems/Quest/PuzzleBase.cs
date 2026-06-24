using System;
using UnityEngine;

/// <summary>
/// Abstract base class cho tất cả các puzzle UI controllers.
/// Mỗi puzzle type sẽ kế thừa class này và implement logic riêng.
/// </summary>
public abstract class PuzzleBase : MonoBehaviour
{
    /// <summary>
    /// PuzzleData chứa config của puzzle này.
    /// </summary>
    protected PuzzleData puzzleData;

    /// <summary>
    /// Event khi puzzle kết thúc. bool = true nếu hoàn thành, false nếu thất bại/thoát.
    /// </summary>
    public event Action<bool> OnPuzzleFinished;

    /// <summary>
    /// Khởi tạo và bắt đầu puzzle với dữ liệu từ PuzzleData.
    /// </summary>
    /// <param name="data">PuzzleData ScriptableObject chứa config</param>
    /// <param name="source">PuzzleTrigger đã kích hoạt puzzle này</param>
    public virtual void StartPuzzle(PuzzleData data, PuzzleTrigger source)
    {
        puzzleData = data;
        gameObject.SetActive(true);
    }

    /// <summary>
    /// Gọi khi người chơi hoàn thành hoặc thoát puzzle.
    /// </summary>
    /// <param name="success">True nếu hoàn thành, false nếu thất bại/thoát</param>
    protected void CompletePuzzle(bool success)
    {
        Debug.Log($"[Puzzle] {(success ? "✅ Hoàn thành" : "❌ Thất bại")}: {puzzleData?.puzzleName}");

        OnPuzzleFinished?.Invoke(success);
        ClosePuzzle();
    }

    /// <summary>
    /// Đóng và destroy puzzle UI.
    /// </summary>
    public abstract void ClosePuzzle();
}