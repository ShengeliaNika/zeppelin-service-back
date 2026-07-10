using FluentValidation;
using Zeppelin.Dtos.Scheduling;

namespace Zeppelin.Validation;

public class CreateAppointmentTypeRequestValidator : AbstractValidator<CreateAppointmentTypeRequest>
{
    public CreateAppointmentTypeRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.DefaultDurationMinutes).GreaterThan(0);
        RuleFor(x => x.RecallIntervalMonths).GreaterThan(0).When(x => x.RecallIntervalMonths.HasValue);
    }
}

public class CreateChairRequestValidator : AbstractValidator<CreateChairRequest>
{
    public CreateChairRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
    }
}

public class CreateAppointmentRequestValidator : AbstractValidator<CreateAppointmentRequest>
{
    public CreateAppointmentRequestValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.DentistUserId).NotEmpty();
        RuleFor(x => x.AppointmentTypeId).NotEmpty();
        RuleFor(x => x.EndAtUtc).GreaterThan(x => x.StartAtUtc);
    }
}

public class RescheduleAppointmentRequestValidator : AbstractValidator<RescheduleAppointmentRequest>
{
    public RescheduleAppointmentRequestValidator()
    {
        RuleFor(x => x.DentistUserId).NotEmpty();
        RuleFor(x => x.AppointmentTypeId).NotEmpty();
        RuleFor(x => x.EndAtUtc).GreaterThan(x => x.StartAtUtc);
    }
}

public class UpdateAppointmentStatusRequestValidator : AbstractValidator<UpdateAppointmentStatusRequest>
{
    public UpdateAppointmentStatusRequestValidator()
    {
        RuleFor(x => x.CancelledReason).NotEmpty().When(x => x.Status == Enums.AppointmentStatus.Cancelled);
    }
}
