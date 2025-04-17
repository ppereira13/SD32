using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Agregador
{
    class Program
    {
        static readonly int portWavy = 5001;
        static readonly int serverPort = 5000;
        static string serverIP = "127.0.0.1";
        static string csvEstadoWavy = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"C:\Users\Rita\source\repos\SD32\Agregador\estado_wavy.csv");
        static string csvEncaminhamento = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"C:\Users\Rita\source\repos\SD32\Agregador\encaminhamento.csv");




        static void Main(string[] args)
        {
            if (!File.Exists(csvEstadoWavy))
                File.WriteAllText(csvEstadoWavy, "id_wavy,estado,sensores_ativos\n");

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
            byte[] buffer = new byte[2048];
            int bytesRead;

            TcpClient serverClient = new TcpClient();
            serverClient.Connect(serverIP, serverPort);
            NetworkStream serverStream = serverClient.GetStream();

            try
            {
                while ((bytesRead = wavyStream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    string messageFromWavy = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine("Recebido da WAVY: " + messageFromWavy);

                    ProcessarMensagem(messageFromWavy);

                    byte[] messageBytes = Encoding.UTF8.GetBytes(messageFromWavy);
                    serverStream.Write(messageBytes, 0, messageBytes.Length);
                    Console.WriteLine("Encaminhado para o servidor.");

                    byte[] serverBuffer = new byte[2048];
                    int serverBytes = serverStream.Read(serverBuffer, 0, serverBuffer.Length);
                    string serverResponse = Encoding.UTF8.GetString(serverBuffer, 0, serverBytes);
                    Console.WriteLine("Resposta do Servidor: " + serverResponse);

                    wavyStream.Write(Encoding.UTF8.GetBytes(serverResponse), 0, serverResponse.Length);

                    if (messageFromWavy.Contains("QUIT"))
                    {
                        Console.WriteLine("Recebido comando QUIT. Encerrando comunicação.");
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

        static void ProcessarMensagem(string mensagem)
        {
            try
            {
                JObject json = JObject.Parse(mensagem);
                string tipo = (string)json["tipo"];
                string idWavy = (string)json["id_wavy"];

                if (tipo == "DADOS")
                {
                    List<string> sensores = new List<string>();
                    JObject dados = (JObject)json["dados"];

                    if (dados["acelerometro"] != null) sensores.Add("acelerometro");
                    if (dados["giroscopio"] != null) sensores.Add("giroscopio");

                    Console.WriteLine($"[WAVY {idWavy}] Sensores ativos: {string.Join(", ", sensores)}");

                    AtualizarEstadoWavy(idWavy, sensores);

                    if (File.Exists(csvEncaminhamento))
                    {
                        string[] linhas = File.ReadAllLines(csvEncaminhamento);
                        foreach (string sensor in sensores)
                        {
                            foreach (string linha in linhas)
                            {
                                if (linha.StartsWith("tipo_dado")) continue;

                                string[] partes = linha.Split(',');
                                if (partes.Length >= 4 && partes[0] == sensor)
                                {
                                    string servidor = partes[1];
                                    string volume = partes[2];
                                    string preprocess = partes[3];

                                    Console.WriteLine($"Encaminhar {sensor} → {servidor} | volume={volume}, preprocess={preprocess}");
                                }
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("⚠ Ficheiro encaminhamento.csv não encontrado.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro ao processar mensagem DADOS: " + ex.Message);
            }
        }

        static void AtualizarEstadoWavy(string id, List<string> sensores)
        {
            try
            {
                List<string> novasLinhas = new List<string>();
                bool encontrado = false;


                if (!File.Exists(csvEstadoWavy))
                    novasLinhas.Add("id_wavy,estado,sensores_ativos");
                else
                {
                    string[] linhasExistentes = File.ReadAllLines(csvEstadoWavy);
                    if (linhasExistentes.Length == 0 || !linhasExistentes[0].StartsWith("id_wavy"))
                        novasLinhas.Add("id_wavy,estado,sensores_ativos");
                    else
                        novasLinhas.Add(linhasExistentes[0]);
                }


                string[] linhas = File.ReadAllLines(csvEstadoWavy);
                for (int i = 1; i < linhas.Length; i++)
                {
                    string[] colunas = linhas[i].Split(',');
                    if (colunas.Length > 0 && colunas[0] == id)
                    {
                        novasLinhas.Add($"{id},operacao,{string.Join(";", sensores)}");
                        encontrado = true;
                    }
                    else
                    {
                        novasLinhas.Add(linhas[i]);
                    }
                }

                if (!encontrado)
                {
                    novasLinhas.Add($"{id},operacao,{string.Join(";", sensores)}");
                }

                File.WriteAllLines(csvEstadoWavy, novasLinhas);
                Console.WriteLine($"✅ Estado atualizado: {id}, sensores = {string.Join(";", sensores)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro ao atualizar estado_wavy.csv: " + ex.Message);
            }
        }
    }
}