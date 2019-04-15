using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;


namespace XMLHandler
{
    class LidatWebServiceClient
    {
        private HttpClient _httpClient = null;

        private Uri BaseAdress { get; } = new Uri(@"https://www.yourwebsite.com");

        /// <summary>
        /// The Header for basic authentication
        /// </summary>
        private AuthenticationHeaderValue AuthenticationHeader { get; } = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes("USER:PASSWORD")));
        private HttpClient BuildClient(int? timoutInSeconds = null)
        {
            _httpClient = new HttpClient();

            _httpClient.DefaultRequestHeaders.Authorization = AuthenticationHeader;
            _httpClient.BaseAddress = BaseAdress;

            if (timoutInSeconds != null && timoutInSeconds.HasValue && timoutInSeconds.Value < 60)
            {
                _httpClient.Timeout = TimeSpan.FromSeconds(timoutInSeconds.Value);
            }

            return _httpClient;
        }
        public async Task<IList<FileLink>> GetFileLinks(int limit)
        {
            try
            {
                var httpClient = BuildClient();
                var responseStream = await httpClient.GetStreamAsync($"/Sercice/Service.svc/service/{limit}");

                XElement xelement = XElement.Load(responseStream);
                IEnumerable<XElement> fileLinkElements = xelement.Descendants("File");

                var fileLinks = fileLinkElements.Select(fileLinkElement => new FileLink
                {
                    Id = fileLinkElement.Element("Id").Value,
                    CreationTime = fileLinkElement.Element("CreationTime").Value,
                    Name = fileLinkElement.Element("Name").Value,
                    FileType = fileLinkElement.Element("FileType").Value,
                    Link = fileLinkElement.Element("Link").Value,
                    FileSize = fileLinkElement.Element("FileSize").Value,
                    Checksum32 = fileLinkElement.Element("Checksum32").Value,
                }).ToList();

                return fileLinks;
            }
            catch
            {
                return new List<FileLink>();
            }
        }
        public async Task Download(IEnumerable<FileLink> fileLinks, CloudStorageAccount storageAccount)
        {
            var httpClient = BuildClient();

            try
            {
                foreach (var fileLink in fileLinks)
                {
                    string getUrl = $"/Service/Service.svc/File/{fileLink.Id}";
                    var responseString = await httpClient.GetStringAsync(getUrl);

                    if (!string.IsNullOrWhiteSpace(responseString))
                    {
                        /*
                        XElement root = XElement.Parse(responseString);
                        XElement origin = root.Element("Origin");
                        XElement timestamp = origin.Element("TimeStamp");

                        var fileCreatedPath = timestamp.Value.Substring(0, 10).Replace("-", "");
                        var fileDownloadedPath = DateTime.Now.ToString("yyyyMMdd");

                        var tempFile = Path.Combine(path, $"{fileLink.Id}_{fileLink.CreationTime.Substring(0, 10)}.xml");
                        File.WriteAllText(tempFile, responseString);
                        */

                        string filename = $"{fileLink.Id}_{fileLink.CreationTime.Substring(0, 10)}.xml";
                        //Update Storage Queue
                        UpdateQueue(responseString, storageAccount);
                        //Upload file to Blob
                        UpdateBlob(responseString, storageAccount, filename);
                    }
                }
            }
            catch (DirectoryNotFoundException)
            {

            }
            catch (Exception)
            {

            }
        }
        private static async void UpdateQueue(string response, CloudStorageAccount storageAccount)
        {
            try
            {
                CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
                CloudQueue queue = queueClient.GetQueueReference("tobetransformed");
                CloudQueueMessage message = new CloudQueueMessage(response);

                await queue.AddMessageAsync(message);

            }
            catch (Exception)
            {
            }

        }
        private static async void UpdateBlob(string response, CloudStorageAccount storageAccount, string filename)
        {
            try
            {
                CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference("xml");
                var blockBlob = cloudBlobContainer.GetBlockBlobReference(filename);

                await blockBlob.UploadTextAsync(response);

            }
            catch (Exception)
            {
            }

        }
    }

    
    public class FileLink
    {
        public string Id { get; set; }

        public string CreationTime { get; set; }

        public string Name { get; set; }

        public string FileType { get; set; }

        public string Link { get; set; }

        public string FileSize { get; set; }

        public string Checksum32 { get; set; }
    }
}
