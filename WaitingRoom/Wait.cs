using System.ComponentModel.DataAnnotations;

public class Wait
{

    [Required]
    public string QueueToken { get; set; } = default!;

    public int EstimatedWaitSeconds { get; set; }

}