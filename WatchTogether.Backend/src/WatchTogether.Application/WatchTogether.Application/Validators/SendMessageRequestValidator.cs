using FluentValidation;
using WatchTogether.Application.DTOs.Message;

namespace WatchTogether.Application.Validators;

public class SendMessageRequestValidator : AbstractValidator<SendMessageRequest>
{
    public SendMessageRequestValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Message content is required")
            .MaximumLength(2000).WithMessage("Message must be at most 2000 characters");
    }
}
