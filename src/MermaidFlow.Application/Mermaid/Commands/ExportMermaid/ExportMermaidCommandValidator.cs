using FluentValidation;
using MermaidFlow.Application.Common.Interfaces;

namespace MermaidFlow.Application.Mermaid.Commands.ExportMermaid;

public class ExportMermaidCommandValidator : AbstractValidator<ExportMermaidCommand>
{
    private static readonly string[] AllowedFormats = ["svg", "png"];

    public ExportMermaidCommandValidator(IThemeRepository themeRepository)
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(MermaidConstants.MaxCodeLength)
            .WithMessage("Mermaid code must not exceed 50KB.");

        RuleFor(x => x.Theme)
            .NotEmpty()
            .MustAsync(async (theme, ct) =>
            {
                var t = await themeRepository.GetByNameAsync(theme);
                return t is not null && t.IsActive;
            })
            .WithMessage("Theme is not valid or is not active.");

        RuleFor(x => x.Format)
            .NotEmpty()
            .Must(format => AllowedFormats.Contains(format.ToLowerInvariant()))
            .WithMessage($"Format must be one of: {string.Join(", ", AllowedFormats)}.");
    }
}
