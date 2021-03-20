//https://github.com/DeanReynolds/Dcrew.Spatial

using ElementEngine;
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;

namespace Dcrew.Spatial
{
    public struct Quadtree
    {
        struct Node
        {
            public float X, Y, Width, Height;
            public readonly float CenterX, CenterY;
            public int Child;
            public readonly byte Depth;

            public Node(float x, float y, byte depth)
            {
                X = 0;
                Y = 0;
                Width = 0;
                Height = 0;
                CenterX = x;
                CenterY = y;
                Child = 0;
                Depth = depth;
            }
        }

        struct TreeItem
        {
            public float X, Y, Width, Height;
            public int Next;
        }

        /// <summary>A collection of int ids.</summary>
        public struct Yield : IEnumerator<int>, IEnumerable<int>, IDisposable
        {
            internal int[] _items;
            int _cur;

            public int Count { get; internal set; }

            public int Current => _items[_cur];
            object IEnumerator.Current => _items[_cur];

            public Yield(int count)
            {
                _items = ArrayPool<int>.Shared.Rent(count);
                Count = 0;
                _cur = -1;
            }

            public void Dispose() { ArrayPool<int>.Shared.Return(_items); }

            public bool MoveNext() { return ++_cur < Count; }
            public void Reset() { _cur = -1; }

            public Yield GetEnumerator() { return this; }
            IEnumerator<int> IEnumerable<int>.GetEnumerator() { return this; }
            IEnumerator IEnumerable.GetEnumerator() { return this; }
        }

        float _x, _y, _width, _height;
        readonly byte _maxDepth;
        Node[] _node;
        int _freeNode;
        TreeItem[] _item;
        readonly Stack<int> _toProcess;
        float _newX, _newY, _newWidth, _newHeight;

        /// <summary>Get/set the max items that can be set.</summary>
        public int MaxItems
        {
            get { return _item.Length; }
            set
            {
                if (value < _item.Length)
                    for (var i = value; i < _item.Length; i++)
                        if (_item[i].Next != -2)
                            Remove(i);
                Array.Resize(ref _item, value);
            }
        }

        /// <summary>Create a new tree with the desired bounds, max items and max depth.</summary>
        /// <param name="maxItems">Initial max items that can be set.</param>
        /// <param name="maxDepth">Amount of layers this tree can subdivide into.</param>
        public Quadtree(float x, float y, float width, float height, int maxItems, byte maxDepth = 8)
        {
            _newX = _x = x;
            _newY = _y = y;
            _newWidth = _width = width;
            _newHeight = _height = height;
            _maxDepth = maxDepth;
            var nodes = 1;
            for (var i = 0; i < maxDepth; i++)
                nodes += (int)Math.Pow(4, i + 1);
            _node = new Node[nodes];
            _node[0] = new Node(x + (width * .5f), y + (height * .5f), 0);
            _freeNode = 1;
            _item = new TreeItem[maxItems];
            for (var i = 0; i < _item.Length; i++)
                _item[i].Next = -2;
            _toProcess = new Stack<int>();
        }

        /// <summary>Return the latest bounds of item <paramref name="i"/>.</summary>
        public (float X, float Y, float Width, float Height) this[int i]
        {
            get
            {
                ref readonly var item = ref _item[i];
                return (item.X, item.Y, item.Width, item.Height);
            }
        }

