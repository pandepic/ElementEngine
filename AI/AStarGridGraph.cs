using System;
using System.Collections.Generic;
using System.Text;

namespace ElementEngine
{
    public enum AStarGridGraphType
    {
        FourEdges,
        EightEdges,
    }

    public class AStarGridGraph : AStarGraph
    {
        protected static Vector2I[] _surroundingCoords4Edges = new Vector2I[]
        {
            new Vector2I(0, -1),     // top middle
            new Vector2I(-1, 0),     // left
            new Vector2I(1, 0),      // right
            new Vector2I(0, 1),      // bottom middle
        };

        protected static Vector2I[] _surroundingCoords8Edges = new Vector2I[]
        {
            new Vector2I(-1, 0),     // left
            new Vector2I(-1, -1),    // top left
            new Vector2I(0, -1),     // top middle
            new Vector2I(1, -1),     // top right
            new Vector2I(1, 0),      // right
            new Vector2I(1, 1),      // bottom right
            new Vector2I(0, 1),      // bottom middle
            new Vector2I(-1, 1),     // bottom left
        };

        protected Vector2I[] _useSurroundingCoords = null;
        protected AStarNode[] _nodeCache = null;

        public int Width { get; protected set; }
        public int Height { get; protected set; }

        public AStarGridGraph(AStarGridGraphType type, int width, int height, bool lazyLoad = true)
        {
            if (type == AStarGridGraphType.FourEdges)
                _useSurroundingCoords = _surroundingCoords4Edges;
            else if (type == AStarGridGraphType.EightEdges)
                _useSurroundingCoords = _surroundingCoords8Edges;

            Width = width;
            Height = height;
            _nodeCache = new AStarNode[Width * Height];

            if (!IsLazyLoaded)
                GenerateCache();
        }

        protected void GenerateCache()
        {
            for (var y = 0; y < Height; y++)
            {
                for (var x = 0; x < Width; x++)
                {
                    var index = x + Width * y;
                    _nodeCache[index] = GetNode(new Vector2I(x, y), Vector2I.Zero, Vector2I.Zero);
                    AddNodeEdges(_nodeCache[index], Vector2I.Zero, Vector2I.Zero);
                }
            }
        }

        public void ClearCache()
        {
            for (var i = 0; i < _nodeCache.Length; i++)
                _nodeCache[i] = null;
        }

        public override void AddNodeEdges(AStarNode node, Vector2I start, Vector2I end)
        {
            if (node.Edges.Count > 0)
                return;

            for (var i = 0; i < _useSurroundingCoords.Length; i++)
            {
                var edgePos = node.Position + _useSurroundingCoords[i];
                var edgeNode = GetNode(edgePos, start, end);

                if (edgeNode != null)
                    node.Edges.Add(edgeNode);
            }
        }

        public override AStarNode GetNode(Vector2I position, Vector2I start, Vector2I end)
        {
            if (position.X < 0 || position.X >= Width
                || position.Y < 0 || position.Y >= Height)
            {
                return null;
            }

            var cacheIndex = position.X + Width * position.Y;

            if (_nodeCache[cacheIndex] != null)
                return _nodeCache[cacheIndex];

            var node = NodePool.New();
            node.Position = position;
            node.MovementCost = GetMovementCost(position);

            _nodeCache[cacheIndex] = node;
            return node;
        }
    } // AStarGridGraph
}
