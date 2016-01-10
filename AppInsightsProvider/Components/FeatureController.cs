using System;
using System.Linq;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Entities.Modules;
using DotNetNuke.Entities.Modules.Definitions;
using DotNetNuke.Entities.Tabs;
using DotNetNuke.Services.Upgrade;

namespace DotNetNuke.Monitoring.AppInsights.Components
{
    public class FeatureController: IUpgradeable
    {
        public string UpgradeModule(string version)
        {
            try
            {
                var hostTabId = TabController.GetTabByTabPath(Null.NullInteger, "//Host", Null.NullString);
                AddHostPage(hostTabId,
                            "//Host//AppInsights",
                            "AppInsights",
                            "Application Insights",
                            "Application Insights Monitoring",
                            "~/Providers/MonitoringProviders/AppInsights/images/AppInsights.png",
                            "~/Providers/MonitoringProviders/AppInsights/images/AppInsights.png");

                return "Success";

            }
            catch (Exception ex)
            {
                return "Failed: " + ex.Message;
            }
        }


        private static void AddHostPage(int parentId, string tabPath, string moduleFriendlyName, string tabName, string tabDescription, string smallIcon, string largeIcon, bool isVisible = true)
        {
            var tabController = new TabController();
            var moduleController = new ModuleController();
            TabInfo hostTab;

            // Get the module definition
            var moduleDef = ModuleDefinitionController.GetModuleDefinitionByFriendlyName(moduleFriendlyName);

            // Add pages
            var tabId = TabController.GetTabByTabPath(Null.NullInteger, tabPath, Null.NullString);
            if (tabId == Null.NullInteger)
            {
                //Add host page                
                hostTab = Upgrade.AddHostPage(tabName, tabDescription, smallIcon, largeIcon, isVisible);
                hostTab.ParentId = parentId;
                tabController.UpdateTab(hostTab);

                //Add module to page
                Upgrade.AddModuleToPage(hostTab, moduleDef.ModuleDefID, tabName, largeIcon, true);
            }
            else
            {
                hostTab = tabController.GetTab(tabId, Null.NullInteger, false);
                foreach (
                    var kvp in
                        moduleController.GetTabModules(tabId)
                            .Where(kvp => kvp.Value.DesktopModule.ModuleName == moduleFriendlyName))
                {
                    // Remove previous module instance
                    moduleController.DeleteTabModule(tabId, kvp.Value.ModuleID, false);
                    break;

                }

                //Add module to page
                Upgrade.AddModuleToPage(hostTab, moduleDef.ModuleDefID, tabName, largeIcon, true);
            }
        }

    }
}
