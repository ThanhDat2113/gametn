// CombatEnums.cs
public enum CharacterType
{
    Player,     // Nhân vật người chơi (team của ta)
    Enemy       // Quái địch (có thể cho EXP)
}

public enum SkillType 
{ 
    Auto,       // Kỹ năng chủ động (dùng trong lượt)
    Passive     // Kỹ năng bị động (nội tại)
}

public enum DamageType 
{ 
    Physical, 
    Magical, 
    True 
}

public enum TargetType 
{ 
    SingleEnemy, 
    SingleAlly, 
    AllEnemies, 
    AllAllies, 
    Self 
}

public enum StatType 
{ 
    HP, 
    MaxHP,
    ATK, 
    PDEF, 
    MDEF, 
    Speed       // Thay Luck bằng Speed (vì có buff/debuff speed)
}

public enum SkillEffectTrigger 
{ 
    OnUse       // Chỉ còn trigger khi sử dụng skill (bỏ clash win/lose, round end)
}