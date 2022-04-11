using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace ElementEngine
{
    public class AStarNode : IPoolable
    {
        public Vector2I Position;
        public List<Vector2I> Edges = new List<Vector2I>();
        public float MovementCost;

        // temp internal pathfinder values
        internal AStarNode _parent;
        internal bool _isOpen, _isClosed;
        internal float _g, _h, _f;

        public bool IsAlive { get; set; }

        public void Reset()
        {
            Position = Vector2I.Zero;
            Edges.Clear();
            MovementCost = 0f;

            _parent = null;
            _isOpen = false;
            _isClosed = false;
            _g = 0f;
            _h = 0f;
            _f = 0f;
        }
    }

    public struct AStarPathResult
    {
        public Vector2I Position;
        public Vector2I ParentPosition;
        public bool HasParent;
        public bool IsStart, IsEnd;

        public AStarPathResult(AStarNode node, bool start, bool end)
        {
            Position = node.Position;
            IsStart = start;
            IsEnd = end;
            ParentPosition = node._parent == null ? Vector2I.Zero : node._parent.Position;
            HasParent = node._parent != null;
        }
    }

    public enum AStarPathResultType
    {
        Success,
        Failed,
        NoPathFound,
        StartBlocked,
        EndBlocked,
        StartEqualsEnd,
    }

    public class AStarPathfinder
    {
        protected List<AStarNode> _openList = new List<AStarNode>();

        public AStarGraph Graph { get; protected set; }
        public bool IsLazyLoaded => Graph.IsLazyLoaded;

        public List<AStarPathResultType> ResultTypesAllowed = new List<AStarPathResultType>()
        {
            AStarPathResultType.Success,
        };

        public AStarPathfinder(AStarGraph graph)
        {
            Graph = graph;
        }

        public AStarPathResultType GetPath(Vector2I start, Vector2I end, out List<AStarPathResult> path, bool addStart = false)
        {
            var result = AStarPathResultType.Failed;
            path = null;

            if (start == end)
            {
                if (!SetPathResult(AStarPathResultType.StartEqualsEnd, ref result))
                    return result;
            }
            
            _openList.Clear();
            Graph.Reset();

            var startNode = Graph.GetNode(start, start, end);
            var endNode = Graph.GetNode(end, start, end);

            if (Graph.IsNodeBlocked(startNode, end))
            {
                if (!SetPathResult(AStarPathResultType.StartBlocked, ref result))
                    return result;
            }

            if (Graph.IsNodeBlocked(endNode, end))
            {
                if (!SetPathResult(AStarPathResultType.EndBlocked, ref result))
                    return result;
            }

            Graph.CalculateNode(startNode, end);
            _openList.Add(startNode);

            var pathFound = false;

            while (!pathFound)
            {
                if (_openList.Count == 0)
                    return AStarPathResultType.NoPathFound;

                var currentNode = _openList.GetLastItem();
                _openList.Remove(currentNode);

                AStarNode tempNode = null;

                for (var i = 0; i < currentNode.Edges.Count; i++)
                {
                    var edgeNode = Graph.GetNode(currentNode.Edges[i], start, end);

                    if (edgeNode == null)
                        continue;

                    Graph.AddNodeEdges(edgeNode, start, end);

                    if (AddNodeToOpenList(edgeNode, currentNode, endNode))
                    {
                        pathFound = true;
                        tempNode = edgeNode;
                    }
                }

                currentNode._isOpen = false;
                currentNode._isClosed = true;

                if (pathFound)
                {
                    path = GlobalObjectPool<List<AStarPathResult>>.Rent();
                    path.Clear();

                    tempNode._parent = currentNode;
                    bool resultEnd = true;

                    while (tempNode._parent != null)
                    {
                        bool resultStart = (tempNode._parent == null || tempNode._parent._parent == null);
                        if (addStart)
                            resultStart = false;

                        var newResultNode = new AStarPathResult(tempNode, resultStart, resultEnd);
                        path.Add(newResultNode);

                        tempNode = tempNode._parent;
                        resultEnd = false;
                    }

                    if (addStart)
                    {
                        var newResultNode = new AStarPathResult(tempNode, true, false);
                        path.Add(newResultNode);
                    }

                    result = AStarPathResultType.Success;
                } // path found

            } // while path not found

            return result;

        } // GetPath

        protected virtual bool SetPathResult(AStarPathResultType resultType, ref AStarPathResultType currentType)
        {
            currentType = resultType;
            return ResultTypesAllowed.Contains(resultType);
        }

        public bool AddNodeToOpenList(AStarNode node, AStarNode parent, AStarNode end)
        {
            if (Graph.CheckEndNode(node, end.Position))
                return true;

            if (node._isOpen || node._isClosed)
                return false;

            if (Graph.IsNodeBlocked(node, end.Position, parent))
                return false;

            node._parent = parent;
            node._isOpen = true;
            Graph.CalculateNode(node, end.Position);

            for (var i = 0; i < _openList.Count; i++)
            {
                if (_openList[i]._f < node._f)
                {
                    _openList.Insert(i, node);
                    return false;
                }
            }

            _openList.Add(node);
            return false;

        } // AddNodeToOpenList

    } // AStarPathfinder
}
