using NLog.Targets;

namespace ATI.Services.Common.Logging.Configuration;

public class ConfigFileTarget
{
    public string Name { get; set; }
    public int MaxArchiveDays { get; set; } = 7;
    public string FileName { get; set; } = "${currentdir}/Log/NLog.Errors.json";
    public ArchiveNumberingMode ArchiveNumbering { get; set; } = ArchiveNumberingMode.Date;
    public FileArchivePeriod ArchiveEvery { get; set; } = FileArchivePeriod.Day;
    public string ArchiveDateFormat { get; set; } = "yyyyMMdd";
    public string ArchiveFileName { get; set; } = "${currentdir}/Log/NLog.Error.{##}.json";
    public bool AddGeneralAttributes { get; set; } = true;
    public ConfigJsonAttribute[] Attributes { get; set; } = { };
}