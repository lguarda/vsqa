using System.Threading.Tasks;
using TestHarnessMod.Core;
using Vintagestory.API.MathTools;
using Vintagestory.API.Common;
using Vintagestory.API.Client;

namespace TestHarnessMod.Tests
{
public class ExampleBlockPlaceTest : ModTest
{
    //public override async Task Run(ITestContext ctx)
    //{
    //    var pos = new BlockPos(0, 4, 0);
    //    await ctx.PlaceBlock(pos, "game:forestfloor-2");
    //    await ctx.AssertBlock(pos, "game:forestfloor-2");
    //    ctx.Log("Block placed and verified.");
    //}
    public override async Task Run(TestContext ctx)
    {
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
        "game:torch-basic-lit-up",
        ];
        await ctx.SendKey(GlKeys.R, false);
        string[] blocks = [
            "game:forestfloor-2",
            "game:tallgrass-medium-free",
            "game:log-placed-oak-ud",
            "game:crackedrock-andesite",
            "game:torch-basic-extinct-up",
            ];

        for (int i = 0; i < 5; i++)
        {
            var pos = ctx.SpawnRelative(0, 0, i); // adjust via SpawnRelative if you added that helper
            var bpos = ctx.SpawnRelative(2, 0, i); // adjust via SpawnRelative if you added that helper
            await ctx.PlaceBlock(bpos, blocks[i]);
            await ctx.Teleport(pos);
            await ctx.Wait(100);
            await ctx.LookAtBlock(bpos);
            await ctx.Wait(50);
            await ctx.AssertPlayerSlot(tools[i]);
            await ctx.Wait(1000);
        }
        await ctx.Wait(1000);
        await ctx.ReleaseAllKey();
        //await ctx.SendKey(GlKeys.R, true);
        //await ctx.GiveItem("game:forestfloor-2", 0);
        //await ctx.SetTimeOfDay(9f);
        //await ctx.Wait(3000);
        //await ctx.DebugSetPitch(0f);
        //ctx.Log("pitch = 0");
        //await ctx.Wait(3000);

        //await ctx.DebugSetPitch(1.5708f);
        //ctx.Log("pitch = +pi/2");
        //await ctx.Wait(3000);

        //await ctx.DebugSetPitch(-1.5708f);
        //ctx.Log("pitch = -pi/2");
        //await ctx.Wait(3000);

        //ctx.Log("Player teleported, rotated, given forestfloor-2, set to survival, time set to 9:00.");
    }
}
}