        /// <summary>Set id <paramref name="i"/> to the given bounds.</summary>
        public void Update(int i, Rectangle rect) { Update(i, rect.X, rect.Y, rect.Width, rect.Height); }
        /// <summary>Set id <paramref name="i"/> to the given bounds.</summary>
        public void Update(int i, float x, float y, float width, float height)
        {
            if (x + (width * .5f) < _newX)
                _newX = x + (width * .5f);
            if (y + (height * .5f) < _newY)
                _newY = y + (height * .5f);
            if ((x + (width * .5f)) - _newX > _newWidth)
                _newWidth = (x + (width * .5f)) - _newX;
            if ((y + (height * .5f)) - _newY > _newHeight)
                _newHeight = (y + (height * .5f)) - _newY;
            ref var item = ref _item[i];
            var newNode = FindNode(x + (width * .5f), y + (height * .5f));
            if (item.Next != -2)
            {
                var oldNode = FindNode(item.X + (item.Width * .5f), item.Y + (item.Height * .5f));
                if (newNode == oldNode)
                {
                    item.X = x;
                    item.Y = y;
                    item.Width = width;
                    item.Height = height;
                    return;
                }
                Remove(i, oldNode);
            }
            item.Next = -1;
            item.X = x;
            item.Y = y;
            item.Width = width;
            item.Height = height;
            ref var node = ref _node[newNode];
            if (node.Child == 0)
                node.Child = i + 1;
            else
            {
                var idx = node.Child - 1;
                while (_item[idx].Next != -1)
                    idx = _item[idx].Next;
                ref var j = ref _item[idx];
                j.Next = i;
            }
        }
        /// <summary>Remove id <paramref name="i"/>, no re-ordering, <paramref name="i"/> will be available for you to re-use.</summary>
        public void Remove(int i, int nodeStart = 0)
        {
            ref var item = ref _item[i];
            ref var node = ref _node[FindNode(item.X + (item.Width * .5f), item.Y + (item.Height * .5f), nodeStart)];
            int o = node.Child - 1;
            if (o == i)
            {
                node.Child = item.Next + 1;
            }
            else
            {
                int prev;
                do
                {
                    prev = o;
                    o = _item[o].Next;
                } while (o != i);
                ref var j = ref _item[prev];
                j.Next = item.Next;
            }
            item.Next = -2;
        }
        /// <summary>Returns true if id <paramref name="i"/> has been added.</summary>
        public bool Contains(int i)
        {
            return _item[i].Next != -2;
        }
        /// <summary>Clear all items and nodes.</summary>
        public void Clear()
        {
            for (var i = 0; i < _item.Length; i++)
            {
                ref var item = ref _item[i];
                item.Next = -2;
            }
            _node[0].Child = 0;
            _freeNode = 1;
        }
        /// <summary>Call this once per frame preferably at the end of the Update call. This manages sub-dividing and updates node bounds.</summary>
        public void Update()
        {
            if (_newX != _x || _newY != _y || _newWidth != _width || _newHeight != _height)
            {
                _x = _newX;
                _y = _newY;
                _width = _newWidth;
                _height = _newHeight;
                var toAdd = ArrayPool<int>.Shared.Rent(_item.Length);
                var toAddCount = 0;
                for (var i = 0; i < _item.Length; i++)
                {
                    ref var item = ref _item[i];
                    if (item.Next != -2)
                    {
                        toAdd[toAddCount++] = i;
                        item.Next = -2;
                    }
                }
                _node[0].Child = 0;
                _freeNode = 1;
                for (var i = 0; i < toAddCount; i++)
                {
                    ref readonly var item = ref _item[toAdd[i]];
                    Update(toAdd[i], item.X, item.Y, item.Width, item.Height);
                }
                ArrayPool<int>.Shared.Return(toAdd);
            }
            int ni = 0;
            ref var n = ref _node[ni];
            do
            {
                if (n.Child < 0)
                {
                    int c = Math.Abs(n.Child);
                    _toProcess.Push(c);
                    _toProcess.Push(c + 1);
                    _toProcess.Push(c + 2);
                    _toProcess.Push(c + 3);
                    Node nw = _node[c],
                        ne = _node[c + 1],
                        sw = _node[c + 2],
                        se = _node[c + 3];
                    Vector2 nwRect = nw.Child == 0 ? new Vector2(float.MinValue, float.MinValue) : new Vector2(nw.X + nw.Width, nw.Y + nw.Height),
                        neRect = ne.Child == 0 ? new Vector2(float.MinValue, float.MinValue) : new Vector2(ne.X + ne.Width, ne.Y + ne.Height),
                        swRect = sw.Child == 0 ? new Vector2(float.MinValue, float.MinValue) : new Vector2(sw.X + sw.Width, sw.Y + sw.Height),
                        seRect = se.Child == 0 ? new Vector2(float.MinValue, float.MinValue) : new Vector2(se.X + se.Width, se.Y + se.Height);
                    n.X = MathF.Min(nw.X, MathF.Min(sw.X, MathF.Min(ne.X, se.X)));
                    n.Y = MathF.Min(nw.Y, MathF.Min(ne.Y, MathF.Min(sw.Y, se.Y)));
                    n.Width = MathF.Max(neRect.X, MathF.Max(seRect.X, MathF.Max(nwRect.X, swRect.X))) - n.X;
                    n.Height = MathF.Max(swRect.Y, MathF.Max(seRect.Y, MathF.Max(nwRect.Y, neRect.Y))) - n.Y;
                }
                else if (n.Child > 0)
                {
                    int items = 0;
                    float minX = float.MaxValue,
                        minY = float.MaxValue,
                        maxX = float.MinValue,
                        maxY = float.MinValue;
                    ref readonly var i = ref _item[n.Child - 1];
                    do
                    {
                        items++;
                        minX = MathF.Min(minX, i.X);
                        minY = MathF.Min(minY, i.Y);
                        maxX = MathF.Max(maxX, i.X + i.Width);
                        maxY = MathF.Max(maxY, i.Y + i.Height);
                        if (i.Next == -1)
                            break;
                        i = ref _item[i.Next];
                    } while (true);
                    n.X = minX;
                    n.Y = minY;
                    n.Width = maxX - minX;
                    n.Height = maxY - minY;
                    if (items >= 8 && n.Depth < _maxDepth)
                        Subdivide(ni);
                }
                if (_toProcess.Count <= 0)
                    break;
                ni = _toProcess.Pop();
                n = ref _node[ni];
            } while (true);
        }

