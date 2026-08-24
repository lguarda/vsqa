using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

public class AckMsgTracker
{
    private int counter = 1;
    private readonly ConcurrentDictionary<int, TaskCompletionSource<bool>> pending = new();

    public int Register(out TaskCompletionSource<bool> tcs)
    {
        int id = Interlocked.Increment(ref counter);
        if (id == 0) { // this will never happen but WHO KNOWS (0 is for now ack need)
            id = Interlocked.Increment(ref counter);
        }
        tcs = new TaskCompletionSource<bool>();
        pending[id] = tcs;
        return id;
    }

    public void Complete(int id)
    {
        if (pending.TryRemove(id, out var tcs)) tcs.TrySetResult(true);
    }
}
