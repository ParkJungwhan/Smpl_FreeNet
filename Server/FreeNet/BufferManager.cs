using System.Net.Sockets;

namespace FreeNet
{
    public class BufferManager
    {
        private int m_numBytes;
        private byte[] m_buffer;
        private Stack<int> m_freeIndexPool;
        private int m_currentIndex;
        private int m_bufferSize;

        public BufferManager(int totalBytes, int bufferSize)
        {
            m_numBytes = totalBytes;
            m_currentIndex = 0;
            m_bufferSize = bufferSize;
            m_freeIndexPool = new Stack<int>();
        }

        public void InitBuffer()
        {
            //IniBuffer 메서드에서 버퍼를 초기화 : 하나의 거대한 바이트 배열을 생성

            m_buffer = new byte[m_numBytes];
        }

        public bool SetBuffer(SocketAsyncEventArgs args)
        {
            // SetBuffer 메서드에서 SocketAsyncEventArgs 객체에 버퍼를 설정
            // 하나의 버퍼를 설정 한 다음에는 index값을 증가시켜 다음 버퍼 위치를 가리킬 수 있도록 처리
            // (넓은 땅에 금을 그어서 이쪽은 내것, 저쪽은 네것 하는 식으로 나눈다고 생각하면 이해하기 쉬울듯)

            if (m_freeIndexPool.Count > 0)
            {
                //  스택에서 인덱스를 가져와서 사용
                args.SetBuffer(m_buffer, m_freeIndexPool.Pop(), m_bufferSize);
            }
            else
            {
                if (m_numBytes - m_bufferSize < m_currentIndex)
                {
                    // 더 이상 사용할 수 있는 버퍼가 없을 때
                    return false;
                }

                args.SetBuffer(m_buffer, m_currentIndex, m_bufferSize);
                m_currentIndex += m_bufferSize;
            }
            return true;
        }

        public void FreeBuffer(SocketAsyncEventArgs args)
        {
            // FreeBuffer 메서드에서는 사용하지 않는 버퍼를 반환시키기 위한일을 수행
            // 이 메서드는 지금 우리가 만들고 있는 네트워크 모듈에서는 사용하지 않을듯
            // 왜냐면 프로그램 시작시 최대동시접속수치만큼 버퍼할당 후 중간에 해제하지 않고 계속 물고있을것이기 떄문
            // SocketAsyncEventArgs만 풀링하여 재사용할 수 있도록 처리해 놓으면
            // 이 객체에 할당된 버퍼도 같이 따라가게 되기 때문

            m_freeIndexPool.Push(args.Offset);
            args.SetBuffer(null, 0, 0); // 버퍼를 해제
        }
    }
}