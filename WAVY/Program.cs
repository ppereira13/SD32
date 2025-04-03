using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Wavy
{
    class Program
    {
        static readonly int agregadorPort = 5001;
        static string agregadorIP = "127.0.0.1";
        static readonly int servidorPort = 5000;

        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                agregadorIP = args[0];
            }

            Console.WriteLine("Aguardando o servidor...");
            while (!VerificarServidor()) Thread.Sleep(2000);

            Console.WriteLine("Aguardando o agregador...");
            while (!VerificarAgregador()) Thread.Sleep(2000);

            Console.WriteLine("Servidor e Agregador estão ativos. Iniciando Wavy...");
            try
            {
                TcpClient client = new TcpClient();
                client.Connect(agregadorIP, agregadorPort);
                NetworkStream stream = client.GetStream();

                Console.WriteLine("Conectado ao Agregador em " + agregadorIP + ":" + agregadorPort);

                while (true)
                {
                    Console.WriteLine("\nEscolha uma opção:");
                    Console.WriteLine("1 - Enviar OLÁ");
                    Console.WriteLine("2 - Enviar DADOS");
                    Console.WriteLine("3 - Enviar QUIT");
                    Console.Write("Opção: ");
                    string opcao = Console.ReadLine();
                    string mensagem = "";

                    if (opcao == "1")
                    {
                        mensagem = "{\"tipo\": \"OLA\", \"id_wavy\": \"WAVY_001\", \"timestamp\": \"" + DateTime.UtcNow.ToString("o") + "\", \"dados\": {}}";
                    }
                    else if (opcao == "2")
                    {
                        mensagem = "{\"tipo\": \"DADOS\", \"id_wavy\": \"WAVY_001\", \"timestamp\": \"" + DateTime.UtcNow.ToString("o") + "\", \"dados\": {\"acelerometro\": {\"x\": 0.12, \"y\": -0.03, \"z\": 0.98}, \"giroscopio\": {\"x\": 0.02, \"y\": 0.01, \"z\": 0.00}}}";
                    }
                    else if (opcao == "3")
                    {
                        mensagem = "{\"tipo\": \"QUIT\", \"id_wavy\": \"WAVY_001\", \"timestamp\": \"" + DateTime.UtcNow.ToString("o") + "\", \"dados\": {}}";
                    }
                    else
                    {
                        Console.WriteLine("Opção inválida.");
                        continue;
                    }

                    byte[] msgBytes = Encoding.UTF8.GetBytes(mensagem);
                    stream.Write(msgBytes, 0, msgBytes.Length);
                    Console.WriteLine("Mensagem enviada: " + mensagem);

                    byte[] buffer = new byte[1024];
                    int bytes = stream.Read(buffer, 0, buffer.Length);
                    string resposta = Encoding.UTF8.GetString(buffer, 0, bytes);
                    Console.WriteLine("Resposta recebida: " + resposta);

                    if (opcao == "3")
                    {
                        Console.WriteLine("Encerrando comunicação.");
                        break;
                    }
                }
                stream.Close();
                client.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro: " + ex.Message);
            }
        }

        static bool VerificarServidor() => VerificarConexao("127.0.0.1", servidorPort);
        static bool VerificarAgregador() => VerificarConexao(agregadorIP, agregadorPort);
        static bool VerificarConexao(string ip, int port)
        {
            try { using (var client = new TcpClient()) { client.Connect(ip, port); return client.Connected; } }
            catch { return false; }
        }
    }
}
