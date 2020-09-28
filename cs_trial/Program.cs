using System;
using System.Threading.Tasks;

using System.IO.Pipes;
using System.Text;

class Program
{
    public static async Task Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--trial") {
            Nni.TrialRuntime.Run(args[1]);

        } else {
            string trialClassName = "NaiveExample.NaiveTrial";
            string searchSpace = "-0.5,0.5";
            string tuner = "Random";
            var exp = new Nni.Experiment(trialClassName, tuner, searchSpace);

            int trialNum = 2;
            var result = await exp.Run(trialNum);

            Console.WriteLine("=== Experiment Result ===");
            foreach (var kv in result)
            {
                (string parameter, double metric) = kv;
                Console.WriteLine($"Parameter: {parameter}  Result: {metric}");
            }
        }
    }
}
