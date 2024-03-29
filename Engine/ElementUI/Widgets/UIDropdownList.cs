﻿using System;
using System.Collections.Generic;

namespace ElementEngine.ElementUI
{
    public class UIDropdownListItem<T>
    {
        public string Text;
        public T Value;

        public UIDropdownListItem(T value)
        {
            Text = value?.ToString() ?? "";
            Value = value;
        }

        public UIDropdownListItem(string text, T value)
        {
            Text = text;
            Value = value;
        }
    }

    public static class UIDropdownHelper
    {
        public static UIDropdownList<E> CreateFromEnum<E>(string name, UIDropdownListStyle style) where E : Enum
        {
            var items = new List<UIDropdownListItem<E>>();

            foreach (E enumVal in Enum.GetValues(typeof(E)))
                items.Add(new UIDropdownListItem<E>(Enum.GetName(typeof(E), enumVal), enumVal));

            var ddl = new UIDropdownList<E>(name, style, items);
            return ddl;
        }

        public static List<UIDropdownListItem<E>> ItemsFromEnum<E>() where E : Enum
        {
            var items = new List<UIDropdownListItem<E>>();

            foreach (E enumVal in Enum.GetValues(typeof(E)))
                items.Add(new UIDropdownListItem<E>(Enum.GetName(typeof(E), enumVal), enumVal));

            return items;
        }

        public static List<UIDropdownListItem<T>> ItemsFrom<T>(ICollection<T> list)
        {
            var items = new List<UIDropdownListItem<T>>();

            foreach (var e in list)
                items.Add(new(e));

            return items;
        }
    }

    public interface IUIDropdownList
    {
        void Collapse();
        void Expand();
    }

    public class UIDropdownList<T> : UIObject, IUIDropdownList
    {
        public new UIDropdownListStyle Style => (UIDropdownListStyle)_style;
        public event Action<UIOnValueChangedArgs<UIDropdownListItem<T>>> OnValueChanged;

        public List<UIDropdownListItem<T>> Items;

        internal UIDropdownListItem<T> _selectedItem;
        public UIDropdownListItem<T> SelectedItem
        {
            get => _selectedItem;
            set
            {
                var prev = _selectedItem;
                _selectedItem = value;

                ButtonCollapsedLabel.Text = value.Text;
                ButtonExpandedLabel.Text = value.Text;

                if (prev != _selectedItem)
                    OnValueChanged?.Invoke(new UIOnValueChangedArgs<UIDropdownListItem<T>>(this, prev, _selectedItem));
            }
        }

        public readonly UIButton ButtonCollapsed;
        public readonly UILabel ButtonCollapsedLabel;
        public readonly UIButton ButtonExpanded;
        public readonly UILabel ButtonExpandedLabel;
        public readonly UIContainer ListContainer;

        protected bool _isExpanded;
        public bool IsExpanded { get => _isExpanded; }

        public UIDropdownList(string name, UIDropdownListStyle style, List<T> items)
            : this(name, style, UIDropdownHelper.ItemsFrom(items))
        {
        }

        public UIDropdownList(string name, UIDropdownListStyle style, List<UIDropdownListItem<T>> items) : base(name)
        {
            if (items.Count == 0)
                throw new ArgumentException("Dropdown items list can't be empty.", nameof(items));

            ApplyStyle(style);
            ApplyDefaultSize(Style.ButtonCollapsed.ImageNormal.Sprite);

            Style.OverflowType = OverflowType.Show;
            Style.ListContainer.OverflowType = OverflowType.Scroll;

            Items = items;

            ButtonCollapsed = new UIButton(name + "_ButtonCollapsed", Style.ButtonCollapsed);
            ButtonCollapsedLabel = new UILabel(name + "_ButtonCollapsedLabel", Style.SelectedLabelStyle, "");
            ButtonCollapsed.AddChild(ButtonCollapsedLabel);
            ButtonCollapsed.UpdateLayout();

            ButtonExpanded = new UIButton(name + "_ButtonExpanded", Style.ButtonExpanded);
            ButtonExpandedLabel = new UILabel(name + "_ButtonExpandedLabel", Style.SelectedLabelStyle, "");
            ButtonExpanded.AddChild(ButtonExpandedLabel);
            ButtonExpanded.UpdateLayout();

            ListContainer = new UIContainer(name + "_ListContainer", Style.ListContainer);
            ListContainer.SetPosition(0, ButtonExpanded.Height);
            ListContainer.CenterX = true;

            AddChild(ListContainer);
            AddChild(ButtonCollapsed);
            AddChild(ButtonExpanded);

            Collapse();
            Refresh();

            SelectedItem = items[0];

            ButtonCollapsed.OnClick += (args) =>
            {
                Expand();
            };

            ButtonExpanded.OnClick += (args) =>
            {
                Collapse();
            };
        }

        public void Collapse()
        {
            ButtonCollapsed.Show();
            ButtonCollapsed.Enable();
            ButtonExpanded.Hide();
            ButtonExpanded.Disable();
            ListContainer.Hide();
            ListContainer.Disable();

            if (Parent != null && ParentScreen.ExpandedDropdown == this)
                ParentScreen.ExpandedDropdown = null;

            _isExpanded = false;
            _inputSize = null;
        }

        public void Expand()
        {
            ButtonCollapsed.Hide();
            ButtonCollapsed.Disable();
            ButtonExpanded.Show();
            ButtonExpanded.Enable();
            ListContainer.Show();
            ListContainer.Enable();

            if (Parent != null)
                ParentScreen.ExpandedDropdown = this;

            _isExpanded = true;
            _inputSize = _size + new Vector2I(0, ListContainer.Height);
        }

        public void SetSelectedValue(T value)
        {
            foreach (var item in Items)
            {
                if (item.Value.Equals(value))
                    SelectedItem = item;
            }
        }

        public void Refresh()
        {
            ListContainer.ClearChildrenByType<UIButton>();
            var nextButtonPosition = new Vector2I();

            foreach (var item in Items)
            {
                var itemButton = new UIButton(
                    Name + "_ListContainer_" + (item.Value?.ToString() ?? "NULL"),
                    Style.ItemButtonStyle);

                var itemLabel = new UILabel(
                    Name + "_ListContainer_" + (item.Value?.ToString() ?? "NULL") + "_Label",
                    Style.ItemButtonLabelStyle,
                    item.Text);

                itemButton.AddChild(itemLabel);
                itemButton.SetPosition(nextButtonPosition);
                itemButton.UpdateLayout();
                ListContainer.AddChild(itemButton);

                nextButtonPosition.Y += itemButton.Height;

                itemButton.OnClick += (args) =>
                {
                    SelectedItem = item;
                    Collapse();
                };
            }
        }
    }
}
