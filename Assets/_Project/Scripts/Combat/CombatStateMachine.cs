public enum CombatPhase
{
    None,
    Intro,
    PlayerTurn,
    EnemyTurn,
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