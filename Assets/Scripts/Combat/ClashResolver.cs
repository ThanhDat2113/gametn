using UnityEngine;

// ClashResult.cs — thêm visual data
public class ClashResult
{
    public CombatUnit Winner;
    public CombatUnit Loser;
    public SkillData WinnerSkill;
    public SkillData LoserSkill;
    public int WinnerScore;
    public int LoserScore;
    public ClashVisualData VisualData; // thêm dòng này
}

public class ClashResolver
{
    // Tung xúc xắc có tính Luck
    private int RollScore(int basePoint, int luck)
    {
        int dice = Random.Range(1, 7);          // 1~6
        int luckBonus = luck / 20;                   // mỗi 20 Luck → +1
        return basePoint + dice + luckBonus;
    }

    public ClashResult Resolve(CombatUnit attacker, CombatUnit defender,
                            SkillData atkSkill, SkillData defSkill)
    {
        int atkDice, defDice;
        int atkScore, defScore;
        int attempts = 0;

        do
        {
            atkDice = Random.Range(1, 7);
            defDice = Random.Range(1, 7);
            atkScore = atkSkill.basePoint + atkDice + attacker.Luck / 20;
            defScore = defSkill.basePoint + defDice + defender.Luck / 20;
            attempts++;
        }
        while (atkScore == defScore && attempts < 10);

        bool attackerWins = atkScore > defScore;

        return new ClashResult
        {
            Winner = attackerWins ? attacker : defender,
            Loser = attackerWins ? defender : attacker,
            WinnerSkill = attackerWins ? atkSkill : defSkill,
            LoserSkill = attackerWins ? defSkill : atkSkill,
            WinnerScore = attackerWins ? atkScore : defScore,
            LoserScore = attackerWins ? defScore : atkScore,

            // Data cho visual
            VisualData = new ClashVisualData
            {
                PlayerBasePoint = atkSkill.basePoint,
                PlayerDiceResult = atkDice,
                PlayerTotalScore = atkScore,
                EnemyBasePoint = defSkill.basePoint,
                EnemyDiceResult = defDice,
                EnemyTotalScore = defScore,
                PlayerWins = attackerWins
            }
        };
    }
}
