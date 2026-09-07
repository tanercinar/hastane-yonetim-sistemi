using System.ComponentModel.DataAnnotations;

namespace HospitalManagement.Host.Api;

public sealed class ValidationProbeRequest
{
    [Required(ErrorMessage = "required")]
    [StringLength(32, MinimumLength = 3, ErrorMessage = "length_out_of_range")]
    public string? ClientName
    {
        get;
        init;
    }
}
