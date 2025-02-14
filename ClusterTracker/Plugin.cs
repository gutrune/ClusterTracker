using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using ClusterTracker.Windows;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;



namespace ClusterTracker;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;

    private const string CommandName = "/ct";

    public static Dictionary<string, MobInfo> zadnorDict =
        new Dictionary<string, MobInfo>{
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
    public static Dictionary<string, MobInfo> bsfDict = 
        new Dictionary<string, MobInfo>{
            {"slashers", new MobInfo() {rank= 1, kills= 0, clusters = 0}},
            {"Nimrod", new MobInfo() {rank= 2, kills= 0, clusters = 0}},
            {"gunship", new MobInfo() {rank= 3, kills= 0, clusters = 0}},
            {"hexadrone", new MobInfo() {rank= 3, kills= 0, clusters = 0}},
            {"vanguard", new MobInfo() {rank= 1, kills= 0, clusters = 0}},
            {"Avenger", new MobInfo() {rank= 2, kills= 0, clusters = 0}},
            {"Death Claw", new MobInfo() {rank= 3, kills= 0, clusters = 0}},
            {"Armored Weapon", new MobInfo() {rank= 3, kills= 0, clusters = 0}},
            {"hexadrone", new MobInfo() {rank= 1, kills= 0, clusters = 0}},
            {"Scorpion", new MobInfo() {rank= 2, kills= 0, clusters = 0}},
        };

    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("SamplePlugin");
    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        // you might normally want to embed resources and load them from the manifest stream
        var goatImagePath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "goat.png");

        ConfigWindow = new ConfigWindow(this);
        MainWindow = new MainWindow(this, goatImagePath);

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Display Cluster Tracker Menu"
        });

        ChatGui.ChatMessage += OnChatMessage;

        PluginInterface.UiBuilder.Draw += DrawUI;

        // This adds a button to the plugin installer entry of this plugin which allows
        // to toggle the display status of the configuration ui
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;

        // Adds another button that is doing the same but for the main ui of the plugin
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUI;

        // Add a simple message to the log with level set to information
        // Use /xllog to open the log window in-game
        // Example Output: 00:57:54.959 | INF | [SamplePlugin] ===A cool log message from Sample Plugin===
        Log.Information($"===A cool log message from {PluginInterface.Manifest.Name}===");
        
    }

    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();
        MainWindow.Dispose();

        CommandManager.RemoveHandler(CommandName);

        ChatGui.ChatMessage -= OnChatMessage;
    }

    private void OnCommand(string command, string args){
        // in response to the slash command, just toggle the display status of our main ui
        ToggleMainUI();
    }

    private string lastEnemyKilled = "";
    private int numEnemiesKilled = 0;
    private int totalClusters;

    public Dictionary<string, int[]> BSFMobs = new(){
        {"slashers", [1, 0, 0]},
        {"vanguard", [1, 0, 0]},
        {"hexadrone", [1, 0, 0]},
        {"nimrod", [2, 0, 0]},
        {"avenger", [2, 0, 0]},
        {"scorpion", [2, 0, 0]},
        {"gunship", [3, 0, 0]},
        {"roader", [3, 0, 0]},
        {"death claw", [3, 0, 0]},
        {"armored weapon", [3, 0, 0]}
    };

    public Dictionary<string, int[]> zadnorMobs = new(){
        {"nimrod", [1, 0, 0]},
        {"infantry", [2, 0, 0]},
        {"gunship", [3, 0, 0]},
        {"hexadrone", [3, 0, 0]},
        {"death machine", [1, 0, 0]},
        {"armored weapon", [2, 0, 0]},
        {"satellite", [3, 0, 0]},
        {"colossus", [3, 0, 0]},
        {"roader", [1, 0, 0]},
        {"rearguard", [2, 0, 0]},
        {"cavalry", [3, 0, 0]},
        {"helldiver", [3, 0, 0]}
    };


     private void OnChatMessage(XivChatType type, int timestamp, ref SeString sender, ref SeString message, ref bool isHandled){
        if(message.TextValue.Contains("You defeat the 4th Legion") && (ushort)type == 2874){

            ChatGui.Print("Detected Kill");
            numEnemiesKilled++;
            lastEnemyKilled = Regex.Match(message.TextValue, "You defeat the 4th Legion (.*).$").Groups[1].Value;
        }

        //Checks to see if the last message was you getting a bozjan cluster
        if (message.TextValue.Contains("You obtain a Bozjan cluster") && (ushort)type == 2110 && lastEnemyKilled != ""){
            ChatGui.Print("Detected Bozjan cluster drop!");
            ChatGui.Print(lastEnemyKilled);
            totalClusters++;
            lastEnemyKilled = "";
        }
    }

    private void DrawUI() => WindowSystem.Draw();

    public void ToggleConfigUI() => ConfigWindow.Toggle();
    public void ToggleMainUI() => MainWindow.Toggle();
}
