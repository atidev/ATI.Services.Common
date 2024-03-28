using System.Collections.Generic;

namespace ATI.Services.Common.Metrics;

internal static class MetricsHelper
{
    /// <summary>
    /// Метод управляющий порядком лэйблов и их значений
    /// <param name="actionName"> Указывать только при объявлении лейблов. Записывается он в таймере, так как нужен для трейсинга</param>
    /// </summary>
    public static string[] ConcatLabels(
        string className,
        string machineName,
        string actionName,
        string entityName,
        string externHttpService,
        string[] userLabels,
        params string[] additionalLabels)
    {
        var labels = new List<string>
        {
            className,
            actionName
        };
            
        if (machineName != null)
            labels.Add(machineName);

        if (entityName != null)
            labels.Add(entityName);

        if (externHttpService != null)
            labels.Add(externHttpService);

        if (userLabels != null && userLabels.Length != 0)
            labels.AddRange(userLabels);

        if (additionalLabels.Length != 0)
            labels.AddRange(additionalLabels);

        return labels.ToArray();
    }
}