namespace Assistant.Domain.Itineraries;

public class Place
{
    public required string Reference { get; set; }
    public required string Uri { get; set; }
    public required string SearchQuery { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required float Latitude { get; set; }
    public required float Longitude { get; set; }
}