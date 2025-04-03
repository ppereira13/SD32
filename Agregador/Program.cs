using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Agregador
{
    class Program
    {
        static readonly int portaAgregador = 5001;
        static readonly int portaServidor = 5002;
        static string servidorIP = "127.0.0.1";

        static void Main()
        {
            TcpListener listener = new TcpListener(IPAddress.Any, portaAgregador);
            listener.Start();
            Console.WriteLine("Agregador pronto na porta " + portaAgregador);

            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();
                Thread thread = new Thread(HandleClient);
                thread.Start(client);
            }
        }

        static void HandleClient(object obj)
        {
            TcpClient client = (TcpClient)obj;
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];

            while (true)
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;

                string mensagem = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine("Recebido da WAVY: " + mensagem);

                TcpClient servidorClient = new TcpClient();
                servidorClient.Connect(servidorIP, portaServidor);
                NetworkStream servidorStream = servidorClient.GetStream();
                servidorStream.Write(buffer, 0, bytesRead);

                byte[] respostaBuffer = new byte[1024];
                int respostaBytes = servidorStream.Read(respostaBuffer, 0, respostaBuffer.Length);
                string respostaServidor = Encoding.UTF8.GetString(respostaBuffer, 0, respostaBytes);
                Console.WriteLine("Resposta do Servidor: " + respostaServidor);

                servidorClient.Close();
                byte[] resposta = Encoding.UTF8.GetBytes("Mensagem recebida e enviada ao servidor.");
                stream.Write(resposta, 0, resposta.Length);
            }
            client.Close();
        }
    }
}
