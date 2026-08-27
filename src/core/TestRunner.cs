using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Vintagestory.API.Server;
using System.Reflection;

//using System;

namespace TestHarnessMod.Core
{
    [AttributeUsage(AttributeTargets.Class)]
    public class TestFixtureAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method)]
    public class TestAttribute : Attribute
    {
        public string Name { get; }
        public TestAttribute(string name = null) => Name = name;
    }
}
//

namespace TestHarnessMod.Core
{
    public class TestRunner
    {
        public static async Task<List<(string Name, bool Passed, List<string> Logs)>> RunAll(
            ICoreServerAPI sapi,
            IServerNetworkChannel channel,
            AckMsgTracker ackTracker,
            string targetFixture = null,
            string targetTest = null)
        {
            var results = new List<(string, bool, List<string>)>();

            var testTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => (typeof(ModTest).IsAssignableFrom(t) || t.GetCustomAttribute<TestFixtureAttribute>() != null) && !t.IsAbstract);

            foreach (var type in testTypes)
            {
                // Filter by specific fixture class if specified
                if (!string.IsNullOrEmpty(targetFixture) && !type.Name.Equals(targetFixture, StringComparison.OrdinalIgnoreCase))
                    continue;

                // Find all methods decorated with [Test]
                var testMethods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(m => m.GetCustomAttribute<TestAttribute>() != null);

                foreach (var method in testMethods)
                {
                    var attr = method.GetCustomAttribute<TestAttribute>();
                    string testName = $"{type.Name}.{(string.IsNullOrEmpty(attr.Name) ? method.Name : attr.Name)}";

                    // Filter by specific test method if specified
                    if (!string.IsNullOrEmpty(targetTest) && !method.Name.Equals(targetTest, StringComparison.OrdinalIgnoreCase))
                        continue;

                    var instance = Activator.CreateInstance(type);
                    var ctx = new TestContext(sapi, channel, ackTracker);
                    bool passed = true;

                    try
                    {
                        var task = (Task)method.Invoke(instance, new object[] { ctx });
                        if (task != null)
                        {
                            await task;
                        }
                    }
                    catch (Exception ex)
                    {
                        passed = false;
                        ctx.Logs.Add($"[ERROR] Test failed: {ex.InnerException?.Message ?? ex.Message}");
                    }

                    results.Add((testName, passed, ctx.Logs));
                }
            }

            return results;
        }
    }
}
