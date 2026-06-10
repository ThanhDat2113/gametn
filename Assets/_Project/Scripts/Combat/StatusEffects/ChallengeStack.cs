public class ChallengeStack
{
    public const int MaxStacks = 20;
    public int Stacks { get; private set; } = 0;

    public float GetCritRateBonus() => Stacks * 0.05f;
    public float GetCritDmgBonus() => Stacks * 0.10f;

    public void AddStack(int amount = 1)
    {
        Stacks = UnityEngine.Mathf.Min(Stacks + amount, MaxStacks);
    }

    public void Explode()
    {
        Stacks = 0;
    }
}