using System;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using GatwaybackendHealth.Models;
using GatwaybackendHealth.Util;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace GatwaybackendHealth
{
    public static class GetBackendHealthTrigger
    {
        private static readonly HttpClient client = new HttpClient();

        [FunctionName("GatewayBackendHealth")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log
            )
        {

                string gatewayResouceId=req.Query["gatwayid"];

                string filter=req.Query["ingnorehealth"];

                if(gatewayResouceId == null)
                {
                    return new  HttpResponseMessage(HttpStatusCode.BadRequest) {
                       Content = new StringContent("Please pass a name on the query string or in the request body")
                    };
                }
                    
        
         

                log.LogInformation("Function will check backend for gatway ID =" + gatewayResouceId);
                try
                {

                    // Get token
                    string token = AuthHelper.GetTokenAsync().Result;
                    log.LogInformation($"Token Received: {token}");


                    // set url
                    string api_url="https://management.azure.com"+gatewayResouceId+"?api-version=2020-11-01";
                    log.LogInformation("Current url : "+api_url);
                    
                    // Call first API to get gatway operationID
                    
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    var response = await client.PostAsync(api_url,null);
                    response.EnsureSuccessStatusCode();
                    log.LogInformation("Current Status code"+(response.IsSuccessStatusCode).ToString());
                    log.LogInformation("Cheack header");

                    string location = response.Headers.GetValues("location").FirstOrDefault();;
                    log.LogInformation("found location : " + location);

                    
                    HttpClient client2 = new HttpClient();
                    client2.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    string responseBody ="Content is blank";
                    while(true)
                    {

                        var httpResponse = client2.GetAsync(location).Result;
                        httpResponse.EnsureSuccessStatusCode();
                        log.LogInformation("Current Status code "+httpResponse.StatusCode.ToString());
                        
                        if (httpResponse.StatusCode.ToString()=="OK")
                        {
                            responseBody = httpResponse.Content.ReadAsStringAsync().Result;
                            break;
                        }
                        else
                        {
                            location = httpResponse.Headers.GetValues("location").FirstOrDefault();
                            Thread.Sleep(100);
                        }
                        


                    }
                    log.LogInformation(responseBody);



                    if(filter != null)
                    {
                        if((filter.ToUpper()=="UP") || (filter.ToUpper()=="HEALTHY"))
                        {
                            Boolean found =false;
                            Boolean foundglobal =false;
                            Root unhealthypools = new Root();
                            Root healthpools = JsonConvert.DeserializeObject<Root>(responseBody);
                            foreach(BackendAddressPool pool in healthpools.backendAddressPools )
                            {  
                                found =false;
                                BackendAddressPool unBackendAddressPool = new BackendAddressPool();
                                unBackendAddressPool.backendAddressPool=pool.backendAddressPool;
                                
                                foreach(BackendHttpSettingsCollection col in pool.backendHttpSettingsCollection)
                                {
                                    BackendHttpSettingsCollection unBackendHttpSettingsCollection = new BackendHttpSettingsCollection();
                                    unBackendHttpSettingsCollection.backendHttpSettings=col.backendHttpSettings;

                                    foreach(Server s in col.servers)
                                    {
                                        if ((s.health!="HEALTHY") & (s.health!="UP"))
                                        {
                                            found = true;
                                            foundglobal = true;
                                            Server unServer = new Server();
                                            unBackendHttpSettingsCollection.servers.Add(unServer);
                                        }
                                    }

                                    if (found)
                                    {
                                        unBackendAddressPool.backendHttpSettingsCollection.Add(unBackendHttpSettingsCollection);
                                    }   

                                }

                                    if (found)
                                    {
                                        unhealthypools.backendAddressPools.Add(unBackendAddressPool);
                                    }


                            }

                            if(foundglobal)
                            {
                                responseBody = JsonConvert.SerializeObject(unhealthypools);
                            }
                        }
                    }

                    return new HttpResponseMessage(HttpStatusCode.OK) {
                        Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
                    };
                                
                }
                catch (System.Exception exe)
                {
                    log.LogInformation("Error during process the action: {}",exe.Message);
                }

                return null;
                
        }


        public static string GetEnvironmentVariable(string variableName)
        {
            return Environment.GetEnvironmentVariable(variableName);
        }

   
    }
}
