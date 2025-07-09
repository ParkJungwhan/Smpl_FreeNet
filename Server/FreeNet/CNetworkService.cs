using System.Diagnostics;
using System.Net.Sockets;

namespace FreeNet
{
    public class CNetworkService
    {
        // 클라이언트의 접속을 받아들이기 위한 객체
        //CListener client_listener;

        // 메시지 수신, 전송시 필요한 객체
        private SocketAsyncEventArgsPool receive_event_args_pool;

        private SocketAsyncEventArgsPool send_event_args_pool;

        // 메시지 수신, 전송시 닷넷 비동기 소켓에서 사용할 버퍼를 관리하는 객체
        private BufferManager buffer_manager;

        //클라이언트의 접속이 이루어졌을 때 호출되는 델리게이트
        public delegate void SeesionHandler(CUserToken token);

        public SeesionHandler session_created_callback { get; set; }

        private readonly int max_connections;

        public CNetworkService()
        {
            receive_event_args_pool = new SocketAsyncEventArgsPool(max_connections);
            send_event_args_pool = new SocketAsyncEventArgsPool(max_connections);

            Init_Connections();
        }

        private void Init_Connections()
        {
            for (int i = 0; i < max_connections; i++)
            {
                CUserToken token = new CUserToken();

                //recv pool
                {
                    var arg = new SocketAsyncEventArgs();
                    //arg.Completed += on_receive_completed;
                    arg.UserToken = token;

                    this.buffer_manager.SetBuffer(arg);
                    // 이 부분이 바로 SocketAsyncEventArgs 객체에 버퍼를 설정하는 코드
                    // SetBuffer 내부를 보면 범위를 잡아서 arg.SetBuffer를 호출하게 되어 있음
                    // m_currentIndex += m_bufferSize; 이런식으로 버퍼 크기만큼 인덱스 값을 늘려서 서로 겹치지 않게 해주고 있는것.
                    // => 하나의 거대한 버퍼를 만들고 버퍼 사이즈만큼 범위를 잡아 SocketAsyncEventArgs에 하나씩 할당해주는것
                    // 이런구현방식으로 SocketAsyncEventArgs 객체와 버퍼 메모리를 풀링하여 사용함
                    this.receive_event_args_pool.Push(arg);
                }

                //send pool
                {
                    var arg = new SocketAsyncEventArgs();
                    //arg.Completed += on_send_completed;
                    arg.UserToken = token;

                    this.buffer_manager.SetBuffer(arg);
                    this.send_event_args_pool.Push(arg);
                }
            }
        }

        public void listen(string host, int port, int backlog)
        {
            // 클라이언트 접속을 받아들이기 위한 리스너를 생성한다
            CListener client_listener = new CListener();
            client_listener.callback_on_newclient = this.on_new_client;
            // 리스너를 시작한다
            client_listener.start(host, port, backlog);
        }

        private void on_new_client(Socket client_socket, object token)
        {
            //(1)

            // 폴에서 하나 꺼내와 사용한다.
            var recv_args = this.receive_event_args_pool.Pop(); //(2)
            var send_args = this.send_event_args_pool.Pop();

            // SocketAsyncEventArgs를 생성할 때 만들어 두었던 CUserToken을 꺼내와서
            // 콜백 메서드의 파라미터로 넘겨준다
            if (this.session_created_callback != null)
            {
                CUserToken cUserToken = recv_args.UserToken as CUserToken;
                Debug.Assert(cUserToken != null);
                this.session_created_callback(cUserToken);
            }

            // 클라이언트로 부터 데이터를 수신할 준비를 한다
            begin_receive(client_socket, recv_args, send_args);
        }

        private void begin_receive(Socket client_socket, SocketAsyncEventArgs recv_args, SocketAsyncEventArgs send_args)
        {
        }
    }
}