using System;
using System.Linq;

namespace ElementEngine.UI
{
    public class IMGUIEnumCombo<T> : IMGUIListCombo<T> where T : struct, Enum
    {
        public IMGUIEnumCombo(string label, bool emptyOption = false, bool showFilter = false)
            : base(label, null, emptyOption, showFilter)
        {
            _sourceData = Enum.GetValues<T>().ToList();
            Reload();
        }
    }
}
