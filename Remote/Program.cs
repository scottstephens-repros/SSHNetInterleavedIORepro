using System;
using System.Threading;

namespace Remote
{
    class Program
    {
        static void Main(string[] args)
        {
            var shared = new SharedData();
            var cancel_source = new CancellationTokenSource();

            var output_handler = new OutputHandler() { Data = shared };
            var output_thread = output_handler.RunOnNewThread(cancel_source.Token);

            while (true)
            {
                var key = Console.ReadKey();

                if (key.KeyChar == 'q')
                    break;
                else
                    shared.LastChar = key.KeyChar;
            }
            cancel_source.Cancel();
            output_thread.Join();
        }

        internal class SharedData
        {
            public volatile char LastChar;
        }

        internal class OutputHandler
        {
            public SharedData Data;

            public void Run(CancellationToken cancel_token)
            {
                while (!cancel_token.IsCancellationRequested)
                {
                    Thread.Sleep(1000);
                    Console.WriteLine($"Last char: {this.Data.LastChar}");
                }
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
