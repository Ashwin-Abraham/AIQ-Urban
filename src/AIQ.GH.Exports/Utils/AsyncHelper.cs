using System;
using System.Threading;
using System.Threading.Tasks;

namespace AIQ.GH.Exports.Utils;

public static class AsyncHelper
{
    public static T RunSync<T>(Func<CancellationToken, Task<T>> func, CancellationToken token)
    {
        return Task.Run(() => func(token)).GetAwaiter().GetResult();
    }
    
    public static void RunSync(Func<CancellationToken, Task> func, CancellationToken token)
    {
        Task.Run(() => func(token)).GetAwaiter().GetResult();
    }

    public static T RunSync<T>(Func<Task<T>> func)
    {
        return Task.Run(func).GetAwaiter().GetResult();
    }
    
    public static void RunSync(Func<Task> func)
    {
        Task.Run(func).GetAwaiter().GetResult();
    }
}
