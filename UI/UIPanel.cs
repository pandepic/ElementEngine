using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ElementEngine
{
    public class UIPanel : UIWidget
    {
        public List<UIWidget> Widgets { get; set; } = new List<UIWidget>();

        public UIPanel() { }

        public UIPanel(UIFrame parent, XElement el)
        {
            var scrollBarH = GetXMLElement("HScrollBar");
            var scrollBarV = GetXMLElement("VScrollBar");
        }

    } // UIPanel
}
