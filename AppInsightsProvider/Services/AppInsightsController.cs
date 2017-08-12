using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Web.Http;
using Dnn.PersonaBar.Library;
using Dnn.PersonaBar.Library.Attributes;
using DotNetNuke.Instrumentation;
using DotNetNuke.Web.Api;
using System.Net;
using DotNetNuke.Entities.Controllers;
using DotNetNuke.Monitoring.AppInsights.Components;

namespace DotNetNuke.Monitoring.AppInsights.Services
{
    [MenuPermission(Scope = ServiceScope.Host)]
    public class AppInsightsController: PersonaBarApiController
    {
        private static readonly ILog Logger = LoggerSource.Instance.GetLogger(typeof(AppInsightsController));

        /// GET: api/AppInsights/GetSettings
        /// <summary>
        /// Gets the Application Insights settings
        /// </summary>
        /// <returns>Application Insights settings</returns>
        [HttpGet]
        public HttpResponseMessage GetSettings()
        {
            try
            {
                var settings = new AppInsightsSettings()
                {
                    Enabled = HostController.Instance.GetBoolean("AppInsights.Enabled"),
                    InstrumentationKey = HostController.Instance.GetString("AppInsights.InstrumentationKey")
                };
                return Request.CreateResponse(HttpStatusCode.OK, settings);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
            }            
        }


        // POST: api/AppInsights/UpdateSettings
        /// <summary>
        /// Updates the Application Insights settings
        /// </summary>
        /// <param name="appInsightSettings"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public HttpResponseMessage UpdateSettings(AppInsightsSettings appInsightSettings)
        {
            try
            {
                var settings = new Dictionary<string, string>
                {
                    {"AppInsights.Enabled", appInsightSettings.Enabled.ToString()},
                    {"AppInsights.InstrumentationKey", appInsightSettings.InstrumentationKey}
                };
                HostController.Instance.Update(settings);

                // Update the configuration files
                AppInsightsConfig.ModifyLog4NetConfigFile(!appInsightSettings.Enabled);
                AppInsightsConfig.ModifyAppInsightsConfigFile(!appInsightSettings.Enabled);
                AppInsightsConfig.ModifyAppInsightsJsFile(!appInsightSettings.Enabled);
                AppInsightsConfig.ModifyWebConfigFile(!appInsightSettings.Enabled);

                // Increment the CRM version to avoid appinsights.js caching
                HostController.Instance.IncrementCrmVersion(true);


                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
            }
        }


    }
}
