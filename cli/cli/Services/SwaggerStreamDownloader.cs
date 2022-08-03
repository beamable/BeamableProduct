namespace cli;

public interface ISwaggerStreamDownloader
{
	Task<Stream> GetStreamAsync(string url);
}

public class SwaggerStreamDownloader : HttpClient, ISwaggerStreamDownloader
{

}
