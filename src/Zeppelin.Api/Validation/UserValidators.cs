using FluentValidation;
using Zeppelin.Api.Dtos.Admin;
using Zeppelin.Domain.Common;

namespace Zeppelin.Api.Validation;

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.FirstName).NotEmpty();
        RuleFor(x => x.LastName).NotEmpty();
        RuleFor(x => x.Roles).NotEmpty();
        RuleForEach(x => x.Roles).Must(Roles.All.Contains).WithMessage("Unknown role.");
    }
}

public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty();
        RuleFor(x => x.LastName).NotEmpty();
    }
}

public class SetUserRolesRequestValidator : AbstractValidator<SetUserRolesRequest>
{
    public SetUserRolesRequestValidator()
    {
        RuleFor(x => x.Roles).NotEmpty();
        RuleForEach(x => x.Roles).Must(Roles.All.Contains).WithMessage("Unknown role.");
    }
}
