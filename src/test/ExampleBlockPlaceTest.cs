using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using TestHarnessMod.Core;

namespace TestHarnessMod.Tests
{
    [TestFixture]
    public class ExampleBlockPlaceTest
    {
        [Test("Place Peat Bricks With Ctrl+RightClick")]
        public async Task TestPeatBrickPlacement(TestContext ctx)
        {
            //await ctx.ResetChunk();
            IServerPlayer player = ctx.GetPlayer();
            player.InventoryManager.DiscardAll();

            var pos = ctx.SpawnRelative(0, 0, -2);
            var bpos = ctx.SpawnRelative(1, -1, -2);

            await ctx.Teleport(pos);
            await ctx.Wait(100);
            await ctx.LookAtBlock(bpos);
            ctx.SetPlayerActiveSlot(0);
            await ctx.GiveItem("game:peatbrick", 0);
            await ctx.GiveItem("game:peatbrick", 1);

            await ctx.Wait(50);
            await ctx.SendKey(GlKeys.ControlLeft, true);
            await ctx.SendMouseButton(EnumMouseButton.Right, true);
            await ctx.SendMouseButton(EnumMouseButton.Right, false);
            await ctx.SendKey(GlKeys.ControlLeft, false);
            await ctx.Wait(200);
            await ctx.AssertPlayerSlot("game:peatbrick");
        }

