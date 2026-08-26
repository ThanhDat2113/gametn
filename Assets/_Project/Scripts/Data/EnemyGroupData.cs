using System;
using UnityEngine;

[CreateAssetMenu(fileName = "EG_New", menuName = "RPG/EnemyGroup")]
public class EnemyGroupData : ScriptableObject
{
    [Serializable]
    public class EnemyEntry
    {
        public CharacterData data;
        public int level = 1;
        [Range(0, 8)]
        public int gridSlot = 0;
    }

    [Header("BGM & Zone")]
    public int combatArea = 1;
    public AudioClip bgmClip;

    [Header("Background")]
    [Tooltip("Background image riêng cho enemy group này khi vào combat scene.")]
    public Sprite backgroundImage;

    [Header("Audio")]
    public AudioClip introStinger;
    public AudioClip victoryFanfare;

    [Header("Enemies")]
    public EnemyEntry[] enemies;
}