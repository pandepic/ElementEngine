using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine
{
    public abstract class AStarGraph
    {
        public ObjectPool<AStarNode> NodePool = new ObjectPool<AStarNode>(1000, true);
        public bool IsLazyLoaded { get; protected set; }
        public float DiagonalPenalty = 1f;

        public abstract AStarNode GetNode(Vector2I position, Vector2I start, Vector2I end);
        public abstract void AddNodeEdges(AStarNode node, Vector2I start, Vector2I end);

        public virtual float GetNodePenalty(AStarNode node)
        {
            if (DiagonalPenalty == 0f)
                return 0f;

            if (node._parent != null)
            {
                var offset = node._parent.Position - node.Position;
                if (offset.X != 0 && offset.Y != 0)
                    return DiagonalPenalty;
            }

            return 0f;
        }

        public virtual void Reset()
        {
            NodePool.Clear();
        }

        public virtual bool IsNodeBlocked(AStarNode node, Vector2I end, AStarNode parent = null)
        {
            if (node == null)
                return true;

            if (node.MovementCost >= 0f)
                return false;
            else
                return true;
        }

        public virtual void CalculateNode(AStarNode node, Vector2I end)
        {
            if (node == null)
                return;

            if (node._parent == null)
                node._g = 0f;
            else
                node._g = node._parent._g + 1f + node.MovementCost;

            node._g += GetNodePenalty(node);
            node._h = GetNodeHeuristic(node, end);
            node._f = node._g + node._h;
        }

        public virtual float GetNodeHeuristic(AStarNode node, Vector2I end)
        {
            if (node == null)
                return 0f;

            return node.Position.GetDistance(end);
        }

        public virtual bool CheckEndNode(AStarNode node, Vector2I end)
        {
            if (node == null)
                return false;

            return node.Position == end;
        }

        public virtual float GetMovementCost(Vector2I position)
        {
            return 0f;
        }

    } // AStarGraph
}
