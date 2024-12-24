using Newtonsoft.Json;

namespace fnCosmosDbPost
{
    internal class MoviesRequest
    {
        //[JsonProperty("id")]
        public string id { get { return Guid.NewGuid().ToString("N"); } }
        public string TenantId { get { return "movies"; } }
        public string Title { get; set; }
        public string Year { get; set; }
        public string Video { get; set; }
        public string Thumb { get; set; }
    }
}
