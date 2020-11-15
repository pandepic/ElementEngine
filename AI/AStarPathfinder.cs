using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace ElementEngine
{
    //public interface IAStarGraph
    //{

    //}

    //public class AStarPathfinder
    //{
    //}

    public class Node
    {
        public Vector2 Position { get; set; } = Vector2.Zero;
        public float G { get; set; } = 0.0f;
        public float H { get; set; } = 0.0f;
        public float F { get; set; } = 0.0f;
        public bool Open { get; set; } = false;
        public bool Closed { get; set; } = false;

        public Node Parent { get; set; } = null;

        public Node() { }

        public Node(int x, int y)
        {
            Position = new Vector2(x, y);
        }

        public float Calculate()
        {
            F = G + H;

            return F;
        }
    }

    public class AStarPathfinder
    {
        protected Node[,] _worldMatrix = null;
        protected int _worldWidth = 0;
        protected int _worldHeight = 0;

        protected List<Vector2> _checkOffsets = new List<Vector2>();

        public AStarPathfinder() { }

        public virtual void Initialise(int worldWidth, int worldHeight)
        {
            if (worldWidth <= 0 || worldHeight <= 0)
                return;

            this._worldWidth = worldWidth;
            this._worldHeight = worldHeight;

            _worldMatrix = new Node[worldWidth, worldHeight];

            _checkOffsets.Add(new Vector2(-1, 0)); // left
            _checkOffsets.Add(new Vector2(+1, 0)); // right
            _checkOffsets.Add(new Vector2(0, +1)); // top
            _checkOffsets.Add(new Vector2(0, -1)); // bottom
            _checkOffsets.Add(new Vector2(-1, +1)); // top left
            _checkOffsets.Add(new Vector2(+1, +1)); // top right
            _checkOffsets.Add(new Vector2(-1, -1)); // bottom left
            _checkOffsets.Add(new Vector2(+1, -1)); // bottom right
        }

        public virtual List<Vector2> GeneratePath(Vector2 start, Vector2 end)
        {
            try
            {
                if (_worldMatrix == null)
                    return null;

                if (_worldWidth == 0 || _worldHeight == 0 || start == null || end == null)
                    return null;

                if (start == end)
                    return new List<Vector2>();

                List<Vector2> path = new List<Vector2>();
                List<Node> openList = new List<Node>();
                List<Node> closedList = new List<Node>();

                for (int x = 0; x < _worldWidth; x++)
                {
                    for (int y = 0; y < _worldHeight; y++)
                    {
                        _worldMatrix[x, y] = new Node(x, y);
                    }
                }

                Node startNode = GetNodeAtPosition(start);
                Node endNode = GetNodeAtPosition(end);

                if (startNode == null || endNode == null)
                    return null;

                startNode.G = 0.0f;
                startNode.H = GetNodeHeuristic(startNode, endNode);
                startNode.Parent = null;
                startNode.Calculate();

                openList.Add(startNode);

                bool pathFound = false;
                bool pathFinished = false;

                int tempX = 0, tempY = 0;

                while (!pathFound)
                {
                    if (openList.Count == 0)
                        return null;

                    Node currentNode = openList.Last();

                    if (currentNode == null)
                        return null;

                    openList.Remove(openList.Last());

                    // Add the surrounding nodes to the open list
                    for (var i = 0; i < _checkOffsets.Count; i++)
                    {
                        var checkOffset = _checkOffsets[i];
                        if (AddNodeToOpenList(openList, (int)(currentNode.Position.X + checkOffset.X),
                                                (int)(currentNode.Position.Y + checkOffset.Y), currentNode, endNode) == true)
                        {
                            pathFinished = true;
                            tempX = (int)checkOffset.X;
                            tempY = (int)checkOffset.Y;
                        }
                    }

                    AddNodeToClosedList(closedList, currentNode);

                    // Path found, generate the list
                    if (pathFinished == true)
                    {
                        Node temp = GetNodeAtPosition((int)currentNode.Position.X + tempX, (int)currentNode.Position.Y + tempY);
                        temp.Parent = currentNode;

                        startNode.Parent = null;

                        while (temp.Parent != null)
                        {
                            path.Add(temp.Position);

                            temp = temp.Parent;
                        }

                        pathFound = true;
                    }
                }

                return new List<Vector2>(path);
            }
            catch (Exception)
            {
                return null;
            }
        }

        protected void AddNodeToClosedList(List<Node> closedList, Node currentNode)
        {
            if (currentNode == null)
                return;

            currentNode.Open = false;
            currentNode.Closed = true;

            closedList.Add(currentNode);
        }

        protected bool AddNodeToOpenList(List<Node> openList, int x, int y, Node parent, Node end)
        {
            Node currentNode = GetNodeAtPosition(x, y);

            if (currentNode == null)
                return false;

            float movementPenalty = GetNodeMovementPenalty(currentNode, parent, end);

            if (movementPenalty == -1.0f)
                return false;

            if (currentNode == end)
                return true;

            if (currentNode.Open == true || currentNode.Closed == true)
                return false;

            currentNode.G = parent.G + 1.0f + movementPenalty;
            currentNode.H = GetNodeHeuristic(currentNode, end);
            currentNode.Calculate();
            currentNode.Parent = parent;
            currentNode.Open = true;

            // keep the open list sorted
            for (int i = 0; i < openList.Count; i++)
            {
                if (openList[i].F < currentNode.F)
                {
                    openList.Insert(i, currentNode);

                    return false;
                }
            }

            openList.Add(currentNode);

            return false;
        }

        public Node GetNodeAtPosition(Vector2 position)
        {
            return GetNodeAtPosition((int)position.X, (int)position.Y);
        }

        public Node GetNodeAtPosition(int x, int y)
        {
            if (x >= _worldWidth || y >= _worldHeight || x < 0 || y < 0)
                return null;

            if (_worldMatrix == null)
                return null;

            return _worldMatrix[x, y];
        }

        protected virtual float GetNodeMovementPenalty(Node currentNode, Node parent, Node end)
        {
            return 0.0f;
        }

        protected virtual float GetNodeHeuristic(Node node, Node end)
        {
            if (node == null || end == null)
                return 0.0f;

            return Vector2.Distance(node.Position, end.Position);
        }
    }
}
