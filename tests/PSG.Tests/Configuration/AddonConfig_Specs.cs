using System;
using System.IO;
using FluentAssertions;
using NUnit.Framework;
using PSG.Configuration;

namespace PSG.Tests.Configuration
{
    [TestFixture]
    public class Reading_config_from_stream
    {
        private AddonConfig _config;
        private Stream _configStream;

        [Test]
        public void Should_fill_config_properties()
        {
            var expected = new AddonConfig
            {
                Url = new Uri("https://psg.gsfc.nasa.gov/api.php"),
                RequestOpts = @"type=trn&file=",
            };

            _config.ReadFrom(_configStream);

            _config.Should().BeEquivalentTo(expected);
        }

        [SetUp]
        public void SetUp()
        {
            var config = Path.Combine(TestContext.CurrentContext.TestDirectory, "Configuration", "config.txt");
            _configStream = File.OpenRead(config);

            _config = new AddonConfig();
        }

        [TearDown]
        public void TearDown()
        {
            _configStream.Dispose();
        }
    }
}
