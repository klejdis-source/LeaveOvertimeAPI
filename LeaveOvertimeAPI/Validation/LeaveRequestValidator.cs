using FluentValidation;
using LeaveOvertimeAPI.DTOs;

namespace LeaveOvertimeAPI.Validation
{
    public class LeaveRequestValidator : AbstractValidator<CreateLeaveRequestDto>
    {
        public LeaveRequestValidator()
        {
            RuleFor(x => x.Type)
                .NotEmpty()
                .WithMessage("Tipi i lejes është i detyrueshëm.")
                .Must(t => t == LeaveType.Vacation || t == LeaveType.Sick || t == LeaveType.Unpaid)
                .WithMessage("Tipi duhet të jetë: Vacation, Sick ose Unpaid.");

            RuleFor(x => x.StartDate)
                .NotEmpty()
                .WithMessage("StartDate është e detyrueshme.")
                .GreaterThanOrEqualTo(DateTime.Today)
                .WithMessage("StartDate nuk mund të jetë në të kaluarën.");

            RuleFor(x => x.EndDate)
                .NotEmpty()
                .WithMessage("EndDate është e detyrueshme.")
                .GreaterThanOrEqualTo(x => x.StartDate)
                .WithMessage("EndDate nuk mund të jetë para StartDate.");

            RuleFor(x => x.Reason)
                .NotEmpty()
                .WithMessage("Arsyeja është e detyrueshme.")
                .MinimumLength(5)
                .WithMessage("Arsyeja duhet të ketë të paktën 5 karaktere.")
                .MaximumLength(500)
                .WithMessage("Arsyeja nuk mund të kalojë 500 karaktere.");
        }
    }
}