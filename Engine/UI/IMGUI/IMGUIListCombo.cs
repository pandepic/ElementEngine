using System;
using System.Collections.Generic;
using ImGuiNET;

namespace ElementEngine.UI
{
    public class IMGUIListCombo<T>
    {
        public string Label;

        protected List<T> _sourceData;
        public List<T> SourceData
        {
            get => _sourceData;
            set { _sourceData = value; Reload(); }
        }

        private List<T> _filteredData = new List<T>();

        private bool _hasEmptyOption;
        public bool HasEmptyOption
        {
            get => _hasEmptyOption;
            set { _hasEmptyOption = value; Reload(); }
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
        public int SelectedIndex { get => _comboIndex; }
        public string SelectedName { get => SelectedValue?.ToString() ?? null; }

        public bool IsEmpty => !_hasEmptyOption ? false : _comboIndex == 0;

        public IMGUIListCombo(string label, List<T> list, bool emptyOption = false, bool showFilter = false)
        {
            Label = label;
            SourceData = list;

            _hasEmptyOption = emptyOption;
            _showFilter = showFilter;

            Reload();
        }

        private T GetSelectedValue()
        {
            if (_comboData == null || _comboIndex < 0 || _comboIndex >= _filteredData.Count)
                return default;
            if (IsEmpty)
                return default;

            return _filteredData[_comboIndex];
        }

        protected void Reload()
        {
            T prevSelected = GetSelectedValue();

            _filteredData.Clear();
            if (SourceData != null)
                _filteredData.AddRange(SourceData);

            if (_showFilter && !string.IsNullOrEmpty(_filter))
            {
                for (var i = _filteredData.Count - 1; i >= 0; i--)
                {
                    if (!_filteredData[i].ToString().Contains(_filter, StringComparison.InvariantCultureIgnoreCase))
                        _filteredData.RemoveAt(i);
                }
            }

            if (_hasEmptyOption)
                _filteredData.Insert(0, default);

            if (_comboData == null || _comboData.Length < _filteredData.Count)
                _comboData = new string[_filteredData.Count];

            if (_comboData.Length > 0)
            {
                for (var i = 0; i < _filteredData.Count; i++)
                    _comboData[i] = _filteredData[i]?.ToString() ?? "";

                if (_hasEmptyOption)
                    _comboData[0] = "";
            }

            if (prevSelected != null)
            {
                _comboIndex = _filteredData.IndexOf(prevSelected);

                if (_comboIndex < 0)
                    _comboIndex = 0;
            }
        }

        public bool Draw()
        {
            if (_showFilter)
            {
                if (ImGui.InputText($"Filter##{Label}_Filter", ref _filter, 200))
                    Reload();
            }

            return ImGui.Combo(Label, ref _comboIndex, _comboData, _filteredData.Count);
        }

        public bool TrySetIndex(int index)
        {
            if (index < 0 || index >= _filteredData.Count)
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
