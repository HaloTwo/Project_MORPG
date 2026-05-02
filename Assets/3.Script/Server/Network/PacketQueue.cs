using System.Collections.Generic;

public sealed class PacketQueue
{
    private readonly Queue<PacketBase> packets = new Queue<PacketBase>();
    private readonly object syncRoot = new object();

    // 네트워크 스레드나 로컬 시뮬레이션에서 받은 패킷을 큐에 넣습니다.
    public void Enqueue(PacketBase packet)
    {
        if (packet == null)
        {
            return;
        }

        lock (syncRoot)
        {
            packets.Enqueue(packet);
        }
    }

    // Unity 메인 스레드에서 처리할 패킷 하나를 꺼냅니다.
    public bool TryDequeue(out PacketBase packet)
    {
        lock (syncRoot)
        {
            if (packets.Count > 0)
            {
                packet = packets.Dequeue();
                return true;
            }
        }

        packet = null;
        return false;
    }

    // 연결 종료나 씬 변경 시 남아 있는 패킷을 모두 비웁니다.
    public void Clear()
    {
        lock (syncRoot)
        {
            packets.Clear();
        }
    }
}

