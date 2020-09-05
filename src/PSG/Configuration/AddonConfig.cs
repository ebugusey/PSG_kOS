using System;
using System.IO;
using System.Text;

namespace PSG.Configuration
{
    /// <summary>
    /// Main configuration file reader.
    /// </summary>
    public class AddonConfig : IConfig
    {
        public Uri Url { get; set; }
        public string RequestOpts { get; set; }

        /// <summary>
        /// Read config from provided <see cref="filename"/>.
        /// </summary>
        /// <param name="filename">Config file location.</param>
        /// <returns>Read config.</returns>
        public static IConfig ReadFrom(string filename)
        {
            using var file = File.OpenRead(filename);

            var config = new AddonConfig();
            config.ReadFrom(file);

            return config;
        }

        /// <summary>
        /// Fill current config properties from provided stream.
        /// </summary>
        /// <param name="stream">
        /// Stream, from which config is read.
        /// </param>
        public void ReadFrom(Stream stream)
        {
            using var reader = new StreamReader(stream, Encoding.UTF8);

            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                var parameters = line.Split('|');

                if (parameters[0] == "url")
                {
                    Url = new Uri(parameters[1]);
                }

                if (parameters[0] == "params")
                {
                    RequestOpts = parameters[1];
                }
            }
        }
    }
}
