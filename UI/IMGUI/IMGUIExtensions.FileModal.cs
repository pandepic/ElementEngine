using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;

namespace ElementEngine.UI
{
    public enum FileModalMode
    {
        OpenFile,
        SaveFile,
        OpenDirectory,
    }

    internal class FileModal
    {
        public string Name { get; set; }
        public string CurrentPath { get; set; }
        public string EditingPath;
        public string Search = "";
        public string FileName = "";
        public int FiltersIndex = 0;

        public void SetPath(string path)
        {
            if (!Directory.Exists(path))
                return;

            CurrentPath = path;
            EditingPath = path;
        }
    }

    public static partial class IMGUIExtensions
    {
        private static readonly Dictionary<string, FileModal> _fileModalPaths = new Dictionary<string, FileModal>();

        public static string FileModal(string name, FileModalMode mode, string startPath = null, string[] filters = null, string fileName = null)
        {
            bool open = true;
            return FileModal(name, ref open, mode, startPath, filters, fileName);
        }

        public static string FileModal(string name, ref bool open, FileModalMode mode, string startPath = null, string[] filters = null, string fileName = null)
        {
            string selectedPath = null;

            if (ImGui.BeginPopupModal(name, ref open, ImGuiWindowFlags.AlwaysAutoResize))
            {
                if (!_fileModalPaths.TryGetValue(name, out var modal))
                {
                    modal = new UI.FileModal()
                    {
                        Name = name,
                        CurrentPath = startPath,
                        EditingPath = startPath,
                    };

                    _fileModalPaths.Add(name, modal);
                }

                var currentPath = modal.CurrentPath ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                if (modal.EditingPath == null)
                    modal.EditingPath = currentPath;

                if (fileName != null)
                    modal.FileName = fileName;

                var baseDirInfo = new DirectoryInfo(currentPath);
                var directories = Directory.GetDirectories(currentPath).OrderBy(d => d);
                var files = Directory.GetFiles(currentPath).OrderBy(f => f);

                ImGui.InputText("Path", ref modal.EditingPath, 200);
                ImGui.InputText("Search", ref modal.Search, 200);

                IMGUIManager.PushStyleColor(ImGuiCol.Button, new Vector4(0, 0, 0, 0));
                IMGUIManager.PushStyleColor(ImGuiCol.Text, new Vector4(1, 1, 0, 1));

                ImGui.BeginChild("List", new Vector2(400, 300));

                if (baseDirInfo.Parent != null)
                {
                    if (ImGui.Button("..."))
                        _fileModalPaths[name].SetPath(baseDirInfo.Parent.FullName);
                }

                foreach (var directory in directories)
                {
                    var dirInfo = new DirectoryInfo(directory);

                    if (!string.IsNullOrEmpty(modal.Search) && !dirInfo.Name.ToUpper().Contains(modal.Search.ToUpper()))
                        continue;
                    
                    if (ImGui.Button(dirInfo.Name))
                    {
                        _fileModalPaths[name].SetPath(dirInfo.FullName);
                    }
                }

                IMGUIManager.PopAllStyleColors();
                IMGUIManager.PushStyleColor(ImGuiCol.Button, new Vector4(0, 0, 0, 0));

                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);

                    if (!string.IsNullOrEmpty(modal.Search) && !fileInfo.Name.ToUpper().Contains(modal.Search.ToUpper()))
                        continue;

                    if ((mode == FileModalMode.SaveFile || mode == FileModalMode.OpenFile) && filters != null && fileInfo.Extension.ToUpper() != filters[modal.FiltersIndex].ToUpper())
                        continue;

                    if (ImGui.Button(fileInfo.Name))
                    {
                        if (mode == FileModalMode.OpenFile)
                        {
                            selectedPath = fileInfo.FullName;
                            ImGui.CloseCurrentPopup();
                        }
                        else if (mode == FileModalMode.SaveFile)
                        {
                            modal.FileName = fileInfo.Name;
                        }
                    }
                }
                
                ImGui.EndChild();

                if (mode == FileModalMode.SaveFile)
                {
                    ImGui.InputText("File", ref modal.FileName, 200);

                    if (filters != null)
                        ImGui.Combo("Filter", ref modal.FiltersIndex, filters, filters.Length);

                    IMGUIManager.PopAllStyleColors();
                    if (ImGui.Button("Save"))
                    {
                        selectedPath = Path.Combine(modal.CurrentPath, modal.FileName);
                        ImGui.CloseCurrentPopup();
                    }
                }
                else if (mode == FileModalMode.OpenFile)
                {
                    if (filters != null)
                        ImGui.Combo("Filter", ref modal.FiltersIndex, filters, filters.Length);
                }

                if (modal.CurrentPath != modal.EditingPath && Directory.Exists(modal.EditingPath))
                    modal.CurrentPath = modal.EditingPath;

                IMGUIManager.PopAllStyleColors();
                ImGui.EndPopup();
            }

            return selectedPath;

        } // FileModal
    }
}
