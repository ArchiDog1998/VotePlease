using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.IoC;

namespace VotePlease;

public class Service
{
    [PluginService]
    public static Framework Framework { get; private set; }

    [PluginService]
    public static CommandManager CommandManager { get; private set; }

    [PluginService]
    public static GameGui GameGui { get; private set; }

    [PluginService]
    internal static PartyList PartyList { get; private set; }

    [PluginService]
    internal static ClientState ClientState { get; private set; }

    [PluginService]
    public static ChatGui ChatGui { get; private set; }
}
