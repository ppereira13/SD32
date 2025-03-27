﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Servidor
{
    class Program
    {
        // Porta de escuta do servidor
        static readonly int port = 5000;

        static void Main(string[] args)
        {
            TcpListener listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            Console.WriteLine("Servidor iniciado. Aguardando conexões...");

            // Loop para aceitar múltiplas conexões
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
            bool running = true;
            bool firstMessage = true;


            try
            {
                while (running && (bytesRead = stream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine("Mensagem recebida: " + message);

                    // Responde com "100 OK" na primeira mensagem
                    if (firstMessage)
                    {
                        string response = "{\"status\": \"100 OK\", \"mensagem\": \"Conexão estabelecida.\"}";
                        byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                        stream.Write(responseBytes, 0, responseBytes.Length);
                        firstMessage = false;
                    }

                    // Se a mensagem contiver "QUIT", responde e encerra a comunicação
                    if (message.Contains("QUIT"))
                    {
                        string response = "{\"status\": \"400 BYE\", \"mensagem\": \"Encerrando comunicação.\"}";
                        byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                        stream.Write(responseBytes, 0, responseBytes.Length);
                        running = false;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro: " + ex.Message);
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