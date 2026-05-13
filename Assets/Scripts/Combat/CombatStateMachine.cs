public enum CombatPhase
{
    None,
    Intro,
    EnemyPlan,
    PlayerPlan,
    RetargetCheck,
    Execute,
    RoundEnd,
    Victory,
    Defeat
}

public class CombatStateMachine
{
    public CombatPhase Current { get; private set; } = CombatPhase.None;

    public event System.Action<CombatPhase, CombatPhase> OnPhaseChanged;

    public void TransitionTo(CombatPhase next)
    {
        UnityEngine.Debug.Log($"[Phase] {Current} → {next}");
        var prev = Current;
        Current = next;
        OnPhaseChanged?.Invoke(prev, next);
    }
}