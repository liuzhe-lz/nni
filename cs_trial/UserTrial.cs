using System;
using System.Threading;

namespace NaiveExample {
    class NaiveTrial : Nni.ITrial
    {
        public double Run(string parameter)
        {
            double x = Double.Parse(parameter);
            Thread.Sleep(1000);
            return x * 2;
        }
    }
}
