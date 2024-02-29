using System;

namespace ATI.Services.Common.Logging
{
    [Flags]
    public enum WarningError
    {
        BadRequestException = 1,
        ConnectionResetException = 2
    }
}