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

        [Function("detail")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)

        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            string id = req.Query["id"];
            if (string.IsNullOrEmpty(id))
            {
                return new BadRequestObjectResult("Please pass an id on the query string");
            }

            var _container = _cosmosClient.GetContainer(Environment.GetEnvironmentVariable("DatabaseName"),
                    Environment.GetEnvironmentVariable("ContainerName"));
            try
            {
                var query = new QueryDefinition("SELECT * FROM c WHERE c.id = @id")
                    .WithParameter("@id", id);

                var iterator = _container.GetItemQueryIterator<dynamic>(query);
                if (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    if (response.Resource.Any())
                    {
                        var movieResponse = JsonConvert.DeserializeObject<MovieResponse>(response.Resource.First().ToString());
                        return new OkObjectResult(movieResponse);
                    }
                }

                return new NotFoundObjectResult("Item not found");
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return new NotFoundObjectResult("Item not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while querying the item with ID: {Id}", id);
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
