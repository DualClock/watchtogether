using FluentValidation;
using WatchTogether.Application.DTOs.Room;

namespace WatchTogether.Application.Validators;

public class CreateRoomRequestValidator : AbstractValidator<CreateRoomRequest>
{
    public CreateRoomRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Room name is required")
            .MinimumLength(3).WithMessage("Room name must be at least 3 characters")
            .MaximumLength(100).WithMessage("Room name must be at most 100 characters");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must be at most 500 characters");

        RuleFor(x => x.Type)
            .Must(type => new[] { "public", "bylink", "private" }.Contains(type.ToLower()))
            .WithMessage("Type must be public, bylink, or private");

        RuleFor(x => x.Password)
            .NotEmpty().When(x => x.Type.ToLower() == "private")
            .WithMessage("Password is required for private rooms")
            .MinimumLength(4).When(x => !string.IsNullOrEmpty(x.Password))
            .WithMessage("Password must be at least 4 characters");

        RuleFor(x => x.MaxUsers)
            .InclusiveBetween(2, 50).WithMessage("Max users must be between 2 and 50");
    }
}
