namespace dto;

using System.Text.Json.Serialization;

public class Response
{
    [JsonPropertyName("type")]
    public Status Status { get; set; }
}