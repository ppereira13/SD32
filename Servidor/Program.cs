using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Servidor
{
    class Program
    {
        static readonly int portaServidor = 5002;
        static readonly object lockObj = new object();

        static void Main()
        {
            TcpListener listener = new TcpListener(IPAddress.Any, portaServidor);
            listener.Start();
            Console.WriteLine("Servidor pronto na porta " + portaServidor);

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
                Console.WriteLine("Dados recebidos: " + mensagem);

                lock (lockObj)
                {
                    File.AppendAllText("dados_servidor.txt", mensagem + Environment.NewLine);
                }

                byte[] resposta = Encoding.UTF8.GetBytes("Dados processados com sucesso.");
                stream.Write(resposta, 0, resposta.Length);
            }
            client.Close();
        }
    }
}
