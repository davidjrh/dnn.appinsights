using System;
using System.Web;
using System.IO;
using System.Configuration;
using System.Xml;
using System.Text.RegularExpressions;
using Microsoft.Web.XmlTransform;
using DotNetNuke.Entities.Controllers;

namespace DotNetNuke.Monitoring.AppInsights.Components
{
    internal class AppInsightsConfig
    {
        internal static void ModifyWebConfigFile(bool removeSettings = false)
        {
            var source = HttpContext.Current.Server.MapPath("~/Web.config");
            var transform =
                HttpContext.Current.Server.MapPath("~/DesktopModules/AppInsights/Web." +
                               (removeSettings ? "uninstall" : "install") + ".config");
            TransformXmlDoc(source, transform);
        }

        internal static void ModifyLog4NetConfigFile(bool removeSettings = false)
        {
            var source = HttpContext.Current.Server.MapPath("~/DotNetNuke.log4net.config");
            var transform =
                HttpContext.Current.Server.MapPath("~/DesktopModules/AppInsights/DotNetNuke.log4net." +
                               (removeSettings ? "uninstall" : "install") + ".config");
            TransformXmlDoc(source, transform);
        }


        internal static void ModifyAppInsightsConfigFile(bool removeSettings = false)
        {
            var configFile = HttpContext.Current.Server.MapPath("~/ApplicationInsights.config");
            if (!File.Exists(configFile))
            {
                File.Copy(HttpContext.Current.Server.MapPath("~/DesktopModules/AppInsights/ApplicationInsights.config"), configFile);
            }

            // Load the ApplicationInsights.config file
            var config = new ConfigXmlDocument();
            config.Load(configFile);
            var key = removeSettings ? string.Empty : HostController.Instance.GetString("AppInsights.InstrumentationKey");

            const string namespaceUri = "http://schemas.microsoft.com/ApplicationInsights/2013/Settings";
            var nsmgr = new XmlNamespaceManager(config.NameTable);
            nsmgr.AddNamespace("nr", namespaceUri);

            // Change instrumentation key
            var keyNode = config.SelectSingleNode("//nr:InstrumentationKey", nsmgr);
            if (keyNode != null)
            {
                keyNode.InnerText = key;
            }

            // Save the modifications
            config.Save(configFile);
        }


        internal static void ModifyAppInsightsJsFile(bool removeSettings = false)
        {
            var configFile = HttpContext.Current.Server.MapPath("~/DesktopModules/AppInsights/js/appinsights.js");

            if (!File.Exists(configFile))
            {
                throw new Exception($"Couldn't find the Application Insights javascript file on '{configFile}'");
            }

            var key = removeSettings ? string.Empty : HostController.Instance.GetString("AppInsights.InstrumentationKey");
            var config = File.ReadAllText(configFile);
            const string pattern = @"(?<left>.*)(?<instrumentationKey>instrumentationKey:"".*"")(?<right>.*)";
            var rgx = new Regex(pattern);
            var replacement = $@"$1 instrumentationKey:""{key}"" $3";
            var result = rgx.Replace(config, replacement);
            File.WriteAllText(configFile, result);
        }

        private static void TransformXmlDoc(string sourceFile, string transFile, string destFile = "")
        {
            if (string.IsNullOrEmpty(destFile))
                destFile = sourceFile;
            // The translation at-hand
            using (var xmlDoc = new XmlTransformableDocument())
            {
                xmlDoc.PreserveWhitespace = true;
                xmlDoc.Load(sourceFile);

                using (var xmlTrans = new XmlTransformation(transFile))
                {
                    if (xmlTrans.Apply(xmlDoc))
                    {
                        xmlDoc.Save(destFile);
                    }
                }
            }
        }

    }
}
