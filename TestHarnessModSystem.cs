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
    private AckTracker ackTracker = new AckTracker();

    public BlockPos SpawnRelative(int dx, int dy, int dz) => sapi.World.DefaultSpawnPosition.AsBlockPos.AddCopy(dx, dy,
                                                                                                                dz);

    private void OnAck(IPlayer fromPlayer, AckMessage msg) {
        var elapsed = sapi.World.ElapsedMilliseconds;
        sapi.Logger.Notification($"OMG RECIEV ACK {msg.RequestId}  {elapsed}");
        ackTracker.Complete(msg.RequestId);
    }
    public override void StartServerSide(ICoreServerAPI api) {
        sapi = api;

        serverChannel = api.Network.RegisterChannel("testharness")
        .RegisterMessageType<AckMessage>()
        .RegisterMessageType<SetLookMessage>()
        .RegisterMessageType<KeyAction>()
        //.SetMessageHandler<AckMessage>((fromPlayer, msg) => ackTracker.Complete(msg.RequestId));
        .SetMessageHandler<AckMessage>(OnAck);

        sapi.ChatCommands.Create("runtests")
            .WithDescription("Runs all discovered ModTests")
            .RequiresPrivilege(Privilege.controlserver)
            .HandleWith(OnRunTests);

        sapi.ChatCommands.Create("debugplace")
            .WithDescription("Raw block placement debug, no harness involved")
            .RequiresPrivilege(Privilege.controlserver)
            .HandleWith(args => {
                var pos = SpawnRelative(0, 0, 5);

                var block = sapi.World.GetBlock(new AssetLocation("game:forestfloor-2"));

                sapi.Logger.Notification($"Resolved block: id={block?.Id}, code={block?.Code}");

                sapi.World.BlockAccessor.SetBlock(block.Id, pos);

                var readback = sapi.World.BlockAccessor.GetBlock(pos);
                sapi.Logger.Notification($"Immediate readback: {readback?.Code} {pos}");

                return TextCommandResult.Success($"Set {block.Code}, readback {readback?.Code}");
            });
    }

    private TextCommandResult OnRunTests(TextCommandCallingArgs args) {
        _ = RunTestsAsync(args);
        return TextCommandResult.Success("Running tests...");
    }

    private async Task RunTestsAsync(TextCommandCallingArgs args) {
        var results = await TestRunner.RunAll(sapi, serverChannel, ackTracker);

        foreach (var (name, passed, logs) in results) {
            string status = passed ? "PASS" : "FAIL";
            sapi.Logger.Notification($"[TestHarness] {status} - {name}");
            foreach (var line in logs)
                sapi.Logger.Notification($"[TestHarness]    {line}");
        }

        int passedCount = results.FindAll(r => r.Passed).Count;

        if (args.Caller.Player != null) {
            sapi.SendMessage(args.Caller.Player, args.Caller.FromChatGroupId,
                             $"Tests complete: {passedCount}/{results.Count} passed. See server log for details.",
                             EnumChatType.CommandSuccess);
        }
    }
}
}
