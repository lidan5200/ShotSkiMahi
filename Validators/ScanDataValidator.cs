using FluentValidation;
using ShotSkiMahiD.Models;

namespace ShotSkiMahiD.Validators
{
    public class ScanDataValidator : AbstractValidator<ScanData>
    {
        private readonly ScanConfig _config;

        public ScanDataValidator(ScanConfig config)
        {
            _config = config;

            RuleFor(x => x.SFC)
                .NotEmpty().WithMessage("SFC不能为空")
                .Length(_config.AssemblyScanMinLength, _config.AssemblyScanMaxLength)
                .WithMessage($"SFC长度必须在{_config.AssemblyScanMinLength}到{_config.AssemblyScanMaxLength}之间");

            RuleFor(x => x.MagnetCode)
                .NotEmpty().WithMessage("磁铁码不能为空")
                .Length(_config.GlueScanMinLength, _config.GlueScanMaxLength)
                .WithMessage($"磁铁码长度必须在{_config.GlueScanMinLength}到{_config.GlueScanMaxLength}之间")
                .Matches(GetMagnetCodePattern())
                .WithMessage($"磁铁码格式不符合规则: {_config.RMRule}");
        }

        private string GetMagnetCodePattern()
        {
            var pattern = _config.RMRule
                .Replace("?", "[A-Za-z0-9]")
                .Replace("*", "[A-Za-z0-9]*");
            return $"^{pattern}$";
        }
    }

    public class SfcValidator : AbstractValidator<string>
    {
        private readonly ScanConfig _config;

        public SfcValidator(ScanConfig config)
        {
            _config = config;

            RuleFor(sfc => sfc)
                .NotEmpty().WithMessage("SFC不能为空")
                .Length(config.AssemblyScanMinLength, config.AssemblyScanMaxLength)
                .WithMessage($"SFC长度必须在{config.AssemblyScanMinLength}到{config.AssemblyScanMaxLength}之间");
        }
    }

    public class MagnetCodeValidator : AbstractValidator<string>
    {
        private readonly ScanConfig _config;

        public MagnetCodeValidator(ScanConfig config)
        {
            _config = config;

            RuleFor(code => code)
                .NotEmpty().WithMessage("磁铁码不能为空")
                .Length(config.GlueScanMinLength, config.GlueScanMaxLength)
                .WithMessage($"磁铁码长度必须在{config.GlueScanMinLength}到{config.GlueScanMaxLength}之间")
                .Matches(GetMagnetCodePattern())
                .WithMessage($"磁铁码格式不符合规则: {config.RMRule}");
        }

        private string GetMagnetCodePattern()
        {
            var pattern = _config.RMRule
                .Replace("?", "[A-Za-z0-9]")
                .Replace("*", "[A-Za-z0-9]*");
            return $"^{pattern}$";
        }
    }
}