        /// <summary>Query and return a disposable collection of ids that intersect the given rectangle.</summary>
        public Yield Query(Rectangle rect) { return Query(rect.X, rect.Y, rect.Width, rect.Height); }
        /// <summary>Query and return a disposable collection of ids that intersect the given rectangle.</summary>
        public Yield Query(float x, float y, float width, float height)
        {
            float right = x + width,
                bottom = y + height;
            var yield = new Yield(_item.Length);
            var totalItems = 0;
            ref readonly var n = ref _node[0];
            do
            {
                if (n.Child < 0)
                {
                    int c = Math.Abs(n.Child);
                    Node nw = _node[c],
                        ne = _node[c + 1],
                        sw = _node[c + 2],
                        se = _node[c + 3];
                    if (nw.X < right && nw.X + nw.Width > x && nw.Y < bottom && nw.Y + nw.Height > y)
                        _toProcess.Push(c);
                    if (ne.X < right && ne.X + ne.Width > x && ne.Y < bottom && ne.Y + ne.Height > y)
                        _toProcess.Push(c + 1);
                    if (sw.X < right && sw.X + sw.Width > x && sw.Y < bottom && sw.Y + sw.Height > y)
                        _toProcess.Push(c + 2);
                    if (se.X < right && se.X + se.Width > x && se.Y < bottom && se.Y + se.Height > y)
                        _toProcess.Push(c + 3);
                }
                else if (n.Child > 0)
                {
                    int i = n.Child - 1;
                    do
                    {
                        ref readonly var item = ref _item[i];
                        if (item.X < right && item.X + item.Width > x && item.Y < bottom && item.Y + item.Height > y)
                            yield._items[totalItems++] = i;
                        i = item.Next;
                    } while (i != -1);
                }
                if (_toProcess.Count <= 0)
                    break;
                n = ref _node[_toProcess.Pop()];
            } while (true);
            yield.Count = totalItems;
            return yield;
        }
        /// <summary>Query and return a disposable collection of ids that intersect the given rectangle.</summary>
        public Yield Query(Rectangle rect, float rotation, float originX, float originY) { return Query(rect.X, rect.Y, rect.Width, rect.Height, rotation, originX, originY); }
        /// <summary>Query and return a disposable collection of ids that intersect the given rectangle.</summary>
        public Yield Query(Rectangle rect, float rotation, Vector2 origin) { return Query(rect.X, rect.Y, rect.Width, rect.Height, rotation, origin.X, origin.Y); }
        /// <summary>Query and return a disposable collection of ids that intersect the given rectangle.</summary>
        public Yield Query(float x, float y, float width, float height, float rotation, Vector2 origin) { return Query(x, y, width, height, rotation, origin.X, origin.Y); }
        /// <summary>Query and return a disposable collection of ids that intersect the given rectangle.</summary>
        public Yield Query(float x, float y, float width, float height, float rotation, float originX, float originY)
        {
            float cos = MathF.Cos(rotation),
                sin = MathF.Sin(rotation),
                sx = -originX,
                sy = -originY,
                w = width + sx,
                h = height + sy,
                xcos = sx * cos,
                ycos = sy * cos,
                xsin = sx * sin,
                ysin = sy * sin,
                wcos = w * cos,
                wsin = w * sin,
                hcos = h * cos,
                hsin = h * sin;
            Vector2 tl = new Vector2(xcos - ysin + x, xsin + ycos + y),
                tr = new Vector2(wcos - ysin + x, wsin + ycos + y),
                br = new Vector2(wcos - hsin + x, wsin + hcos + y),
                bl = new Vector2(xcos - hsin + x, xsin + hcos + y);
            (float x1, float y1, float x2, float y2) t = (tl.X, tl.Y, tr.X, tr.Y), r = (tr.X, tr.Y, br.X, br.Y), b = (br.X, br.Y, bl.X, bl.Y), l = (bl.X, bl.Y, tl.X, tl.Y);
            bool Intersects((float x, float y, float width, float height) rect)
            {
                Vector2 vtl = new Vector2(rect.x, rect.y),
                    vtr = new Vector2(rect.x + rect.width, rect.y),
                    vbr = new Vector2(vtr.X, rect.y + rect.height),
                    vbl = new Vector2(rect.x, vbr.Y);
                (float x1, float y1, float x2, float y2) vt = (vtl.X, vtl.Y, vtr.X, vtr.Y), vr = (vtr.X, vtr.Y, vbr.X, vbr.Y), vb = (vbr.X, vbr.Y, vbl.X, vbl.Y), vl = (vbl.X, vbl.Y, vtl.X, vtl.Y);
                static bool LinesIntersect((float x1, float y1, float x2, float y2) a, (float x1, float y1, float x2, float y2) b)
                {
                    (float x1, float y1) = (a.x2 - a.x1, a.y2 - a.y1);
                    (float x2, float y2) = (b.x2 - b.x1, b.y2 - b.y1);
                    var denominator = x1 * y2 - y1 * x2;
                    if (MathF.Abs(denominator) < float.Epsilon)
                        return false;
                    (float x3, float y3) = (b.x1 - a.x1, b.y1 - a.y1);
                    var t = (x3 * y2 - y3 * x2) / denominator;
                    if (t < 0 || t > 1)
                        return false;
                    var u = (x3 * y1 - y3 * x1) / denominator;
                    if (u < 0 || u > 1)
                        return false;
                    return true;
                }
                static bool Ints((float x1, float y1, float x2, float y2) a, (float x1, float y1, float x2, float y2) b, (float x1, float y1, float x2, float y2) c, (float x1, float y1, float x2, float y2) d, (float x1, float y1, float x2, float y2) value)
                {
                    return LinesIntersect(a, value) || LinesIntersect(b, value) || LinesIntersect(c, value) || LinesIntersect(d, value);
                }
                static float IsLeft(Vector2 a, Vector2 b, Vector2 p)
                {
                    return (b.X - a.X) * (p.Y - a.Y) - (p.X - a.X) * (b.Y - a.Y);
                }
                static bool InRect(Vector2 x, Vector2 y, Vector2 z, Vector2 w, Vector2 p)
                {
                    return IsLeft(x, y, p) > 0 && IsLeft(y, z, p) > 0 && IsLeft(z, w, p) > 0 && IsLeft(w, x, p) > 0;
                }
                return Ints(t, r, b, l, vt) || Ints(t, r, b, l, vr) || Ints(t, r, b, l, vb) || Ints(t, r, b, l, vl) ||
                    InRect(tl, tr, br, bl, vtl) || InRect(vtl, vtr, vbr, vbl, tl);
            }
            float right = x + width,
                bottom = y + height;
            var yield = new Yield(_item.Length);
            var totalItems = 0;
            ref readonly var n = ref _node[0];
            do
            {
                if (n.Child < 0)
                {
                    int c = Math.Abs(n.Child);
                    Node nw = _node[c],
                        ne = _node[c + 1],
                        sw = _node[c + 2],
                        se = _node[c + 3];
                    if (Intersects((nw.X, nw.Y, nw.Width, nw.Height)))
                        _toProcess.Push(c);
                    if (Intersects((ne.X, ne.Y, ne.Width, ne.Height)))
                        _toProcess.Push(c + 1);
                    if (Intersects((sw.X, sw.Y, sw.Width, sw.Height)))
                        _toProcess.Push(c + 2);
                    if (Intersects((se.X, se.Y, se.Width, se.Height)))
                        _toProcess.Push(c + 3);
                }
                else if (n.Child > 0)
                {
                    int i = n.Child - 1;
                    do
                    {
                        ref readonly var item = ref _item[i];
                        if (Intersects((item.X, item.Y, item.Width, item.Height)))
                            yield._items[totalItems++] = i;
                        i = item.Next;
                    } while (i != -1);
                }
                if (_toProcess.Count <= 0)
                    break;
                n = ref _node[_toProcess.Pop()];
            } while (true);
            yield.Count = totalItems;
            return yield;
        }
        /// <summary>Query and return a disposable collection of ids that intersect the given point/radius.</summary>
        public Yield Query(Vector2I p, float radius = 1) { return Query(p.X, p.Y, radius); }
        /// <summary>Query and return a disposable collection of ids that intersect the given point/radius.</summary>
        public Yield Query(Vector2 p, float radius = 1) { return Query(p.X, p.Y, radius); }
        /// <summary>Query and return a disposable collection of ids that intersect the given point/radius.</summary>
        public Yield Query(float x, float y, float radius = 1)
        {
            bool Intersects((float x, float y, float width, float height) rect)
            {
                float dx = MathF.Abs(x - (rect.x + (rect.width * .5f))),
                    dy = MathF.Abs(y - (rect.y + (rect.height * .5f)));
                if (dx > (rect.width * .5f) + radius || dy > (rect.height * .5f) + radius)
                    return false;
                if (dx <= rect.width * .5f || dy <= rect.height * .5f)
                    return true;
                float fx = dx - (rect.width * .5f),
                    fy = dy - (rect.height * .5f);
                fx *= fx;
                fy *= fy;
                return fx + fy <= radius * radius;
            }
            var yield = new Yield(_item.Length);
            var totalItems = 0;
            ref readonly var n = ref _node[0];
            do
            {
                if (n.Child < 0)
                {
                    int c = Math.Abs(n.Child);
                    Node nw = _node[c],
                        ne = _node[c + 1],
                        sw = _node[c + 2],
                        se = _node[c + 3];
                    if (Intersects((nw.X, nw.Y, nw.Width, nw.Height)))
                        _toProcess.Push(c);
                    if (Intersects((ne.X, ne.Y, ne.Width, ne.Height)))
                        _toProcess.Push(c + 1);
                    if (Intersects((sw.X, sw.Y, sw.Width, sw.Height)))
                        _toProcess.Push(c + 2);
                    if (Intersects((se.X, se.Y, se.Width, se.Height)))
                        _toProcess.Push(c + 3);
                }
                else if (n.Child > 0)
                {
                    int i = n.Child - 1;
                    do
                    {
                        ref readonly var item = ref _item[i];
                        if (Intersects((item.X, item.Y, item.Width, item.Height)))
                            yield._items[totalItems++] = i;
                        i = item.Next;
                    } while (i != -1);
                }
                if (_toProcess.Count <= 0)
                    break;
                n = ref _node[_toProcess.Pop()];
            } while (true);
            yield.Count = totalItems;
            return yield;
        }
        /// <summary>Query and return a disposable collection of ids that intersect the given ray.</summary>
        public Yield Raycast(Vector2 position, Vector2I direction, float thickness = 1) { return Raycast(position.X, position.Y, direction.X, direction.Y, thickness); }
        /// <summary>Query and return a disposable collection of ids that intersect the given ray.</summary>
        public Yield Raycast(Vector2I position, Vector2I direction, float thickness = 1) { return Raycast(position.X, position.Y, direction.X, direction.Y, thickness); }
        /// <summary>Query and return a disposable collection of ids that intersect the given ray.</summary>
        public Yield Raycast(Vector2I position, Vector2 direction, float thickness = 1) { return Raycast(position.X, position.Y, direction.X, direction.Y, thickness); }
        /// <summary>Query and return a disposable collection of ids that intersect the given ray.</summary>
        public Yield Raycast(Vector2I position, float directionX, float directionY, float thickness = 1) { return Raycast(position.X, position.Y, directionX, directionY, thickness); }
        /// <summary>Query and return a disposable collection of ids that intersect the given ray.</summary>
        public Yield Raycast(Vector2I position, float rotation, float thickness = 1) { return Raycast(position.X, position.Y, rotation, thickness); }
        /// <summary>Query and return a disposable collection of ids that intersect the given ray.</summary>
        public Yield Raycast(Vector2 position, Vector2 direction, float thickness = 1) { return Raycast(position.X, position.Y, direction.X, direction.Y, thickness); }
        /// <summary>Query and return a disposable collection of ids that intersect the given ray.</summary>
        public Yield Raycast(Vector2 position, float directionX, float directionY, float thickness = 1) { return Raycast(position.X, position.Y, directionX, directionY, thickness); }
        /// <summary>Query and return a disposable collection of ids that intersect the given ray.</summary>
        public Yield Raycast(Vector2 position, float rotation, float thickness = 1) { return Raycast(position.X, position.Y, rotation, thickness); }
        /// <summary>Query and return a disposable collection of ids that intersect the given ray.</summary>
        public Yield Raycast(float x, float y, Vector2 direction, float thickness = 1) { return Raycast(x, y, direction.X, direction.Y, thickness); }
        /// <summary>Query and return a disposable collection of ids that intersect the given ray.</summary>
        public Yield Raycast(float x, float y, float directionX, float directionY, float thickness = 1)
        {
            var rotation = MathF.Atan2(directionY, directionX);
            return Query(x, y, float.MaxValue, thickness, rotation, 0, thickness * .5f);
        }
        /// <summary>Query and return a disposable collection of ids that intersect the given ray.</summary>
        public Yield Raycast(float x, float y, float rotation, float thickness = 1) { return Query(x, y, float.MaxValue, thickness, rotation, 0, thickness * .5f); }
        /// <summary>Query and return a disposable collection of ids that intersect the given line.</summary>
        public Yield Linecast(Vector2I a, Vector2I b, float thickness = 1) { return Linecast(a.X, a.Y, b.X, b.Y, thickness); }
        /// <summary>Query and return a disposable collection of ids that intersect the given line.</summary>
        public Yield Linecast(Vector2I a, Vector2 b, float thickness = 1) { return Linecast(a.X, a.Y, b.X, b.Y, thickness); }
        /// <summary>Query and return a disposable collection of ids that intersect the given line.</summary>
        public Yield Linecast(Vector2 a, Vector2I b, float thickness = 1) { return Linecast(a.X, a.Y, b.X, b.Y, thickness); }
        /// <summary>Query and return a disposable collection of ids that intersect the given line.</summary>
        public Yield Linecast(float aX, float aY, Vector2I b, float thickness = 1) { return Linecast(aX, aY, b.X, b.Y, thickness); }
        /// <summary>Query and return a disposable collection of ids that intersect the given line.</summary>
        public Yield Linecast(Vector2I a, float bX, float bY, float thickness = 1) { return Linecast(a.X, a.Y, bX, bY, thickness); }
        /// <summary>Query and return a disposable collection of ids that intersect the given line.</summary>
        public Yield Linecast(Vector2 a, Vector2 b, float thickness = 1) { return Linecast(a.X, a.Y, b.X, b.Y, thickness); }
        /// <summary>Query and return a disposable collection of ids that intersect the given line.</summary>
        public Yield Linecast(float aX, float aY, Vector2 b, float thickness = 1) { return Linecast(aX, aY, b.X, b.Y, thickness); }
        /// <summary>Query and return a disposable collection of ids that intersect the given line.</summary>
        public Yield Linecast(Vector2 a, float bX, float bY, float thickness = 1) { return Linecast(a.X, a.Y, bX, bY, thickness); }
        /// <summary>Query and return a disposable collection of ids that intersect the given line.</summary>
        public Yield Linecast(float x1, float y1, float x2, float y2, float thickness = 1)
        {
            var rotation = MathF.Atan2(y2, x2);
            return Query(x1, y1, float.MaxValue, thickness, rotation, 0, thickness * .5f);
        }
        /// <summary>Query and return a disposable collection of ids that have been added.</summary>
        public Yield All
        {
            get
            {
                var yield = new Yield(_item.Length);
                var totalItems = 0;
                for (var i = 0; i < _item.Length; i++)
                    if (_item[i].Next != -2)
                        yield._items[totalItems++] = i;
                yield.Count = totalItems;
                return yield;
            }
        }

