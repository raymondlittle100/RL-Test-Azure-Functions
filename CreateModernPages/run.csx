using System;
using System.Net;
using Newtonsoft.Json;
using Microsoft.SharePoint.Client;
using OfficeDevPnP.Core;
using OfficeDevPnP.Core.Pages;

private static readonly string ADMIN_USER_CONFIG_KEY = "SharePointAdminUser";
private static readonly string ADMIN_PASSWORD_CONFIG_KEY = "SharePointAdminPassword";

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{
    log.Info("C# HTTP trigger function processed a request.");

    // parse query parameter
    string name = req.GetQueryNameValuePairs()
        .FirstOrDefault(q => string.Compare(q.Key, "name", true) == 0)
        .Value;

    // Get request body
    dynamic data = await req.Content.ReadAsAsync<object>();

    string siteUrl = data.SiteUrl; 
    string pageName = data.PageName; 
    string pageText = data.PageText; 

    log.Info($"Received siteUrl={siteUrl}, pageName={pageName}, pageText={pageText}");
    
    if (siteUrl.Contains("www.contoso.com")) {
        return req.CreateResponse(HttpStatusCode.BadRequest, "Error: please run in the context of a real SharePoint site, not the local workbench. We need this to know which site to create the page in!");
    }
    
    string adminUserName = System.Environment.GetEnvironmentVariable(ADMIN_USER_CONFIG_KEY, EnvironmentVariableTarget.Process);
    string adminPassword = System.Environment.GetEnvironmentVariable(ADMIN_PASSWORD_CONFIG_KEY, EnvironmentVariableTarget.Process);

    log.Info($"Will attempt to authenticate to SharePoint with username {adminUserName}");

    ClientContext siteContext = new OfficeDevPnP.Core.AuthenticationManager().GetSharePointOnlineAuthenticatedContextTenant(siteUrl, adminUserName, adminPassword);
    Site site = siteContext.Site;
    siteContext.Load(site);
    siteContext.ExecuteQueryRetry();

    log.Info($"Successfully authenticated to site {siteContext.Url}..");

    log.Info($"Will attempt to create page with name {data.PageName}");

    ClientSidePage page = new ClientSidePage(siteContext);
    ClientSideText txt1 = new ClientSideText() { Text = pageText };
    page.AddControl(txt1, 0);

    // page will be created if it doesn't exist, otherwise overwritten if it does..
    page.Save(pageName);    

    return name == null
        ? req.CreateResponse(HttpStatusCode.BadRequest, "Please pass a name on the query string or in the request body")
        : req.CreateResponse(HttpStatusCode.OK, "Hello " + name);
}
