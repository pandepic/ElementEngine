using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ElementEngine.UI
{
    public static class UIDataBinding
    {
        public static Dictionary<string, Dictionary<string, PropertyInfo>> CachedProperties = new Dictionary<string, Dictionary<string, PropertyInfo>>();

        private static List<XElement> _tempElements = new List<XElement>();

        public static List<XElement> GetRepeaterOutput(UIMenu menu, string repeaterName, XElement elWidget)
        {
            _tempElements.Clear();

            if (!menu.BoundObjects.TryGetValue(repeaterName, out var repeaterObj))
                return null;

            if (repeaterObj is not List<IUIBoundType> list)
                return null;

            if (list.Count == 0)
                return null;

            var type = list[0].GetType();
            var properties = GetPropertyInfo(type);

            foreach (var obj in list)
            {
                var xml = elWidget.ToString();

                foreach (var (propertyName, property) in properties)
                {
                    xml = xml.Replace("{" + propertyName + "}", property.GetValue(obj).ToString());
                }

                var element = XElement.Parse(xml);
                _tempElements.Add(element);
            }

            return _tempElements;

        } // GetRepeaterOutput

        public static XElement GetElement(UIMenu menu, string bindName, XElement elWidget)
        {
            if (!menu.BoundObjects.TryGetValue(bindName, out var bindObj))
                return null;

            var type = bindObj.GetType();
            var properties = GetPropertyInfo(type);

            var xml = elWidget.ToString();

            foreach (var (propertyName, property) in properties)
            {
                xml = xml.Replace("{" + propertyName + "}", property.GetValue(bindObj).ToString());
            }

            return XElement.Parse(xml);

        } // GetElement

        public static Dictionary<string, PropertyInfo> GetPropertyInfo(Type type)
        {
            if (!CachedProperties.TryGetValue(type.Name, out var propertyInfo))
            {
                propertyInfo = new Dictionary<string, PropertyInfo>();

                var properties = type.GetProperties();

                foreach (var prop in properties)
                    propertyInfo.Add(prop.Name, prop);

                CachedProperties.Add(type.Name, propertyInfo);
            }

            return propertyInfo;
        }

    } // UIDataBinding
}
