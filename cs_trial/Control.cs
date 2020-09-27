using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Nni
{
    class Experiment
    {
        // FIXME: hard-coded installation information
        public string NodePath = "node";
        public string NniManagerPath = "/home/lz/.local/nni";
        public string TrialDir = "/home/lz/code/nni/cs_trial";
        public string TrialCommand = "./bin/Debug/netcoreapp3.1/cs_trial --trial ";

        public Experiment(string trialClassName, string tunerName, string searchSpace)
        {
            _trialClassName = trialClassName;
            _tunerName = tunerName;
            _searchSpace = searchSpace;
        }

        public async Task<Dictionary<int, double>> Run(int trialNum, int port)
        {
            await Launch(trialNum, port);
            while (await CheckStatus() == "RUNNING")
                await Task.Delay(5000);
            var ret = await GetResults();
            Stop();
            return ret;
        }

        public async Task Launch(int trialNum, int port)
        {
            Console.WriteLine("Starting NNI experiment...");
            var startInfo = new ProcessStartInfo("node")
            {
                ArgumentList = {
                    "--max-old-space-size=4096",
                    Path.Join(NniManagerPath, "main.js"),
                    "--port", port.ToString(),
                    "--mode", "local",
                    "--start_mode", "new",
                    "--log_level", "debug",
                }
            };
            _proc = Process.Start(startInfo);

            _host = $"http://localhost:{port}/api/v1/nni";

            Console.WriteLine("Waiting REST API online...");
            while (await CheckStatus() == null)
                await Task.Delay(1000);

            Console.WriteLine("Setting up NNI manager...");
            _experimentId = await SetupNniManager(trialNum);

            Console.WriteLine($"NNI experiment started (ID:{_experimentId})");
        }

        public void Stop()
        {
            _proc.Kill(true);
        }

        public async Task<Dictionary<int, double>> GetResults()
        {
            var resp = await _client.GetAsync(_host + "/metric-data/?type=FINAL");
            string content = await resp.Content.ReadAsStringAsync();
            var metrics = JsonSerializer.Deserialize<List<MetricData>>(content, _jsonOptions);

            var ret = new Dictionary<int, double>();
            foreach (var metric in metrics)
            {
                int paramId = Int32.Parse(metric.ParameterId);
                double result = Double.Parse(JsonSerializer.Deserialize<string>(metric.Data));
                ret.Add(paramId, result);
            }
            return ret;
        }

        public async Task<string> CheckStatus()
        {
            HttpResponseMessage resp;
            try
            {
                resp = await _client.GetAsync(_host + "/check-status");
            }
            catch (Exception)
            {
                return null;
            }
            if (!resp.IsSuccessStatusCode)  // TODO: check if this may happen
                return null;

            string content = await resp.Content.ReadAsStringAsync();
            var status = JsonSerializer.Deserialize<NniManagerStatus>(content, _jsonOptions);
            Console.WriteLine("NNI manager status: " + status.Status);
            return status.Status;
        }

        private readonly static HttpClient _client = new HttpClient();
        private static JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private string _trialClassName;
        private string _tunerName;
        private string _searchSpace;

        private string _host; 
        private Process _proc;
        private string _experimentId;

        private async Task<string> SetupNniManager(int trialNum)
        {
            var trialConfig = new TrialConfig
            {
                Command = TrialCommand + _trialClassName,
                CodeDir = TrialDir,
                GpuNum = 0
            };

            string trialConfigJson = JsonSerializer.Serialize(trialConfig, _jsonOptions);
            string contentStr = "{\"trial_config\":" + trialConfigJson + "}";
            var content = new StringContent(contentStr, Encoding.UTF8, "application/json");
            var resp = await _client.PutAsync(_host + "/experiment/cluster-metadata", content);
            resp.EnsureSuccessStatusCode();

            var tunerConfig = new TunerConfig { BuiltinTunerName = _tunerName };

            ClusterConfigKV[] clusterConfigs = {
                new ClusterConfigKV { Key = "codeDir", Value = TrialDir },
                new ClusterConfigKV { Key = "command", Value = TrialCommand + _trialClassName }
            };

            var expConfig = new ExperimentConfig
            {
                AuthorName = "ML.NET",
                ExperimentName = "Model Builder",
                TrialConcurrency = 1,
                MaxExecDuration = 999999,
                MaxTrialNum = trialNum,
                SearchSpace = _searchSpace,
                TrainingServicePlatform = "local",
                Tuner = tunerConfig,
                VersionCheck = false,
                ClusterMetaData = clusterConfigs
            };

            string expConfigJson = JsonSerializer.Serialize(expConfig, _jsonOptions);
            content = new StringContent(expConfigJson, Encoding.UTF8, "application/json");
            resp = await _client.PostAsync(_host + "/experiment", content);
            resp.EnsureSuccessStatusCode();

            string expIdContent = await resp.Content.ReadAsStringAsync();
            var expId = JsonSerializer.Deserialize<NniExperimentId>(expIdContent);
            return expId.ExperimentId;
        }
    }

    class TrialConfig {
        public string Command { get; set; }
        public string CodeDir { get; set; }
        public int GpuNum { get; set; }
    }

    class ExperimentConfig {
        public string AuthorName { get; set; }
        public string ExperimentName { get; set; }
        public int TrialConcurrency { get; set; }
        public int MaxExecDuration { get; set; }
        public int MaxTrialNum { get; set; }
        public string SearchSpace { get; set; }
        public string TrainingServicePlatform { get; set; }
        public TunerConfig Tuner { get; set; }
        public bool VersionCheck { get; set; }
        public IList<ClusterConfigKV> ClusterMetaData { get; set; }
    }

    class TunerConfig {
        public string BuiltinTunerName { get; set; }
        public Dictionary<string, string> ClassArgs { get; set; }
    }

    class ClusterConfigKV {
        public string Key { get; set; }
        public string Value { get; set; }
    }

    class NniManagerStatus {
        public string Status { get; set; }
        public IList<string> Errors { get; set; }
    }

    class NniExperimentId {
        [JsonPropertyName("experiment_id")]
        public string ExperimentId { get; set; }
    }

    class MetricData {
        public long Timestamp { get; set; }
        public string TrialJobId { get; set; }
        public string ParameterId { get; set; }
        public string Type { get; set; }
        public int Sequence { get; set; }
        public string Data { get; set; }
    }
}
