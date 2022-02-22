namespace Experiment.Process
{
    public class Constants
    {
        public const string SERVICEID = "Dolphin";
        public const string LOCALCLUSTERID = "Local";
        public const string EC2CLUSTERID = "ec22Dolphin";

        public const string STREAMPROVIDER = "SMSProvider";

        //ec2
        public const string ACCESSKEY = "AKIAJJJ2TPHXMULDWG5Q";
        public const string SECRETKEY = "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";
        public const string TABLENAME = "dolphinmembershiptable";
        public const string SERVICE = "eu-central-1";

        public const double MAXSPEED = 22;//max-80km/h

        public const double QUERYSIZE = 500;//real-1000x1000

        public const double GAUSSSTD = 1;
        public const double GAUSSSPCE = 1000;
        internal static double UPDATEFREQUENCY = 3;
    }
}
