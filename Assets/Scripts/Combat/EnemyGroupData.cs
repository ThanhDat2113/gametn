using System;
using UnityEngine;

/// <summary>
/// ScriptableObject định nghĩa 1 trận đấu cụ thể:
/// loại quái, số lượng, vị trí, level.
///
/// Tạo asset: chuột phải trong Project → Create → RPG → EnemyGroup
/// Mỗi trận đấu tạo 1 asset riêng, ví dụ:
///   EG_Forest_01, EG_Boss_Chapter1, EG_Cave_03...
///
/// Lưới 3x3 — cách đánh số ô (GridSlot):
///
///   ENEMY SIDE (nhìn từ player)
///   [Col 0] [Col 1] [Col 2]
///    (6)     (7)     (8)    ← Row 0  (hàng xa nhất, Back)
///    (3)     (4)     (5)    ← Row 1  (hàng giữa,   Mid)
///    (0)     (1)     (2)    ← Row 2  (hàng gần nhất, Front)
///
/// GridSlot = Row * 3 + Col  →  0..8
/// GridRow  = GridSlot / 3   →  0=Back, 1=Mid, 2=Front
/// </summary>
[CreateAssetMenu(fileName = "EG_New", menuName = "RPG/EnemyGroup")]
public class EnemyGroupData : ScriptableObject
{
    [Serializable]
    public class EnemyEntry
    {
        [Tooltip("CharacterData của kẻ địch")]
        public CharacterData data;

        [Tooltip("Level của kẻ địch")]
        public int level = 1;

        [Tooltip("Ô trong lưới 3x3 (0-8). Row=slot/3, Col=slot%3.\n" +
                 "0-2 = hàng Front, 3-5 = Mid, 6-8 = Back")]
        [Range(0, 8)]
        public int gridSlot = 0;
    }

    [Tooltip("Tối đa 9 kẻ địch (1 ô / 1 kẻ địch)")]
    public EnemyEntry[] enemies;
}