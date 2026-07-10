using FluentValidation;
using Zeppelin.Dtos.Inventory;

namespace Zeppelin.Validation;

public class CreateInventoryItemRequestValidator : AbstractValidator<CreateInventoryItemRequest>
{
    public CreateInventoryItemRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Unit).NotEmpty();
        RuleFor(x => x.ParLevel).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PurchaseFee).GreaterThanOrEqualTo(0).When(x => x.PurchaseFee is not null);
        RuleFor(x => x.SaleFee).GreaterThanOrEqualTo(0).When(x => x.SaleFee is not null);
        RuleFor(x => x.Weight).GreaterThanOrEqualTo(0).When(x => x.Weight is not null);
    }
}

public class UpdateInventoryItemRequestValidator : AbstractValidator<UpdateInventoryItemRequest>
{
    public UpdateInventoryItemRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Unit).NotEmpty();
        RuleFor(x => x.ParLevel).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PurchaseFee).GreaterThanOrEqualTo(0).When(x => x.PurchaseFee is not null);
        RuleFor(x => x.SaleFee).GreaterThanOrEqualTo(0).When(x => x.SaleFee is not null);
        RuleFor(x => x.Weight).GreaterThanOrEqualTo(0).When(x => x.Weight is not null);
    }
}

public class CreateStockMovementRequestValidator : AbstractValidator<CreateStockMovementRequest>
{
    public CreateStockMovementRequestValidator()
    {
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.UnitCost).GreaterThanOrEqualTo(0).When(x => x.UnitCost is not null);
    }
}

public class CreateSupplierRequestValidator : AbstractValidator<CreateSupplierRequest>
{
    public CreateSupplierRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}

public class UpdateSupplierRequestValidator : AbstractValidator<UpdateSupplierRequest>
{
    public UpdateSupplierRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}

public class LinkItemSupplierRequestValidator : AbstractValidator<LinkItemSupplierRequest>
{
    public LinkItemSupplierRequestValidator()
    {
        RuleFor(x => x.SupplierId).NotEmpty();
        RuleFor(x => x.LastUnitCost).GreaterThanOrEqualTo(0).When(x => x.LastUnitCost is not null);
    }
}

public class UpdateItemSupplierLinkRequestValidator : AbstractValidator<UpdateItemSupplierLinkRequest>
{
    public UpdateItemSupplierLinkRequestValidator()
    {
        RuleFor(x => x.LastUnitCost).GreaterThanOrEqualTo(0).When(x => x.LastUnitCost is not null);
    }
}
