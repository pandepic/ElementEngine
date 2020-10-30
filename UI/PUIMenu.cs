//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Xml.Linq;

//namespace PandaEngine
//{
//    public delegate void BASICEVENT_FUNC(string arg);

//    public class PUIScript
//    {
//        protected PUIMenu _parent = null;

//        protected ScriptEngine _engine = Python.CreateEngine();
//        protected ScriptScope _scope = null;
//        public ScriptScope Scope
//        {
//            get { return _scope; }
//        }

//        protected ScriptSource _source = null;
//        protected CompiledCode _compiled = null;

//        public string Name { get; set; }
//        public string Filepath { get; set; }

//        public List<string> Classes { get; set; } = new List<string>();
//        protected Dictionary<string, object> _classObjects = new Dictionary<string, object>();

//        public PUIScript(PUIMenu parent)
//        {
//            this._parent = parent;

//            _engine.Runtime.IO.RedirectToConsole();
//        }

//        public void CreateScope()
//        {
//            _scope = _engine.CreateScope();
//        }

//        public void Compile(bool execute = true)
//        {
//            if (_scope == null)
//                CreateScope();

//            foreach (var kvp in _parent.Methods)
//            {
//                _scope.SetVariable(kvp.Key, kvp.Value);
//            }

//            _source = _engine.CreateScriptSourceFromFile(Filepath, Encoding.Unicode, SourceCodeKind.Statements);
//            _compiled = _source.Compile();

//            if (execute)
//            {
//                dynamic result = _compiled.Execute(_scope);
//            }

//            foreach (var className in Classes)
//            {
//                _classObjects.Add(className, _engine.Operations.Invoke(_scope.GetVariable(className)));
//            } // foreach
//        } // compile

//        public void CallClassMethod(string className, string methodName, object[] parameters)
//        {
//            _engine.Operations.InvokeMember(_classObjects[className], methodName, parameters);
//        } // callClassMethod
//    } // PUIScript

//    public delegate void MENU_METHOD_CALL(params object[] arguments);

//    public class PUIMenu
//    {
//        protected Dictionary<string, DynamicSpriteFont> _fonts = new Dictionary<string, DynamicSpriteFont>();
//        public Dictionary<string, PUIScript> Scripts { get; set; } = new Dictionary<string, PUIScript>();
//        public Dictionary<string, PUIScene> Scenes { get; set; } = new Dictionary<string, PUIScene>();
        
//        public Dictionary<string, MENU_METHOD_CALL> Methods { get; set; } = new Dictionary<string, MENU_METHOD_CALL>();
//        public Dictionary<string, XElement> Templates { get; set; } = new Dictionary<string, XElement>();

//        public bool Focused { get; private set; } = false;

//        public PUIMenu() { }

//        public void HandleEvents(string methodPath)
//        {
//            object[] parameters = new object[0];
            
//            if (string.IsNullOrWhiteSpace(methodPath))
//                return;
            
//            var startParams = methodPath.IndexOf("(");
//            var endParams = methodPath.IndexOf(")");

//            if (startParams > -1 && endParams > -1)
//            {
//                var paramString = methodPath.Substring(startParams + 1, endParams - startParams - 1);
//                paramString = paramString.Replace(" ", "");
//                var paramList = paramString.Split(',');
//                parameters = paramList.ToArray<object>();
//                methodPath = methodPath.Substring(0, startParams);
//            }

//            string[] pathParts = methodPath.Split('.');

//            Scripts[pathParts[0]].CallClassMethod(pathParts[1], pathParts[2], parameters);

//        } // handleButtonClicks

//        public void AddMethod(MENU_METHOD_CALL method)
//        {
//            Methods.Add(method.Method.Name, method);
//        }

//        public void Load(GraphicsDevice graphics, string assetName, string templatesName = "")
//        {
//            if (PandaMonogameConfig.Logging)
//                Console.WriteLine("Importing UI instance: " + assetName);

//            using (var fs = ModManager.Instance.AssetManager.GetFileStreamByAsset(assetName))
//            {
//                XDocument doc = XDocument.Load(fs);
//                XElement menuRoot = doc.Element("Menu");