        [Test] // Uses method name automatically if no name string is provided
        public async Task TestSecondaryBlockInteraction(TestContext ctx)
        {
            //await ctx.ResetChunk();
            ICoreServerAPI sapi = ctx.GetSapi();
            var entityType = sapi.World.GetEntityType(
                new AssetLocation("game:hare-arctic-adult-male")
            );

            var entity = sapi.World.ClassRegistry.CreateEntity(entityType);

            entity.Pos.SetPos(ctx.SpawnRelative(2, 0, 8));

            sapi.World.SpawnEntity(entity);

            // Kill it immediately
            if (entity is EntityAgent agent)
            {
                agent.Die(EnumDespawnReason.Death);
            }

            {
                var bpos = ctx.SpawnRelative(1, 0, 6);
                await ctx.PlaceBlock(bpos, "game:bloomerybase-east");
                bpos = ctx.SpawnRelative(1, 1, 6);
                await ctx.PlaceBlock(bpos, "game:bloomerychimney");
            }
            IServerPlayer player = ctx.GetPlayer();
            player.InventoryManager.DiscardAll();
            await ctx.SetGameMode(EnumGameMode.Survival);

            await ctx.GiveItem("game:shovel-steel", 1);
            await ctx.GiveItem("game:pickaxe-steel", 2);
            await ctx.GiveItem("game:knife-generic-steel", 3);
            await ctx.GiveItem("game:axe-felling-steel", 4);
            await ctx.GiveItem("game:torch-basic-lit-up", 5);

            string[] tools = [
            "game:shovel-steel",
            "game:knife-generic-steel",
            "game:axe-felling-steel",
            "game:pickaxe-steel",
            "game:knife-generic-steel",
            "game:torch-basic-lit-up",
            ];
            await ctx.SendKey(GlKeys.R, true);
            string[] blocks = [
                "game:forestfloor-2",
                "game:tallgrass-medium-free",
                "game:log-placed-oak-ud",
                "game:crackedrock-andesite",
                "game:mushroom-almondmushroom-normal",
                "game:torch-basic-extinct-up",
                ];

            for (int i = 0; i < 6; i++)
            {
                var pos = ctx.SpawnRelative(0, 0, i); // adjust via SpawnRelative if you added that helper
                var bpos = ctx.SpawnRelative(1, 0, i); // adjust via SpawnRelative if you added that helper
                await ctx.PlaceBlock(bpos, blocks[i]);
                bpos = ctx.SpawnRelative(1, -1, i); // adjust via SpawnRelative if you added that helper
                await ctx.Teleport(pos);
                await ctx.Wait(100);
                await ctx.LookAtBlock(bpos);
                await ctx.Wait(100);
                await ctx.AssertPlayerSlot(tools[i]);
                await ctx.Wait(400);
            }
            await ctx.ReleaseAllKey();
        }
    }
}
//using System.Threading.Tasks;
//using TestHarnessMod.Core;
//using Vintagestory.API.MathTools;
//using Vintagestory.API.Common;
//using Vintagestory.API.Client;
//using Vintagestory.API.Server;
//
//namespace TestHarnessMod.Tests
//{
//public class ExampleBlockPlaceTest : ModTest
//{
//    public override async Task Run(TestContext ctx)
//    {
//        IServerPlayer player = ctx.GetPlayer();
//        player.InventoryManager.DiscardAll();
//        ICoreServerAPI sapi = ctx.GetSapi();
//
//        var pos = ctx.SpawnRelative(0, 0, -2); // adjust via SpawnRelative if you added that helper
//        var bpos = ctx.SpawnRelative(1, -1, -2); // adjust via SpawnRelative if you added that helper
//
//        await ctx.Teleport(pos);
//        await ctx.Wait(100);
//        await ctx.LookAtBlock(bpos);
//        ctx.SetPlayerActiveSlot(0);
//        await ctx.GiveItem("game:peatbrick", 0);
//        await ctx.GiveItem("game:peatbrick", 1);
//
//        await ctx.Wait(50);
//        await ctx.SendKey(GlKeys.ControlLeft, true);
//        await ctx.SendMouseButton(EnumMouseButton.Right, true);
//        await ctx.SendMouseButton(EnumMouseButton.Right, false);
//        await ctx.SendKey(GlKeys.ControlLeft, false);
//        await ctx.AssertPlayerSlot("game:peatbrick")
//    }
//    /*
//    public override async Task Run(TestContext ctx)
//    {
//        ICoreServerAPI sapi = ctx.GetSapi();
//        var entityType = sapi.World.GetEntityType(
//            new AssetLocation("game:hare-arctic-adult-male")
//        );
//
//        var entity = sapi.World.ClassRegistry.CreateEntity(entityType);
//
//        entity.Pos.SetPos(ctx.SpawnRelative(2, 0, 8));
//
//        sapi.World.SpawnEntity(entity);
//
//        // Kill it immediately
//        if (entity is EntityAgent agent)
//        {
//            //Logger.slog("6 oooooooooooooooo");
//            agent.Die(EnumDespawnReason.Death);
//        }
//
//        {
//            var bpos = ctx.SpawnRelative(1, 0, 6);
//            await ctx.PlaceBlock(bpos, "game:bloomerybase-east");
//            bpos = ctx.SpawnRelative(1, 1, 6);
//            await ctx.PlaceBlock(bpos, "game:bloomerychimney");
//        }
//        IServerPlayer player = ctx.GetPlayer();
//        player.InventoryManager.DiscardAll();
//        await ctx.SetGameMode(EnumGameMode.Survival);
//
//        await ctx.GiveItem("game:shovel-steel", 1);
//        await ctx.GiveItem("game:pickaxe-steel", 2);
//        await ctx.GiveItem("game:knife-generic-steel", 3);
//        await ctx.GiveItem("game:axe-felling-steel", 4);
//        await ctx.GiveItem("game:torch-basic-lit-up", 5);
//
//        string[] tools = [
//        "game:shovel-steel",
//        "game:knife-generic-steel",
//        "game:axe-felling-steel",
//        "game:pickaxe-steel",
//        "game:knife-generic-steel",
//        "game:torch-basic-lit-up",
//        ];
//        await ctx.SendKey(GlKeys.R, true);
//        string[] blocks = [
//            "game:forestfloor-2",
//            "game:tallgrass-medium-free",
//            "game:log-placed-oak-ud",
//            "game:crackedrock-andesite",
//            "game:mushroom-almondmushroom-normal",
//            "game:torch-basic-extinct-up",
//            ];
//
//        for (int i = 0; i < 6; i++)
//        {
//            var pos = ctx.SpawnRelative(0, 0, i); // adjust via SpawnRelative if you added that helper
//            var bpos = ctx.SpawnRelative(1, 0, i); // adjust via SpawnRelative if you added that helper
//            await ctx.PlaceBlock(bpos, blocks[i]);
//            await ctx.Teleport(pos);
//            await ctx.Wait(100);
//            await ctx.LookAtBlock(bpos);
//            await ctx.Wait(50);
//            await ctx.AssertPlayerSlot(tools[i]);
//            await ctx.Wait(400);
//        }
//        await ctx.ReleaseAllKey();
//        // THIS one fail randomly because smart cursor clash with refill module this need to be fixed
//        await ctx.AssertPlayerSlot("");
//        //await ctx.SendKey(GlKeys.R, false);
//        //await ctx.GiveItem("game:forestfloor-2", 0);
//        //await ctx.SetTimeOfDay(9f);
//        //await ctx.Wait(3000);
//        //await ctx.DebugSetPitch(0f);
//        //ctx.Log("pitch = 0");
//        //await ctx.Wait(3000);
//
//        //await ctx.DebugSetPitch(1.5708f);
//        //ctx.Log("pitch = +pi/2");
//        //await ctx.Wait(3000);
//
//        //await ctx.DebugSetPitch(-1.5708f);
//        //ctx.Log("pitch = -pi/2");
//        //await ctx.Wait(3000);
//
//        //ctx.Log("Player teleported, rotated, given forestfloor-2, set to survival, time set to 9:00.");
//    }
//    */
//}
//}
