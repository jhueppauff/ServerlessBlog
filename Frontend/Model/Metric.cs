using System.Text.Json.Serialization;

namespace ServerlessBlog.Frontend.Model
{

    public class Metric
    {
        /// <summary>
        /// Slug
        /// </summary>
        [JsonPropertyName("slug")]
        public string Slug { get; set; }

        /// <summary>
        /// Session Id
        /// </summary>
        [JsonPropertyName("sessionId")]
        public string SessionId { get; set; }
    }
}