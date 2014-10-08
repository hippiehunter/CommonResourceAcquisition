using CommonResourceAcquisition.ImageAcquisition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonResourceAcquisition.VideoAcquisition
{
	public class Liveleak
	{
		public static IVideoResult GetVideoResult(string originalUrl)
		{
			return new VideoResult(originalUrl);
		}

		internal class VideoResult : IVideoResult
		{
			Lazy<Task<string>> _pageContents;
			public VideoResult(string originalUrl)
			{
				_pageContents = new Lazy<Task<string>>(() =>
				{
					if (originalUrl.Contains("&ajax=1"))
						return HttpClientUtility.Get(originalUrl);
					else
						return HttpClientUtility.Get(originalUrl + "&ajax=1");
				});
			}
			public async Task<string> PreviewUrl(System.Threading.CancellationToken cancelToken)
			{
				
				var pageContents = await _pageContents.Value;
				if (pageContents != null)
				{
					var targetString = "image: \"http://edge.liveleak.com";
					var targetStringStart = pageContents.IndexOf(targetString);
					if (targetStringStart != -1)
					{
						var endofTargetString = pageContents.IndexOf("\",", targetStringStart);
						if (endofTargetString != -1)
						{
							targetStringStart += "image: \"".Length;
							var fileUrl = pageContents.Substring(targetStringStart, endofTargetString - targetStringStart);
							return fileUrl;
						}
					}
				}
				return null;
			}

			public async Task<IEnumerable<Tuple<string, string>>> PlayableStreams(System.Threading.CancellationToken cancelToken)
			{
				var pageContents = await _pageContents.Value;
				if (pageContents != null)
				{
					var targetString = "file: \"http://edge.liveleak.com";
					var targetStringStart = pageContents.IndexOf(targetString);
					if (targetStringStart != -1)
					{
						var endofTargetString = pageContents.IndexOf("\",", targetStringStart);
						if (endofTargetString != -1)
						{
							targetStringStart += "file: \"".Length;
							var fileUrl = pageContents.Substring(targetStringStart, endofTargetString - targetStringStart);
							return new List<Tuple<string, string>> { Tuple.Create(fileUrl, "mp4") };
						}
					}
				}
				return Enumerable.Empty<Tuple<string, string>>();
			}
		}
	}
}
