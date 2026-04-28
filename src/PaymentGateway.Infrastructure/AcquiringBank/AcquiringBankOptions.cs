using System.ComponentModel.DataAnnotations;

namespace PaymentGateway.Infrastructure.AcquiringBank;

public class AcquiringBankOptions
{
    public const string SectionName = "AcquiringBank";

    [Required(AllowEmptyStrings = false)]
    [Url]
    public string BaseUrl { get; set; } = string.Empty;
}
