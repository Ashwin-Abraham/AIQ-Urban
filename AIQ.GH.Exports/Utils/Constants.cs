using System;
using System.Threading;

namespace AIQ.GH.Exports.Utils;
public static class Constants
{
    public static CancellationToken CommonToken =>
        new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;
}
