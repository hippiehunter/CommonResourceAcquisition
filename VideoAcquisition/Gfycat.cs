using CommonResourceAcquisition.ImageAcquisition;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CommonResourceAcquisition.VideoAcquisition
{
	public class Gfycat
	{
		public static IVideoResult GetVideoResult(string originalUrl)
		{
			return new VideoResult(originalUrl);
		}

		private static Regex gfyRegex = new Regex(@"^https?:\/\/[a-zA-Z0-9\-\.]*gfycat\.com\/(\w+)\.?");
		public static string GetGfyName(string originalUrl)
		{
			if (gfyRegex.IsMatch(originalUrl))
			{
				return gfyRegex.Match(originalUrl).Groups[1].Value;
			}
			else
				return null;
		}

		internal class VideoResult : IVideoResult
		{
			public class GfyItem
			{
				public string gfyId { get; set; }
				public string gfyName { get; set; }
				public string gfyNumber { get; set; }
				public string userName { get; set; }
				public int width { get; set; }
				public int height { get; set; }
				public int frameRate { get; set; }
				public int numFrames { get; set; }
				public string mp4Url { get; set; }
				public string webmUrl { get; set; }
				public string gifUrl { get; set; }
				public int gifSize { get; set; }
				public int mp4Size { get; set; }
				public int webmSize { get; set; }
				public string createDate { get; set; }
				public string views { get; set; }
				public object title { get; set; }
				public object md5 { get; set; }
				public object tags { get; set; }
				public object nsfw { get; set; }
				public object sar { get; set; }
				public object url { get; set; }
				public object source { get; set; }
				public object dynamo { get; set; }
				public object uploadGifName { get; set; }
			}

			public class GfyResult
			{
				public GfyItem gfyItem { get; set; }
			}
			string _originalUrl;
			public VideoResult(string originalUrl)
			{
				_originalUrl = originalUrl;
			}

			public Task<string> PreviewUrl(IResourceNetworkLayer networkLayer, IProgress<float> progress, System.Threading.CancellationToken cancelToken)
			{
				string gfyName = GetGfyName(_originalUrl);
				var thumbUrl = string.Format("http://thumbs.gfycat.com/{0}-poster.jpg", gfyName);
				return Task.FromResult(thumbUrl);
			}

			public async Task<IEnumerable<Tuple<string, string>>> PlayableStreams(IResourceNetworkLayer networkLayer, IProgress<float> progress, System.Threading.CancellationToken cancelToken)
			{
				string gfyName = GetGfyName(_originalUrl);
				var jsonResult = await networkLayer.Get("http://gfycat.com/cajax/get/" + gfyName, cancelToken, progress, null, false);
				var gfyResult = JsonConvert.DeserializeObject<GfyResult>(jsonResult);
				return new List<Tuple<string, string>> { Tuple.Create(gfyResult.gfyItem.mp4Url, "MP4") };
			}
		}
	}
}
