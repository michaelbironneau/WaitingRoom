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
}