using FluentValidation;
using Zeppelin.Api.Dtos.Team;

namespace Zeppelin.Api.Validation;

public class CreateTeamTaskRequestValidator : AbstractValidator<CreateTeamTaskRequest>
{
    public CreateTeamTaskRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty();
        RuleFor(x => x.AssignedToUserId).NotEmpty();
    }
}
