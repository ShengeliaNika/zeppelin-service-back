using FluentValidation;
using Zeppelin.Api.Dtos.Inventory;

namespace Zeppelin.Api.Validation;

public class CreateInventoryItemRequestValidator : AbstractValidator<CreateInventoryItemRequest>
{
    public CreateInventoryItemRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Unit).NotEmpty();
        RuleFor(x => x.ParLevel).GreaterThanOrEqualTo(0);
    }
}

public class UpdateInventoryItemRequestValidator : AbstractValidator<UpdateInventoryItemRequest>
{
    public UpdateInventoryItemRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Unit).NotEmpty();
        RuleFor(x => x.ParLevel).GreaterThanOrEqualTo(0);
    }
}

public class CreateStockMovementRequestValidator : AbstractValidator<CreateStockMovementRequest>
{
    public CreateStockMovementRequestValidator()
    {
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}
