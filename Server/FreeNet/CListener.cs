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
            // accept 처리 제어를 위해 이벤트 객체를 생성한다.(1)
            this.flow_control_event = new AutoResetEvent(false);
            // 스레드의 시작 부분에는 이벤트 객체를 생성하는 코드가 있음.
            // 하나의 접속 처리가 완료된 이후 그다음 접속 처리를 수행하기 위해서 스레드의 흐름을 제어할 필요가 있는데,
            // 이때 사용되는 이벤트 객체임

            //루프를 돌며 클라이언트의 접속을 받아들임
            while (true)//(2)
            {
                // socketasynceventargs 를 재사용하기 위해서 null로 만들어 준다.
                this.accept_args.AcceptSocket = null;

                bool pending = true;
                try
                {
                    // 비동기 accept를 호출하여 클라이언트의 접속을 받아들인다.
                    // 비동기 메서드이지만 동기적으로 수행이 완료될 경우도 있으니 리턴 값을
                    // 확인하여 분기처리를 해줘야 한다.
                    pending = this.listen_socket.AcceptAsync(this.accept_args);//(3)
                    // AcceptAsync의 리턴값에 따라 즉시 완료 처리를 할  것인지 통보가 오기를 기다릴 것인지 구분해줘야함.
                    // Accept 처리가 동기적으로 수행이 완료된 경우에는 콜백 메서드가 호출되지 않고 false를 리턴함
                    // 따라서 이 경우에는 완료 처리를 담당하는 메서드를 직접 호출해줘야한다.
                    // 그 외의 경우(true 리턴) 에는 닷넷 프레임워크에서 콜백 메서드를 호출해주기 때문에 직접 호출필요 x
                    // 이 메서드뿐만 아니라 다른 비동기 메서드를 호출할 때는 즉시 완료될 경우와 그렇지 않을 경우를 구분해서 처리해줘야함
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
                if (!pending)
                {
                    // 즉시 완료된 경우
                    this.on_accept_completed(null, this.accept_args);
                }
                else
                {
                    // 클라이언트 접속 처리가 완료되면 이벤트 객체의 신호를 전달받아 다시 루프를 수행한다.
                    this.flow_control_event.WaitOne();//(4)

                    //AcceptAsync를 통해서 하나의 클라이언트가 접속되기를 기다린 후 이벤트 객체를 이용하여 스레드를 잠시 대기상태로 둔다.
                    // 이벤트 객체는 두 가지가 있음
                    // AutoResetEvent는 한번 set이 도니 이후 자동으로 Reset 상태로 만들어줌
                    // ManualREsetEvent는 직접 Reset 메서드를 호출하지 않는다면 계속 Set 상태로 남아있음.
                    // 여기서는 AutoResetEvent를 사용
                }
            }
        }

        private void on_accept_completed(object? sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                // 새로 생긴 소켓을 보관해 놓은 뒤
                Socket client_socket = e.AcceptSocket;

                // 다음 연결을 받아들임
                this.flow_control_event.Set();
                // 이벤트 객체를 Set 상태로 만들어서 대기 중인 스레드를 깨운다.

                // 이 클래스에서는 accept까지의 역할만 수행하고 클라이언트의 접속 이후의 처리는
                // 외부로 넘기기 위해서 콜백 메서드를 호출해 주도록 한다.
                // 그 이유는 소켓 처리부와 콘텐츠 구현부를 분리하기 위해서
                // 콘텐츠 구현 부분은 자주 바뀔 가능성이 있지만,
                // 소켓 Accept 부분은 상대적으로 변경이 적은 부분이기 때문에 양쪽을 분리시켜 주는것이 좋다.
                // 또한 클래스 설계 방침에 따라 Listen에 고나련된 코드만 존재하도록
                // 하기위한 이유도 있다.
                if (this.callback_on_newclient != null)
                {
                    // 콜백 메서드가 설정되어 있다면 호출한다.
                    this.callback_on_newclient(client_socket, e.UserToken);
                }
                return;
            }
            else
            {
                // 접속 실패 시 예외 처리
                Console.WriteLine($"Accept failed: {e.SocketError}");
                Console.WriteLine("Failed to accept client");
            }

            // 다음 연결을 받아들인다.
            this.flow_control_event.Set();
        }
    }
}