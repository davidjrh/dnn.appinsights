using System;
using System.Runtime.Serialization;

namespace DotNetNuke.Monitoring.AppInsights.Services
{
    [DataContract]
    public class AppInsightsSettings
    {
        [DataMember(Name = "enabled")]
        public bool Enabled { get; set; }

        [DataMember(Name = "instrumentationKey")]
        public string InstrumentationKey { get; set; }
    }
}
