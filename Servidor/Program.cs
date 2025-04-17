using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Servidor
{
    class Program
    {
        static readonly int port = 5000;

        // Mutex global para proteger secções críticas (como acesso a ficheiros no futuro)
        static Mutex mutex = new Mutex();

        static void Main(string[] args)
        {
            TcpListener listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            Console.WriteLine("Servidor iniciado. Aguardando conexões...");

            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();
                Thread thread = new Thread(new ParameterizedThreadStart(HandleClient));
                thread.Start(client);
            }
        }

        static void HandleClient(object obj)
        {
            TcpClient client = (TcpClient)obj;
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];
            int bytesRead;
            bool firstMessage = true;

            try
            {
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    mutex.WaitOne(); // entra na secção crítica

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine("Mensagem recebida: " + message);

                    if (firstMessage)
                    {
                        string response = "{\"status\": \"100 OK\", \"mensagem\": \"Conexão estabelecida.\"}";
                        byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                        stream.Write(responseBytes, 0, responseBytes.Length);
                        firstMessage = false;
                    }
                    else if (message.Contains("QUIT"))
                    {
                        string response = "{\"status\": \"400 BYE\", \"mensagem\": \"Encerrando comunicação.\"}";
                        byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                        stream.Write(responseBytes, 0, responseBytes.Length);
                        break;
                    }
                    else
                    {
                        string response = "{\"status\": \"200 OK\", \"mensagem\": \"Dados recebidos.\"}";
                        byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                        stream.Write(responseBytes, 0, responseBytes.Length);
                    }

                    mutex.ReleaseMutex(); // sai da secção crítica
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro: " + ex.Message);
                if (mutex.WaitOne(0)) mutex.ReleaseMutex(); // prevenção de deadlock
            }
            finally
            {
                stream.Close();
                client.Close();
                Console.WriteLine("Conexão encerrada.");
            }
        }
    }
}
