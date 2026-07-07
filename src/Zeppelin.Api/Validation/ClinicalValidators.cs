using FluentValidation;
using Zeppelin.Api.Dtos.Clinical;

namespace Zeppelin.Api.Validation;

public class UpsertToothRecordRequestValidator : AbstractValidator<UpsertToothRecordRequest>
{
    public UpsertToothRecordRequestValidator()
    {
        RuleFor(x => x.ToothNumber).InclusiveBetween(1, 32);
    }
}

public class CreateTreatmentPlanItemRequestValidator : AbstractValidator<CreateTreatmentPlanItemRequest>
{
    public CreateTreatmentPlanItemRequestValidator()
    {
        RuleFor(x => x.Description).NotEmpty();
        RuleFor(x => x.ToothNumber).InclusiveBetween(1, 32).When(x => x.ToothNumber.HasValue);
        RuleFor(x => x.EstimatedCost).GreaterThanOrEqualTo(0).When(x => x.EstimatedCost.HasValue);
    }
}

public class CreateTreatmentPlanRequestValidator : AbstractValidator<CreateTreatmentPlanRequest>
{
    public CreateTreatmentPlanRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty();
        RuleForEach(x => x.Items).SetValidator(new CreateTreatmentPlanItemRequestValidator());
    }
}

public class CreateVisitNoteRequestValidator : AbstractValidator<CreateVisitNoteRequest>
{
    public CreateVisitNoteRequestValidator()
    {
        RuleFor(x => x.NoteText).NotEmpty();
    }
}
