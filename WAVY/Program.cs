using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Wavy
{
    class Program
    {
        // Porta do agregador e do servidor
        static readonly int agregadorPort = 5001;
        static readonly int servidorPort = 5000;
        static string agregadorIP = "127.0.0.1"; // Valor padrão caso não seja informado
        static string servidorIP = "127.0.0.1"; // Valor padrão caso não seja informado

        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Uso: Wavy.exe <IP_do_Agregador> <IP_do_Servidor>");
                return;
            }
            agregadorIP = args[0];
            servidorIP = args[1];

            try
            {
                // Conexão com o Agregador
                Console.WriteLine($"Tentando conectar ao Agregador em {agregadorIP}:{agregadorPort}...");
                TcpClient agregadorClient = new TcpClient();
                agregadorClient.Connect(agregadorIP, agregadorPort);
                NetworkStream agregadorStream = agregadorClient.GetStream();
                Console.WriteLine("Conectado ao Agregador.");

                // Conexão com o Servidor
                Console.WriteLine($"Tentando conectar ao Servidor em {servidorIP}:{servidorPort}...");
                TcpClient servidorClient = new TcpClient();
                servidorClient.Connect(servidorIP, servidorPort);
                NetworkStream servidorStream = servidorClient.GetStream();
                Console.WriteLine("Conectado ao Servidor.");

                // Envia mensagem de INICIO com ID e sensores ativos para o Agregador e o Servidor
                string inicioMsg = "{\"tipo\": \"INICIO\", \"id_wavy\": \"WAVY_001\", \"timestamp\": \""
                    + DateTime.UtcNow.ToString("o")
                    + "\", \"dados\": {\"status\": \"conectado\", \"sensores_ativos\": [\"acelerometro\", \"giroscopio\", \"transdutor\", \"hidrofone\", \"camera\"]}}";
                byte[] inicioBytes = Encoding.UTF8.GetBytes(inicioMsg);
                agregadorStream.Write(inicioBytes, 0, inicioBytes.Length);
                servidorStream.Write(inicioBytes, 0, inicioBytes.Length);
                Console.WriteLine("Mensagem de INICIO enviada.");

                // Loop para interação com o usuário via interface de texto
                while (true)
                {
                    Console.WriteLine("\nDigite um comando (DADOS, QUIT): ");
                    string comando = Console.ReadLine();
                    if (string.IsNullOrEmpty(comando))
                        continue;

                    string mensagem = "";
                    if (comando.ToUpper() == "QUIT")
                    {
                        mensagem = "{\"tipo\": \"QUIT\", \"id_wavy\": \"WAVY_001\", \"timestamp\": \""
                            + DateTime.UtcNow.ToString("o")
                            + "\", \"dados\": {}}";
                    }
                    else if (comando.ToUpper() == "DADOS")
                    {
                        // Simulação do envio de dados dos sensores
                        mensagem = "{\"tipo\": \"DADOS\", \"id_wavy\": \"WAVY_001\", \"timestamp\": \""
                            + DateTime.UtcNow.ToString("o")
                            + "\", \"dados\": {\"acelerometro\": {\"x\": 0.12, \"y\": -0.03, \"z\": 0.98}, \"giroscopio\": {\"x\": 0.02, \"y\": 0.01, \"z\": 0.00}}}";
                    }
                    else
                    {
                        Console.WriteLine("Comando não reconhecido.");
                        continue;
                    }

                    byte[] msgBytes = Encoding.UTF8.GetBytes(mensagem);
                    agregadorStream.Write(msgBytes, 0, msgBytes.Length);
                    servidorStream.Write(msgBytes, 0, msgBytes.Length);
                    Console.WriteLine("Mensagem enviada: " + mensagem);

                    // Espera resposta do agregador
                    byte[] agregadorBuffer = new byte[1024];
                    int agregadorBytes = agregadorStream.Read(agregadorBuffer, 0, agregadorBuffer.Length);
                    string agregadorResposta = Encoding.UTF8.GetString(agregadorBuffer, 0, agregadorBytes);
                    Console.WriteLine("Resposta do Agregador recebida: " + agregadorResposta);

                    // Espera resposta do servidor
                    byte[] servidorBuffer = new byte[1024];
                    int servidorBytes = servidorStream.Read(servidorBuffer, 0, servidorBuffer.Length);
                    string servidorResposta = Encoding.UTF8.GetString(servidorBuffer, 0, servidorBytes);
                    Console.WriteLine("Resposta do Servidor recebida: " + servidorResposta);

                    if (comando.ToUpper() == "QUIT")
                    {
                        Console.WriteLine("Encerrando comunicação.");
                        break;
                    }
                }

                agregadorStream.Close();
                agregadorClient.Close();
                servidorStream.Close();
                servidorClient.Close();
            }
            catch (SocketException se)
            {
                Console.WriteLine("Erro de socket: " + se.Message);
                Console.WriteLine("Verifique se o IP e a porta do agregador e do servidor estão corretos e se os servidores estão acessíveis.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro: " + ex.Message);
            }
        }
    }
}