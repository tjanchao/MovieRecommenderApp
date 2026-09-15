using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace movieRecommender.Identity;

/// <summary>
/// Length validation that counts what a reader would call characters.
/// </summary>
/// <remarks>
/// EC-5: a display name containing emoji or non-Latin script is accepted, with its
/// length counted in characters rather than bytes. <see cref="StringLengthAttribute"/>
/// counts UTF-16 code units, so "🎬" costs two and a family emoji can cost seven —
/// which would reject names well inside the 50-character limit of §5.1.
/// </remarks>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class TextElementLengthAttribute : ValidationAttribute
{
    public TextElementLengthAttribute(int minimum, int maximum)
    {
        Minimum = minimum;
        Maximum = maximum;
    }

    public int Minimum { get; }

    public int Maximum { get; }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        // A missing value is [Required]'s business, not ours.
        if (value is not string text)
        {
            return ValidationResult.Success;
        }

        var length = new StringInfo(text.Trim()).LengthInTextElements;
        if (length >= Minimum && length <= Maximum)
        {
            return ValidationResult.Success;
        }

        return new ValidationResult(
            ErrorMessage ?? $"{validationContext.DisplayName} must be between {Minimum} and {Maximum} characters.",
            validationContext.MemberName is null ? null : [validationContext.MemberName]);
    }
}
