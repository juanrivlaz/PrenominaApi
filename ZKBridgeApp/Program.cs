using Newtonsoft.Json;
using System;

namespace ZKBridgeApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Aplicar filtro de logs del SDK zkemkeeper pero permitir JSON
            var originalOut = Console.Out;
            Console.SetOut(new ConnectZK.FilteredConsoleWriter(originalOut));

            if (args.Length < 3)
            {
                Console.Error.WriteLine("Uso: ZKBridgeApp.exe <ip> <port> <method>");
                return;
            }

            string ip = args[0];
            int port = int.Parse(args[1]);
            string method = args[2].ToLowerInvariant();

            try
            {
                var zk = ConnectZK.ConnectZK.GetInstance(ip, port);

                switch (method)
                {
                    case "getusers":
                        OutputJson(zk.GetUsers());
                        break;

                    case "getfullusers":
                        OutputJson(zk.GetFullUsers());
                        break;

                    case "getcheckins":
                        OutputJson(zk.GetCheckIns());
                        break;

                    case "clearcheckins":
                        zk.ClearCheckIns();
                        Console.WriteLine("{\"status\":\"ok\"}");
                        break;

                    case "cleanup":
                        ConnectZK.ConnectZK.CleanupIdleConnections();
                        Console.WriteLine("{\"status\":\"ok\"}");
                        break;

                    default:
                        Console.Error.WriteLine($"Método '{method}' no reconocido.");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(JsonConvert.SerializeObject(new
                {
                    status = "error",
                    message = ex.Message
                }));
            }
            finally
            {
                Console.Out.Flush();
            }
        }

        static void OutputJson<T>(T data)
        {
            Console.WriteLine(JsonConvert.SerializeObject(data));
        }
    }
}
