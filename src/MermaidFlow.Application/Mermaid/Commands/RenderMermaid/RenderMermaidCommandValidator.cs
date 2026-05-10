using FluentValidation;
using MermaidFlow.Application.Common.Interfaces;

namespace MermaidFlow.Application.Mermaid.Commands.RenderMermaid;

public class RenderMermaidCommandValidator : AbstractValidator<RenderMermaidCommand>
{
    public RenderMermaidCommandValidator(IThemeRepository themeRepository)
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
    }
}
