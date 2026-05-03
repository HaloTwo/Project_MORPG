using UnityEngine;

public sealed class NetworkManager : MonoBehaviour
{
    private static NetworkManager instance;

    [SerializeField] private string serverHost = "127.0.0.1";
    [SerializeField] private int serverPort = 7777;
    [SerializeField] private int maxPacketsPerFrame = 64;

    private readonly PacketQueue receiveQueue = new PacketQueue();
    private ServerTextProtocol textProtocol;
    private TcpServerConnection tcpConnection;
    private bool serverDisconnectedPopupRequested;
    private bool serverDisconnectedPopupVisible;
    private bool applicationQuitting;

    public PacketDispatcher Dispatcher { get; private set; } = new PacketDispatcher();
    public bool IsConnected { get; private set; }

    public static NetworkManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<NetworkManager>();
            }

            if (instance == null)
            {
                GameObject go = new GameObject("NetworkManager");
                instance = go.AddComponent<NetworkManager>();
            }

            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        textProtocol = new ServerTextProtocol();
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        ProcessIncomingPackets();
        ShowServerDisconnectedPopupIfNeeded();
    }

    public void Connect(string host = "127.0.0.1", int port = 7777)
    {
        string targetHost = string.IsNullOrWhiteSpace(host) ? serverHost : host;
        int targetPort = port <= 0 ? serverPort : port;
        tcpConnection?.Dispose();
        tcpConnection = new TcpServerConnection(textProtocol, receiveQueue);
        tcpConnection.Disconnected += HandleTcpDisconnected;
        IsConnected = tcpConnection.Connect(targetHost, targetPort);
        Debug.Log($"[NetworkManager] TCP connect to {targetHost}:{targetPort} success={IsConnected}");

        if (!IsConnected)
        {
            RequestServerDisconnectedPopup();
        }
    }

    public void Disconnect()
    {
        IsConnected = false;
        receiveQueue.Clear();
        if (tcpConnection != null)
        {
            tcpConnection.Disconnected -= HandleTcpDisconnected;
        }

        tcpConnection?.Dispose();
        tcpConnection = null;
        Debug.Log("[NetworkManager] Disconnect");
    }

    // 패킷을 보내는 입구입니다. 나중에 TcpClient 또는 Socket 송신 코드로 교체하면 됩니다.
    public void SendPacket(PacketBase packet)
    {
        if (packet == null)
        {
            return;
        }

        if (!IsConnected)
        {
            Connect();
        }

        Debug.Log($"[NetworkManager] Send {packet.Id}");

        if (!IsConnected || tcpConnection == null)
        {
            RequestServerDisconnectedPopup();
            return;
        }

        if (!tcpConnection.Send(packet))
        {
            IsConnected = false;
            RequestServerDisconnectedPopup();
        }
    }

    // 큐에 쌓인 패킷을 Unity 메인 스레드에서 하나씩 처리합니다.
    private void ProcessIncomingPackets()
    {
        int processed = 0;
        while (processed < maxPacketsPerFrame && receiveQueue.TryDequeue(out PacketBase packet))
        {
            Dispatcher.Dispatch(packet);
            processed++;
        }
    }

    /// TCP 수신 스레드에서 서버 끊김을 알려오면 메인 스레드 팝업을 예약합니다.
    private void HandleTcpDisconnected()
    {
        IsConnected = false;
        RequestServerDisconnectedPopup();
    }

    /// 서버 연결 실패나 끊김을 한 번만 UI로 보여주기 위해 플래그를 세웁니다.
    private void RequestServerDisconnectedPopup()
    {
        if (applicationQuitting)
        {
            return;
        }

        serverDisconnectedPopupRequested = true;
    }

    /// Unity UI는 메인 스레드에서만 만들 수 있으므로 Update에서 실제 팝업을 생성합니다.
    private void ShowServerDisconnectedPopupIfNeeded()
    {
        if (!serverDisconnectedPopupRequested || serverDisconnectedPopupVisible)
        {
            return;
        }

        serverDisconnectedPopupVisible = true;
        RuntimeUiFactory.ShowServerDisconnectedDialog(QuitGame);
    }

    /// 확인 버튼을 누르면 플레이 중인 게임을 종료합니다.
    private void QuitGame()
    {
        applicationQuitting = true;
        Disconnect();
        RuntimeUiFactory.QuitGame();
    }

    private void OnApplicationQuit()
    {
        applicationQuitting = true;
        Disconnect();
    }
}

