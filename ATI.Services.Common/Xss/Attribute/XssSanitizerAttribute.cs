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
    public bool IsReplace { get; set; }

    private static readonly HtmlSanitizer Sanitizer = new ();

    protected override ValidationResult IsValid(object value, ValidationContext context)
    {
        var rawValue = value as string;
        if (rawValue == null)
            return ValidationResult.Success;

        var sanitised = Sanitizer.Sanitize(rawValue);

        if (sanitised != rawValue)
        {
            context.ObjectType.GetProperty(context.MemberName)
                ?.SetValue(context.ObjectInstance, sanitised);
        }
        return ValidationResult.Success;
    }
}