using System;
using System.Collections;
using System.Configuration;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using DotNetNuke.Collections;
using DotNetNuke.Entities.Modules;
using DotNetNuke.Framework;
using DotNetNuke.Framework.JavaScriptLibraries;
using DotNetNuke.Services.Exceptions;
using Microsoft.Web.XmlTransform;

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
                ModifyWebConfigFile(!chkEnabled.Checked);

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

        private void ModifyWebConfigFile(bool removeSettings = false)
        {
            var source = Server.MapPath("~/Web.config");
            var transform =
                Server.MapPath("~/DesktopModules/AppInsights/Web." +
                               (removeSettings ? "uninstall" : "install") + ".config");
            TransformXmlDoc(source, transform);
        }

        private void ModifyLog4NetConfigFile(bool removeSettings = false)
        {
            var source = Server.MapPath("~/DotNetNuke.log4net.config");
            var transform =
                Server.MapPath("~/DesktopModules/AppInsights/DotNetNuke.log4net." +
                               (removeSettings ? "uninstall" : "install") + ".config");
            TransformXmlDoc(source, transform);
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
