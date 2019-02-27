using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Http.Results;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;

namespace CWX.Files
{
    public static class Upload
    {
        [FunctionName("Upload")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequestMessage req, TraceWriter log, ExecutionContext context)
        {
            var provider = new MultipartMemoryStreamProvider();

            await req.Content.ReadAsMultipartAsync(provider);

            var file = provider.Contents.First();
            var fileInfo = file.Headers.ContentDisposition;
            var fileData = await file.ReadAsByteArrayAsync();

            var connectionString = Environment.GetEnvironmentVariable("BlobConnectionString", EnvironmentVariableTarget.Process);
            var containerName = Environment.GetEnvironmentVariable("ContainerName", EnvironmentVariableTarget.Process);
            var fileName = fileInfo.FileName.TrimStart('\"').TrimEnd('\"');

            var blobName = Guid.NewGuid() + fileName;

            var storageAccount = CloudStorageAccount.Parse(connectionString);
            var client = storageAccount.CreateCloudBlobClient();
            var container = client.GetContainerReference(containerName);

            var blob = container.GetBlockBlobReference(blobName);
            await blob.UploadFromByteArrayAsync(fileData, 0, fileData.Length);

            var url = blob.Uri;

            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(url.AbsoluteUri)
            };
        }
    }
}
