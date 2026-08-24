using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Client;
using System.Linq;

namespace TestHarnessMod.Core
{
public class TestContext
{
    private readonly ICoreServerAPI sapi;
    public List<string> Logs = new();
    private AckMsgTracker ackTracker;

    private readonly IServerNetworkChannel channel;

    public TestContext(ICoreServerAPI sapi, IServerNetworkChannel channel, AckMsgTracker ackTracker)
    {
        this.sapi = sapi;
        this.channel = channel;
        this.ackTracker = ackTracker;
    }

    private async Task SendLook(IServerPlayer player, float yaw, float pitch, int timeoutMs = 2000)
    {
        await SendPacketWithAck(channel, new SetLookMessage { Yaw = yaw, Pitch = pitch }, player);
    }

    // Helper to get Block pos from world coordinate
    public BlockPos SpawnRelative(int dx, int dy, int dz) =>
        sapi.World.DefaultSpawnPosition.AsBlockPos.AddCopy(dx, dy, dz);

    public Task PlaceBlock(BlockPos pos, string blockCode)
    {
        var block = sapi.World.GetBlock(new AssetLocation(blockCode));
        if (block == null) { Fail($"Unknown block {blockCode}"); return Task.CompletedTask; }
        sapi.World.BlockAccessor.SetBlock(block.Id, pos);
        return Task.CompletedTask;
    }

    public Task<bool> AssertBlock(BlockPos pos, string expectedCode)
    {
        var actual = sapi.World.BlockAccessor.GetBlock(pos);
        bool ok = actual?.Code?.ToString() == expectedCode;
        if (!ok) Fail($"Expected {expectedCode} at {pos}, got {actual?.Code}");
        return Task.FromResult(ok);
    }

    public Task<EntityPlayer> SpawnTestPlayer(string name, Vec3d pos)
    {
        // placeholder - real bot/player spawn logic later
        throw new NotImplementedException();
    }

    public Task Teleport(BlockPos pos)
    {
        var player = sapi.World.AllOnlinePlayers.FirstOrDefault();
        if (player == null) { Fail("No online player to teleport"); return Task.CompletedTask; }
        player.Entity.TeleportTo(pos.ToVec3d().Add(0.5, 0, 0.5));
        return Task.CompletedTask;
    }

    public Task GiveItem(string code, int slotIndex)
    {
        var player = sapi.World.AllOnlinePlayers.FirstOrDefault() as IServerPlayer;
        if (player == null) { Fail("No online player for inventory"); return Task.CompletedTask; }

        var loc = new AssetLocation(code);
        CollectibleObject collectible = sapi.World.GetItem(loc);
        if (collectible == null) collectible = sapi.World.GetBlock(loc);

        if (collectible == null) { Fail($"Unknown item/block {code}"); return Task.CompletedTask; }

        var hotbar = player.InventoryManager.GetHotbarInventory();
        var slot = hotbar[slotIndex];
        slot.Itemstack = new ItemStack(collectible);
        slot.MarkDirty();
        return Task.CompletedTask;
    }

    // TODO move all setup into separeted stuff from assert/Task
    public ICoreServerAPI GetSapi() {
        return sapi;
    }
    public IServerPlayer GetPlayer() {
        return sapi.World.AllOnlinePlayers.FirstOrDefault() as IServerPlayer;
    }

    public void SetPlayerActiveSlot(int slotNumber) {
        var player = sapi.World.AllOnlinePlayers.FirstOrDefault() as IServerPlayer;
        player?.InventoryManager.ActiveHotbarSlotNumber = slotNumber;
    }

    public Task<bool> AssertPlayerSlot(string expectedCode) {
        var player = sapi.World.AllOnlinePlayers.FirstOrDefault() as IServerPlayer;
        if (player == null) { Fail("No online player"); return Task.FromResult(false); }

        var ActiveSlot = player.InventoryManager.ActiveHotbarSlot;
        string actualCode = ActiveSlot?.Itemstack?.Collectible?.Code?.ToString() ?? "";

        bool ok = actualCode == expectedCode;
        if (!ok) Fail($"Expected {expectedCode} got {actualCode}");
        return Task.FromResult(true);
    }

    public Task SetGameMode(EnumGameMode mode)
    {
        var player = sapi.World.AllOnlinePlayers.FirstOrDefault() as IServerPlayer;
        if (player == null) { Fail("No online player for gamemode"); return Task.CompletedTask; }
        player.WorldData.CurrentGameMode = mode;
        player.BroadcastPlayerData(false);
        return Task.CompletedTask;
    }

    public Task SetTimeOfDay(float targetHour)
    {
        var cal = sapi.World.Calendar;
        float delta = targetHour - cal.HourOfDay;
        if (delta < 0) delta += cal.HoursPerDay; // wrap forward to target hour
        cal.Add(delta);
        return Task.CompletedTask;
    }

    public Task ReleaseAllKey() {
        var player = sapi.World.AllOnlinePlayers.FirstOrDefault() as IServerPlayer;
        if (player == null) { Fail("No online player to rotate"); return Task.CompletedTask; }
        channel.SendPacket(new KeyAction(true), player);
        return Task.CompletedTask;
    }

    public Task SendKey(GlKeys key, bool keyUp) {
        try
        {
            var player = sapi.World.AllOnlinePlayers.FirstOrDefault() as IServerPlayer;
            if (player == null) { Fail("No online player to rotate"); return Task.CompletedTask; }
            Log($"Server: button:{key}, state: {keyUp}");
            channel.SendPacket(new KeyAction(key, keyUp), player);
        }
        catch (Exception ex)
        {
            Log($"Packet send failed: {ex}");
        }
        return Task.CompletedTask;
    }

    // Todo move this out? maybe i will need this for client as well
    public async Task<bool> SendPacketWithAck<T>(IServerNetworkChannel channel, T message, IServerPlayer player, int timeoutMs = 2000) where T : IAckable
    {
        int id = ackTracker.Register(out var tcs);
        message.RequestId = id;
        channel.SendPacket(message, player);

        var winner = await Task.WhenAny(tcs.Task, Task.Delay(timeoutMs));
            if (winner != tcs.Task)
        {
            Fail($"{typeof(T).Name} ack timed out");
            return false;
        }
        return true;
    }

    public async Task LookAt(float yaw, float pitch)
    {
        var player = sapi.World.AllOnlinePlayers.FirstOrDefault() as IServerPlayer;
        if (player == null) { Fail("No player to rotate"); return; }
        await SendLook(player, yaw, pitch);
    }

    public async Task LookAtBlock(BlockPos targetPos)
    {
        var player = sapi.World.AllOnlinePlayers.FirstOrDefault() as IServerPlayer;
        if (player == null) { Fail("No player to rotate"); return; }

        var eyePos = player.Entity.Pos.XYZ.Add(0, player.Entity.LocalEyePos.Y, 0);
        var target = targetPos.ToVec3d().Add(0.5, 0.5, 0.5);

        double dx = target.X - eyePos.X;
        double dy = target.Y - eyePos.Y;
        double dz = target.Z - eyePos.Z;
        double horizDist = Math.Sqrt(dx * dx + dz * dz);

        float yaw = (float)Math.Atan2(dx, dz);
        float pitch = (float)(Math.PI - Math.Atan2(dy, horizDist));

        await SendLook(player, yaw, pitch);
    }
    public Task Wait(int ms) => Task.Delay(ms);

    // TODO REFACTO THIS AND OUTPUT TAP FORMAT
    public void Log(string msg) => Logs.Add(msg);
    public void Fail(string reason) { Logs.Add("FAIL: " + reason); throw new TestFailedException(reason); }
}
}
