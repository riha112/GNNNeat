[System.Serializable]
public struct Connect
{
    public ushort inID;       // Connection from
    public ushort outID;      // Connection to
    public double weight;   // Weight between connection
    public bool isDisabled;

    public ushort innov;
}
