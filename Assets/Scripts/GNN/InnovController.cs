using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InnovController : MonoBehaviour
{
    public static List<Innovation> innovations = new List<Innovation>();
    public static ushort innov = CONFIG.INPUT + CONFIG.OUTPUT;

    public static Innovation AddInnovation(InovType type, int conn_from, int conn_to)
    {
        Innovation? innovation = FindInnovation(type, conn_from, conn_to);
        if (innovation != null)
            return innovation.Value;

        Innovation inov = new Innovation
        {
            ID = innov++,
            type = type,
            conn_from = conn_from,
            conn_to = conn_to
        };

        innovations.Add(inov);

        return inov;
    }

    private static Innovation? FindInnovation(InovType type, int conn_from, int conn_to)
    {
        foreach(Innovation innov in innovations)
            if(innov.type == type)
                if((innov.conn_from == conn_from && innov.conn_to == conn_to)
                  || (type == InovType.NEW_NODE && innov.conn_from == conn_to && innov.conn_to == conn_from))
                return innov;
        return null;
    }

    public static void BackUp()
    {
        string type = "innovation";

        Dictionary<string, string> data = new Dictionary<string, string>();
        data.Add("data", "data");
        foreach (Innovation innov in innovations)
        {
            data["data"] = JsonUtility.ToJson(innov);
            DB.SendData(type, data);
        }
    }
}
