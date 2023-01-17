using System.Security.Cryptography;
using System.Text;

public class WaitToken
{

    public const int SecretLength = 32;
    private static readonly String Secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    public static int DefaultExpirySeconds = 300;

    public Int64 QueuePosition;
    public DateTimeOffset Expiry;

    public WaitToken(Int64 queuePosition)
    {
        QueuePosition = queuePosition;
        Expiry = DateTimeOffset.UtcNow.AddSeconds(DefaultExpirySeconds);
    }

    public WaitToken(Int64 queuePosition, DateTimeOffset expiry)
    {
        QueuePosition = queuePosition;
        Expiry = expiry;
    }

    private String _payload
    {
        get
        {
            return QueuePosition.ToString() + "." + Expiry.ToUnixTimeSeconds().ToString();
        }
    }

    private static byte[] Sign(String payload)
    {
        using (SHA256 hash = SHA256.Create())
        {
            return hash.ComputeHash(Encoding.Unicode.GetBytes(payload + Secret));
        }
    }


    public override String ToString()
    {
        String payload = _payload;
        return _payload + "." + Convert.ToBase64String(Sign(_payload));
    }

    public static WaitToken Parse(String tokenStr)
    {
        string[] parts = tokenStr.Split(".", 3);
        if (parts.Length != 3)
        {
            throw new InvalidTokenException("Expected token to have 3 parts separated by '.'");
        }
        byte[] signature = Convert.FromBase64String(parts[2]);
        byte[] expectedSignature = Sign(parts[0] + "." + parts[1]);
        if (!signature.SequenceEqual(expectedSignature)) // TODO: Constant-time compare, if we really care about security
        {
            throw new InvalidTokenException("Invalid token signature");
        }
        try
        {
            long expiry = long.Parse(parts[1]);
            DateTimeOffset exp = DateTimeOffset.FromUnixTimeSeconds(expiry);
            if (exp <= DateTimeOffset.UtcNow)
            {
                throw new InvalidTokenException("Token expired");
            }
            return new WaitToken(int.Parse(parts[0]), DateTimeOffset.FromUnixTimeSeconds(expiry));
        }
        catch (FormatException)
        {
            throw new InvalidTokenException("Invalid token payload, expected an integer representing the queue position");
        }

    }
}