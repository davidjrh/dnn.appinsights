// -----------------------------------------------------------------------
//  <copyright file="AITelemetry.cs" company="Dominion Enterprise">
//       Copyright (c) Dominion Enterprise LLC. All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------

namespace DotNetNuke.Monitoring.AppInsights
{
    using System;
    using System.Configuration;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;

    public static class Dx1Configuration
    {
        private const string EnvironmentPreFix = "APPSETTING_";

        private static string GetAzureAppSetting(string appSettingKey)
        {
            var key = EnvironmentPreFix + appSettingKey;
            return Environment.GetEnvironmentVariable(key);

        }

        public static string GetSetting(string settingKey)
        {
            var azureValue = GetAzureAppSetting(settingKey);
            if (!string.IsNullOrEmpty(azureValue))
            {
                return azureValue;
            }
            return GetSettingFromConfigFile(settingKey);
        }

        private static string GetSettingFromConfigFile(string settingKey)
        {
            return ConfigurationManager.AppSettings[settingKey];
        }
    }


    public class TelemetryInitializer : ITelemetryInitializer
    {

        private string NewOperationName(string operationaName)
        {
            return "DX1DNN " + operationaName;
        }

        /// <summary>
        /// Set the AI base settings for all events
        /// </summary>
        /// <param name="telemetry"></param>
        public void Initialize(ITelemetry telemetry)
        {
            //change the way we track operation name
            var telemetryType = telemetry as RequestTelemetry;
            var name = NewOperationName(telemetry.Context.Operation.Name);

            if (telemetryType != null)
            {
                telemetryType.Name = name;
            }

            // Get the website name from azure environment
            var webname = Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME");
            if (string.IsNullOrEmpty(webname))
            {
                webname = Environment.MachineName;
            }

            // this is how to add properties to every telemetry item sent
            // Check to see if we are already initiated
            if (!telemetry.Context.Properties.ContainsKey("GEO"))
            {
                telemetry.Context.Properties.Add("GEO", Dx1Configuration.GetSetting("GEO"));
            }
            if (!telemetry.Context.Properties.ContainsKey("App"))
            {
                telemetry.Context.Properties.Add("App", "DX1DNN");
            }

            if (!telemetry.Context.Properties.ContainsKey("PodName"))
            {
                telemetry.Context.Properties.Add("PodName", webname);
            }
            telemetry.Context.Component.Version = "DX1DNN";
        }
    }

    public class DependencyFilter : ITelemetryProcessor
    {
        private ITelemetryProcessor Next { get; set; }

        // You can pass values from .config
        public string MyParamFromConfigFile { get; set; }

        // Link processors to each other in a chain.
        public DependencyFilter(ITelemetryProcessor next)
        {
            this.Next = next;
        }
        public void Process(ITelemetry item)
        {

            var sqlMs = Dx1Configuration.GetSetting("SqlMs");
            var queueMs = Dx1Configuration.GetSetting("QueueMs");
            var blobMs = Dx1Configuration.GetSetting("BlobMs");
            var tableMs = Dx1Configuration.GetSetting("TableMs");
            var httpMs = Dx1Configuration.GetSetting("HttpMs");

            var trackSqlConnection = Dx1Configuration.GetSetting("TrackSqlConnection");

            int sqlMsTime;
            int.TryParse(sqlMs, out sqlMsTime);

            int queueMsTime;
            int.TryParse(queueMs, out queueMsTime);

            int blobMsTime;
            int.TryParse(blobMs, out blobMsTime);

            int tableMsTime;
            int.TryParse(tableMs, out tableMsTime);

            int httpMsTime;
            int.TryParse(httpMs, out httpMsTime);

            bool trackSqlConnections;
            bool.TryParse(trackSqlConnection, out trackSqlConnections);

            // Dependency checking - if less than configured amount dont track
            var dependency = item as DependencyTelemetry;

            if (dependency != null)
            {
                if (dependency.Type == "Http")
                {
                    if (httpMsTime > 0 && dependency.Duration.Milliseconds < httpMsTime)
                    {
                        return;
                    }
                }

                if (dependency.Type == "SQL")
                {
                    // Check to see if we track open commands - the Name contains server | db | command so if 2 or less its an open call
                    if (!trackSqlConnections)
                    {
                        var items = dependency.Name.Split('|');
                        if (items.Length <= 2)
                        {
                            return;
                        }
                    }
                    if (sqlMsTime > 0 && dependency.Duration.Milliseconds < sqlMsTime)
                    {
                        return;
                    }
                }

                if (dependency.Type == "Azure queue")
                {
                    if (queueMsTime > 0 && dependency.Duration.Milliseconds < queueMsTime)
                    {
                        return;
                    }
                }


                if (dependency.Type == "Azure table")
                {
                    if (tableMsTime > 0 && dependency.Duration.Milliseconds < tableMsTime)
                    {
                        return;
                    }
                }

                if (dependency.Type == "Azure blob")
                {
                    if (blobMsTime > 0 && dependency.Duration.Milliseconds < blobMsTime)
                    {
                        return;
                    }
                }
            }
            this.Next.Process(item);
        }
    }


    public class RequestFilter : ITelemetryProcessor
    {
        private ITelemetryProcessor Next { get; set; }

        // You can pass values from .config
        public string MyParamFromConfigFile { get; set; }

        // Link processors to each other in a chain.
        public RequestFilter(ITelemetryProcessor next)
        {
            this.Next = next;
        }
        public void Process(ITelemetry item)
        {

            var httpCode20XMs = Dx1Configuration.GetSetting("HttpCode20xMs");
            var httpCode30XMs = Dx1Configuration.GetSetting("HttpCode30xMs");
            
            int httpCode20XMsTime;
            int.TryParse(httpCode20XMs, out httpCode20XMsTime);

            int httpCode30XMsTime;
            int.TryParse(httpCode30XMs, out httpCode30XMsTime);
              

            // Request checking - if less than configured amount dont track
            var request = item as RequestTelemetry;

            if (request != null)
            {
                 

                if (request.ResponseCode.StartsWith("2", StringComparison.OrdinalIgnoreCase))
                {
                    if (httpCode20XMsTime > 0 && request.Duration.Milliseconds < httpCode20XMsTime)
                    {
                        return;
                    }

                }

                if (request.ResponseCode.StartsWith("3", StringComparison.OrdinalIgnoreCase))
                {
                    if (httpCode30XMsTime > 0 && request.Duration.Milliseconds < httpCode30XMsTime)
                    {
                        return;
                    }
                }
                 
            }
            this.Next.Process(item);
        }
    }

}