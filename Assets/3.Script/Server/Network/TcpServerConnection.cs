using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public sealed class TcpServerConnection : IDisposable
{
    private readonly ServerTextProtocol protocol;
    private readonly PacketQueue receiveQueue;
    private TcpClient client;
    private StreamWriter writer;
    private Thread receiveThread;
    private volatile bool running;

    public bool IsConnected => client != null && client.Connected;
    public event Action Disconnected;

    public TcpServerConnection(ServerTextProtocol protocol, PacketQueue receiveQueue)
    {
        this.protocol = protocol;
        this.receiveQueue = receiveQueue;
    }

    /// C++ TCP 서버에 접속하고 서버 응답을 받는 백그라운드 스레드를 시작합니다.
    public bool Connect(string host, int port)
    {
        try
        {
            client = new TcpClient();
            client.Connect(host, port);
            writer = new StreamWriter(client.GetStream(), new UTF8Encoding(false))
            {
                AutoFlush = true
            };

            running = true;
            receiveThread = new Thread(ReceiveLoop)
            {
                IsBackground = true
            };
            receiveThread.Start();
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError($"[TcpServerConnection] Connect failed: {exception.Message}");
            Dispose();
            return false;
        }
    }

    /// Unity 패킷을 서버 텍스트 명령어로 바꿔 전송합니다.
    public bool Send(PacketBase packet)
    {
        if (!IsConnected || writer == null)
        {
            Debug.LogWarning("[TcpServerConnection] Not connected.");
            return false;
        }

        string line = protocol.Encode(packet);
        if (string.IsNullOrEmpty(line))
        {
            Debug.LogWarning($"[TcpServerConnection] Unsupported packet: {packet.Id}");
            return true;
        }

        try
        {
            writer.WriteLine(line);
            Debug.Log($"[TcpServerConnection] Send: {line}");
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[TcpServerConnection] Send failed: {exception.Message}");
            return false;
        }
    }

    public void Dispose()
    {
        running = false;
        writer?.Dispose();
        writer = null;
        client?.Close();
        client = null;
    }

    private void ReceiveLoop()
    {
        bool disconnectedByServer = false;
        try
        {
            using StreamReader reader = new StreamReader(client.GetStream(), Encoding.UTF8);
            while (running)
            {
                string line = reader.ReadLine();
                if (line == null)
                {
                    disconnectedByServer = true;
                    break;
                }

                Debug.Log($"[TcpServerConnection] Receive: {line}");
                foreach (PacketBase packet in protocol.DecodeLine(line))
                {
                    receiveQueue.Enqueue(packet);
                }
            }
        }
        catch (Exception exception)
        {
            if (running)
            {
                Debug.LogWarning($"[TcpServerConnection] Receive stopped: {exception.Message}");
                disconnectedByServer = true;
            }
        }
        finally
        {
            if (running && disconnectedByServer)
            {
                Disconnected?.Invoke();
            }
        }
    }
}
