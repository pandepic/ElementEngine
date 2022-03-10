using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ElementEngine
{
    public class Setting
    {
        public string Name { get; set; }
        public string Value { get; set; }

        public Dictionary<string, string> OtherAttributes { get; set; }
    } // Setting

    public class SettingsSection
    {
        public string Name { get; set; }
        public Dictionary<string, Setting> Settings { get; set; }

        public SettingsSection()
        {
            Settings = new Dictionary<string, Setting>();
        }
    } // SettingsSection

    public static class SettingsManager
    {
        public static Dictionary<string, SettingsSection> Sections { get; set; } = new Dictionary<string, SettingsSection>();

        public static void LoadFromAsset(string assetName)
        {
            LoadFromPath(AssetManager.Instance.GetAssetPath(assetName));
        }

        public static void LoadFromPath(string filePath)
        {
            using var fs = AssetManager.Instance.GetFileStream(filePath);
            LoadFromStream(fs);
        }

        public static void LoadFromStream(FileStream fs)
        {
            var stopWatch = Stopwatch.StartNew();
            var loadedCount = 0;

            Sections.Clear();

            XDocument doc = XDocument.Load(fs);
            XElement settingsRoot = doc.Element("Settings");
            List<XElement> docSections = settingsRoot.Elements("Section").ToList();

            foreach (var docSection in docSections)
            {
                SettingsSection section = new SettingsSection
                {
                    Name = docSection.Attribute("Name").Value
                };

                List<XElement> sectionSettings = docSection.Elements("Setting").ToList();

                foreach (var sectionSetting in sectionSettings)
                {
                    var newSetting = new Setting()
                    {
                        Name = sectionSetting.Attribute("Name").Value,
                        Value = sectionSetting.Attribute("Value").Value,
                        OtherAttributes = new Dictionary<string, string>(),
                    };

                    foreach (var att in sectionSetting.Attributes())
                    {
                        if (att.Name != "Name" && att.Name != "Value")
                            newSetting.OtherAttributes.Add(att.Name.ToString(), att.Value);
                    }

                    section.Settings.Add(sectionSetting.Attribute("Name").Value, newSetting);
                    loadedCount += 1;

                    Logging.Information("[{component}] ({section}) loaded setting {name} - {value}", "SettingsManager", section.Name, newSetting.Name, newSetting.Value);
                } // foreach

                Sections.Add(section.Name, section);

            } // foreach

            stopWatch.Stop();
            Logging.Information("[{component}] loaded {count} settings from {path} in {time:0.00} ms.", "SettingsManager", loadedCount, Path.GetFileName(fs.Name), stopWatch.Elapsed.TotalMilliseconds);
        } // Load

        public static void Save(string filePath)
        {
            Logging.Information("[{component}] saving settings to {path}", "SettingsManager", filePath);

            XDocument doc = new XDocument();

            XElement root = new XElement("Settings");

            foreach (var section in Sections)
            {
                XElement xSection = new XElement("Section");
                xSection.SetAttributeValue("Name", section.Value.Name);

                foreach (var setting in section.Value.Settings)
                {
                    XElement xSetting = new XElement("Setting");
                    xSetting.SetAttributeValue("Name", setting.Value.Name);
                    xSetting.SetAttributeValue("Value", setting.Value.Value);

                    foreach (var kvp in setting.Value.OtherAttributes)
                        xSetting.SetAttributeValue(kvp.Key, kvp.Value);

                    xSection.Add(xSetting);
                } // foreach

                root.Add(xSection);
            } // foreach

            // root node
            doc.Add(root);

            doc.Save(filePath);
        } // Save

        public static T GetSetting<T>(string section, string name)
        {
            var setting = Sections[section].Settings[name].Value;

            return setting.ConvertTo<T>();
        } // GetSetting

        public static string UpdateSetting<T>(string section, string name, T value)
        {
            return Sections[section].Settings[name].Value = value.ToString();
        } // UpdateSetting

        public static List<Setting> GetSettings(string section)
        {
            var settings = new List<Setting>();

            foreach (var kvp in Sections[section].Settings)
                settings.Add(kvp.Value);

            return settings;
        } // GetSettings

    } // SettingsManager
}
