using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;

public class Access
{

    private static readonly String Secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    public static int TokenValidSeconds = 60 * 10;

    [Required]
    public string AccessToken { get; set; } = default!;

    [Required]
    public string BackendUri { get; set; } = default!;

    public Access()
    {
        string payload = DateTime.UtcNow.AddSeconds(TokenValidSeconds).ToLongTimeString();
        this.AccessToken = payload + "." + Convert.ToBase64String(Sign(payload));
    }

    private static byte[] Sign(String payload)
    {
        using (SHA256 hash = SHA256.Create())
        {
            return hash.ComputeHash(Encoding.Unicode.GetBytes(payload + Secret));
        }
    }

    public static void Validate(string tokenStr)
    {
        string[] parts = tokenStr.Split(".", 2);
        if (parts.Length != 2)
        {
            throw new InvalidTokenException("Expected token to have 2 parts separated by '.'");
        }
        byte[] signature = Convert.FromBase64String(parts[1]);
        byte[] expectedSignature = Sign(parts[0]);
        if (!signature.SequenceEqual(expectedSignature)) // TODO: Constant-time compare, if we really care about security
        {
            throw new InvalidTokenException("Invalid token signature");
        }
        try
        {
            DateTime validUntil = DateTime.Parse(parts[0]);
            if (validUntil <= DateTime.UtcNow)
            {
                throw new InvalidTokenException("Access token expired");
            }
        }
        catch (FormatException)
        {
            throw new InvalidTokenException("Could not parse validity DateTime in access token");
        }

    }


}