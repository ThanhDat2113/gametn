using System.Collections.Generic;

namespace Game.AI.BehaviorTree
{
    public class Sequence : Node
    {
        private List<Node> _children = new List<Node>();

        public Sequence(List<Node> children)
        {
            _children = children;
        }

        public override NodeState Evaluate()
        {
            foreach (var node in _children)
            {
                switch (node.Evaluate())
                {
                    case NodeState.Failure:
                        state = NodeState.Failure;
                        return state;
                    case NodeState.Success:
                        continue;
                    case NodeState.Running:
                        state = NodeState.Running;
                        return state;
                    default:
                        state = NodeState.Success;
                        return state;
                }
            }
            state = NodeState.Success;
            return state;
        }
    }
}