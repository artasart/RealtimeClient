using Cysharp.Threading.Tasks;
using Framework.Network;
using Newtonsoft.Json.Linq;
using Protocol;
using System.Net;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class GameClientManager : MonoBehaviour
{
    #region Singleton

    public static GameClientManager Instance
    {
        get
        {
            if (instance != null)
            {
                return instance;
            }

            instance = FindObjectOfType<GameClientManager>();
            return instance;
        }
    }
    private static GameClientManager instance;

    #endregion

    public Client Client { get; private set; }

    private void Start()
    {
        GameManager.UI.StackPanel<Panel_Network>();
    }

    public async Task<IPEndPoint> GetAddress()
    {
        using UnityWebRequest webRequest = UnityWebRequest.Get("http://20.200.230.139:32000/");
        _ = await webRequest.SendWebRequest();
        if (webRequest.result == UnityWebRequest.Result.Success)
        {
            string response = webRequest.downloadHandler.text;
            JObject jsonResponse = JObject.Parse(response);

            string address = jsonResponse["status"]["address"].ToString();

            int defaultPort = 0;
            JArray ports = (JArray)jsonResponse["status"]["ports"];
            foreach (JObject port in ports)
            {
                if (port["name"].ToString() == "default")
                {
                    defaultPort = port["port"].ToObject<int>();
                    break;
                }
            }

            return defaultPort != 0 ? new(IPAddress.Parse(address), defaultPort) : null;
        }
        else
        {
            return null;
        }
    }

    public async void Connect( string connectionId )
    {
        if (Client != null)
        {
            return;
        }

        IPEndPoint endPoint = await GetAddress();
        if (endPoint == null)
        {
            Debug.Log("GetAddress Fail!");
            return;
        }

        Client = (Client)ConnectionManager.GetConnection<Client>();

        bool success = await ConnectionManager.Connect(endPoint, Client);
        if (success)
        {
            Client.ClientId = connectionId;

            C_ENTER enter = new()
            {
                ClientId = "Main" + connectionId
            };
            Client.Send(PacketManager.MakeSendBuffer(enter));

            GameManager.UI.FetchPanel<Panel_Network>().isConnect = false;
            GameManager.UI.PopPanel();
        }
    }

    public void Disconnect()
    {
        if (Client == null)
        {
            return;
        }

        Client.Send(PacketManager.MakeSendBuffer(new C_LEAVE()));
        Client = null;
    }
}
