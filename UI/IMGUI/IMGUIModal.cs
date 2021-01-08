using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Text;

namespace ElementEngine.UI
{
    public class IMGUIModal
    {
        private bool _result;

        public string Name { get; set; }
        public string Type { get; set; }

        public bool IsOpen = false;
        public ImGuiWindowFlags Flags;

        public IMGUIModal(string name, ImGuiWindowFlags flags)
        {
            Name = name;
            Flags = ImGuiWindowFlags.Modal | flags;
        }

        public void Open()
        {
            IsOpen = true;
            OnOpen();
        }

        public void Close()
        {
            IsOpen = false;
        }

        public virtual void OnOpen() { }

        public bool Begin()
        {
            if (IsOpen)
                ImGui.OpenPopup(Name);
            else
               return false;

            _result = ImGui.BeginPopupModal(Name, ref IsOpen, Flags);
            return _result;
        }

        public virtual void Update(GameTimer gameTimer) { }
        public virtual void Draw() { }

        public void End()
        {
            if (_result)
                ImGui.EndPopup();
        }
    }
}
