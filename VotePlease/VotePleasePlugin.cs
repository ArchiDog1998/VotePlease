using Dalamud.Game;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Logging;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using ValueType = FFXIVClientStructs.FFXIV.Component.GUI.ValueType;

namespace VotePlease;

internal class VotePleasePlugin : IDalamudPlugin, IDisposable
{
    public string Name => "Vote Please";

    public VotePleasePlugin(DalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<Service>();
        Service.Framework.Update += FrameworkUpdate;
    }

    public void Dispose()
    {
        Service.Framework.Update -= FrameworkUpdate;
    }

    private unsafe void FrameworkUpdate(Framework framework)
    {
        var bannerWindow = (AtkUnitBase*)Service.GameGui.GetAddonByName("BannerMIP", 1);
        if (bannerWindow == null) return;
        if (Service.ClientState.LocalPlayer == null) return;

        try
        {
            VoteBanner(bannerWindow, ChoosePlayer());
        }
        catch (Exception e)
        {
            PluginLog.Error(e, "Failed to vote!");
        }
    }

    private static unsafe int ChoosePlayer()
    {
        var hud =
    FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->GetUiModule()->GetAgentModule()->
        GetAgentHUD();

        if(hud == null) throw new Exception("HUD is empty!");

        var list = Service.PartyList.Where(i => 
        i.ObjectId != Service.ClientState.LocalPlayer.ObjectId && i.GameObject != null)
            .Select(PartyMember => (Math.Max(0, GetPartySlotIndex(PartyMember.ObjectId, hud) - 1), PartyMember));

        if (!list.Any()) throw new Exception("Party list is empty! Can't vote anyone!");

        var tanks = list.Where(i => i.PartyMember.ClassJob.GameData.Role == 1);
        var healer = list.Where(i => i.PartyMember.ClassJob.GameData.Role == 4);
        var dps = list.Where(i => !(i.PartyMember.ClassJob.GameData.Role is 1 or 4));

        (int index, PartyMember member) voteTarget;
        switch (Service.ClientState.LocalPlayer.ClassJob.GameData.Role)
        {
            //tank
            case 1:
                if (tanks.Any()) voteTarget = RandomPick(tanks);
                else if (healer.Any()) voteTarget = RandomPick(healer);
                else voteTarget = RandomPick(dps);
                break;

            //Healer
            case 4:
                if (healer.Any()) voteTarget = RandomPick(healer);
                else if (tanks.Any()) voteTarget = RandomPick(tanks);
                else voteTarget = RandomPick(dps);
                break;

            //DPS
            default:
                if (dps.Any()) voteTarget = RandomPick(dps);
                else if (tanks.Any()) voteTarget = RandomPick(tanks);
                else voteTarget = RandomPick(healer);
                break;
        }

        if (voteTarget.member == null) throw new Exception("No members! Can't vote!");

        Service.ChatGui.Print(new SeString(new List<Payload>()
        {
            new TextPayload("Vote to "),
            voteTarget.member.ClassJob.GameData.Role switch
            {
                1 => new IconPayload(BitmapFontIcon.Tank),
                4 => new IconPayload(BitmapFontIcon.Healer),
                _ => new IconPayload(BitmapFontIcon.DPS),
            },
            new PlayerPayload(voteTarget.member.Name.TextValue, voteTarget.member.World.GameData.RowId),
        }));
        return voteTarget.index;
    }

    static unsafe int GetPartySlotIndex(uint objectId, AgentHUD* hud)
    {
        var list = (HudPartyMember*)hud->PartyMemberList;
        for (var i = 0; i < hud->PartyMemberCount; i++)
        {
            if (list[i].ObjectId == objectId)
            {
                return i;
            }
        }

        return 0;
    }

    private static T RandomPick<T>(IEnumerable<T> list)
        => list.ElementAt(new Random().Next(list.Count()));

    private static unsafe void VoteBanner(AtkUnitBase* bannerWindow, int index)
    {
        var atkValues = (AtkValue*)Marshal.AllocHGlobal(2 * sizeof(AtkValue));
        atkValues[0].Type = atkValues[1].Type = ValueType.Int;
        atkValues[0].Int = 12;
        atkValues[1].Int = index;
        try
        {
            bannerWindow->FireCallback(2, atkValues);
        }
        finally
        {
            Marshal.FreeHGlobal(new IntPtr(atkValues));
        }
    }
}