using cli.Services;
using Serilog;
using System.CommandLine;
using System.IO.Compression;

namespace cli.UnityCommands;

public class DownloadNugetDepToUnityCommandArgs : CommandArgs
{
	public string packageId;
	public string packageVersion;
	public string packageSrcPath;
	public string outputPath;
}

public class DownloadNugetDepToUnityCommandOutput
{
	
}
public class DownloadNugetDepToUnityCommand : AtomicCommand<DownloadNugetDepToUnityCommandArgs, DownloadNugetDepToUnityCommandOutput>
{
	public override bool IsForInternalUse => true;

	public DownloadNugetDepToUnityCommand() : base("download-nuget-package", "Download a beamable nuget package dep into Unity ")
	{
	}

	public override void Configure()
	{
		AddArgument(new Argument<string>("packageId", "the nuget id of the package dep"), (args, i) => args.packageId = i);
		AddArgument(new Argument<string>("packageVersion", "the version of the package"), (args, i) => args.packageVersion = i);
		AddArgument(new Argument<string>("src", "the file path inside the package to copy"), (args, i) => args.packageSrcPath = i);
		AddArgument(new Argument<string>("dst", "the target location to place the copied files"), (args, i) => args.outputPath = i);
	}

	public override async Task<DownloadNugetDepToUnityCommandOutput> GetResult(DownloadNugetDepToUnityCommandArgs args)
	{
		await DownloadPackage(args.packageId, args.packageVersion, args.packageSrcPath, args.outputPath);
		return new DownloadNugetDepToUnityCommandOutput();
	}
	
	
	
	// this method is adapted from a ChatGpt 3.5 response.
	public static async Task DownloadPackage(string packageId, string packageVersion, string packageSrc, string outputPath)
	{
		var packageUrl = $"https://www.nuget.org/api/v2/package/{packageId}/{packageVersion}";
		var detailUrl = $"https://www.nuget.org/packages/{packageId}/{packageVersion}";
		
		ReleaseSharedUnityCodeCommand.DeleteAllFilesWithExtensions(outputPath, new string[]{".cs", ".cs.meta"});

		using (HttpClient client = new HttpClient())
		{
			// Download the zip file as a byte array
			byte[] zipBytes = await client.GetByteArrayAsync(packageUrl);

			// Extract files in memory without saving the entire archive to disk
			using (MemoryStream memoryStream = new MemoryStream(zipBytes))
			{
				using (ZipArchive zipArchive = new ZipArchive(memoryStream))
				{
					// Iterate through each entry in the zip archive
					Log.Debug($"checking log entries for prefix=[{packageSrc}]");
					var tasks = new List<Task>();
					foreach (ZipArchiveEntry entryIterator in zipArchive.Entries)
					{
						var entry = entryIterator; // capture.
						// Check if the entry is one of the files we want to save
						if (entry.FullName.StartsWith(packageSrc))
						{
							var relativePath = entry.FullName.Substring(packageSrc.Length);
							string filePath = Path.Combine(outputPath, relativePath);

							// Ensure the directory for the file exists
							Directory.CreateDirectory(Path.GetDirectoryName(filePath));

							// Extract the entry to a file on disk
							
							{
								try
								{
									if (File.Exists(filePath)) File.Delete(filePath);
									
									using (Stream entryStream = entry.Open())
									using (FileStream fileStream = File.Create(filePath))
									using (StreamWriter writer = new StreamWriter(fileStream))
									{
										await writer.WriteLineAsync(
											$"// this file was copied from nuget package {packageId}@{packageVersion}\n// {detailUrl}\n");
										await writer.FlushAsync();
										await entryStream.CopyToAsync(fileStream);
									}
									
									Log.Debug($"Extracted and saved: {entry.FullName} to {filePath}");
									
									var metaDesc = UnityCliGenerator.GenerateMetaFile(filePath);
									await File.WriteAllTextAsync(metaDesc.FileName, metaDesc.Content);
									Log.Debug($"Saved meta file: {metaDesc.FileName}");

								}
								catch (Exception ex)
								{
									Log.Warning($"Failed to handle {entry.FullName}. {ex.GetType().Name} - {ex.Message}");
									throw new CliException(
										$"Failed to handle {entry.FullName}. {ex.GetType().Name} - {ex.Message}");
								}
							}

						}
						else
						{
							Log.Debug($"skipping {entry.FullName}");
						}
					}

				}
			}
		}
	}

}
