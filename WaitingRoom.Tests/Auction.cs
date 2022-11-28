using System.Collections.Concurrent;

namespace WaitingRoomTests;

public class AuctionTests
{
    [Fact]
    public void TestAuctionStates()
    {
        Auction ac = new Auction(100);
        Assert.Throws<InvalidStateException>(() => ac.Close());
        ac.Open();
        Assert.Throws<InvalidStateException>(() => ac.Open());
        ac.Close();
        Assert.Throws<InvalidStateException>(() => ac.Open());
        Assert.Throws<InvalidStateException>(() => ac.Close());
    }

    private void assertDict(ConcurrentDictionary<int, bool> d, int entry, bool expected)
    {
        bool actual;
        d.TryGetValue(entry, out actual);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TestAuction1()
    {

        Auction ac = new Auction(3);
        Assert.Throws<InvalidStateException>(() => ac.Enter(1));

        ac.Open();
        ConcurrentDictionary<int, bool> results = new ConcurrentDictionary<int, bool>();
        Thread[] threads = new Thread[6];
        for (int i = 1; i < 6; i++)
        {
            int pos = i;
            threads[i - 1] = new Thread(() => results[pos] = ac.Enter(pos));
            threads[i - 1].Start();
        }
        threads[5] = new Thread(() => { Thread.Sleep(500); ac.Close(); });
        threads[5].Start();

        foreach (Thread thread in threads)
        {
            thread.Join();
        }
        assertDict(results, 1, true);
        assertDict(results, 2, true);
        assertDict(results, 3, true);
        assertDict(results, 4, false);
        assertDict(results, 5, false);

        Assert.Throws<InvalidStateException>(() => ac.Enter(1));



    }


}