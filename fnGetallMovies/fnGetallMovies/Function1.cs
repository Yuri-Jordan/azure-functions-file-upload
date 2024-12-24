using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fnGetMovieDetail
{
    public class Function1
    {
        private readonly ILogger<Function1> _logger;
        private readonly CosmosClient _cosmosClient;

        public Function1(ILogger<Function1> logger, CosmosClient cosmosClient)
        {
            _logger = logger;
            _cosmosClient = cosmosClient;
        }

        [Function("all")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)

        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var _container = _cosmosClient.GetContainer(Environment.GetEnvironmentVariable("DatabaseName"),
                    Environment.GetEnvironmentVariable("ContainerName"));
            try
            {
                var query = new QueryDefinition("SELECT * FROM c");
                var iterator = _container.GetItemQueryIterator<MovieResponse>(query);
                var results = new List<MovieResponse>();

                while (iterator.HasMoreResults)
                {
                    foreach (var item in await iterator.ReadNextAsync())
                    {
                        results.Add(item);
                    }
                }

                if (results.Any())
                {
                    return new OkObjectResult(results);
                }

                return new NotFoundObjectResult("Items not found");
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return new NotFoundObjectResult("Items not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while querying the items");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
