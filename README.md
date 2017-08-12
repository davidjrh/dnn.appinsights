# AppInsights module for DNN Platform
A module to use Visual Studio Application Insights with the DNN Platform CMS. The telemetry sent to AppInsights includes: page views, web requests, trace information (log4net log file content) and exceptions (including client side browser exceptions).

## Getting started
This module is a DNN Platorm extension to integrate Visual Studio Application Insights to monitor your DNN installation. To setup the module on your installation, follow these steps:

1. Provision a new AppInsights service following the guide at https://azure.microsoft.com/en-us/documentation/articles/app-insights-overview/. Ensure you choose "ASP.net web application" on the "Application Type" parameter
2. Once provisioned, copy the "Instrumentation Key" available on the resource Essentials properties 
![alt text](https://intelequia.blob.core.windows.net/images/AppInsights_InstrumentationKey.png "Instrumentation Key")
3. Now from the Releases folder https://github.com/davidjrh/dnn.appinsights/tree/master/Releases, download the latest module package version ending on "...Install.zip" (the Source.zip package contains the source code that is not needed for production websites). 
4. Install the extension package in your DNN instance from the "Settings > Extensions" persona bar menu like any other module
5. Once installed, a new menu under "Settings > Application Insights" will allow you to paste the instrumentation key obtained on step 2. After appying the changes, you will start receiving data on AppInsights after a few minutes.
![alt text](https://intelequia.blob.core.windows.net/images/AppInsights_InstrumentationKey3.png "Instrumentation Key")
