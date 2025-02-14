using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace ClusterTracker.Windows;

public class ConfigWindow : Window, IDisposable
{
    private Plugin Plugin;
    private Configuration Configuration;

    public ConfigWindow(Plugin plugin) : base("Cluster Tracker Config")
    {
        Flags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoResize;

        Size = new Vector2(232, 110);
        SizeCondition = ImGuiCond.Always;

        Plugin = plugin;

        Configuration = plugin.Configuration;
    }

    public void Dispose() { }

    public override void PreDraw()
    {
        // Flags must be added or removed before Draw() is being called, or they won't apply
        if (Configuration.IsConfigWindowMovable)
        {
            Flags &= ~ImGuiWindowFlags.NoMove;
        }
        else
        {
            Flags |= ImGuiWindowFlags.NoMove;
        }
    }

    public override void Draw()
    {
        var changed = false;

        var width = ImGui.GetWindowWidth();
        ImGui.SetNextItemWidth(width / 2f);
        changed |= ImGui.Checkbox("Disable while in party", ref Configuration.DisableParty);
        ImGui.Dummy(new Vector2(0, 5));
        if (ImGui.Button("Clear Log"))
        {
            Plugin.zadnorDict = new Dictionary<string, MobInfo>{
                {"nimrod", new MobInfo() {rank= 1, kills= 0, clusters = 0}},
                {"infantry", new MobInfo() {rank= 2, kills= 0, clusters = 0}},
                {"gunship", new MobInfo() {rank= 3, kills= 0, clusters = 0}},
                {"hexadrone", new MobInfo() {rank= 3, kills= 0, clusters = 0}},
                {"death machine", new MobInfo() {rank= 1, kills= 0, clusters = 0}},
                {"armored weapon", new MobInfo() {rank= 2, kills= 0, clusters = 0}},
                {"satellite", new MobInfo() {rank= 3, kills= 0, clusters = 0}},
                {"colossus", new MobInfo() {rank= 3, kills= 0, clusters = 0}},
                {"roader", new MobInfo() {rank= 1, kills= 0, clusters = 0}},
                {"rearguard", new MobInfo() {rank= 2, kills= 0, clusters = 0}},
                {"cavalry", new MobInfo() {rank= 3, kills= 0, clusters = 0}},
                {"helldiver", new MobInfo() {rank= 3, kills= 0, clusters = 0}}
            };

            Plugin.bsfDict = new Dictionary<string, MobInfo>{
                {"slashers", new MobInfo() {rank= 1, kills= 0, clusters = 0}},
                {"nimrod", new MobInfo() {rank= 2, kills= 0, clusters = 0}},
                {"gunship", new MobInfo() {rank= 3, kills= 0, clusters = 0}},
                {"vanguard", new MobInfo() {rank= 1, kills= 0, clusters = 0}},
                {"avenger", new MobInfo() {rank= 2, kills= 0, clusters = 0}},
                {"death claw", new MobInfo() {rank= 3, kills= 0, clusters = 0}},
                {"armored weapon", new MobInfo() {rank= 3, kills= 0, clusters = 0}},
                {"hexadrone", new MobInfo() {rank= 1, kills= 0, clusters = 0}},
                {"scorpion", new MobInfo() {rank= 2, kills= 0, clusters = 0}},
            };
            Plugin.SaveData();
        }

        if (changed)
            Configuration.Save();

        ImGui.EndTabItem();

    }
}
