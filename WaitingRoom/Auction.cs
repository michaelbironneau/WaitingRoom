public class Auction
{
    public int Size = 0;
    public AuctionStatus Status = AuctionStatus.New;

    public int TimeoutSeconds = 30;

    private const int queueLockTimeoutMs = 1000;
    private const int queuePollMs = 100;

    private SortedSet<long> _queue = new SortedSet<long>();
    private ReaderWriterLock _queueLock = new ReaderWriterLock();

    public Auction(int size)
    {
        Size = size;
    }

    ///<summary>
    /// Open the auction to new queuers. 
    /// Opening an already-open or a closed auction will result in an exception.
    ///</summary>
    public void Open()
    {
        if (Status != AuctionStatus.New)
        {
            throw new InvalidStateException("Auction must be in New state to be opened");
        }

        Status = AuctionStatus.Open;

    }


    ///<summary>
    ///  Close the auction to new queuers and accept the Size best queuers.
    ///  Closing a non-open or already closed auction will result in an exception.
    ///</summary>
    public void Close()
    {
        if (Status != AuctionStatus.Open)
        {
            throw new InvalidStateException("Auction must be in Open state to be closed");
        }

        Status = AuctionStatus.Closed;

    }



    private bool addToQueue(long queuePosition)
    {
        try
        {
            _queueLock.AcquireReaderLock(queueLockTimeoutMs);
            try
            {
                if (queuePosition >= _queue.Max && _queue.Count >= Size)
                {
                    // queuer is guaranteed not to be chosen in this auction
                    return false;
                }

                LockCookie lc = _queueLock.UpgradeToWriterLock(queueLockTimeoutMs);
                try
                {
                    if (_queue.Count < Size)
                    {
                        // add queuer
                        _queue.Add(queuePosition);
                        return true;
                    }
                    else if (queuePosition < _queue.Max && _queue.Count >= Size)
                    {
                        // remove previous worst entry and add us
                        _queue.Remove(_queue.Max);
                        _queue.Add(queuePosition);
                        return true;
                    }
                    else
                    {
                        return false; //unreachable
                    }
                }
                catch (Exception)
                {
                    // TODO - log and/or something more elegant
                }
                finally
                {
                    _queueLock.DowngradeFromWriterLock(ref lc);
                }

            }
            catch (Exception)
            {
                return false; // TODO - log and/or something more elegant
            }
            finally
            {
                _queueLock.ReleaseReaderLock();
            }

        }
        catch (ApplicationException)
        {
            // TODO - log and/or do something more elegant
            return false;
        }
        return false; // should be unreachable
    }

    ///<summary>
    /// Enter the auction as a queuer of given ID. Blocking until the enterer is rejected or auction closes. 
    ///</summary>
    ///<returns>
    /// True if the queuer is chosen, False otherwise. 
    ///</returns>
    public bool Enter(long queuePosition)
    {
        if (Status != AuctionStatus.Open)
        {
            throw new InvalidStateException("Cannot enter Auction if is it not Open!");
        }

        int timeoutCounterMs = 0;

        if (!addToQueue(queuePosition)) return false;

        while (true)
        {
            Thread.Sleep(queuePollMs);
            timeoutCounterMs += 100;

            if (timeoutCounterMs >= TimeoutSeconds * 1000)
            {
                return false;
            }

            try
            {
                _queueLock.AcquireReaderLock(queueLockTimeoutMs);
                try
                {
                    if (queuePosition > _queue.Max)
                    {
                        return false;
                    }
                }
                finally
                {
                    _queueLock.ReleaseReaderLock();
                }

            }
            catch (ApplicationException)
            {
                // lock has timed out - 
            }




            if (Status == AuctionStatus.Closed)
            {
                // If we're still here, we were picked
                return true;
            }


        }

    }



}

public enum AuctionStatus
{
    New,
    Open,
    Closed
}

public class InvalidStateException : Exception
{
    public InvalidStateException(string message)
        : base(message)
    {
    }
}