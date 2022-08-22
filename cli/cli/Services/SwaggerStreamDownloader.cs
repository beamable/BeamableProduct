namespace cli;

/// <summary>
/// An interface that abstracts away HOW we download streams. Maybe its an HTTP stream... maybe its a test stream... who could say?
/// </summary>
public interface ISwaggerStreamDownloader
{
	Task<Stream> GetStreamAsync(string url);
}

public class SwaggerStreamDownloader : HttpClient, ISwaggerStreamDownloader
{

}
