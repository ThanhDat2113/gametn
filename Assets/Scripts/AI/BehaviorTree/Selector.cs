using System.Collections.Generic;

namespace Game.AI.BehaviorTree
{
    public class Selector : Node
    {
        private List<Node> _children = new List<Node>();

        public Selector(List<Node> children)
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
                        continue;
                    case NodeState.Success:
                        state = NodeState.Success;
                        return state;
                    case NodeState.Running:
                        state = NodeState.Running;
                        return state;
                    default:
                        state = NodeState.Failure;
                        return state;
                }
            }
            state = NodeState.Failure;
            return state;
        }
    }
}