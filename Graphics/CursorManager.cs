using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace PandaEngine
{
    public class Cursor
    {
        public string Name { get; set; }
        public Sprite Sprite { get; set; }
        public Vector2 Offset;

        public Cursor(string name, string assetName, Vector2? offset = null)
        {
            Name = name;
            Sprite = new Sprite(AssetManager.LoadTexture2D(assetName));

            if (offset.HasValue)
                Offset = offset.Value;
            else
                Offset = Vector2.Zero;
        }
    } // Cursor

    public static class CursorManager
    {
        internal static bool _registered = false;

        public static Dictionary<string, Cursor> Cursors { get; set; } = new Dictionary<string, Cursor>();
        public static Cursor CurrentCursor { get; set; }

        public static void SetCursor(string name, string assetName, Vector2? offset = null)
        {
            PandaGlobals.TryRegisterScreenSpaceDraw(Draw);

            if (Cursors.TryGetValue(name, out var cursor))
            {
                CurrentCursor = cursor;

                if (offset.HasValue)
                    CurrentCursor.Offset = offset.Value;
            }
            else
            {
                var newCursor = new Cursor(name, assetName, offset);
                Cursors.Add(name, newCursor);
                CurrentCursor = newCursor;
                PandaGlobals.Window.CursorVisible = false;
            }
        } // SetCursor

        public static void Disable()
        {
            CurrentCursor = null;
            PandaGlobals.Window.CursorVisible = true;
        }

        public static void Draw()
        {
            if (CurrentCursor == null)
                return;

            PandaGlobals.ScreenSpaceSpriteBatch2D.DrawSprite(CurrentCursor.Sprite, InputManager.MousePosition + CurrentCursor.Offset);
        }
    } // CursorManager
}
