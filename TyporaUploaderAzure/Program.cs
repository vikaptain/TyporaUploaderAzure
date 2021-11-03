using Azure.Storage.Blobs.Models;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace TyporaUploaderAzure
{
	internal class Program
	{
		private static async Task Main(string[] args)
		{
			if (args.Length < 2)
			{
				throw new Exception("未设置链接字符串和图片");
			}
			var connectionString = args[0];

			var serviceClient = new Azure.Storage.Blobs.BlobServiceClient(connectionString: connectionString);

			var containerClient = serviceClient.GetBlobContainerClient(blobContainerName: "noteimages");

			var files = args[1..];

			var tasks = files.Select(async file =>
				  {
					  var fi = new FileInfo(file);
					  var blobName = $"{DateTime.Now:yyyyMMdd}/{Guid.NewGuid()}{fi.Extension}";

					  var blobClient = containerClient.GetBlobClient(blobName);

					  using var hashAlgo = MD5.Create();

					  var bytes = await File.ReadAllBytesAsync(path: fi.FullName);

					  string mimeType(string extension) => extension switch
					  {
						  ".png" => "image/png",
						  ".jpeg" => "image/jpeg",
						  ".jpg" => "image/jpeg",
						  _ => "application/octet-stream",
					  };

					  if (!await blobClient.ExistsAsync())
					  {
						  await using var ms = new MemoryStream(bytes);
						  await blobClient.UploadAsync(ms);

						  var headers = new BlobHttpHeaders
						  {
							  ContentType = mimeType(fi.Extension),
						  };

						  await blobClient.SetHttpHeadersAsync(headers);
					  }

					  return blobClient.Uri.AbsoluteUri;
				  }
			).ToList();

			await Task.WhenAll(tasks);

			tasks
				.Select(t => t.Result)
				.ToList()
				.ForEach(Console.WriteLine);
		}
	}
}