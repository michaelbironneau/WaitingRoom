using System.ComponentModel.DataAnnotations;

public class Access
{

    [Required]
    public string AccessToken { get; set; } = default!;

    [Required]
    public string BackendUri { get; set; } = default!;

}