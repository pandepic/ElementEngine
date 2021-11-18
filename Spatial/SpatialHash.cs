using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace ElementEngine
{
    public interface ISpatialHashObject
    {
        Rectangle SpatialHashRect { get; }
    }

    public class SpatialHashCell<T> where T : class, ISpatialHashObject
    {
        public Vector2I Position;
        public List<T> CellObjects { get; set; } = new List<T>();

        public SpatialHashCell(Vector2I position)
        {
            Position = position;
        }
    } // SpatialHashCell

    public class SpatialHash<T> where T : class, ISpatialHashObject
    {
        public Dictionary<Vector2I, SpatialHashCell<T>> CellMap { get; set; } = new Dictionary<Vector2I, SpatialHashCell<T>>();
        public Dictionary<T, List<SpatialHashCell<T>>> ObjectCellsMap { get; set; } = new Dictionary<T, List<SpatialHashCell<T>>>();

        /// <summary>
        /// Shared object list used for things like getting object collisions
        /// </summary>
        public HashSet<T> SharedObjectSet { get; set; } = new HashSet<T>();
        public HashSet<Vector2I> SharedCellSet { get; set; } = new HashSet<Vector2I>();

        public Vector2I CellSize;

        protected List<Vector2I> _internalPositionList = new List<Vector2I>();

        public SpatialHash(int cellWidth, int cellHeight) : this(new Vector2I(cellWidth, cellHeight)) { }

        public SpatialHash(Vector2I cellSize)
        {
            CellSize = cellSize;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SpatialHashCell<T> GetCell(Vector2I cellPosition)
        {
            if (CellMap.TryGetValue(cellPosition, out var cell))
                return cell;
            else
                return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SpatialHashCell<T> GetCellFromWorldPosition(Vector2I worldPosition)
        {
            var cellPosition = worldPosition / CellSize;
            return GetCell(cellPosition);
        }

        protected SpatialHashCell<T> AddCell(Vector2I cellPosition)
        {
            var newCell = new SpatialHashCell<T>(cellPosition);

            CellMap.TryAdd(cellPosition, newCell);
            return GetCell(cellPosition);
        }

        public void AddObject(T obj)
        {
            ObjectCellsMap.TryAdd(obj, new List<SpatialHashCell<T>>());
            UpdateObjectCells(obj, true);
        }

        public void UpdateObject(T obj)
        {
            UpdateObjectCells(obj, true);
        }

        public void RemoveObject(T obj)
        {
            if (!ObjectCellsMap.TryGetValue(obj, out var objCells))
                return;

            for (var i = 0; i < objCells.Count; i++)
                objCells[i].CellObjects.Remove(obj);

            ObjectCellsMap.Remove(obj);
        }

        protected void SetInternalPositionCellsFromWorldRect(Rectangle rect)
        {
            var topLeft = rect.Location / CellSize;
            var bottomRight = rect.BottomRight / CellSize;

            SetInternalPositionCells(topLeft, bottomRight);
        }

        protected void SetInternalPositionCellsFromRect(Rectangle rect)
        {
            SetInternalPositionCells(rect.Location, rect.BottomRight);
        }

        protected void SetInternalPositionCells(Vector2I topLeft, Vector2I bottomRight)
        {
            _internalPositionList.Clear();

            for (var x = topLeft.X; x <= bottomRight.X; x++)
            {
                for (var y = topLeft.Y; y <= bottomRight.Y; y++)
                {
                    _internalPositionList.Add(new Vector2I(x, y));
                }
            }
        }

        protected void UpdateObjectCells(T obj, bool createMissing)
        {
            var objCells = ObjectCellsMap[obj];

            for (var i = 0; i < objCells.Count; i++)
                objCells[i].CellObjects.Remove(obj);

            objCells.Clear();

            SetInternalPositionCellsFromWorldRect(obj.SpatialHashRect);

            for (var i = 0; i < _internalPositionList.Count; i++)
            {
                var cell = GetCell(_internalPositionList[i]);

                if (cell == null)
                {
                    if (createMissing)
                        cell = AddCell(_internalPositionList[i]);
                    else
                        continue;
                }

                cell.CellObjects.Add(obj);
                ObjectCellsMap[obj].Add(cell);
            }
        } // UpdateObjectCells

        public HashSet<T> IntersectsObject(T obj)
        {
            return IntersectsRect(obj.SpatialHashRect, obj);
        }

        public HashSet<T> IntersectsRect(Rectangle rect, T excludeObj = null)
        {
            SharedObjectSet.Clear();
            SetInternalPositionCellsFromWorldRect(rect);

            for (var i = 0; i < _internalPositionList.Count; i++)
            {
                var cell = GetCell(_internalPositionList[i]);
                if (cell == null)
                    continue;

                for (var o = 0; o < cell.CellObjects.Count; o++)
                {
                    var cellObject = cell.CellObjects[o];
                    if (cellObject == excludeObj)
                        continue;

                    if (cellObject.SpatialHashRect.Intersects(rect))
                        SharedObjectSet.Add(cellObject);
                }
            }

            return SharedObjectSet;
        } // IntersectsRect

        public HashSet<Vector2I> GetObjectCellPositions(T obj)
        {
            SharedCellSet.Clear();

            for (var i = 0; i < ObjectCellsMap[obj].Count; i++)
            {
                var cell = ObjectCellsMap[obj][i];
                SharedCellSet.Add(cell.Position);
            }

            return SharedCellSet;
        }
        
    } // SpatialHash
}
