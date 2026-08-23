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

    private readonly IServerNetworkChannel channel;

    public TestContext(ICoreServerAPI sapi, IServerNetworkChannel channel)
    {
        this.sapi = sapi;
        this.channel = channel;
    }

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

    private void SendLook(IServerPlayer player, float yaw, float pitch)
    {
        try
        {
            Log($"OMG 5 {channel}");
            channel.SendPacket(new SetLookMessage { Yaw = yaw, Pitch = pitch }, player);
            Log("OMG 4");
        }
        catch (Exception ex)
        {
            Log($"Packet send failed: {ex}");
        }
    }

    public Task LookAt(float yaw, float pitch)
    {
        Log("OMG 1");
        var player = sapi.World.AllOnlinePlayers.FirstOrDefault() as IServerPlayer;
        Log("OMG 2");
        if (player == null) { Fail("No online player to rotate"); return Task.CompletedTask; }
        Log("OMG 3");

        SendLook(player, yaw, pitch);
        return Task.CompletedTask;
    }

    public Task LookAtBlock(BlockPos targetPos)
    {
        var player = sapi.World.AllOnlinePlayers.FirstOrDefault() as IServerPlayer;
        if (player == null) { Fail("No online player to rotate"); return Task.CompletedTask; }

        var eyePos = player.Entity.Pos.XYZ.Add(0, player.Entity.LocalEyePos.Y, 0);
        var target = targetPos.ToVec3d().Add(0.5, 0.5, 0.5);

        double dx = target.X - eyePos.X;
        double dy = target.Y - eyePos.Y;
        double dz = target.Z - eyePos.Z;
        double horizDist = Math.Sqrt(dx * dx + dz * dz);

        float yaw = (float)Math.Atan2(dx, dz);
        float pitch = (float)(Math.PI - Math.Atan2(dy, horizDist));

        SendLook(player, yaw, pitch);
        return Task.CompletedTask;
    }
    public Task Wait(int ms) => Task.Delay(ms);
    public void Log(string msg) => Logs.Add(msg);
    public void Fail(string reason) { Logs.Add("FAIL: " + reason); throw new TestFailedException(reason); }
}
}
