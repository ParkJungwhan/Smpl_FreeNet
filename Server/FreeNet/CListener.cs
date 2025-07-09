using System.Net;
using System.Net.Sockets;

namespace FreeNet
{
    public class CListener
    {
        // 비동기 Accept를 위한 EventArgs
        private SocketAsyncEventArgs accept_args;

        //클라이언트의 접속을 처리할 소켓
        private Socket listen_socket;

        // Accept 처리의 순서를 제어하기 위한 이벤트 변수
        private AutoResetEvent flow_control_event;

        // 새로운 클라이언트가 접속했을 때 호출되는 델리게이트
        public delegate void NewclientHandler(Socket client_socket, object token);

        public NewclientHandler callback_on_newclient;

        public CListener()
        {
            this.callback_on_newclient = null;
        }

        public void start(string host, int port, int backlog)
        {
            this.listen_socket = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp);

            IPAddress address;
            if (host == "0.0.0.0")
            {
                address = IPAddress.Any;
            }
            else
            {
                address = IPAddress.Parse(host);
            }

            IPEndPoint endPoint = new IPEndPoint(address, port);

            try
            {
                // 소켓에 host 정보를 바인딩 시킨 뒤 Listen 메서드를 호출하여 대기한다
                this.listen_socket.Bind(endPoint);
                this.listen_socket.Listen(backlog);

                this.accept_args = new SocketAsyncEventArgs();
                this.accept_args.Completed += this.on_accept_completed;

                /// 클라이언트가 들어오기를 기다린다. 비동기 메서드이므로 블로킹되지 않고 바로 리턴되며 콜백 메서드를 통해서 접속 통보를 받는다.
                //this.listen_socket.AcceptAsync(this.accept_args);
                Thread listen_thread = new Thread(do_listen);
                listen_thread.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting listener: {ex.Message}");
            }
        }

        private void do_listen()
        {
            // accept 처리 제어를 위해 이벤트 객체를 생성한다.
            this.flow_control_event = new AutoResetEvent(false);

            while (true)
            {
                // socketasynceventargs 를 재사용하기 위해서 null로 만들어 준다.
                this.accept_args.AcceptSocket = null;

                bool pending = true;
                try
                {
                    // 비동기 accept를 호출하여 클라이언트의 접속을 받아들인다.
                    // 비동기 메서드이지만 동기적으로 수행이 완료될 경우도 있으니 리턴 값을
                    // 확인하여 분기처리를 해줘야 한다.
                    pending = this.listen_socket.AcceptAsync(this.accept_args);
                }
                catch (Exception e)
                {
                    // 소켓이 닫힌 경우 예외가 발생할 수 있다.
                    Console.WriteLine(e.Message);
                    continue;
                }

                // 즉시 완료(리턴 값이 false일 때)가 되면
                // 이벤트가 발생하지 않으므로 콜백 메서드를 직접 호출해줘야 한다
                // pending 상태라면 비동기 요청이 들어간 상태라는 뜻이며 콜백 메서드를 기다린다
            }
        }

        private void on_accept_completed(object? sender, SocketAsyncEventArgs e)
        {
        }
    }
}