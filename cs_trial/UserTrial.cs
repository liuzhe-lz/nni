using System.Text.Json;
using System.Threading;

namespace NaiveExample {
    class NaiveTrial : Nni.ITrial
    {
        public double Run(JsonElement parameters)
        {
            Thread.Sleep(1000);
            return 2.34;
        }
    }
}
