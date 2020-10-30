//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Xml.Linq;

//namespace PandaEngine
//{
//    // frame collection manager class
//    public class PUIScene
//    {
//        public PUIFrameList SceneFrames { get; set; } = new PUIFrameList();
//        public string Name { get; set; }
//        public bool Visible { get; set; }
//        public bool Active { get; set; }

//        public PUIScene()
//        {
//            SceneFrames = new PUIFrameList();
//        }

//        public PUIScene(XElement el)
//        {
//            bool sceneVisible = true;
//            bool sceneActive = true;

//            if (el.Attribute("Visible") != null)
//                sceneVisible = Convert.ToBoolean(el.Attribute("Visible").Value);

//            if (el.Attribute("Active") != null)
//                sceneActive = Convert.ToBoolean(el.Attribute("Active").Value);

//            Visible = sceneVisible;
//            Active = sceneActive;
//            Name = el.Attribute("Name").Value;

//            if (PandaMonogameConfig.Logging)
//                Console.WriteLine("New scene: " + Name + " [active:" + Active.ToString() + "] [visible:" + Visible.ToString() + "]");
//        }

//        public virtual void Draw(SpriteBatch spriteBatch)
//        {
//            if (!Visible)
//                return;

//            SceneFrames.Draw(spriteBatch);
//        } // draw

//        public virtual void Update(GameTime gameTime)
//        {
//            if (!Active)
//                return;

//            SceneFrames.Update(gameTime);
//        }

//        public virtual void OnMouseMoved(Vector2 originalPosition, GameTime gameTime)
//        {
//            if (!Active)
//                return;

//            MouseState mouseState = Mouse.GetState();

//            SceneFrames.OnMouseMoved(originalPosition, new Vector2(mouseState.Position.X, mouseState.Position.Y), gameTime);
//        }

//        public virtual void OnMouseDown(MouseButtonID button, GameTime gameTime)
//        {
//            if (!Active)
//                return;

//            MouseState mouseState = Mouse.GetState();

//            SceneFrames.OnMouseDown(button, new Vector2(mouseState.Position.X, mouseState.Position.Y), gameTime);
//        }

//        public virtual void OnMouseClicked(MouseButtonID button, GameTime gameTime)
//        {
//            if (!Active)
//                return;

//            MouseState mouseState = Mouse.GetState();

//            SceneFrames.OnMouseClicked(button, new Vector2(mouseState.Position.X, mouseState.Position.Y), gameTime);
//        }

//        public void OnMouseScroll(MouseScrollDirection direction, int scrollValue, GameTime gameTime)
//        {
//            if (!Active)
//                return;

//            SceneFrames.OnMouseScroll(direction, scrollValue, gameTime);
//        }

//        public virtual void OnKeyPressed(Keys key, GameTime gameTime, CurrentKeyState currentKeyState)
//        {
//            if (!Active)
//                return;

//            SceneFrames.OnKeyPressed(key, gameTime, currentKeyState);
//        }

//        public virtual void OnKeyReleased(Keys key, GameTime gameTime, CurrentKeyState currentKeyState)
//        {
//            if (!Active)
//                return;

//            SceneFrames.OnKeyReleased(key, gameTime, currentKeyState);
//        }

//        public virtual void OnKeyDown(Keys key, GameTime gameTime, CurrentKeyState currentKeyState)
//        {
//            if (!Active)
//                return;

//            SceneFrames.OnKeyDown(key, gameTime, currentKeyState);
//        }

//        public virtual void OnTextInput(TextInputEventArgs e, GameTime gameTime, CurrentKeyState currentKeyState)
//        {
//            if (!Active)
//                return;

//            SceneFrames.OnTextInput(e, gameTime, currentKeyState);
//        }
//    }
//}
