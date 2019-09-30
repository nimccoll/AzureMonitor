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
using System.Linq;
using System.Net.Http;
using System.Text;

namespace QueryResourceGraphREST
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
                    // Execute a basic query to return all virtual machines
                    HttpClient httpClient = new HttpClient();
                    httpClient.BaseAddress = new Uri("https://management.azure.com/");
                    string requestUri = "/providers/Microsoft.ResourceGraph/resources?api-version=2019-04-01";
                    string query = $"{{\"subscriptions\": [\"{subscriptionId}\"], \"query\": \"where type =~ 'Microsoft.Compute/virtualMachines'\"}}";
                    StringContent stringContent = new StringContent(query, Encoding.UTF8, "application/json");
                    httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                    HttpResponseMessage queryResponse = httpClient.PostAsync(requestUri, stringContent).Result;
                    if (queryResponse.IsSuccessStatusCode)
                    {
                        Console.WriteLine("*** Basic Query Results ***");
                        string queryResults = queryResponse.Content.ReadAsStringAsync().Result;
                        IEnumerable<string> values;
                        string quotaRemaining = string.Empty;
                        string quotaResetsAfter = string.Empty;
                        if (queryResponse.Headers.TryGetValues("x-ms-user-quota-remaining", out values)) quotaRemaining = values.FirstOrDefault();
                        if (queryResponse.Headers.TryGetValues("x-ms-user-quota-resets-after", out values)) quotaResetsAfter = values.FirstOrDefault();
                        Console.WriteLine($"Quota Remaining: {quotaRemaining}");
                        Console.WriteLine($"Quota Resets After: {quotaResetsAfter}");
                        Console.WriteLine(queryResults);
                        Console.WriteLine();
                    }
                    else
                    {
                        Console.WriteLine("Query Failed!");
                    }

                    // Pagination Example
                    query = $"{{\"subscriptions\": [\"{subscriptionId}\"], \"query\": \"where type =~ 'Microsoft.Compute/virtualMachines'\", \"options\": {{\"$top\": 3,\"$skip\": 0}}}}";
                    stringContent = new StringContent(query, Encoding.UTF8, "application/json");
                    httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                    queryResponse = httpClient.PostAsync(requestUri, stringContent).Result;
                    if (queryResponse.IsSuccessStatusCode)
                    {
                        Console.WriteLine("*** Pagination Results ***");
                        string skipToken = string.Empty;
                        string queryResults = queryResponse.Content.ReadAsStringAsync().Result;
                        IEnumerable<string> values;
                        string quotaRemaining = string.Empty;
                        string quotaResetsAfter = string.Empty;
                        if (queryResponse.Headers.TryGetValues("x-ms-user-quota-remaining", out values)) quotaRemaining = values.FirstOrDefault();
                        if (queryResponse.Headers.TryGetValues("x-ms-user-quota-resets-after", out values)) quotaResetsAfter = values.FirstOrDefault();
                        Console.WriteLine($"Quota Remaining: {quotaRemaining}");
                        Console.WriteLine($"Quota Resets After: {quotaResetsAfter}");
                        Console.WriteLine(queryResults);
                        Console.WriteLine();
                        JObject resultsObject = JObject.Parse(queryResults);
                        if (resultsObject.ContainsKey("$skipToken"))
                        {
                            skipToken = resultsObject["$skipToken"].Value<string>();
                        }
                        while (!string.IsNullOrEmpty(skipToken))
                        {
                            query = $"{{\"subscriptions\": [\"{subscriptionId}\"], \"query\": \"where type =~ 'Microsoft.Compute/virtualMachines'\", \"options\": {{\"$skipToken\": \"{skipToken}\"}}}}";
                            stringContent = new StringContent(query, Encoding.UTF8, "application/json");
                            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                            queryResponse = httpClient.PostAsync(requestUri, stringContent).Result;
                            if (queryResponse.IsSuccessStatusCode)
                            {
                                queryResults = queryResponse.Content.ReadAsStringAsync().Result;
                                if (queryResponse.Headers.TryGetValues("x-ms-user-quota-remaining", out values)) quotaRemaining = values.FirstOrDefault();
                                if (queryResponse.Headers.TryGetValues("x-ms-user-quota-resets-after", out values)) quotaResetsAfter = values.FirstOrDefault();
                                Console.WriteLine($"Quota Remaining: {quotaRemaining}");
                                Console.WriteLine($"Quota Resets After: {quotaResetsAfter}");
                                Console.WriteLine(queryResults);
                                Console.WriteLine();
                                resultsObject = JObject.Parse(queryResults);
                                if (resultsObject.ContainsKey("$skipToken"))
                                {
                                    skipToken = resultsObject["$skipToken"].Value<string>();
                                }
                                else
                                {
                                    skipToken = string.Empty;
                                }
                            }
                            else
                            {
                                Console.WriteLine("Skip Token Query Failed!");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("Paginated Query Failed!");
                    }

                    // Change Tracking Example
                    requestUri = "/providers/Microsoft.ResourceGraph/resourceChanges?api-version=2018-09-01-preview";
                    DateTime endDateTime = DateTime.UtcNow;
                    DateTime startDateTime = endDateTime.AddDays(-7);
                    string startDateTimeString = startDateTime.ToString("yyyy-MM-ddTHH:mm:ss.sssZ");
                    string endDateTimeString = endDateTime.ToString("yyyy-MM-ddTHH:mm:ss.sssZ");
                    query = $"{{\"resourceId\": \"{resourceId}\", \"interval\": {{\"start\": \"{startDateTimeString}\", \"end\": \"{endDateTimeString}\"}}}}";
                    stringContent = new StringContent(query, Encoding.UTF8, "application/json");
                    httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                    queryResponse = httpClient.PostAsync(requestUri, stringContent).Result;
                    if (queryResponse.IsSuccessStatusCode)
                    {
                        Console.WriteLine("*** Change Tracking Results ***");
                        string queryResults = queryResponse.Content.ReadAsStringAsync().Result;
                        JObject resultsObject = JObject.Parse(queryResults);
                        foreach (JToken change in resultsObject["changes"])
                        {
                            string changeId = change["changeId"].Value<string>();
                            requestUri = "/providers/Microsoft.ResourceGraph/resourceChangeDetails?api-version=2018-09-01-preview";
                            changeId = changeId.Replace("\"", "\\\"");
                            query = $"{{\"resourceId\": \"{resourceId}\", \"changeId\": \"{changeId}\"}}";
                            stringContent = new StringContent(query, Encoding.UTF8, "application/json");
                            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                            queryResponse = httpClient.PostAsync(requestUri, stringContent).Result;
                            if (queryResponse.IsSuccessStatusCode)
                            {
                                queryResults = queryResponse.Content.ReadAsStringAsync().Result;
                                IEnumerable<string> values;
                                string quotaRemaining = string.Empty;
                                string quotaResetsAfter = string.Empty;
                                if (queryResponse.Headers.TryGetValues("x-ms-user-quota-remaining", out values)) quotaRemaining = values.FirstOrDefault();
                                if (queryResponse.Headers.TryGetValues("x-ms-user-quota-resets-after", out values)) quotaResetsAfter = values.FirstOrDefault();
                                Console.WriteLine($"Quota Remaining: {quotaRemaining}");
                                Console.WriteLine($"Quota Resets After: {quotaResetsAfter}");
                                Console.WriteLine(queryResults);
                                Console.WriteLine();
                            }
                        }
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
