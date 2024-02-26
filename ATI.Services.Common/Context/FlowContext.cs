using System.Threading;
using JetBrains.Annotations;

namespace ATI.Services.Common.Context;

[PublicAPI]
public static class FlowContext<T> where T : new()
{
    private static readonly AsyncLocal<T> AsyncLocal = new();

    public static T Current
    {
        get
        {
            if (AsyncLocal.Value != null)
                return AsyncLocal.Value;
            return AsyncLocal.Value = new ();
        }
        set => AsyncLocal.Value = value;
    }
}