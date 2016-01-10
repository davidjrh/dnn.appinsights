using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using System.Xml.Linq;
using System.Web.DynamicData;
using System.Xml;
using DotNetNuke.Collections;
using DotNetNuke.Entities.Modules;
using DotNetNuke.Framework;
using DotNetNuke.Framework.JavaScriptLibraries;
using DotNetNuke.Services.Exceptions;
using DotNetNuke.Services.Installer.Log;

namespace DotNetNuke.Monitoring.AppInsights
{
    public partial class AppInsightsSetup : PortalModuleBase
    {
        protected override void OnInit(EventArgs e)
        {
            InitializeComponent();

            base.OnInit(e);
        }

        private static void InitializeComponent()
        {
            JavaScript.RequestRegistration(CommonJs.DnnPlugins);
            ServicesFramework.Instance.RequestAjaxScriptSupport();
            ServicesFramework.Instance.RequestAjaxAntiForgerySupport();
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            try
            {
                if (!Page.IsPostBack)
                {
                    chkEnabled.Checked = ModuleSettings.GetValueOrDefault("Enabled", false);
                    txtInstrumentationKey.Text = ModuleSettings.GetValueOrDefault("InstrumentationKey", string.Empty);
                }
                txtInstrumentationKey.Enabled = chkEnabled.Checked;
                rqInstrumentationKey.Enabled = txtInstrumentationKey.Enabled;
            }
            catch (Exception ex)
            {
                Exceptions.ProcessModuleLoadException(this, ex);
            }
        }

        protected void lnkSave_OnClick(object sender, EventArgs e)
        {
            try
            {
                var moduleController = new ModuleController();
                moduleController.UpdateModuleSetting(ModuleId, "Enabled", chkEnabled.Checked.ToString());
                moduleController.UpdateModuleSetting(ModuleId, "InstrumentationKey", txtInstrumentationKey.Text.Trim());
                txtInstrumentationKey.Enabled = chkEnabled.Checked;
                rqInstrumentationKey.Enabled = txtInstrumentationKey.Enabled;

                // Update the configuration files
                ModifyLog4NetConfigFile(!chkEnabled.Checked);
                ModifyAppInsightsConfigFile(!chkEnabled.Checked);
                ModifyAppInsightsJsFile(!chkEnabled.Checked);

            }
            catch (Exception ex)
            {
                Exceptions.ProcessModuleLoadException(this, ex);
            }
        }


        protected Hashtable ModuleSettings
        {
            get
            {
                var module = (new ModuleController()).GetModule(ModuleId);
                return module.ModuleSettings;
            }
        }

        private void ModifyLog4NetConfigFile(bool removeSettings = false)
        {
            var configFile = Server.MapPath("~/DotNetNuke.log4net.config");
            if (!File.Exists(configFile))
            {
                return;
            }
            // Load the config file
            var config = new ConfigXmlDocument();
            config.Load(configFile);
            var log4net = config.SelectSingleNode("/log4net");
            var root = config.SelectSingleNode("/log4net/root");
            if (log4net == null || root == null)
            {
                return;
            }

            // Find ai node
            var node = config.SelectSingleNode("/log4net/appender[@name='aiAppender']");
            if (node == null && !removeSettings)
            {
                node = config.CreateNode(XmlNodeType.Element, "appender", "");
                var name = config.CreateAttribute("name");
                name.InnerText = "aiAppender";
                node.Attributes.Append(name);
                var type = config.CreateAttribute("type");
                type.InnerText = "log4net.Appender.TraceAppender, log4net";
                node.Attributes.Append(type);
                node.InnerXml = @"<layout type = 'log4net.Layout.PatternLayout'><conversionPattern value = '%message%newline' /></layout>";
                log4net.AppendChild(node);
            }
            if (node != null && removeSettings)
            {
                log4net.RemoveChild(node);
            }

            // Find appender-ref node
            node = config.SelectSingleNode("/log4net/root/appender-ref[@ref='aiAppender']");
            if (node == null && !removeSettings)
            {
                node = config.CreateNode(XmlNodeType.Element, "appender-ref", "");
                var refAtt = config.CreateAttribute("ref");
                refAtt.InnerText = "aiAppender";
                node.Attributes.Append(refAtt);
                root.AppendChild(node);
            }
            if (node != null && removeSettings)
            {
                root.RemoveChild(node);
            }

            // Save the modifications
            config.Save(configFile);
        }


        private void ModifyAppInsightsConfigFile(bool removeSettings = false)
        {
            var configFile = Server.MapPath("~/ApplicationInsights.config");
            if (!File.Exists(configFile))
            {
                File.Copy(Server.MapPath("~/DesktopModules/AppInsights/ApplicationInsights.config"), configFile);
            }

            // Load the ApplicationInsights.config file
            var config = new ConfigXmlDocument();
            config.Load(configFile);
            var key = removeSettings ? string.Empty : ModuleSettings.GetValueOrDefault("InstrumentationKey", string.Empty);

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


        private void ModifyAppInsightsJsFile(bool removeSettings = false)
        {
            var configFile = Server.MapPath("~/DesktopModules/AppInsights/js/appinsights.js");

            if (!File.Exists(configFile))
            {
                throw new Exception($"Couldn't find the Application Insights javascript file on '{configFile}'");
            }

            var key = removeSettings ? string.Empty : ModuleSettings.GetValueOrDefault("InstrumentationKey", string.Empty);
            var config = File.ReadAllText(configFile);
            const string pattern = @"(?<left>.*)(?<instrumentationKey>instrumentationKey:"".*"")(?<right>.*)";
            var rgx = new Regex(pattern);
            var replacement = $@"$1 instrumentationKey:""{key}"" $3";
            var result = rgx.Replace(config, replacement);
            File.WriteAllText(configFile, result);
        }


    }
}
