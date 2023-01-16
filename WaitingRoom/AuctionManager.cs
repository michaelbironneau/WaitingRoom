public sealed class AuctionManager
{
    public static int AuctionSize = 10;
    public static int AuctionIntervalSeconds = 5;

    public static string BackendUri = "/backend-resource";


    private static AuctionManager? instance;
    private static readonly object _instanceLock = new object();

    private static readonly object _auctionLock = new object();

    private Auction auction;
    private bool closed = false;

    private long counter = 0;

    private static void Initialize()
    {
        lock (_instanceLock)
        {
            if (instance == null)
            {
                instance = new AuctionManager();
            }
        }
    }

    private AuctionManager()
    {
        this.auction = new Auction(AuctionSize);
    }

    public static object EnterNew()
    {
        if (instance == null)
        {
            Initialize();
        }
        Int64 pos = Interlocked.Increment(ref instance.counter);
        return EnterAt(pos);
    }

    public static object EnterAt(long pos)
    {
        if (instance == null)
        {
            Initialize();
        }
        bool success = instance.auction.Enter(pos);
        if (success)
        {
            Access token = new Access();
            token.BackendUri = BackendUri;
            token.AccessToken = "aaa-bbb-ccc"; // TODO: Implement this
            return token;
        }
        else
        {
            return new WaitToken(pos);
        }
    }

    public static void Start()
    {
        if (instance == null)
        {
            Initialize();
        }

        instance.auction.Open();
        while (true)
        {
            DateTime t1 = DateTime.UtcNow;
            lock (_auctionLock)
            {
                if (instance.closed)
                {
                    return;
                }
            }
            DateTime t2 = DateTime.UtcNow;
            Thread.Sleep(AuctionIntervalSeconds * 1000 - t2.Subtract(t1).Milliseconds);

            // close and swap auction
            instance.auction.Close();
            lock (_auctionLock)
            {
                // TODO: Do we need to do anything to clean up?
                instance.auction = new Auction(AuctionSize);
            }
        }

    }

    public static void Stop()
    {
        if (instance == null)
        {
            return;
        }

        instance.auction.Close();
        lock (_auctionLock)
        {
            instance.closed = true;
        }
    }





}