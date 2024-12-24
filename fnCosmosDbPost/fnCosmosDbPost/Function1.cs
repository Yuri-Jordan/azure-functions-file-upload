using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fnCosmosDbPost
{
    public class Function1
    {
        private readonly ILogger<Function1> _logger;

        public Function1(ILogger<Function1> logger)
        {
            _logger = logger;
        }

        [Function("movies")]
        public async Task<object?> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var endpointUri = Environment.GetEnvironmentVariable("EndPointUri");
            var primaryKey = Environment.GetEnvironmentVariable("CdbConnection");
            var databaseId = Environment.GetEnvironmentVariable("DatabaseName");
            var containerId = Environment.GetEnvironmentVariable("ContainerName");

            var cosmosClient = new CosmosClient(endpointUri, primaryKey);
            var database = await cosmosClient.CreateDatabaseIfNotExistsAsync(databaseId);
            var container = await database.Database.CreateContainerIfNotExistsAsync(containerId, "/TenantId");

            MoviesRequest movieRequest = new MoviesRequest();
            var content = await new StreamReader(req.Body).ReadToEndAsync();

            try
            {
                movieRequest = JsonConvert.DeserializeObject<MoviesRequest>(content);
            }
            catch (Exception ex)
            {

                _logger.LogError($"Erro ao descerializar objeto: {ex.Message}");
                return new BadRequestObjectResult("Erro ao enviar objeto ao cosmos");
            }


            try
            {
                var response = await container.Container.CreateItemAsync(movieRequest, partitionKey: new PartitionKey(movieRequest.TenantId));
                _logger.LogInformation($"Item created with id: {response.Resource.TenantId}");
            }
            catch (CosmosException ex)
            {
                _logger.LogError($"Erro ao enviar objeto: {ex.Message}");
                return new BadRequestObjectResult("Erro ao enviar objeto ao cosmos");
            }

            return JsonConvert.SerializeObject(movieRequest);
        }
    }
}
