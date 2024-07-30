namespace Kerbalism.Comms
{
    public interface IConnectionInfo
    {
        bool HasActiveAntenna { get; }

        bool Linked { get; }

        double Ec { get; }

        double EcIdle { get; }

        double DataRate { get; }

        double Strength { get; }
    }
}