        int FindNode(float x, float y, int i = 0)
        {
            ref readonly var n = ref _node[i];
            while (n.Child < 0)
            {
                i = x > n.CenterX ?
                    y > n.CenterY ?
                    Math.Abs(n.Child) + 3 :
                    Math.Abs(n.Child) + 1 :
                    y > n.CenterY ?
                    Math.Abs(n.Child) + 2 :
                    Math.Abs(n.Child);
                n = ref _node[i];
            }
            return i;
        }

        void Subdivide(int node)
        {
            ref var n = ref _node[node];
            var d = (byte)(n.Depth + 1);
            var dp2 = 1 << d;
            float w = _width * .5f / dp2,
                h = _height * .5f / dp2;
            Node nw = new Node(n.CenterX - w, n.CenterY - h, d),
                ne = new Node(n.CenterX + w, nw.CenterY, d),
                sw = new Node(nw.CenterX, n.CenterY + h, d),
                se = new Node(ne.CenterX, sw.CenterY, d);
            int i = n.Child - 1;
            n.Child = -_freeNode;
            _node[_freeNode++] = nw;
            _node[_freeNode++] = ne;
            _node[_freeNode++] = sw;
            _node[_freeNode++] = se;
            do
            {
                ref var item = ref _item[i];
                var next = item.Next;
                item.Next = -1;
                n = ref _node[FindNode(item.X + (item.Width * .5f), item.Y + (item.Height * .5f), node)];
                if (n.Child == 0)
                    n.Child = i + 1;
                else
                {
                    var idx = n.Child - 1;
                    while (_item[idx].Next != -1)
                        idx = _item[idx].Next;
                    ref var j = ref _item[idx];
                    j.Next = i;
                }
                i = next;
            } while (i != -1);
        }
    }
}