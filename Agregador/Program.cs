using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Agregador
{
    class Program
    {
        // Porta para as conexões das WAVYs
        static readonly int portWavy = 5001;
        // Informações de conexão do servidor (supondo localhost)
        static readonly int serverPort = 5000;
        static string serverIP = "127.0.0.1";

        static void Main(string[] args)
        {
            TcpListener listener = new TcpListener(IPAddress.Any, portWavy);
            listener.Start();
            Console.WriteLine("Agregador iniciado. Aguardando conexões das WAVYs...");

            while (true)
            {
                TcpClient wavyClient = listener.AcceptTcpClient();
                Thread thread = new Thread(new ParameterizedThreadStart(HandleWavy));
                thread.Start(wavyClient);
            }
        }

        static void HandleWavy(object obj)
        {
            TcpClient wavyClient = (TcpClient)obj;
            NetworkStream wavyStream = wavyClient.GetStream();
            byte[] buffer = new byte[1024];
            int bytesRead;

            // Conecta-se ao servidor para encaminhar as mensagens
            TcpClient serverClient = new TcpClient();
            serverClient.Connect(serverIP, serverPort);
            NetworkStream serverStream = serverClient.GetStream();

            try
            {
                while ((bytesRead = wavyStream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    string messageFromWavy = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine("Recebido da WAVY: " + messageFromWavy);

                    // Aqui poderia ser implementado o processamento dos CSV e atualização do estado da WAVY

                    // Encaminha a mensagem para o servidor
                    byte[] messageBytes = Encoding.UTF8.GetBytes(messageFromWavy);
                    serverStream.Write(messageBytes, 0, messageBytes.Length);
                    Console.WriteLine("Encaminhado para o servidor.");

                    // Aguarda e exibe a resposta do servidor
                    byte[] serverBuffer = new byte[1024];
                    int serverBytes = serverStream.Read(serverBuffer, 0, serverBuffer.Length);
                    string serverResponse = Encoding.UTF8.GetString(serverBuffer, 0, serverBytes);
                    Console.WriteLine("Resposta do Servidor: " + serverResponse);

                    // Opcional: encaminha a resposta do servidor de volta à WAVY
                    wavyStream.Write(Encoding.UTF8.GetBytes(serverResponse), 0, serverResponse.Length);

                    if (messageFromWavy.Contains("QUIT"))
                    {
                        Console.WriteLine("Recebido comando de QUIT. Encerrando comunicação.");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro: " + ex.Message);
            }
            finally
            {
                wavyStream.Close();
                wavyClient.Close();
                serverStream.Close();
                serverClient.Close();
            }
        }
    }
}
