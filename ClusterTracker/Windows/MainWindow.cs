using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using Lumina.Excel.Sheets;

namespace ClusterTracker.Windows;

public class MainWindow : Window, IDisposable
{
    private Plugin Plugin;

    // We give this window a hidden ID using ##
    // So that the user will see "My Amazing Window" as window title,
    // but for ImGui the ID is "My Amazing Window##With a hidden ID"
    public MainWindow(Plugin plugin)
        : base("Cluster Tracker", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse){
        
        SizeConstraints = new WindowSizeConstraints{
            MinimumSize = new Vector2(175, 120),
            MaximumSize = new Vector2(175, 120)
            
        };

        Plugin = plugin;
    }

    public void Dispose() { }

    public override void Draw()
    {

        Dictionary<int, (int kills, int clusters)> rankTotals = new();


        void ProcessStats(Dictionary<String, MobInfo> dict){
            
            foreach(var mob in dict.Values){
                if(!rankTotals.ContainsKey(mob.rank)){
                    rankTotals[mob.rank] = (0,0);
                }

                rankTotals[mob.rank] = (rankTotals[mob.rank].kills + mob.kills, rankTotals[mob.rank].clusters + mob.clusters);
            }
        }

        ProcessStats(Plugin.zadnorDict);
        ProcessStats(Plugin.bsfDict);

        ImGui.TextUnformatted("Cluster Statistics");
        foreach (var (rank, stats) in rankTotals){
            ImGui.TextUnformatted($"Rank {rank}: {stats.kills} Kills | {stats.clusters} Clusters");
        }

    }
}
