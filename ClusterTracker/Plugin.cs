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
using System.Text.Json;



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
    [PluginService] internal static IPartyList PartyMembers { get; private set; } = null;

    private const string CommandName = "/ct";

    private const string configCommandName = "/ctcfg";

    public readonly string saveDataFilePath = Path.Combine(PluginInterface.ConfigDirectory.FullName, "ClusterData.json");

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
            {"slasher", new MobInfo() {rank= 1, kills= 0, clusters = 0}},
            {"nimrod", new MobInfo() {rank= 2, kills= 0, clusters = 0}},
            {"roader", new MobInfo() {rank = 3, kills = 0, clusters = 0}},
            {"death claw", new MobInfo() {rank = 3, kills = 0, clusters = 0}},
            {"vanguard", new MobInfo() {rank= 1, kills= 0, clusters = 0}},
            {"avenger", new MobInfo() {rank= 2, kills= 0, clusters = 0}},
            {"gunship", new MobInfo() {rank = 3, kills = 0, clusters = 0}},
            {"hexadrone", new MobInfo() {rank= 1, kills= 0, clusters = 0}},
            {"scorpion", new MobInfo() {rank= 2, kills= 0, clusters = 0}},
            {"armored weapon", new MobInfo() {rank = 3, kills = 0, clusters = 0}},
        };

    private void LoadData()
    {
        try
        {
            if (File.Exists(saveDataFilePath))
            {
                string json = File.ReadAllText(saveDataFilePath);
                var data = JsonSerializer.Deserialize<SavedData>(json);

                if (data != null)
                {
                    zadnorDict = data.ZadnorData;
                    bsfDict = data.BSFData;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to load data: {ex}");
        }
    }
    public void SaveData()
    {
        try
        {
            var data = new SavedData { ZadnorData = zadnorDict, BSFData = bsfDict };
            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(saveDataFilePath, json);
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to save data: {ex}");
        }
    }


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
        MainWindow = new MainWindow(this);

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Display Cluster Tracker Menu"
        });

        CommandManager.AddHandler(configCommandName, new CommandInfo(OnConfigCommand)
        {
            HelpMessage = "Display Cluster Tracker Settings"
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

        LoadData();


    }

    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();
        MainWindow.Dispose();

        CommandManager.RemoveHandler(CommandName);

        ChatGui.ChatMessage -= OnChatMessage;
        SaveData();
    }

    private void OnCommand(string command, string args)
    {
        // in response to the slash command, just toggle the display status of our main ui
        ToggleMainUI();
    }

    private void OnConfigCommand(string command, string args)
    {
        ToggleConfigUI();
    }

    internal static string lastEnemyKilled = "";


    private void OnChatMessage(XivChatType type, int timestamp, ref SeString sender, ref SeString message, ref bool isHandled)
    {
        if (!Configuration.DisableParty || PartyMembers.PartyId == 0)
        {
            if (message.TextValue.Contains("You defeat the 4th Legion") && (ushort)type == 2874)
            {

                lastEnemyKilled = Regex.Match(message.TextValue, "You defeat the 4th Legion (.*).$").Groups[1].Value;

                //Access BSF dictionary
                if (ClientState.TerritoryType == 920)
                {
                    if (bsfDict.TryGetValue(lastEnemyKilled, out MobInfo mob))
                    {
                        mob.kills++;
                    }
                }

                //Access Zadnor Dictionary
                if (ClientState.TerritoryType == 975)
                {
                    if (zadnorDict.TryGetValue(lastEnemyKilled, out MobInfo mob))
                    {
                        mob.kills++;
                    }
                }

            }

            //Checks to see if the last message was you getting a bozjan cluster
            if (message.TextValue.Contains("You obtain a Bozjan cluster") && (ushort)type == 2110 && lastEnemyKilled != "")
            {

                //Access BSF dictionary
                if (ClientState.TerritoryType == 920)
                {
                    if (bsfDict.TryGetValue(lastEnemyKilled, out MobInfo mob))
                    {
                        mob.clusters++;
                    }
                }

                //Access Zadnor Dictionary
                if (ClientState.TerritoryType == 975)
                {
                    if (zadnorDict.TryGetValue(lastEnemyKilled, out MobInfo mob))
                    {
                        mob.clusters++;

                    }
                }
                lastEnemyKilled = "";
            }
        }
    }

    private void DrawUI() => WindowSystem.Draw();

    public void ToggleConfigUI() => ConfigWindow.Toggle();
    public void ToggleMainUI() => MainWindow.Toggle();
}
