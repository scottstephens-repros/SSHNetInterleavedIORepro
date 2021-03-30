using Renci.SshNet;
using System;
using System.IO;
using System.Threading;

namespace Local
{
    class Program
    {
        static void Main(string[] args)
        {
            var host = "";
            var user = "";
            var key_file = @"";
            using (var client = new SshClient(host, user, new PrivateKeyFile(key_file)))
            {
                int timoutCounter = 0;
                do
                {
                    timoutCounter++;
                    try { client.Connect(); }
                    catch (Exception e)
                    {
                        Console.WriteLine("Connection error: " + e.Message);
                    }
                } while (!client.IsConnected);

                ShellStream stream = client.CreateShellStream("shell", 200, 24, 1920, 1080, 4096);

                // starts the process on remote ssh server
                stream.WriteLine("dotnet ~/Remote/Remote.dll");

                var cancel_source = new CancellationTokenSource();
                var output_handler = new OutputHandler(stream);
                var output_thread = output_handler.RunOnNewThread(cancel_source.Token);

                while (true)
                {
                    var key = Console.ReadKey();

                    if (key.KeyChar == 'x')
                        break;
                    else
                        stream.Write(key.KeyChar.ToString());
                }

                cancel_source.Cancel();

                stream.Write("q");

                client.Disconnect();

                output_thread.Join();
            }
        }

        internal class OutputHandler
        {
            ShellStream Stream;

            public OutputHandler(ShellStream stream)
            {
                this.Stream = stream;
            }

            public void Run(CancellationToken cancel_token)
            {
                var reader = new StreamReader(this.Stream);
                int null_count = 0;
                while (!cancel_token.IsCancellationRequested)
                {
                    var line = reader.ReadLine();

                    if (line == null)
                        null_count += 1;
                    else
                        Console.WriteLine($"Output from remote: {line}");
                }
                Console.WriteLine($"null count: {null_count}");
            }

            public Thread RunOnNewThread(CancellationToken cancel_token)
            {
                var ts = new ThreadStart(() => this.Run(cancel_token));
                var thread = new Thread(ts);
                thread.Name = "Output";
                thread.IsBackground = false;
                thread.Start();
                return thread;
            }
        }
    }
}
