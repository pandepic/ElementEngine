using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ElementEngine.UI
{
    public class IMGUIListCombo<T>
    {
        public string Label;
        public List<T> List;

        public bool IsEmpty => List.Count == 0;

        protected string[] _data;
        protected int _selectedIndex = 0;
        protected bool _emptyOption;

        public int SelectedIndex { get => _selectedIndex; protected set => _selectedIndex = value; }
        public T SelectedValue => _emptyOption ? (SelectedIndex == 0 ? default : List[_selectedIndex - 1]) : List[_selectedIndex];
        public string SelectedName => _data[_selectedIndex];

        public bool ShowFilter;
        public string Filter = "";

        public IMGUIListCombo(string label, List<T> list, bool emptyOption = false)
        {
            _emptyOption = emptyOption;

            Label = label;
            List = list;

            RefreshData();
        }

        public void RefreshData()
        {
            if (List == null)
                return;

            _data = new string[List.Count + (_emptyOption ? 1 : 0)];

            if (_emptyOption)
                _data[0] = "";

            for (var i = 0; i < List.Count; i++)
                _data[i + (_emptyOption ? 1 : 0)] = List[i].ToString();

            if (_selectedIndex >= _data.Length)
                _selectedIndex = _data.Length - 1;

            if (_selectedIndex < 0)
                _selectedIndex = 0;
        }

        public void Draw()
        {
            var data = _data;

            if (ShowFilter && !string.IsNullOrEmpty(Filter))
                data = data.Where(d => d.ToUpper().Contains(Filter.ToUpper())).ToArray();

            if (ShowFilter)
                ImGui.InputText($"Filter##{Label}_Filter", ref Filter, 200);

            if (_selectedIndex >= data.Length)
                _selectedIndex = 0;

            ImGui.Combo(Label, ref _selectedIndex, data, data.Length);
        }

        public bool TrySetIndex(int index)
        {
            if (index < 0 || index >= _data.Length)
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
            for (var i = 0; i < _data.Length; i++)
            {
                if (_data[i] == value)
                    return TrySetIndex(i);
            }

            return false;
        }
    } // IMGUIListCombo

    public class IMGUIArrayCombo<T>
    {
        public string Label;
        public T[] Data;

        protected string[] _data;
        protected int _selectedIndex = 0;
        protected bool _emptyOption;

        public int SelectedIndex { get => _selectedIndex; protected set => _selectedIndex = value; }
        public T SelectedValue => _emptyOption ? (SelectedIndex == 0 ? default : Data[_selectedIndex - 1]) : Data[_selectedIndex];
        public string SelectedName => _data[_selectedIndex];

        public bool ShowFilter;
        public string Filter = "";

        public IMGUIArrayCombo(string label, T[] data, bool emptyOption = false)
        {
            _emptyOption = emptyOption;

            Label = label;
            Data = data;
            
            RefreshData();
        }

        public void RefreshData()
        {
            if (Data == null)
                return;

            _data = new string[Data.Length + (_emptyOption ? 1 : 0)];

            if (_emptyOption)
                _data[0] = "";

            for (var i = 0; i < Data.Length; i++)
                _data[i + (_emptyOption ? 1 : 0)] = Data[i].ToString();

            if (_selectedIndex >= _data.Length)
                _selectedIndex = _data.Length - 1;

            if (_selectedIndex < 0)
                _selectedIndex = 0;
        }

        public void Draw()
        {
            var data = _data;

            if (ShowFilter && !string.IsNullOrEmpty(Filter))
                data = data.Where(d => d.ToUpper().Contains(Filter.ToUpper())).ToArray();

            if (ShowFilter)
                ImGui.InputText($"Filter##{Label}_Filter", ref Filter, 200);

            if (_selectedIndex >= data.Length)
                _selectedIndex = 0;

            ImGui.Combo(Label, ref _selectedIndex, data, data.Length);
        }

        public bool TrySetIndex(int index)
        {
            if (index < 0 || index >= _data.Length)
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
            for (var i = 0; i < _data.Length; i++)
            {
                if (_data[i] == value)
                    return TrySetIndex(i);
            }

            return false;
        }
    } // IMGUIArrayCombo
}
