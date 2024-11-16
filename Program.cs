using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MySocket
{
    class Program
    {
        static async Task Main(string[] args)
        {
            HttpListener httpListener = new HttpListener();
            httpListener.Prefixes.Add("http://localhost:5000/");
            httpListener.Start();
            Console.WriteLine("WebSocketサーバーが起動しました...");

            while (true)
            {
                HttpListenerContext httpContext = await httpListener.GetContextAsync();
                if (httpContext.Request.IsWebSocketRequest)
                {
                    ProcessRequest(httpContext);
                }
                else
                {
                    httpContext.Response.StatusCode = 400;
                    httpContext.Response.Close();
                }
            }
        }

        static async void ProcessRequest(HttpListenerContext httpContext)
        {
            WebSocketContext webSocketContext = await httpContext.AcceptWebSocketAsync(subProtocol: null);
            WebSocket webSocket = webSocketContext.WebSocket;

            // 時刻を定期的に送信するタスクを開始
            var sendTimeTask = SendTimeAsync(webSocket);

            byte[] buffer = new byte[1024];

            while (webSocket.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                string receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Console.WriteLine("受信メッセージ: " + receivedMessage);

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    // 送信
                    byte[] responseMessage = Encoding.UTF8.GetBytes("I'm Server. " + "メッセージを受信しました: " + receivedMessage);
                    await webSocket.SendAsync(new ArraySegment<byte>(responseMessage), WebSocketMessageType.Text, endOfMessage: true, CancellationToken.None);
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "閉じる", CancellationToken.None);
                }

                await sendTimeTask;
            }
        }

        static async Task SendTimeAsync(WebSocket webSocket)
        {
            while (webSocket.State == WebSocketState.Open)
            {
                string currentTime = DateTime.Now.ToString("HH:mm:ss");
                byte[] timeMessage = Encoding.UTF8.GetBytes(currentTime);
                await webSocket.SendAsync(new ArraySegment<byte>(timeMessage), WebSocketMessageType.Text, endOfMessage: true, CancellationToken.None);
                await Task.Delay(5000); // 5秒ごとに時刻を送信
            }
        }
    }
}
