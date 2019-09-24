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
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using System;

namespace QueryVMActivityLog
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

            // Authenticate and connect to your Azure subscription
            var credentials = SdkContext.AzureCredentialsFactory.FromServicePrincipal(clientId, clientSecret, tenantId, AzureEnvironment.AzureGlobalCloud);
            var azure = Azure
                .Configure()
                .Authenticate(credentials)
                .WithSubscription(subscriptionId);

            // Query activity for the last 7 days - adjust the timeframe as needed
            DateTime recordDateTime = DateTime.Now;

            var logs = azure.ActivityLogs.DefineQuery()
                    .StartingFrom(recordDateTime.AddDays(-7).ToUniversalTime())
                    .EndsBefore(recordDateTime.ToUniversalTime())
                    .WithAllPropertiesInResponse()
                    .FilterByResource(resourceId)
                    .Execute();

            foreach (var eventData in logs)
            {
                // Extract the successful start and stop virtual machine events
                if ((eventData.OperationName.LocalizedValue.Contains("Start Virtual Machine")
                    || eventData.OperationName.LocalizedValue.Contains("Deallocate Virtual Machine"))
                    && eventData.Status.LocalizedValue == "Succeeded")
                {
                    Console.WriteLine("\tEvent: " + eventData.EventName?.LocalizedValue);
                    Console.WriteLine("\tOperation: " + eventData.OperationName?.LocalizedValue);
                    Console.WriteLine("\tCaller: " + eventData.Caller);
                    Console.WriteLine("\tCorrelationId: " + eventData.CorrelationId);
                    Console.WriteLine("\tSubscriptionId: " + eventData.SubscriptionId);
                    Console.WriteLine("\tEventTimeStamp: " + eventData.EventTimestamp);
                    Console.WriteLine("\tStatus: " + eventData.Status.LocalizedValue);
                    Console.WriteLine();
                }
            }
        }
    }
}
