using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CommonResourceAcquisition.VideoAcquisition
{
	class Vimeo
	{

		private static async Task<string> HttpGet(string uri, string referer)
		{
			using (var client = new HttpClient())
			{
				client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:12.0) Gecko/20100101 Firefox/12.0");
				client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
				client.DefaultRequestHeaders.Add("Accept-Language", "ru,en;q=0.8,en-us;q=0.5,uk;q=0.3");
				client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
				client.DefaultRequestHeaders.Referrer = new Uri(referer);
				var response = await client.GetAsync(uri);
				return await response.Content.ReadAsStringAsync();
			}
		}
		public static IVideoResult GetVideoResult(string originalUrl)
		{
			return new VideoResult(originalUrl);
		}

		private class VideoResult : IVideoResult
		{
			public class AboutRequest
			{
				public int id { get; set; }
				public string title { get; set; }
				public string description { get; set; }
				public string url { get; set; }
				public string upload_date { get; set; }
				public string mobile_url { get; set; }
				public string thumbnail_small { get; set; }
				public string thumbnail_medium { get; set; }
				public string thumbnail_large { get; set; }
				public int user_id { get; set; }
				public string user_name { get; set; }
				public string user_url { get; set; }
				public string user_portrait_small { get; set; }
				public string user_portrait_medium { get; set; }
				public string user_portrait_large { get; set; }
				public string user_portrait_huge { get; set; }
				public int duration { get; set; }
				public int width { get; set; }
				public int height { get; set; }
				public string tags { get; set; }
				public string embed_privacy { get; set; }
			}

			private Lazy<Task<string>> _initialPageData;
			public VideoResult(string originalUrl)
			{
				_initialPageData = new Lazy<Task<string>>(() =>
				{
					return HttpGet(originalUrl, "http://www.google.com");
				});
			}
			public async Task<string> PreviewUrl(System.Threading.CancellationToken cancelToken)
			{
				var pageData = await _initialPageData.Value;
				string clipId = null;
				if (Regex.Match(pageData, @"clip_id=(\d+)", RegexOptions.Singleline).Success)
				{
					clipId = Regex.Match(pageData, @"clip_id=(\d+)", RegexOptions.Singleline).Groups[1].ToString();
				}
				else if (Regex.Match(pageData, @"(\d+)", RegexOptions.Singleline).Success)
				{
					clipId = Regex.Match(pageData, @"(\d+)", RegexOptions.Singleline).Groups[1].ToString();
				}
				var about = JsonConvert.DeserializeObject<AboutRequest>(await HttpGet(string.Format("http://vimeo.com/api/v2/video/{0}.json", clipId), "http://www.google.com"));
				return about.thumbnail_large;
			}

			public async Task<IEnumerable<Tuple<string, string>>> PlayableStreams(System.Threading.CancellationToken cancelToken)
			{
				var pageData = await _initialPageData.Value;
				string clipId = null;
				if (Regex.Match(pageData, @"clip_id=(\d+)", RegexOptions.Singleline).Success)
				{
					clipId = Regex.Match(pageData, @"clip_id=(\d+)", RegexOptions.Singleline).Groups[1].ToString();
				}
				else if (Regex.Match(pageData, @"(\d+)", RegexOptions.Singleline).Success)
				{
					clipId = Regex.Match(pageData, @"(\d+)", RegexOptions.Singleline).Groups[1].ToString();
				}

				string sig = Regex.Match(pageData, "\"signature\":\"(.+?)\"", RegexOptions.Singleline).Groups[1].ToString();
				string timestamp = Regex.Match(pageData, "\"timestamp\":(\\d+)", RegexOptions.Singleline).Groups[1].ToString();

				string videoUrl = string.Format("http://player.vimeo.com/play_redirect?clip_id={0}&sig={1}&time={2}&quality=hd&codecs=H264,VP8,VP6&type=moogaloop_local&embed_location=", clipId, sig, timestamp);

				using (var client = new HttpClient())
				{
					client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:12.0) Gecko/20100101 Firefox/12.0");
					client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
					client.DefaultRequestHeaders.Add("Accept-Language", "ru,en;q=0.8,en-us;q=0.5,uk;q=0.3");
					client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
					client.DefaultRequestHeaders.Referrer = new Uri("http://a.vimeocdn.com/p/flash/moogaloop/5.2.25/moogaloop.swf?v=1.0.0");
					var response = await client.GetAsync(videoUrl);
					return new List<Tuple<string, string>> { Tuple.Create(response.Headers.Location.ToString(), "H264") };
				}
			}
		}
	}
}
