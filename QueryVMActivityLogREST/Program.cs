//===============================================================================
// Microsoft FastTrack for Azure
// Azure Monitor Samples
//===============================================================================
// Copyright © Microsoft Corporation.  All rights reserved.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
// OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE.
//===============================================================================
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;

namespace QueryVMActivityLogREST
{
    class Program
    {
        static void Main(string[] args)
        {
            // Populate with the appropriate values for your environment
            string clientId = "{your client ID here}";
            string clientSecret = "{your client secret here}";
            string tenantId = "{your tenant ID here}";
            string subscriptionId = "{your subscription ID here}";
            string resourceId = "/subscriptions/{your subscription ID here}/resourceGroups/{your resource group name here}/providers/Microsoft.Compute/virtualMachines/{your VM name here}";
            string accessToken = string.Empty;

            // Authenticate
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri($"https://login.microsoftonline.com");
            List<KeyValuePair<string, string>> postBody = new List<KeyValuePair<string, string>>();
            postBody.Add(new KeyValuePair<string, string>("grant_type", "client_credentials"));
            postBody.Add(new KeyValuePair<string, string>("client_id", clientId));
            postBody.Add(new KeyValuePair<string, string>("client_secret", clientSecret));
            postBody.Add(new KeyValuePair<string, string>("resource", "https://management.core.windows.net/"));

            FormUrlEncodedContent content = new FormUrlEncodedContent(postBody);
            HttpResponseMessage response = client.PostAsync($"/{tenantId}/oauth2/token", content).Result;
            if (response.IsSuccessStatusCode)
            {
                Stream responseStream = response.Content.ReadAsStreamAsync().Result;
                using (StreamReader streamReader = new StreamReader(responseStream))
                {
                    string responseContent = streamReader.ReadToEnd();
                    JObject responseObject = JObject.Parse(responseContent);
                    accessToken = responseObject["access_token"].Value<string>();
                }

                if (!string.IsNullOrEmpty(accessToken))
                {
                    // Query activity for the last 7 days - adjust the timeframe as needed
                    HttpClient httpClient = new HttpClient();
                    httpClient.BaseAddress = new Uri("https://management.azure.com/");
                    DateTime endDateTime = DateTime.UtcNow;
                    DateTime startDateTime = endDateTime.AddDays(-7);
                    string startDateTimeString = startDateTime.ToString("yyyy-MM-ddTHH:mm:ss.sssZ");
                    string endDateTimeString = endDateTime.ToString("yyyy-MM-ddTHH:mm:ss.sssZ");
                    string filter = $"eventTimestamp ge '{startDateTimeString}' and eventTimestamp le '{endDateTimeString}' and resourceUri eq '{resourceId}'";
                    string requestUri = $"/subscriptions/{subscriptionId}/providers/microsoft.insights/eventtypes/management/values?api-version=2015-04-01&$filter={filter}";
                    httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                    HttpResponseMessage logResponse = httpClient.GetAsync(requestUri).Result;
                    if (logResponse.IsSuccessStatusCode)
                    {
                        Stream logResponseStream = logResponse.Content.ReadAsStreamAsync().Result;
                        using (StreamReader streamReader = new StreamReader(logResponseStream))
                        {
                            string logContent = streamReader.ReadToEnd();
                            JObject logEntries = JObject.Parse(logContent);
                            foreach (KeyValuePair<string, JToken> logEntry in logEntries)
                            {
                                foreach (JToken activity in logEntry.Value)
                                {
                                    string operationName = activity["operationName"]["localizedValue"].Value<string>();
                                    string status = activity["status"]["localizedValue"].Value<string>();

                                    // Extract the successful start and stop virtual machine events
                                    if ((operationName.Contains("Start Virtual Machine")
                                        || operationName.Contains("Deallocate Virtual Machine"))
                                        && status == "Succeeded")
                                    {
                                        Console.WriteLine("\tEvent: " + activity["eventName"]["localizedValue"].Value<string>());
                                        Console.WriteLine("\tOperation: " + operationName);
                                        Console.WriteLine("\tCaller: " + activity["caller"].Value<string>());
                                        Console.WriteLine("\tCorrelationId: " + activity["correlationId"].Value<string>());
                                        Console.WriteLine("\tSubscriptionId: " + activity["subscriptionId"].Value<string>());
                                        Console.WriteLine("\tEventTimeStamp: " + activity["eventTimestamp"].Value<string>());
                                        Console.WriteLine("\tStatus: " + status);
                                        Console.WriteLine();
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("Management API call Failed!");
                    }
                }
            }
            else
            {
                Console.WriteLine("Authentication Failed!");
            }
        }
    }
}
