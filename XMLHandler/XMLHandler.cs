using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;

namespace XMLHandler
{
    public static class XMLHandler
    {
        [FunctionName("XMLHandler")]
        public static void Run([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            CloudStorageAccount storageAccount;

            //Get Connectionstring from app settings
            string storageConnectionString = Environment.GetEnvironmentVariable("storageconnectionstring");

            try
            {
                CloudStorageAccount.TryParse(storageConnectionString, out storageAccount);
                DownloadFiles(storageAccount);

            }
            catch(Exception)
            {

            }

        }
        private static void DownloadFiles(CloudStorageAccount storageAccount)
        {
            var client = new LidatWebServiceClient();
            var fileLinksTask = client.GetFileLinks(500);
            fileLinksTask.Wait();

            var fileLinks = fileLinksTask.Result;

            while (fileLinks.Count > 0)
            {
                var downloadTask = client.Download(fileLinks, storageAccount);
                downloadTask.Wait();

                fileLinksTask = client.GetFileLinks(500);
                fileLinksTask.Wait();

                fileLinks = fileLinksTask.Result;
            }
        }
    }
}
