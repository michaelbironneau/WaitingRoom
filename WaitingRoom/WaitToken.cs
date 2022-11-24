using System.Security.Cryptography;
using System.Text;

public class WaitToken
{

    public const int SecretLength = 32;
    private static readonly String Secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    public Int64 QueuePosition;

    public WaitToken(Int64 queuePosition)
    {
        QueuePosition = queuePosition;
    }

    private String _payload
    {
        get
        {
            return QueuePosition.ToString();
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
            return new WaitToken(int.Parse(parts[0]));
        }
        catch (FormatException)
        {
            throw new InvalidTokenException("Invalid token payload, expected an integer representing the queue position");
        }

    }
}

class InvalidTokenException : Exception
{
    public InvalidTokenException(string message)
        : base(message)
    {
    }
}