//                XElement templatesRoot = null;

//                if (!string.IsNullOrWhiteSpace(templatesName))
//                {
//                    using (var fsTemplates = ModManager.Instance.AssetManager.GetFileStreamByAsset(templatesName))
//                    {
//                        templatesRoot = XDocument.Load(fsTemplates).Element("Templates");

//                        foreach (var template in templatesRoot.Elements("Template"))
//                        {
//                            var name = template.Attribute("TemplateName").Value;
//                            Templates.Add(name, template);
//                        }
//                    }
//                }

//                List<XElement> fontElements = menuRoot.Elements("Font").ToList();

//                foreach (var fontElement in fontElements)
//                {
//                    _fonts.Add(fontElement.Attribute("AssetName").Value, ModManager.Instance.AssetManager.LoadDynamicSpriteFont(fontElement.Attribute("AssetName").Value));
//                    if (PandaMonogameConfig.Logging)
//                        Console.WriteLine("New font: " + fontElement.Attribute("AssetName").Value);
//                }

//                List<XElement> scenes = menuRoot.Elements("Scene").ToList();

//                foreach (var scene in scenes)
//                {
//                    PUIScene newScene = new PUIScene(scene);

//                    List<XElement> frames = scene.Elements("Frame").ToList();

//                    foreach (var frame in frames)
//                    {
//                        PUIFrame newFrame = new PUIFrame(graphics, this, frame, _fonts, HandleEvents, Templates);

//                        newScene.SceneFrames.Add(newFrame);

//                    } // foreach

//                    newScene.SceneFrames.OrderByDrawOrder();
//                    Scenes.Add(scene.Attribute("Name").Value, newScene);
//                } // foreach

//                List<XElement> scripts = menuRoot.Elements("Script").ToList();

//                foreach (var script in scripts)
//                {
//                    PUIScript newScript = new PUIScript(this)
//                    {
//                        Name = script.Element("Name").Value,
//                        Filepath = ModManager.Instance.AssetManager.GetAssetPath(script.Element("AssetName").Value),
//                    };

//                    if (PandaMonogameConfig.Logging)
//                        Console.WriteLine("New script: " + newScript.Name + " - " + newScript.Filepath);

//                    List<XElement> scriptClasses = script.Elements("Class").ToList();

//                    foreach (var scriptClass in scriptClasses)
//                    {
//                        newScript.Classes.Add(scriptClass.Attribute("Name").Value);
//                    } // foreach

//                    newScript.CreateScope();
//                    newScript.Scope.SetVariable("MouseState", Mouse.GetState());
//                    newScript.Scope.SetVariable("MenuState", this);

//                    foreach (var scene in Scenes)
//                    {
//                        var widgetScriptList = scene.Value.SceneFrames.GetWidgetScriptList();

//                        foreach (var s in widgetScriptList)
//                        {
//                            newScript.Scope.SetVariable(scene.Key + "_" + s.Key, s.Value);
//                        }

//                        foreach (var f in scene.Value.SceneFrames.GetFrameScriptList())
//                        {
//                            newScript.Scope.SetVariable(scene.Key + "_" + f.Key, f.Value);
//                        }
//                    }

//                    newScript.Compile();

//                    Scripts.Add(newScript.Name, newScript);
//                } // foreach

//                if (PandaMonogameConfig.Logging)
//                    Console.WriteLine("Finished importing UI instance: " + assetName);
//            }
//        } // load

//        public void SetScriptScopeVariable(string name, object obj)
//        {
//            foreach (var script in Scripts)
//            {
//                script.Value.Scope.SetVariable(name, obj);
//            }
//        } // setScriptScopeVariable

//        public PUIFrame GetFrame(string name)
//        {
//            foreach (var s in Scenes)
//            {
//                var frame = s.Value.SceneFrames[name];
//                if (frame != null)
//                    return frame;
//            }

//            return null;
//        }

//        public PUIFrame GetFrame(string scene, string frame)
//        {
//            return Scenes[scene].SceneFrames[frame];
//        }

