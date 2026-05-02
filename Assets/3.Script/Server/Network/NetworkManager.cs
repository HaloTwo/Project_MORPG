using UnityEngine;

public sealed class NetworkManager : MonoBehaviour
{
    private static NetworkManager instance;

    [SerializeField] private bool useLocalSimulation = true;
    [SerializeField] private int maxPacketsPerFrame = 64;

    private readonly PacketQueue receiveQueue = new PacketQueue();

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
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        ProcessIncomingPackets();
    }

    // 나중에 TCP 서버에 연결할 자리입니다. 지금은 Mock 연결 상태로만 바꿉니다.
    public void Connect(string host = "127.0.0.1", int port = 7777)
    {
        IsConnected = true;
        Debug.Log($"[NetworkManager] Mock connect to {host}:{port}");
    }

    // 나중에 실제 소켓 연결을 닫을 자리입니다. 지금은 큐를 비우고 연결 상태를 해제합니다.
    public void Disconnect()
    {
        IsConnected = false;
        receiveQueue.Clear();
        Debug.Log("[NetworkManager] Mock disconnect");
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

        if (useLocalSimulation)
        {
            MockReceivePacket(packet);
        }
    }

    // 실제 서버에서 받은 패킷과 같은 흐름으로 테스트 패킷을 넣습니다.
    public void MockReceivePacket(PacketBase packet)
    {
        receiveQueue.Enqueue(packet);
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
}

