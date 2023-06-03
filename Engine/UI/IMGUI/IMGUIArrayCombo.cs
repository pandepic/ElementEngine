using System.Linq;

namespace ElementEngine.UI
{
    public class IMGUIArrayCombo<T> : IMGUIListCombo<T>
    {
        public IMGUIArrayCombo(string label, T[] array, bool emptyOption = false, bool showFilter = false)
            : base(label, array.ToList(), emptyOption, showFilter) { }
    }
}
