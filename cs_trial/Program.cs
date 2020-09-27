using System;
using System.Threading.Tasks;

using System.IO.Pipes;
using System.Text;

class Program
{
    public static async Task Main(string[] args)
    {
        var inPipe = new NamedPipeServerStream("/home/lz/pipe", PipeDirection.InOut);
        Console.WriteLine("waiting for connection");
        inPipe.WaitForConnection();
        Console.WriteLine("connected");
        while (true)
        {
            byte[] buffer = new byte[4];
            int cnt = await inPipe.ReadAsync(buffer, 0, 4);
            if (cnt == 0)
                break;
            Console.WriteLine(Encoding.ASCII.GetString(buffer));
        }

        /*
        if (args.Length > 0 && args[0] == "--trial") {
            Nni.TrialRuntime.Run(args[1]);

        } else {
            string searchSpace = "{\"x\":{\"_type\":\"uniform\",\"_value\":[0.1,1.0]}}";
            var exp = new Nni.Experiment("NaiveExample.NaiveTrial", "Random", searchSpace);
            var result = await exp.Run(2, 8080);
            foreach (var kv in result)
            {
                Console.WriteLine($"ParameterID: {kv.Key}  Result: {kv.Value}");
            }
        }
        */
    }
}
