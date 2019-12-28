using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

[System.Serializable]
class SaveObject
{
    public ushort GEN;
    public Innovation[] innovations;
    public GNNNet[] networks;
    public ushort innovID;
}

class SaveLoadManager
{
    const string _DIR = @".\Backups\";

    public static void Save(ushort GEN, List<GNNNet> nets)
    {
        SaveObject so = new SaveObject() {
            GEN = GEN,
            innovations = InnovController.innovations.ToArray(),
            networks = nets.ToArray(),
            innovID = InnovController.innov,
        };

        try
        {
            Stream fileStram = File.Create(_DIR + "ra18014_savefile_new.bin");
            BinaryFormatter serializer = new BinaryFormatter();

            serializer.Serialize(fileStram, so);
            fileStram.Close();
        }
        catch (IOException e)
        {
            Debug.Log("Error:" + e.ToString());
        }

    }

    public static SaveObject Load()
    {
        string fileName = _DIR + "ra18014_savefile_new.bin";

        if (File.Exists(fileName))
        {
            try
            {
                Stream fileStram = File.OpenRead(fileName);
                BinaryFormatter deserializer = new BinaryFormatter();
                // Deseriealize datus no faila parversot tos par sarakstu ar objektiem
                SaveObject so = (SaveObject)deserializer.Deserialize(fileStram);
                fileStram.Close();
                return so;
            }
            catch (IOException e)
            {
                Debug.Log("Error:" + e.ToString());
            }
        }

        return null;
    }
}
