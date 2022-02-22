using System;

namespace Dolphin.Utilities
{
    [Serializable]
    public class TooShortIntervalException : Exception
    {
        public TooShortIntervalException()
        { 
            throw new Exception("Interval time is too short. Please increse -iti!"); 
        }
    }

    [Serializable]
    public class SnapshotVersionException : Exception
    {
        public SnapshotVersionException()
        {
            throw new Exception("Unmatched Snapshot Version!");
        }
    }

    [Serializable]
    public class SubscribeUpdateException : Exception
    {
        public SubscribeUpdateException(Exception e)
        {
            throw new Exception("Error in SubscribeUpdate! "+ e);
        }
    }

    [Serializable]
    public class SnapshotBufferedDataUpdateException : Exception
    {
        public SnapshotBufferedDataUpdateException(Exception e)
        {
            throw new Exception("Error in SnapshotBufferedDataUpdate! " + e);
        }
    }

    [Serializable]
    public class MovingActorException : Exception
    {
        public MovingActorException(Exception e)
        {
            throw new Exception("Error in MovingActor! " + e);
        }
    }

    [Serializable]
    public class ReadTjyException : Exception
    {
        public ReadTjyException(Exception e)
        {
            throw new Exception("ReadTjy Error! " + e);
        }
    }

    [Serializable]
    public class ExecuteMoveException : Exception
    {
        public ExecuteMoveException(Exception e)
        {
            throw new Exception("ExecuteMove Error! " + e);
        }
    }

}
