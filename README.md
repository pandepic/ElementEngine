# ElementEngine
A cross-platform C# 2D game engine with native windowing, input, and rendering backends.

Still very much an early stage work in progress with an initial focus on optimised 2D game development.

Documentation is currently limited, but feel free to ask questions on Discord: https://discord.gg/5Q6AHHQwdM

### Features
- Simply inherit from the base game class and overload the load, update and draw methods, and you'll get a fully working game loop out of the box
- Game states are built into the core game loop so you can change states through a single easy method call
- 2D rendering with sprite batching, primitive batching and highly optimised tile maps
- A TTF font renderer that can be batched through the sprite batch
- Sprites, animated sprites and layered animated sprites
- A simple file format for importing and playing sprite animations
- Asset manager to support easy modding and resource loading
- Simple 2D collision handling for rectangles and circles
- A simple state machine and GOAP controller for entity AI
- An easy to use 2D camera
- Keyboard and mouse input management
- A game controls system that transforms keyboard and mouse input combinations into meaningful control names like "Move Up", "Attack" etc.
- Import maps created in Tiled with a built in optimised renderer for them that makes use of custom properties to load assets and control layer ordering
- An XML based UI system that includes most common widget types, and can be easily extended to support custom widgets
- Easy built in Dear IMGUI support

### Graphics Backends
ElementEngine uses its own rendering abstraction and supports DirectX 12 on Windows, Vulkan on Windows and Linux, and Metal on macOS.
Window creation and keyboard, mouse, text, and gamepad input are handled by native engine platform layers on Windows, Linux, and macOS.
Linux uses native Wayland when a Wayland session is available and falls back to X11 or XWayland.
The Wayland platform layer requires the system Wayland client, Wayland cursor, and xkbcommon libraries.
Wayland intentionally does not expose global window positioning or client-controlled display-mode switching.
On Wayland, `FullScreen` and `BorderlessFullScreen` both request compositor-managed fullscreen while preserving the requested engine state, and the compositor chooses the output mode.
Wayland compositors also require the user or compositor to restore minimized windows, so an application cannot force that transition.
The Wayland backend currently uses integer output scaling and one active seat, and it supports xkbcommon compose text but not fractional-scale or text-input-v3 preedit protocols.
Windows gamepad input uses XInput (up to four controllers, no Guide button) plus a raw HID fallback for DirectInput-only and generic pads.
macOS gamepad input uses the GameController framework for MFi and licensed controllers (Xbox, PlayStation, Switch Pro) plus an IOHIDManager fallback for generic USB HID pads on macOS 11 and later.
HID fallback pads use a heuristic button/axis layout; the SDL-style controller mapping database (`EnableGamepads(applyMappings: true)`) is currently applied on Linux only, so unusual HID pads may need remapping in game settings.

### Sample Projects
In the future some better sample projects for specific features will be created, but for now here are some of my open source projects that use this engine:

- GitHub Game Off 2020 entry: https://github.com/pandepic/GAME_OFF_2020
- Ludum Dare 48 entry: https://github.com/pandepic/LudumDare48
