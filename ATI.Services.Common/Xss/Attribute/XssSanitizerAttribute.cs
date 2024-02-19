using System;
using System.ComponentModel.DataAnnotations;
using Ganss.Xss;

namespace ATI.Services.Common.Xss.Attribute;

/// <summary>
/// Валидирует поле на наличие недопустимых символов
/// </summary>
public class XssSanitizerAttribute : ValidationAttribute
{
    /// <summary>
    /// Экранировать XSS и отдавать success
    /// </summary>
    /// <value>true - экранирует XSS </value>
    /// <value>false - возвращает, что модель не валидна</value>>
    public bool IsReplace { get; set; } = true;

    private static readonly HtmlSanitizer Sanitizer = new ();

    protected override ValidationResult IsValid(object value, ValidationContext context)
    {
        var rawValue = value as string;
        if (rawValue == null)
            return ValidationResult.Success;

        // Реплейсим спец. символ, потому что санитайзер его считает за xss
        rawValue = rawValue
            .Replace("\r", String.Empty)
            .TrimStart();

        var sanitised = Sanitizer.Sanitize(rawValue);

        if (sanitised != rawValue)
        {
            if (!IsReplace)
                return new ValidationResult("Xss was detected");
            
            context.ObjectType.GetProperty(context.MemberName)
                ?.SetValue(context.ObjectInstance, sanitised);
        }
        return ValidationResult.Success;
    }
}