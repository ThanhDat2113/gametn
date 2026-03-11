using System;
using UnityEngine;

/// <summary>
/// Lưu đội hình player: ai đứng ô nào.
/// Được tạo từ Formation UI rồi truyền vào CombatManager.
///
/// Lưới 3x3 — cách đánh số ô (GridSlot):
///
///   PLAYER SIDE
///   [Col 0] [Col 1] [Col 2]
///    (6)     (7)     (8)    ← Row 0  (Back)
///    (3)     (4)     (5)    ← Row 1  (Mid)
///    (0)     (1)     (2)    ← Row 2  (Front)
///
/// GridSlot = Row * 3 + Col  →  0..8
/// </summary>
[Serializable]
public class FormationSlot
{
    public CharacterData data;
    public int level = 1;
    public int gridSlot; // 0..8
}

/// <summary>
/// Runtime formation data — không phải ScriptableObject,
/// được tạo bởi FormationUI hoặc hard-code trong CombatTestUI.
/// </summary>
public class FormationData
{
    public FormationSlot[] slots; // tối đa 5 slot có data, còn lại null
}