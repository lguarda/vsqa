using System.Threading.Tasks;


namespace TestHarnessMod.Core
{
public abstract class ModTest
{
    public virtual string Name => GetType().Name;
    public abstract Task Run(TestContext ctx);
}
}
