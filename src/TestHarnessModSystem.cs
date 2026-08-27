using System.Threading.Tasks;
using TestHarnessMod.Core;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using System.Collections.Generic;
using Vintagestory.API.MathTools;
using System.Linq;

namespace TestHarnessMod {


public class TestHarnessModSystem : ModSystem {
    private IServerNetworkChannel serverChannel;

    public override bool ShouldLoad(EnumAppSide side) => side == EnumAppSide.Server;

    private ICoreServerAPI sapi;
    private AckMsgTracker ackTracker = new AckMsgTracker();

    public BlockPos SpawnRelative(int dx, int dy, int dz) => sapi.World.DefaultSpawnPosition.AsBlockPos.AddCopy(dx, dy,
                                                                                                                dz);

    private void OnAck(IPlayer fromPlayer, AckMessage msg) {
        var elapsed = sapi.World.ElapsedMilliseconds;
        Logger.slog($"msg ack:{msg.requestId} ms:{elapsed}");
        ackTracker.Complete(msg.requestId);
    }

    public override void StartServerSide(ICoreServerAPI api) {
        sapi = api;
        Logger.sapi = api;

        serverChannel = api.Network.RegisterChannel("testharness")
        .RegisterMessageType<AckMessage>()
        .RegisterMessageType<SetLookMessage>()
        .RegisterMessageType<KeyAction>()
        .RegisterMessageType<MouseAction>()
        .SetMessageHandler<AckMessage>(OnAck);

        RegisterCommands(sapi);
        //sapi.ChatCommands.Create("runtests")
        //    .WithDescription("Runs all discovered ModTests")
        //    .RequiresPrivilege(Privilege.controlserver)
        //    .HandleWith(OnRunTests);
    }
    public void RegisterCommands(ICoreServerAPI sapi)
    {
        var parsers = sapi.ChatCommands.Parsers;

        sapi.ChatCommands.Create("runtests")
            .WithDescription("Runs all tests, a specific fixture, or a specific test (Fixture.Method).")
            .RequiresPrivilege(Privilege.controlserver)
            .WithArgs(parsers.OptionalWord("filter"))
            .HandleWith(OnRunTests);
sapi.ChatCommands.Create("resetchunks")
    .WithDescription("Deletes chunks in a radius around world origin")
    .RequiresPrivilege(Privilege.controlserver)
    .HandleWith(args =>
    {
        int radius = 6;
        int originCx = sapi.WorldManager.MapSizeX / (2 * sapi.WorldManager.ChunkSize);
        int originCz = sapi.WorldManager.MapSizeZ / (2 * sapi.WorldManager.ChunkSize);

        var farAway = new Vec3d(0, 300, 0); // well outside radius, high up

        foreach (IServerPlayer player in sapi.World.AllOnlinePlayers)
        {
            player.Entity.TeleportTo(farAway);
        }

        for (int cx = originCx - radius; cx <= originCx + radius; cx++)
        {
            for (int cz = originCz - radius; cz <= originCz + radius; cz++)
            {
                sapi.WorldManager.DeleteChunkColumn(cx, cz);
            }
        }

        sapi.Event.RegisterCallback(dt =>
        {
            foreach (IServerPlayer player in sapi.World.AllOnlinePlayers)
            {
                player.CurrentChunkSentRadius = 0;
                var pos = sapi.World.DefaultSpawnPosition.AsBlockPos.AddCopy(0, 2, 0);
                player.Entity.TeleportTo(pos.ToVec3d());
            }
        }, 1000); // delay so delete has time to actually process

        return TextCommandResult.Success($"Deleted chunks in {radius} chunk radius around origin.");
    });
    }

    private TextCommandResult OnRunTests(TextCommandCallingArgs args)
    {
        string targetFixture = null;
        string targetTest = null;

        if (!args.Parsers[0].IsMissing)
        {
            string filter = (string)args[0];

            // Handles "ExampleBlockPlaceTest.TestSecondaryBlockInteraction"
            if (filter.Contains('.'))
            {
                var parts = filter.Split('.', 2);
                targetFixture = parts[0];
                targetTest = parts[1];
            }
            else
            {
                // Handles "ExampleBlockPlaceTest"
                targetFixture = filter;
            }
        }

        _ = RunTestsAsync(args, targetFixture, targetTest);

        string response = "Running tests";
        if (!string.IsNullOrEmpty(targetFixture) && !string.IsNullOrEmpty(targetTest))
        {
            response += $" for {targetFixture}.{targetTest}";
        }
        else if (!string.IsNullOrEmpty(targetFixture))
        {
            response += $" for fixture {targetFixture}";
        }

        return TextCommandResult.Success($"{response}...");
    }

    private async Task RunTestsAsync(TextCommandCallingArgs args, string targetFixture, string targetTest)
    {
        var results = await TestRunner.RunAll(sapi, serverChannel, ackTracker, targetFixture, targetTest);

        if (results.Count == 0)
        {
            if (args.Caller.Player != null)
            {
                sapi.SendMessage(args.Caller.Player, args.Caller.FromChatGroupId,
                                 "No matching tests found.",
                                 EnumChatType.CommandError);
            }
            return;
        }

        foreach (var (name, passed, logs) in results)
        {
            string status = passed ? "PASS" : "FAIL";
            sapi.Logger.Notification($"[TestHarness] {status} - {name}");
            foreach (var line in logs)
                sapi.Logger.Notification($"[TestHarness]    {line}");
        }

        int passedCount = results.Count(r => r.Passed);

        if (args.Caller.Player != null)
        {
            sapi.SendMessage(args.Caller.Player, args.Caller.FromChatGroupId,
                             $"Tests complete: {passedCount}/{results.Count} passed. See server log for details.",
                             EnumChatType.CommandSuccess);
        }
    }

}
}
