﻿using System;
using System.Web;
using System.Web.UI;
using DotNetNuke.Framework;
using DotNetNuke.Web.Client.ClientResourceManagement;

namespace DotNetNuke.Monitoring.AppInsights
{
    public class AppInsightsModule : IHttpModule
    {
        public void Init(HttpApplication context)
        {
            context.PreRequestHandlerExecute += RegisterPagePrerenderHandler;
        }

        public void Dispose()
        {
            // Do nothing
        }

        private static void RegisterPagePrerenderHandler(object s, EventArgs e)
        {
            var page = HttpContext.Current.Handler as Page;
            if (page == null) return;
            page.PreRender += delegate
            {
                if (!(page is CDefault)) return;
                ClientResourceManager.RegisterScript(page, "~/DesktopModules/AppInsights/js/appinsights.js", 0, "DnnPageHeaderProvider");
            };
        }
    }
}
