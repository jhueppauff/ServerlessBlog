namespace Engine
{
    public class ImageProcessor
    {

        [FunctionName(nameof(UploadImage))]
        public static async Task UploadImage([HttpTrigger(AuthorizationLevel.Anonymous, Route = "Image/Upload/{extension}")] HttpRequest request, string extension,
        [Blob("public", FileAccess.Write, Connection = "AzureStorageConnection")]CloudBlobContainer container, ILogger log)
        {
            log.LogInformation("Triggered Upload Function");

            BlobClient blobClient = container.GetBlobClient($"{Guid.NewGuid().ToString()}.{extension}");

            new FileExtensionContentTypeProvider().TryGetContentType($"{Guid.NewGuid()}.{extension}", out string contentType);

            using (Stream stream = await request.Form.Files[0].OpenReadStream())
            {
                await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = contentType });
            }
        }
    }
}