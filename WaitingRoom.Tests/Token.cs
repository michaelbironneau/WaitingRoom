namespace WaitingRoomTests;

public class TokenTests
{
    [Fact]
    public void TestParsingValidation()
    {
        WaitToken t = new WaitToken(1);
        Assert.Equal(1, t.QueuePosition);
        String str = t.ToString();
        WaitToken t2 = WaitToken.Parse(str);
        Assert.Equal(t2.QueuePosition, t.QueuePosition);
    }

    [Fact]
    public void TestParsingInvalid()
    {
        String invalid = "1.xxxxxxxx";
        Assert.Throws<InvalidTokenException>(() => WaitToken.Parse(invalid));
    }

    [Fact]
    public void TestParsingExpired()
    {
        WaitToken t = new WaitToken(1, DateTimeOffset.UtcNow.AddSeconds(-1));
        String str = t.ToString();
        Assert.Throws<InvalidTokenException>(() => WaitToken.Parse(str));
    }

    [Fact]
    public void TestAccessTokenValid()
    {
        Access t = new Access();
        Access.Validate(t.AccessToken); // shouldn't throw
    }

    [Fact]
    public void TestInvalidAccessToken()
    {
        int ts = Access.TokenValidSeconds;
        Access.TokenValidSeconds = -1;
        Access t = new Access(); // t is valid "negative" amount of seconds, i.e. invalid
        Access.TokenValidSeconds = ts; // put it back
        Assert.Throws<InvalidTokenException>(() => Access.Validate(t.AccessToken));
    }
}