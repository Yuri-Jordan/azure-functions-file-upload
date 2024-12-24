using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FunctionApp1
{
    public class Function1
    {
        private readonly ILogger<Function1> _logger;
        private string[] _allowedFileTypes = { "imagens", "videos" };

        public Function1(ILogger<Function1> logger)
        {
            _logger = logger;
        }


        [Function("Function1")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            try
            {
                if (!req.Headers.TryGetValue("file-type", out var fileTypeHeader))
                {
                    return new BadRequestObjectResult("Header file-type obrigatório!");
                }
                var fileType = fileTypeHeader.ToString();

                if (!_allowedFileTypes.Contains(fileType))
                {
                    return new BadRequestObjectResult("Header file-type inválido!");
                }

                var form = await req.ReadFormAsync();
                var file = form.Files["file"];
                if (file == null || file.Length == 0)
                {
                    return new BadRequestObjectResult("File obrigatório!");
                }

                var cString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
                var containerName = fileType;
                var blobContainerClient = new BlobContainerClient(cString, containerName);

                await blobContainerClient.CreateIfNotExistsAsync();
                await blobContainerClient.SetAccessPolicyAsync(PublicAccessType.BlobContainer);

                var blobName = file.FileName;
                var blob = blobContainerClient.GetBlobClient(blobName);

                using (var s = file.OpenReadStream())
                {
                    await blob.UploadAsync(s);
                }

                _logger.LogInformation($"Arquivo {file.FileName} armazenado com sucesso.");

                return new OkObjectResult(new
                {
                    Message = "Arquivo armazenado",
                    BlobUri = blob.Uri,
                });
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult("Erro inesperado!");
            }
        }
    }
}
