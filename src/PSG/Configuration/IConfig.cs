using System;

namespace PSG.Configuration
{
    /// <summary>
    /// Main configuration file.
    /// </summary>
    public interface IConfig
    {
        /// <summary>
        /// Address of PSG instance to which addon connects to.
        /// </summary>
        /// <remarks>
        /// Official API is located at https://psg.gsfc.nasa.gov/api.php.
        /// But it can be locally deployed as Docker container.
        /// </remarks>
        Uri Url { get; }

        /// <summary>
        /// Additional query parameters added to API requests.
        /// </summary>
        string RequestOpts { get; }
    }
}
