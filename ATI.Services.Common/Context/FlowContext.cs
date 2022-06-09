using System.Threading;
using JetBrains.Annotations;

namespace ATI.Services.Common.Context
{
    [PublicAPI]
    public static class FlowContext<T>
    {
        private static readonly AsyncLocal<T> AsyncLocal = new();

        public static T Current
        {
            get => AsyncLocal.Value;
            set => AsyncLocal.Value = value;
        }
    }
}