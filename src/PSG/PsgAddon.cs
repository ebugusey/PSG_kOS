using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using System.IO;
using System.Net;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text.RegularExpressions;
using kOS;
using kOS.AddOns;
using kOS.Safe;
using ILogger = kOS.Safe.ILogger;
using kOS.Safe.Screen;
using kOS.Safe.Utilities;
using kOS.Suffixed;

namespace PSG
{
    [kOSAddon("PSG")]
    [KOSNomenclature("PSGAddon")]
    public class PsgAddon : Addon
    {
        private readonly ILogger _logger;
        private readonly IScreenBuffer _screen;

        public PsgAddon(SharedObjects shared) : base(shared)
        {
            InitializeSuffixies();
            _logger = shared.Logger;
            _screen = shared.Screen;
        }

        private void InitializeSuffixies()
        {
            AddSuffix("GETVERSION", new Suffix<StringValue>(GetVersion, "Get version string"));
            //AddSuffix("ENGAGE", new OneArgsSuffix<BooleanValue, ScalarValue>(Engage, "Time warp ship to n seconds"));
            AddSuffix("OBSERVE", new ThreeArgsSuffix<StringValue,StringValue,ListValue,ScalarValue>(Observe,"Generate planetary spectrum"));
        }

        private StringValue GetVersion()
        {
            return "0.1";
        }

        private StringValue Observe(StringValue body, ListValue instrumentPlusObserveType,ScalarValue exposure)
        {
            string url = "";
            string filename = body+"_"+instrumentPlusObserveType[0]+"_"+instrumentPlusObserveType[1]+".txt";
            string opts = "";

            var lines = File.ReadAllLines("config.txt", Encoding.UTF8);

            foreach(string line in lines)
            {
                string[] parameters = line.Split('|');

                if (parameters[0] == "url")
                {
                    url = parameters[1];
                }

                if (parameters[0] == "params")
                {
                    opts = parameters[1];
                }
            }

            if (File.Exists(filename))
            {
                var configPsg = File.ReadAllText(filename);

                var planetsMap = new Dictionary<string, string>
                {
                    ["PS4892b"] = "PS4892b",
                    ["PS4892c"] = "TauCetif",
                    ["PS4892d"] = "Teegardenb",
                    ["PS4892e"] = "TRAPPIST1b",
                    ["PS4892f"] = "TRAPPIST1c",
                    ["PS4892g"] = "TRAPPIST1e",
                    ["PS4892h"] = "TRAPPIST1f",
                    ["PS4892i"] = "TRAPPIST1d",
                    ["PS4892j"] = "TRAPPIST1g",
                    ["PS4892k"] = "TRAPPIST1h",
                };

                List<CelestialBody> bodies = FlightGlobals.Bodies;

                Boolean bodyExist = false;
                CelestialBody targetBody = bodies[0];

                foreach (var currentBody in bodies)
                {
                    if (currentBody.GetName() == planetsMap[body])
                    {
                        bodyExist = true;
                        targetBody = currentBody;
                    }
                }

                if (bodyExist)
                {
                    Vector3 heading =
                    targetBody.GetTransform().position -
                    FlightGlobals.ActiveVessel.GetTransform().position;

                    double distance = Math.Round(heading.magnitude / 1000.0);

                    double distanceAu = distance / 149600000000;

                    if (distance < 100000000)
                    {
                        configPsg = configPsg.Replace("${DISTANCE}", distance.ToString());
                        configPsg = configPsg.Replace("${ALTITUDEUNIT}", "km");
                    }
                    else
                    {
                        configPsg = configPsg.Replace("${DISTANCE}", distanceAu.ToString());
                        configPsg = configPsg.Replace("${ALTITUDEUNIT}", "AU");
                    }

                    configPsg = configPsg.Replace("${EXPOSURE}", exposure.ToString());

                    configPsg = configPsg.Replace("\r\n", "\n");

                    File.WriteAllText("psg_config_debug.txt", configPsg);

                    var request = WebRequest.Create(url);
                    request.ContentType = "application/x-www-form-urlencoded";
                    request.Method = "POST";

                    using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                    {
                        string req = opts + WebUtility.UrlEncode(configPsg);
                        streamWriter.Write(req);
                    }

                    _logger.Log($"posting to url: {url}");
                    _logger.Log($"post body:\n{WebUtility.UrlEncode(configPsg)}");

                    var httpResponse = request.GetResponse() as HttpWebResponse;
                    using (Stream responseStream = httpResponse.GetResponseStream())
                    using (var reader = new StreamReader(responseStream, Encoding.UTF8))
                    {
                        _logger.Log("got response from PSG");
                        _logger.Log($"PSG response size: {responseStream.Length}");

                        var payloadRows = 0;

                        var output = new List<(string, string)>();
                        while (!reader.EndOfStream)
                        {
                            var currentRow = reader.ReadLine();
                            if (currentRow.StartsWith(@"#", StringComparison.InvariantCulture))
                            {
                                continue;
                            }

                            payloadRows++;

                            var values = currentRow.Split(' ');

                            output.Add((values[0], values[2]));
                        }

                        _logger.Log($"PSG response payload size: {payloadRows} rows");
                        if (payloadRows == 0)
                        {
                            _logger.LogError("No response from PSG");
                            return "No response from PSG";
                        }

                        var csvOutput = new StringBuilder();
                        var psgOutput = new StringBuilder();
                        foreach (var (first, second) in output)
                        {
                            csvOutput.AppendLine($"{first};{second}");
                            psgOutput.AppendLine($"{first}  {second}");
                        }

                        var currentTime = DateTime.Now;
                        var outFile = $"psg_out_{currentTime:O}";
                        outFile = Regex.Replace(outFile, @"\+|:", "_");

                        var csvFilename = $"{outFile}.csv";
                        var psgFilename = $"{outFile}.txt";

                        _logger.Log($"writing PSG response to '{csvFilename}'");
                        File.WriteAllText(csvFilename, csvOutput.ToString());
                        _logger.Log($"writing PSG response to '{psgFilename}'");
                        File.WriteAllText(psgFilename, psgOutput.ToString());
                    }

                    return "Observe complete";
                }
                else
                {
                    return "Celestial body does not exist";
                }
            }
            else
            {
                return "Config file does not exist";
            }

            //return "No result";
        }

        public override BooleanValue Available()
        {
            return true;
        }
    }
}
