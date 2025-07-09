using System.Net.Sockets;

namespace FreeNet
{
    public class SocketAsyncEventArgsPool
    {
        private Stack<SocketAsyncEventArgs> m_pool;

        public SocketAsyncEventArgsPool(int capacity)
        {
            m_pool = new Stack<SocketAsyncEventArgs>(capacity);
        }

        public void Push(SocketAsyncEventArgs item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("Items added to a SocketAsyncEventArgsPool cannot be null");
            }
            lock (m_pool)
            {
                m_pool.Push(item);
            }
        }

        public SocketAsyncEventArgs Pop()
        {
            lock (m_pool)
            {
                //if (m_pool.Count == 0)
                //{
                //    throw new InvalidOperationException("No items available in the pool");
                //}
                return m_pool.Pop();
            }
        }

        public int Count
        {
            get { return m_pool.Count; }
        }
    }
}