using System;
using System.Collections.Generic;
using ElementEngine.ECS;
using ImGuiNET;

namespace ElementEngine
{
    // very quick and ugly just for some basic debugging
    // needs much more improvement
    public static class IMGUIECSBrowser
    {
        public static Registry SelectedRegistry;

        public static HashSet<Type> IncludeComponents = new();
        public static HashSet<Type> ExcludeComponents = new();

        public static void SelectRegistry(Registry registry)
        {
            if (SelectedRegistry == registry)
                return;

            SelectedRegistry = registry;
            IncludeComponents.Clear();
            ExcludeComponents.Clear();
        }

        public static void Draw()
        {
            if (Registry._nextRegistryID == 0)
                return;

            if (SelectedRegistry == null)
                SelectedRegistry = Registry._registries[0];

            ImGui.Begin("ECS Browser", ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.AlwaysAutoResize);

            if (ImGui.BeginMenuBar())
            {
                if (ImGui.BeginMenu("Registries"))
                {
                    foreach (var registry in Registry._registries)
                    {
                        if (registry == null)
                            continue;
                        if (ImGui.MenuItem($"ID {registry.RegistryID}", "", SelectedRegistry == registry))
                            SelectRegistry(registry);
                    }

                    ImGui.EndMenu();
                }

                if (ImGui.BeginMenu("Include"))
                {
                    foreach (var (_, type) in SelectedRegistry.ComponentHashCodeLookup)
                    {
                        var included = IncludeComponents.Contains(type);

                        if (ImGui.MenuItem(type.Name, "", included))
                        {
                            if (included)
                                IncludeComponents.Remove(type);
                            else
                                IncludeComponents.Add(type);
                        }
                    }

                    ImGui.EndMenu();
                }

                if (ImGui.BeginMenu("Exclude"))
                {
                    foreach (var (_, type) in SelectedRegistry.ComponentHashCodeLookup)
                    {
                        var excluded = ExcludeComponents.Contains(type);

                        if (ImGui.MenuItem(type.Name, "", excluded))
                        {
                            if (excluded)
                                ExcludeComponents.Remove(type);
                            else
                                ExcludeComponents.Add(type);
                        }
                    }

                    ImGui.EndMenu();
                }

                ImGui.EndMenuBar();
            }

            var entityViewBuilder = SelectedRegistry.BuildView();

            foreach (var include in IncludeComponents)
                entityViewBuilder.Include(include);
            foreach (var exclude in ExcludeComponents)
                entityViewBuilder.Exclude(exclude);

            var entityView = entityViewBuilder.Build();

            ImGui.Text($"Total Entities: {SelectedRegistry.Entities.Size}");

            if (entityView != null)
            {
                ImGui.Text($"View Entities: {entityView.Count}");
                ImGui.NewLine();

                foreach (var entity in entityView)
                {
                    var status = SelectedRegistry.Entities[entity.ID];

                    if (ImGui.CollapsingHeader($"[ID: {entity.ID}] [Name: {status.Name ?? ""}]"))
                    {
                        foreach (var componentType in status.ComponentTypes)
                        {
                            ImGui.Text(componentType.Name);

                            if (SelectedRegistry.ComponentStores.TryGetValue(componentType.GetHashCode(), out var store)
                                && store is IComponentDebugView componentStore
                                && componentStore.GetBoxed(entity.ID) is object component)
                            {
                                foreach (var fieldInfo in componentStore.PublicFields)
                                    ImGui.Text($"{fieldInfo.Name}: {fieldInfo.GetValue(component)}");
                            }

                            ImGui.Separator();
                        }
                    }
                }
            }

            ImGui.End();
        }

    }
}
