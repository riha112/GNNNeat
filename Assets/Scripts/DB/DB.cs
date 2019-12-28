using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Text;

public class DB
{
    private readonly static string urlAddress = "http://localhost/AI/backup.php";

    public static void SendData(string type, Dictionary<string, string> data)
    {
        using (WebClient client = new WebClient())
        {
            NameValueCollection postData = new NameValueCollection();
            postData.Add("type", type);

            foreach (var par in data)
                postData.Add(par.Key, par.Value);

            string pagesource = Encoding.UTF8.GetString(client.UploadValues(urlAddress, postData));
        }
    }
}
