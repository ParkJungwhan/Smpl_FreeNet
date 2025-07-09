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
    }
}