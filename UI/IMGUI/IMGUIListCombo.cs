using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Text;

namespace ElementEngine.UI
{
    public class IMGUIListCombo<T>
    {
        public string Label;
        public List<T> List;

        protected string[] _data;
        protected int _selectedIndex = 0;

        public int SelectedIndex { get => _selectedIndex; protected set => _selectedIndex = value; }
        public T SelectedValue => List[_selectedIndex];
        public string SelectedName => _data[_selectedIndex];

        public IMGUIListCombo(string label, List<T> list)
        {
            Label = label;
            List = list;
            RefreshData();
        }

        public void RefreshData()
        {
            _data = new string[List.Count];

            for (var i = 0; i < List.Count; i++)
                _data[i] = List[i].ToString();

            if (_selectedIndex >= _data.Length)
                _selectedIndex = _data.Length - 1;

            if (_selectedIndex < 0)
                _selectedIndex = 0;
        }

        public void Draw()
        {
            ImGui.Combo(Label, ref _selectedIndex, _data, _data.Length);
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

        public int SelectedIndex { get => _selectedIndex; protected set => _selectedIndex = value; }
        public T SelectedValue => Data[_selectedIndex];
        public string SelectedName => _data[_selectedIndex];

        public IMGUIArrayCombo(string label, T[] data)
        {
            Label = label;
            Data = data;
            RefreshData();
        }

        public void RefreshData()
        {
            _data = new string[Data.Length];

            for (var i = 0; i < Data.Length; i++)
                _data[i] = Data[i].ToString();

            if (_selectedIndex >= _data.Length)
                _selectedIndex = _data.Length - 1;

            if (_selectedIndex < 0)
                _selectedIndex = 0;
        }

        public void Draw()
        {
            ImGui.Combo(Label, ref _selectedIndex, _data, _data.Length);
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
