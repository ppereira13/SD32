using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Wavy
{
    class Program
    {
        // Porta do agregador
        static readonly int agregadorPort = 5001;
        static string agregadorIP = "127.0.0.1"; // Valor padrão caso não seja informado
        static readonly int servidorPort = 5000; // Porta do servidor (Exemplo)

        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Uso: Wavy.exe <IP_do_Agregador>");
                Console.WriteLine("Pressione qualquer tecla para sair...");
                Console.ReadKey();
                return;
            }
            agregadorIP = args[0];

            // Aguardar Servidor estar ativo
            Console.WriteLine("Aguardando o servidor...");
            while (!VerificarServidor())
            {
                Thread.Sleep(2000);  // Espera 2 segundos antes de tentar novamente
            }

            // Aguardar Agregador estar ativo
            Console.WriteLine("Aguardando o agregador...");
            while (!VerificarAgregador())
            {
                Thread.Sleep(2000);  // Espera 2 segundos antes de tentar novamente
            }

            Console.WriteLine("Servidor e Agregador estão ativos. Iniciando Wavy...");

            try
            {
                TcpClient client = new TcpClient();
                client.Connect(agregadorIP, agregadorPort);
                NetworkStream stream = client.GetStream();

                Console.WriteLine("Conectado ao Agregador em " + agregadorIP + ":" + agregadorPort);

                // Envia mensagem de INICIO com ID e sensores ativos
                string inicioMsg = "{\"tipo\": \"INICIO\", \"id_wavy\": \"WAVY_001\", \"timestamp\": \""
                    + DateTime.UtcNow.ToString("o")
                    + "\", \"dados\": {\"status\": \"conectado\", \"sensores_ativos\": [\"acelerometro\", \"giroscopio\", \"transdutor\", \"hidrofone\", \"camera\"]}}";
                byte[] inicioBytes = Encoding.UTF8.GetBytes(inicioMsg);
                stream.Write(inicioBytes, 0, inicioBytes.Length);
                Console.WriteLine("Mensagem de INICIO enviada.");

                // Loop para interação com o usuário via interface de texto
                while (true)
                {
                    Console.Write("Digite um comando (DADOS, QUIT): ");
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
                    stream.Write(msgBytes, 0, msgBytes.Length);
                    Console.WriteLine("Mensagem enviada: " + mensagem);

                    // Espera resposta do agregador/servidor
                    byte[] buffer = new byte[1024];
                    int bytes = stream.Read(buffer, 0, buffer.Length);
                    string resposta = Encoding.UTF8.GetString(buffer, 0, bytes);
                    Console.WriteLine("Resposta recebida: " + resposta);

                    if (comando.ToUpper() == "QUIT")
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

        // Método para verificar se o Servidor está ativo
        static bool VerificarServidor()
        {
            try
            {
                using (var client = new TcpClient())
                {
                    client.Connect("127.0.0.1", servidorPort);  // Tenta conectar ao Servidor
                    return client.Connected;
                }
            }
            catch
            {
                return false;
            }
        }

        // Método para verificar se o Agregador está ativo
        static bool VerificarAgregador()
        {
            try
            {
                using (var client = new TcpClient())
                {
                    client.Connect(agregadorIP, agregadorPort);  // Tenta conectar ao Agregador
                    return client.Connected;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
