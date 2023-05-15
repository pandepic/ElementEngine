using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Text;

namespace ElementEngine.UI
{
    public class IMGUIEnumCombo<T> where T : Enum
    {
        public string[] TypeNames;
        public string Label;

        protected int _selectedIndex;
        protected bool _emptyOption;

        public int SelectedIndex { get => _selectedIndex; protected set => _selectedIndex = value; }
        public T SelectedValue => (T)Enum.Parse(typeof(T), TypeNames[_selectedIndex]);
        public string SelectedName => TypeNames[_selectedIndex];
        public bool IsEmptyOption => _emptyOption ? _selectedIndex == 0 : false;

        public IMGUIEnumCombo(string label, bool emptyOption = false)
        {
            _emptyOption = emptyOption;

            Label = label;
            TypeNames = Enum.GetNames(typeof(T));

            if (_emptyOption)
            {
                var newNames = new string[TypeNames.Length + 1];
                newNames[0] = "";
                TypeNames.CopyTo(newNames, 1);
                TypeNames = newNames;
            }
        }

        public void Draw()
        {
            ImGui.Combo(Label, ref _selectedIndex, TypeNames, TypeNames.Length);
        }

        public bool TrySetIndex(int index)
        {
            if (index < 0 || index >= TypeNames.Length)
                return false;

            _selectedIndex = index;
            return true;
        }

        public bool TrySetValue(T value)
        {
            return TrySetValue(value.ToString());
        }

        public bool TrySetValue(string value)
        {
            for (var i = 0; i < TypeNames.Length; i++)
            {
                if (TypeNames[i] == value)
                    return TrySetIndex(i);
            }

            return false;
        }

    }
}
