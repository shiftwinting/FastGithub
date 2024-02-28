using System.Text.Json.Serialization;

namespace FastGithub.FlowAnalyze
{
    [JsonSerializable(typeof(FlowStatistics))]
    public partial class FlowStatisticsContext : JsonSerializerContext
    {
    }
}
