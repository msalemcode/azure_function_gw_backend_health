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

            string gatewayResouceId = req.Query["gatwayid"];

            string filter = req.Query["ingnorehealth"];

            if (gatewayResouceId == null)
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("Please pass a name on the query string or in the request body")
                };
            }

            log.LogInformation("Function will check backend for gatway ID =" + gatewayResouceId);

            if (filter != null)
            {
                log.LogInformation("Function will Ingnore the health type =" + filter);

            }

            try
            {


                // Get token
                string token = AuthHelper.GetTokenAsync().Result;
                log.LogInformation($"Token Received: {token}");


                // set url
                string api_url = "https://management.azure.com" + gatewayResouceId + "?api-version=2020-11-01";
                log.LogInformation("Current url : " + api_url);

                // Call first API to get gatway operationID

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var response = await client.PostAsync(api_url, null);
                response.EnsureSuccessStatusCode();
                log.LogInformation("Current Status code" + (response.IsSuccessStatusCode).ToString());
                log.LogInformation("Cheack header");

                string location = response.Headers.GetValues("location").FirstOrDefault(); ;
                log.LogInformation("found location : " + location);


                HttpClient client2 = new HttpClient();
                client2.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                string responseBody = "Content is blank";
                while (true)
                {

                    var httpResponse = client2.GetAsync(location).Result;
                    httpResponse.EnsureSuccessStatusCode();
                    log.LogInformation("Current Status code " + httpResponse.StatusCode.ToString());

                    if (httpResponse.StatusCode.ToString() == "OK")
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



                if (filter != null)
                {
                    log.LogInformation("========================== Function will filter the output now ================================");
                    Root healthpools = JsonConvert.DeserializeObject<Root>(responseBody);
                    Root unhealthpools = JsonConvert.DeserializeObject<Root>(responseBody);

                    Boolean pool = true;
                    while (pool)
                    {
                        pool = false;
                        for (int x = 0; x < healthpools.backendAddressPools.Count; x++)
                        {
                            Boolean col = true;
                            while (col)
                            {
                                col = false;
                                for (int y = 0; y < healthpools.backendAddressPools[x].backendHttpSettingsCollection.Count; y++)
                                {
                                    Boolean server = true;
                                    while (server)
                                    {
                                        server = false;
                                        for (int z = 0; z < healthpools.backendAddressPools[x].backendHttpSettingsCollection[y].servers.Count; z++)
                                        {
                                            if ((healthpools.backendAddressPools[x].backendHttpSettingsCollection[y].servers[z].health.ToUpper() == filter.ToUpper()))
                                            {
                                                healthpools.backendAddressPools[x].backendHttpSettingsCollection[y].servers.RemoveAt(z);
                                                server = true;
                                                log.LogInformation("server from output");
                                            }
                                        }
                                    }

                                    if (healthpools.backendAddressPools[x].backendHttpSettingsCollection[y].servers.Count == 0)
                                    {
                                        healthpools.backendAddressPools[x].backendHttpSettingsCollection.RemoveAt(y);
                                        col = true;
                                        log.LogInformation("Remove collection from output");
                                    }
                                }
                            }
                            if (healthpools.backendAddressPools[x].backendHttpSettingsCollection.Count == 0)
                            {
                                healthpools.backendAddressPools.RemoveAt(x);
                                pool = true;
                                log.LogInformation("Remove Pool from output");
                            }

                        }
                    }

                    responseBody = JsonConvert.SerializeObject(healthpools);

                }


                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
                };

            }
            catch (System.Exception exe)
            {
                log.LogInformation("Error during process the action: {}", exe.Message);
            }

            return null;

        }


        public static string GetEnvironmentVariable(string variableName)
        {
            return Environment.GetEnvironmentVariable(variableName);
        }


    }
}
