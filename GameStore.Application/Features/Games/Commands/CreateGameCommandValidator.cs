using FluentValidation;

namespace GameStore.Application.Features.Games.Commands;

public class CreateGameCommandValidator : AbstractValidator<CreateGameCommand>
{
    public CreateGameCommandValidator()
    {
        RuleFor(v => v.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(50).WithMessage("Name must not exceed 50 characters.");

        RuleFor(v => v.GenreId)
            .GreaterThan(0).WithMessage("A valid Genre must be selected.");

        RuleFor(v => v.Price)
            .GreaterThanOrEqualTo(0).When(v => v.Price.HasValue)
            .WithMessage("Price cannot be negative.");
    }
}