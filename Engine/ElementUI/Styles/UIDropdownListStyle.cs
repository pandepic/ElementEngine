using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine.ElementUI
{
    public class UIDropdownListStyle : UIStyle
    {
        public UIButtonStyle ButtonCollapsed;
        public UIButtonStyle ButtonExpanded;
        public UIContainerStyle ListContainer;
        public UILabelStyle SelectedLabelStyle;
        public UIButtonStyle ItemButtonStyle;
        public UILabelStyle ItemButtonLabelStyle;

        public UIDropdownListStyle(UIDropdownListStyle copyFrom, bool baseCopy = false)
        {
            ButtonCollapsed = copyFrom.ButtonCollapsed;
            ButtonExpanded = copyFrom.ButtonExpanded;
            ListContainer = copyFrom.ListContainer;
            SelectedLabelStyle = copyFrom.SelectedLabelStyle;
            ItemButtonStyle = copyFrom.ItemButtonStyle;
            ItemButtonLabelStyle = copyFrom.ItemButtonLabelStyle;

            if (baseCopy)
                BaseCopy(copyFrom);
        }

        public UIDropdownListStyle(
            UIButtonStyle buttonCollapsed,
            UIButtonStyle buttonExpanded,
            UIContainerStyle listContainer,
            UILabelStyle selectedLabelStyle,
            UIButtonStyle itemButtonStyle,
            UILabelStyle itemButtonLabelStyle)
        {
            ButtonCollapsed = buttonCollapsed;
            ButtonExpanded = buttonExpanded;
            ListContainer = listContainer;
            SelectedLabelStyle = selectedLabelStyle;
            ItemButtonStyle = itemButtonStyle;
            ItemButtonLabelStyle = itemButtonLabelStyle;
        }
    } // UIDropdownListStyle
}
