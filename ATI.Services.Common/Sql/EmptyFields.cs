using Dapper;
using JetBrains.Annotations;

namespace ATI.Services.Common.Sql;

[PublicAPI]
public static class EmptyFields
{
    public static DynamicParameters DynamicParameters => new();
}