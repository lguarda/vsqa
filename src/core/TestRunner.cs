using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Vintagestory.API.Server;

namespace TestHarnessMod.Core
{
public class TestRunner
{
    public static async Task<List<(string Name, bool Passed, List<string> Logs)>> RunAll(ICoreServerAPI sapi, IServerNetworkChannel channel, AckTracker ackTracker)
    {
        var results = new List<(string, bool, List<string>)>();
        var testTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(ModTest).IsAssignableFrom(t) && !t.IsAbstract);

        foreach (var type in testTypes)
        {
            var test = (ModTest)Activator.CreateInstance(type);
            var ctx = new TestContext(sapi, channel, ackTracker);
            bool passed = true;
            try { await test.Run(ctx); }
            catch (Exception) { passed = false; }
            results.Add((test.Name, passed, ctx.Logs));
        }
        return results;
    }
}
}
