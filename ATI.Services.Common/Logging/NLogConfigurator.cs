using System;
using System.Collections.Generic;
using System.Linq;
using ATI.Services.Common.Logging.Configuration;
using JetBrains.Annotations;
using NLog;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;
using NLog.Web;

namespace ATI.Services.Common.Logging
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class NLogConfigurator
    {
        private readonly NLogOptions _options;

        private static readonly List<JsonAttribute> GeneralAttributes = new()
        {
            JsonAttributeHelper.CreateWithoutUnicodeEscaping("timestamp", "${longdate}"),
            JsonAttributeHelper.CreateWithoutUnicodeEscaping("team", "services"),
            JsonAttributeHelper.CreateWithoutUnicodeEscaping("level", "${level:uppercase=true}"),
            JsonAttributeHelper.CreateWithoutUnicodeEscaping("action", "${aspnet-mvc-action}"),
            JsonAttributeHelper.CreateWithoutUnicodeEscaping("controller", "${aspnet-mvc-controller}"),
            JsonAttributeHelper.CreateWithoutUnicodeEscaping("method", "${aspnet-request-method}"),
            JsonAttributeHelper.CreateWithoutUnicodeEscaping("class", "${logger}"),
            JsonAttributeHelper.CreateWithoutUnicodeEscaping("url", "${aspnet-request-url}"),
            JsonAttributeHelper.CreateWithoutUnicodeEscaping("client", "${aspnet-request-ip}"),
            JsonAttributeHelper.CreateWithoutUnicodeEscaping("message", "${message}"),
            JsonAttributeHelper.CreateWithoutUnicodeEscaping("machinename", "${machinename}"),
            JsonAttributeHelper.CreateWithoutUnicodeEscaping("exceptionPretty", "${onexception:${exception:format=ToString,Data:exceptionDataSeparator=\\r\\n}}"),
            JsonAttributeHelper.CreateWithoutUnicodeEscaping("logContext", "${event-properties:logContext}"),
            JsonAttributeHelper.CreateWithoutUnicodeEscaping("metricString", "${event-properties:metricString}"),
            JsonAttributeHelper.CreateWithoutUnicodeEscaping("metricSource", "${event-properties:metricSource}"),
            JsonAttributeHelper.CreateWithoutUnicodeEscaping("responseBody", "${event-properties:responseBody}")
        };

        public NLogConfigurator(NLogOptions options)
        {
            _options = options;
            GeneralAttributes.AddRange(
                options.LoggedRequestHeader.Select(loggedHeader =>
                    JsonAttributeHelper.CreateWithoutUnicodeEscaping(loggedHeader,
                        $"${{aspnet-request:header={loggedHeader}}}")));
        }

        public void ConfigureNLog()
        {
            try
            {
                LogManager.ThrowExceptions = _options.ThrowExceptions;
                
                var configuration = new LoggingConfiguration();

                AddVariables(configuration);
                
                var attributes = MergeAttributes();

                foreach (var target in GenerateTargets(attributes.ToList()))
                {
                    configuration.AddTarget(target);
                }

                ApplyRules(configuration, _options.Rules);

                NLogBuilder.ConfigureNLog(configuration);
            }
            catch (Exception exception)
            {
                LogManager.GetCurrentClassLogger().Error(exception);
            }
        }

        private IEnumerable<JsonAttribute> MergeAttributes()
        {
            var validCustomAttributes = ExcludeInvalidAttributes(_options.Attributes);
            var attributes = _options.AddGeneralAttributes
                ? GeneralAttributes.OverrideBy(validCustomAttributes).ToList()
                : validCustomAttributes.ToList();

            if (!string.IsNullOrEmpty(_options.ApplicationName))
            {
                attributes.Add(JsonAttributeHelper.CreateWithoutUnicodeEscaping("app", _options.ApplicationName));
            }
            if (!string.IsNullOrEmpty(_options.LoggingEnvironment))
            {
                attributes.Add(JsonAttributeHelper.CreateWithoutUnicodeEscaping("env", _options.LoggingEnvironment));
            }
            
            return attributes;
        }

        private void ApplyRules(LoggingConfiguration configuration, Rule[] rules)
        {
            foreach (var rule in rules.Where(r => !string.IsNullOrEmpty(r.TargetName)))
            {
                configuration.AddRule(rule.MinLevel, rule.MaxLevel, rule.TargetName, rule.LoggerNamePattern);
            }
        }

        private void AddVariables(LoggingConfiguration configuration)
        {
            foreach (var variable in _options.Variables)
            {
                if (!string.IsNullOrEmpty(variable.Name) && !string.IsNullOrEmpty(variable.Value))
                {
                    configuration.Variables.Add(variable.Name, variable.Value);
                }
            }
        }

        private IEnumerable<JsonAttribute> ExcludeInvalidAttributes(IEnumerable<ConfigJsonAttribute> attributes) =>
            attributes?.Where(x => !string.IsNullOrEmpty(x?.Name) && !string.IsNullOrEmpty(x.Layout))
                .Select(JsonAttributeHelper.ToJsonAttribute);

        private IEnumerable<Target> GenerateTargets(List<JsonAttribute> attributes)
        {
            foreach (var fileTarget in _options.FileTargets)
            {
                if(string.IsNullOrEmpty(fileTarget.Name))
                    continue;

                var validAttributes = ExcludeInvalidAttributes(fileTarget.Attributes);
                var targetAttributes = fileTarget.AddGeneralAttributes
                    ? attributes.OverrideBy(validAttributes)
                    : validAttributes;

                yield return new FileTarget
                {
                    Name = fileTarget.Name,
                    FileName = fileTarget.FileName,
                    MaxArchiveDays = fileTarget.MaxArchiveDays,
                    ArchiveNumbering = fileTarget.ArchiveNumbering,
                    ArchiveEvery = fileTarget.ArchiveEvery,
                    ArchiveDateFormat = fileTarget.ArchiveDateFormat,
                    ArchiveFileName = fileTarget.ArchiveFileName,
                    Layout = GenerateJsonLayout(targetAttributes)
                };
            }
            
            foreach (var networkTarget in _options.NetworkTargets)
            {
                if (string.IsNullOrEmpty(networkTarget.Name))
                    continue;
                
                var validAttributes = ExcludeInvalidAttributes(networkTarget.Attributes);
                var targetAttributes = networkTarget.AddGeneralAttributes
                    ? attributes.OverrideBy(validAttributes)
                    : validAttributes;
                
                yield return new NetworkTarget
                {
                    Name = networkTarget.Name,
                    Address = networkTarget.Address,
                    KeepConnection = networkTarget.KeepConnection,
                    OnOverflow = networkTarget.OnOverflow,
                    NewLine = networkTarget.NewLine,
                    Layout = GenerateJsonLayout(targetAttributes)
                };
            }
        }
        
        private static JsonLayout GenerateJsonLayout(IEnumerable<JsonAttribute> jsonAttributes)
        {
            var jsonLayout = new JsonLayout();
            foreach (var jsonAttribute in jsonAttributes)
            {
                jsonLayout.Attributes.Add(jsonAttribute);
            }
            return jsonLayout;
        }
    }
}