using System;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Platform;

namespace SayTheSpire2.Buffers;

public class LobbyBuffer : Buffer
{
    private StartRunLobby? _lobby;

    public LobbyBuffer() : base("lobby") { }

    public void Bind(StartRunLobby lobby)
    {
        _lobby = lobby;
    }

    protected override void ClearBinding()
    {
        _lobby = null;
        Clear();
    }

    public override void Update()
    {
        if (_lobby == null) return;
        Repopulate(Populate);
    }

    private void Populate()
    {
        if (_lobby == null) return;

        foreach (var player in _lobby.Players)
        {
            var name = GetPlayerName(player.id);
            var character = player.character?.Title?.GetFormattedText() ?? "No character";
            var ready = player.isReady ? "Ready" : "Not ready";
            Add($"{name}, {character}, {ready}");
        }
    }

    private string GetPlayerName(ulong playerId)
    {
        try
        {
            if (_lobby != null)
                return PlatformUtil.GetPlayerName(_lobby.NetService.Platform, playerId);
        }
        catch { }
        return $"Player {playerId}";
    }
}
