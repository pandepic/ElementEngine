using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ElementEngine.UI
{
    public class IMGUIListCombo<T>
    {
        public string Label;

        private List<T> _sourceData;
        public List<T> SourceData
        {
            get => _sourceData;
            set { _sourceData = value; Reload(); }
        }

        private List<T> _filteredData = new List<T>();

        private bool _emptyOption;
        public bool EmptyOption
        {
            get => _emptyOption;
            set { _emptyOption = value; Reload(); }
}

        private bool _showFilter;
        public bool ShowFilter
        {
            get => _showFilter;
            set { _showFilter = value; Reload(); }
}

        private string _filter = "";
        public string Filter
        {
            get => _filter;
            set { _filter = value; Reload(); }
        }

        public string[] _comboData;
        public int _comboIndex;

        public T SelectedValue => GetSelectedValue();

        public bool IsEmpty => !_emptyOption ? false : _comboIndex == 0;

        public IMGUIListCombo(string label, List<T> list, bool emptyOption = false, bool showFilter = false)
        {
            Label = label;
            SourceData = list;

            _emptyOption = emptyOption;
            _showFilter = showFilter;

            Reload();
        }

        private T GetSelectedValue()
        {
            if (_comboData == null || _comboIndex < 0 || _comboIndex >= _comboData.Length)
                return default;
            if (IsEmpty)
                return default;

            return _filteredData[_comboIndex];
        }

        private void Reload()
        {
            T prevSelected = GetSelectedValue();

            _filteredData.Clear();
            _filteredData.AddRange(SourceData);

            if (_showFilter && !string.IsNullOrEmpty(_filter))
            {
                for (var i = _filteredData.Count - 1; i >= 0; i--)
                {
                    if (!_filteredData[i].ToString().Contains(_filter, StringComparison.InvariantCultureIgnoreCase))
                        _filteredData.RemoveAt(i);
                }
            }

            if (_emptyOption)
                _filteredData.Insert(0, default);

            _comboData = new string[_filteredData.Count];

            for (var i = 0; i < _filteredData.Count; i++)
                _comboData[i] = _filteredData[i]?.ToString() ?? "";

            if (prevSelected != null)
            {
                _comboIndex = _filteredData.IndexOf(prevSelected);

                if (_comboIndex < 0)
                    _comboIndex = 0;
            }
        }

        public void Draw()
        {
            if (_showFilter)
            {
                if (ImGui.InputText($"Filter##{Label}_Filter", ref _filter, 200))
                    Reload();
            }

            ImGui.Combo(Label, ref _comboIndex, _comboData, _comboData.Length);
        }

        public bool TrySetIndex(int index)
        {
            if (index < 0 || index >= _comboData.Length)
                return false;

            _comboIndex = index;
            return true;
        }

        public bool TrySetValue(T value)
        {
            return TrySetIndex(_filteredData.IndexOf(value));
        }

        public bool TrySetValue(string value)
        {
            return TrySetIndex(Array.IndexOf(_comboData, value));
        }
    }
}
