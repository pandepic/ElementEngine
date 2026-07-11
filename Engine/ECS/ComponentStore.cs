using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine.ECS
{
    internal interface IComponentDebugView
    {
        object GetBoxed(int entityID);
        IReadOnlyList<FieldInfo> PublicFields { get; }
    }

    public interface IComponentStore
    {
        public bool Contains(int entityID);
        public void Remove(int entityID);
        public bool TryRemove(int entityID);
        public void Clear();
        public int GetSize();
        public int[] GetDense();
    }

    public class ComponentStore<T> : SparseSet<T>, IComponentStore, IComponentDebugView where T : struct
    {
        private FieldInfo[] _publicFields;

        public ComponentStore(int maxComponents) : base(maxComponents)
        {
        }

        IReadOnlyList<FieldInfo> IComponentDebugView.PublicFields => _publicFields ??= GetPublicFields();

        object IComponentDebugView.GetBoxed(int entityID) => Contains(entityID) ? this[entityID] : null;

        [UnconditionalSuppressMessage(
            "Trimming",
            "IL2090",
            Justification = "Component types reach stores via Registry.TryAddComponent, whose " +
                "DynamicallyAccessedMembers annotation preserves their public fields; missing fields " +
                "only degrade the debug view.")]
        private static FieldInfo[] GetPublicFields() => typeof(T).GetFields();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetSize()
        {
            return Size;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int[] GetDense()
        {
            return Dense;
        }
    }
}
