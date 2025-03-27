using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Entities.Controllers;
using DotNetNuke.Monitoring.AppInsights.Components;
using DotNetNuke.Services.Log.EventLog;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;

namespace DotNetNuke.Monitoring.AppInsights
{
    public class AppInsightsLoggingProvider: DBLoggingProvider
    {
        private static TelemetryClient _appInsightsClient;
        private static TelemetryClient AppInsightsClient => _appInsightsClient ?? (_appInsightsClient = new TelemetryClient(new TelemetryConfiguration()
            {
                ConnectionString = HostController.Instance.GetString("AppInsights.ConnectionString")
            }));
        
        public override void AddLog(LogInfo logInfo)
        {   
            // Add log to DNN event log         
            base.AddLog(logInfo);

            // Add log to Application Insights
            if (!bool.TryParse(HostController.Instance.GetString("AppInsights.Enabled"), out bool appInsightsEnabled) || !appInsightsEnabled && string.IsNullOrEmpty(AppInsightsClient.TelemetryConfiguration.ConnectionString)) return;

            // Repeat base class private check 
            var logTypeConfigInfoByKey = GetLogTypeConfigInfoByKey(logInfo.LogTypeKey, (logInfo.LogPortalID != Null.NullInteger ? logInfo.LogPortalID.ToString() : "*"));
            if (logTypeConfigInfoByKey == null || !logTypeConfigInfoByKey.LoggingIsActive)
            {
                return;
            }
            logInfo.LogConfigID = logTypeConfigInfoByKey.ID;
            var message =
                $"{logInfo.LogTypeKey}{(string.IsNullOrEmpty(logInfo.LogUserName) ? string.Empty : $" Username: {logInfo.LogUserName}")}{(string.IsNullOrEmpty(logInfo.LogProperties.Summary) ? string.Empty : $" Summary: {logInfo.LogProperties.Summary}")}";
            AppInsightsClient.TrackEvent(message, GetProperties(logInfo));
        }        

        private static Dictionary<string, string> GetProperties(LogInfo logInfo)
        {
            var prop = new Dictionary<string, string>();
            prop.AddIfNotNull("TypeKey", logInfo.LogTypeKey);
            prop.AddIfNotNull("ConfigID", logInfo.LogConfigID);
            prop.AddIfNotNull("FileID", logInfo.LogFileID);
            prop.AddIfNotNull("GUID", logInfo.LogGUID);
            prop.AddIfNotNull("PortalName", logInfo.LogPortalName);
            prop.AddIfNotNull("ServerName", logInfo.LogServerName);
            prop.AddIfNotNull("UserName", logInfo.LogUserName);
            prop.AddIfNotNull("CreateDate", logInfo.LogCreateDate.ToString(CultureInfo.InvariantCulture));
            prop.AddIfNotNull("EventID", logInfo.LogEventID.ToString());
            prop.AddIfNotNull("PortalID", logInfo.LogPortalID.ToString());
            prop.AddIfNotNull("UserID", logInfo.LogUserID.ToString());
            prop.AddIfNotNull("Exception.Message", logInfo.Exception.Message);
            prop.AddIfNotNull("Exception.Source", logInfo.Exception.Source);
            prop.AddIfNotNull("Exception.StackTrace", logInfo.Exception.StackTrace);
            prop.AddIfNotNull("Exception.InnerMessage", logInfo.Exception.InnerMessage);
            prop.AddIfNotNull("Exception.InnerStackTrace", logInfo.Exception.InnerStackTrace);
            if (logInfo.LogProperties != null)
            {
                prop.AddIfNotNull("Summary", logInfo.LogProperties.Summary);
                foreach (LogDetailInfo logProperty in logInfo.LogProperties)
                {
                    prop.AddIfNotNull(logProperty.PropertyName, logProperty.PropertyValue);
                }
            }
            return prop;
        } 


        private LogTypeConfigInfo GetLogTypeConfigInfoByKey(string logTypeKey, string logTypePortalId)
        {
            var cache = (Hashtable)DataCache.GetCache("GetLogTypeConfigInfoByKey") ?? FillLogTypeConfigInfoByKey(GetLogTypeConfigInfo());
            var item = (LogTypeConfigInfo)cache[string.Concat(logTypeKey, "|", logTypePortalId)];
            if (item != null)
            {
                return item;
            }
            item = (LogTypeConfigInfo)cache[string.Concat("*|", logTypePortalId)];
            if (item != null)
            {
                return item;
            }
            item = (LogTypeConfigInfo)cache[string.Concat(logTypeKey, "|*")];
            if (item != null)
            {
                return item;
            }
            item = (LogTypeConfigInfo)cache["*|*"];
            return item;
        }

        private static Hashtable FillLogTypeConfigInfoByKey(ArrayList arr)
        {
            var hashtables = new Hashtable();
            for (int i = 0; i <= arr.Count - 1; i++)
            {
                var item = (LogTypeConfigInfo)arr[i];
                if (string.IsNullOrEmpty(item.LogTypeKey))
                {
                    item.LogTypeKey = "*";
                }
                if (string.IsNullOrEmpty(item.LogTypePortalID))
                {
                    item.LogTypePortalID = "*";
                }
                hashtables.Add(string.Concat(item.LogTypeKey, "|", item.LogTypePortalID), item);
            }
            DataCache.SetCache("GetLogTypeConfigInfoByKey", hashtables);
            return hashtables;
        }
    }
}