//        public dynamic GetWidget(string scene, string frame, string widget)
//        {
//            return Scenes[scene].SceneFrames[frame].Widgets[widget];
//        }

//        public T GetWidget<T>(string scene, string frame, string widget) where T : PUIWidget
//        {
//            return (T)GetWidget(scene, frame, widget);
//        }

//        public dynamic GetWidget(string name)
//        {
//            foreach (var s in Scenes)
//            {
//                for (var f = 0; f < s.Value.SceneFrames.Count; f++)
//                {
//                    var frame = s.Value.SceneFrames[f];
//                    var widget = frame.GetWidget(name);
//                    if (widget != null)
//                        return widget;
//                }
//            }

//            return default;
//        }

//        public T GetWidget<T>(string name) where T : PUIWidget
//        {
//            return (T)GetWidget(name);
//        }

//        public void UnFocus()
//        {
//            foreach (var kvp in Scenes)
//            {
//                var scene = kvp.Value;
//                scene.SceneFrames.UnFocusAll();
//            }

//            Focused = false;
//        }

//        internal void GrabFocus(PUIFrame frame)
//        {
//            foreach (var kvp in Scenes)
//            {
//                var scene = kvp.Value;
//                scene.SceneFrames.UnFocusAllExcept(frame.Name);
//            }

//            Focused = true;
//        }

//        internal void DropFocus(PUIFrame frame)
//        {
//            Focused = false;
//        }
        
//        public void Update(GameTime gameTime)
//        {
//            foreach (var script in Scripts)
//            {
//                script.Value.Scope.SetVariable("MouseState", Mouse.GetState());
//            }

//            foreach (var scene in Scenes)
//            {
//                scene.Value.Update(gameTime);
//            }
//        } // update

//        public void Draw(SpriteBatch spriteBatch)
//        {
//            foreach (var s in Scenes)
//            {
//                s.Value.Draw(spriteBatch);
//            }
//        } // draw

//        public void OnMouseDown(MouseButtonID button, GameTime gameTime)
//        {
//            foreach (var s in Scenes)
//            {
//                s.Value.OnMouseDown(button, gameTime);
//            }
//        }

//        public void OnMouseMoved(Vector2 originalPosition, GameTime gameTime)
//        {
//            foreach (var s in Scenes)
//            {
//                s.Value.OnMouseMoved(originalPosition, gameTime);
//            }
//        }

//        public void OnMouseClicked(MouseButtonID button, GameTime gameTime)
//        {
//            foreach (var s in Scenes)
//            {
//                s.Value.OnMouseClicked(button, gameTime);
//            }
//        }

//        public void OnMouseScroll(MouseScrollDirection direction, int scrollValue, GameTime gameTime)
//        {
//            foreach (var s in Scenes)
//            {
//                s.Value.OnMouseScroll(direction, scrollValue, gameTime);
//            }
//        }

//        public void OnKeyDown(Microsoft.Xna.Framework.Input.Keys key, GameTime gameTime, CurrentKeyState currentKeyState)
//        {
//            foreach (var s in Scenes)
//            {
//                s.Value.OnKeyDown(key, gameTime, currentKeyState);
//            }
//        }

//        public void OnKeyPressed(Microsoft.Xna.Framework.Input.Keys key, GameTime gameTime, CurrentKeyState currentKeyState)
//        {
//            foreach (var s in Scenes)
//            {
//                s.Value.OnKeyPressed(key, gameTime, currentKeyState);
//            }
//        }

//        public void OnKeyReleased(Microsoft.Xna.Framework.Input.Keys key, GameTime gameTime, CurrentKeyState currentKeyState)
//        {
//            foreach (var s in Scenes)
//            {
//                s.Value.OnKeyReleased(key, gameTime, currentKeyState);
//            }
//        }

//        public void OnTextInput(TextInputEventArgs e, GameTime gameTime, CurrentKeyState currentKeyState)
//        {
//            foreach (var s in Scenes)
//            {
//                s.Value.OnTextInput(e, gameTime, currentKeyState);
//            }
//        }
//    } // PUIMenu
//}
