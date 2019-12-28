public enum InovType
{
    CONNECTION, NEW_NODE
}

[System.Serializable]
public struct Innovation
{
    public ushort ID;
    public InovType type;

    public int conn_from;
    public int conn_to;
}
