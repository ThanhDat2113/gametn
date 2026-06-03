namespace Game.AI.BehaviorTree
{
    public enum NodeState
    {
        Running,
        Success,
        Failure
    }

    public abstract class Node
    {
        protected NodeState state;
        public abstract NodeState Evaluate();
    }
}