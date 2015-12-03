using CommonResourceAcquisition.ImageAcquisition;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CommonResourceAcquisition.VideoAcquisition
{

	// Developed by Rico Suter (http://rsuter.com), http://mytoolkit.codeplex.com

	public enum YouTubeQuality : short
	{
		// video
		Quality144P,
		Quality240P,
		Quality270P,
		Quality360P,
		Quality480P,
		Quality520P,
		Quality720P,
		Quality1080P,
		Quality2160P,

		// audio
		QualityLow,
		QualityMedium,
		QualityHigh,

		NotAvailable,
		Unknown,
	}

	public enum YouTubeThumbnailSize : short
	{
		Small,
		Medium,
		Large
	}

    public class YouTubeUri
    {
        internal string _url;

        public int Itag { get; internal set; }
        public Uri Uri { get { return new Uri(_url, UriKind.Absolute); } }
        public string Type { get; internal set; }

        public bool HasAudio
        {
            get
            {
                return AudioQuality != YouTubeQuality.Unknown && AudioQuality != YouTubeQuality.NotAvailable;
            }
        }

        public bool HasVideo
        {
            get
            {
                return VideoQuality != YouTubeQuality.Unknown && VideoQuality != YouTubeQuality.NotAvailable;
            }
        }

        public bool Is3DVideo
        {
            get
            {
                if (VideoQuality == YouTubeQuality.Unknown)
                    return false;
                return Itag >= 82 && Itag <= 85;
            }
        }

        public YouTubeQuality VideoQuality
        {
            get
            {
                switch (Itag)
                {
                    // video & audio
                    case 5: return YouTubeQuality.Quality240P;
                    case 6: return YouTubeQuality.Quality270P;
                    case 17: return YouTubeQuality.Quality144P;
                    case 18: return YouTubeQuality.Quality360P;
                    case 22: return YouTubeQuality.Quality720P;
                    case 34: return YouTubeQuality.Quality360P;
                    case 35: return YouTubeQuality.Quality480P;
                    case 36: return YouTubeQuality.Quality240P;
                    case 37: return YouTubeQuality.Quality1080P;
                    case 38: return YouTubeQuality.Quality2160P;

                    // 3d video & audio
                    case 82: return YouTubeQuality.Quality360P;
                    case 83: return YouTubeQuality.Quality480P;
                    case 84: return YouTubeQuality.Quality720P;
                    case 85: return YouTubeQuality.Quality520P;

                    // video only
                    case 133: return YouTubeQuality.Quality240P;
                    case 134: return YouTubeQuality.Quality360P;
                    case 135: return YouTubeQuality.Quality480P;
                    case 136: return YouTubeQuality.Quality720P;
                    case 137: return YouTubeQuality.Quality1080P;
                    case 138: return YouTubeQuality.Quality2160P;
                    case 160: return YouTubeQuality.Quality144P;

                    // audio only
                    case 139: return YouTubeQuality.NotAvailable;
                    case 140: return YouTubeQuality.NotAvailable;
                    case 141: return YouTubeQuality.NotAvailable;
                }

                return YouTubeQuality.Unknown;
            }
        }

        public YouTubeQuality AudioQuality
        {
            get
            {
                switch (Itag)
                {
                    // video & audio
                    case 5: return YouTubeQuality.QualityLow;
                    case 6: return YouTubeQuality.QualityLow;
                    case 17: return YouTubeQuality.QualityLow;
                    case 18: return YouTubeQuality.QualityMedium;
                    case 22: return YouTubeQuality.QualityHigh;
                    case 34: return YouTubeQuality.QualityMedium;
                    case 35: return YouTubeQuality.QualityMedium;
                    case 36: return YouTubeQuality.QualityLow;
                    case 37: return YouTubeQuality.QualityHigh;
                    case 38: return YouTubeQuality.QualityHigh;

                    // 3d video & audio
                    case 82: return YouTubeQuality.QualityMedium;
                    case 83: return YouTubeQuality.QualityMedium;
                    case 84: return YouTubeQuality.QualityHigh;
                    case 85: return YouTubeQuality.QualityHigh;

                    // video only
                    case 133: return YouTubeQuality.NotAvailable;
                    case 134: return YouTubeQuality.NotAvailable;
                    case 135: return YouTubeQuality.NotAvailable;
                    case 136: return YouTubeQuality.NotAvailable;
                    case 137: return YouTubeQuality.NotAvailable;
                    case 138: return YouTubeQuality.NotAvailable;
                    case 160: return YouTubeQuality.NotAvailable;

                    // audio only
                    case 139: return YouTubeQuality.QualityLow;
                    case 140: return YouTubeQuality.QualityMedium;
                    case 141: return YouTubeQuality.QualityHigh;
                }
                return YouTubeQuality.Unknown;
            }
        }

        internal bool IsValid
        {
            get { return _url != null && _url.StartsWith("http") && Itag > 0 && Type != null; }
        }
    }

	public static class YouTube
	{
		public const YouTubeQuality DefaultMinQuality = YouTubeQuality.Quality144P;

		private const string BotUserAgent = "Mozilla/5.0 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)";

        /// <summary>
        /// Returns the title of the YouTube video. 
        /// </summary>
        public static async Task<string> GetVideoTitleAsync(string youTubeId, IResourceNetworkLayer networkLayer, IProgress<float> progress, CancellationToken token)
        {
			using (var client = networkLayer.Clone())
			{
				client.AddHeaders("User-Agent", BotUserAgent);
				var html = await client.Get("http://www.youtube.com/watch?v=" + youTubeId + "&nomobile=1", token, progress, null, true);
				var startIndex = html.IndexOf(" title=\"");
				if (startIndex != -1)
				{
					startIndex = html.IndexOf(" title=\"", startIndex + 1);
					if (startIndex != -1)
					{
						startIndex += 8;
						var endIndex = html.IndexOf("\">", startIndex);
						if (endIndex != -1)
							return html.Substring(startIndex, endIndex - startIndex);
					}
				}
				return null;
			}
        }

		/// <summary>
		/// Returns a thumbnail for the given YouTube ID. 
		/// </summary>
		public static Uri GetThumbnailUri(string youTubeId, YouTubeThumbnailSize size = YouTubeThumbnailSize.Medium)
		{
			switch (size)
			{
				case YouTubeThumbnailSize.Small:
					return new Uri("http://img.youtube.com/vi/" + youTubeId + "/default.jpg", UriKind.Absolute);
				case YouTubeThumbnailSize.Medium:
					return new Uri("http://img.youtube.com/vi/" + youTubeId + "/hqdefault.jpg", UriKind.Absolute);
				case YouTubeThumbnailSize.Large:
					return new Uri("http://img.youtube.com/vi/" + youTubeId + "/maxresdefault.jpg", UriKind.Absolute);
			}
			throw new Exception();
		}

		/// <summary>
		/// Returns the best matching YouTube stream URI which has an audio and video channel and is not 3D.
		/// </summary>
		/// <returns>Returns null when no appropriate URI has been found. </returns>
		public static YouTubeUri GetBestVideoUri(IEnumerable<YouTubeUri> uris, YouTubeQuality minQuality, YouTubeQuality maxQuality)
		{
			return uris
				.Where(u => u.HasVideo && u.HasAudio && !u.Is3DVideo && u.VideoQuality >= minQuality && u.VideoQuality <= maxQuality)
				.OrderByDescending(u => u.VideoQuality)
				.FirstOrDefault();
		}

		/// <summary>
		/// Returns the best matching YouTube stream URI which has an audio and video channel and is not 3D. 
		/// </summary>
		/// <returns>Returns null when no appropriate URI has been found. </returns>
		public static Task<YouTubeUri> GetVideoUriAsync(string youTubeId, YouTubeQuality maxQuality, IResourceNetworkLayer networkLayer, IProgress<float> progress, CancellationToken token)
		{
			return GetVideoUriAsync(youTubeId, DefaultMinQuality, maxQuality, networkLayer, progress, token);
		}


		/// <summary>
		/// Returns the best matching YouTube stream URI which has an audio and video channel and is not 3D. 
		/// </summary>
		/// <returns>Returns null when no appropriate URI has been found. </returns>
		public static async Task<YouTubeUri> GetVideoUriAsync(string youTubeId, YouTubeQuality minQuality, YouTubeQuality maxQuality, IResourceNetworkLayer networkLayer, IProgress<float> progress, CancellationToken token)
		{
			var uris = await GetUrisAsync(youTubeId, networkLayer, progress, token);
			return GetBestVideoUri(uris, minQuality, maxQuality);
		}

		/// <summary>
		/// Returns all available URIs (audio-only and video) for the given YouTube ID. 
		/// </summary>
		public static async Task<YouTubeUri[]> GetUrisAsync(string youTubeId, IResourceNetworkLayer networkLayer, IProgress<float> progress, CancellationToken token)
		{
			var urls = new List<YouTubeUri>();
			string javaScriptCode = null;

			var response = await HttpGet("https://www.youtube.com/watch?v=" + youTubeId + "&nomobile=1", networkLayer, progress, token);
			var match = Regex.Match(response, "url_encoded_fmt_stream_map\": ?\"(.*?)\"");
			var data = Uri.UnescapeDataString(match.Groups[1].Value);
			match = Regex.Match(response, "adaptive_fmts\": ?\"(.*?)\"");
			var data2 = Uri.UnescapeDataString(match.Groups[1].Value);

			var arr = Regex.Split(data + "," + data2, ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)"); // split by comma but outside quotes
			foreach (var d in arr)
			{
				var url = "";
				//var connection = "";
				//var stream = "";
				var signature = "";
				var tuple = new YouTubeUri();
				foreach (var p in d.Replace("\\u0026", "\t").Split('\t'))
				{
					var index = p.IndexOf('=');
					if (index != -1 && index < p.Length)
					{
						try
						{
							var key = p.Substring(0, index);
							var value = Uri.UnescapeDataString(p.Substring(index + 1));
							if (key == "url")
								url = value;
							//else if (key == "conn")
							//    connection = value;
							//else if (key == "stream")
							//    stream = value;
							else if (key == "itag")
								tuple.Itag = int.Parse(value);
							else if (key == "type" && (value.Contains("video/mp4") || value.Contains("audio/mp4")))
								tuple.Type = value;
							else if (key == "sig")
								signature = value;
							else if (key == "s")
							{
								if (javaScriptCode == null)
								{
									var javaScriptUri = "http://s.ytimg.com/yts/jsbin/html5player-" +
														Regex.Match(response,
															"\"\\\\/\\\\/s.ytimg.com\\\\/yts\\\\/jsbin\\\\/html5player-(.+?)\\.js\"")
															.Groups[1] + ".js";
									javaScriptCode = await HttpGet(javaScriptUri, networkLayer, progress, token);
								}

								signature = GenerateSignature(value, javaScriptCode);
							}
						}
						catch (Exception exception)
						{
							Debug.WriteLine("YouTube parse exception: " + exception.Message);
						}
					}
				}

				//if (string.IsNullOrEmpty(url) && !string.IsNullOrEmpty(connection) && !string.IsNullOrEmpty(stream))
				//{
				//    url = connection + "?" + stream; 
				//}

				if (url.Contains("&signature=") || url.Contains("?signature="))
					tuple._url = url;
				else
					tuple._url = url + "&signature=" + signature;

				if (tuple.IsValid)
					urls.Add(tuple);
			}

			return urls.ToArray();
		}

		private static string GenerateSignature(string signature, string javaScriptCode)
		{
			var functionName = Regex.Match(javaScriptCode, "signature=(.*?)\\(").Groups[1].ToString();
			var functionMath = Regex.Match(javaScriptCode, "function " + Regex.Escape(functionName) + "\\((\\w+)\\)\\{(.+?)\\}", RegexOptions.Singleline);

			var parameterName = Regex.Escape(functionMath.Groups[1].ToString());
			var functionBody = functionMath.Groups[2].ToString();

			Dictionary<string, Func<string, int, string>> methods = null;

			//var lo={wS:function(a){return a.reverse()},IC:function(a,b){return a.slice(b)},rw:function(a,b){var c=a[0];a[0]=a[b%a.length];a[b]=c;return a}};
			//function mo(a){a=a.split("");a=lo.rw(a,1);a=lo.rw(a,32);a=lo.IC(a,1);a=lo.wS(a,77);a=lo.IC(a,3);a=lo.wS(a,77);a=lo.IC(a,3);a=lo.wS(a,44);return a.join("")};

			foreach (var line in functionBody.Split(';').Select(l => l.Trim()))
			{
				if (Regex.IsMatch(line, parameterName + "=" + parameterName + "\\.reverse\\(\\)")) // OLD
					signature = Reverse(signature);
				else if (Regex.IsMatch(line, parameterName + "=" + parameterName + "\\.slice\\(\\d+\\)"))
					signature = Slice(signature, Convert.ToInt32(Regex.Match(line, parameterName + "=" + parameterName + "\\.slice\\((\\d+)\\)").Groups[1].ToString()));
				else if (Regex.IsMatch(line, parameterName + "=\\w+\\(" + parameterName + ",\\d+\\)"))
					signature = Swap(signature, Convert.ToInt32(Regex.Match(line, parameterName + "=\\w+\\(" + parameterName + ",(\\d+)\\)").Groups[1].ToString()));
				else if (Regex.IsMatch(line, parameterName + "\\[0\\]=" + parameterName + "\\[\\d+%" + parameterName + "\\.length\\]"))
					signature = Swap(signature, Convert.ToInt32(Regex.Match(line, parameterName + "\\[0\\]=" + parameterName + "\\[(\\d+)%" + parameterName + "\\.length\\]").Groups[1].ToString()));
				else
				{
					var match = Regex.Match(line, "(" + parameterName + "=)?(.*?)\\.(.*?)\\(" + parameterName + ",(.*?)\\)");
					if (match.Success)
					{
						var root = match.Groups[2].ToString();
						var method = match.Groups[3].ToString();
						var parameter = int.Parse(match.Groups[4].ToString());

						if (methods == null)
						{
							// Parse methods
							methods = new Dictionary<string, Func<string, int, string>>();

							var code = Regex.Match(javaScriptCode, "var " + root + "=\\{(.*?)\\};function").Groups[1].ToString();
							var methodsArray = code.Split(new[] { "}," }, StringSplitOptions.None);
							foreach (var m in methodsArray)
							{
								var arr = m.Split(':');
								var methodName = arr[0];
								var methodBody = arr[1];

								if (methodBody.Contains("reverse()"))
									methods[methodName] = (s, i) => Reverse(s);
								else if (methodBody.Contains(".splice(") || methodBody.Contains(".slice("))
									methods[methodName] = Slice;
								else
									methods[methodName] = Swap;
							}
						}

						signature = methods[method](signature, parameter);
					}
					else
					{
					}
				}
			}
			return signature;
		}

		private static string Reverse(string signature)
		{
			var charArray = signature.ToCharArray();
			Array.Reverse(charArray);
			signature = new string(charArray);
			return signature;
		}

		private static string Slice(string input, int length)
		{
			return input.Substring(length);
		}

		private static string Swap(string input, int position)
		{
			var str = new StringBuilder(input);
			var swapChar = str[position];
			str[position] = str[0];
			str[0] = swapChar;
			return str.ToString();
		}

		private static async Task<string> HttpGet(string uri, IResourceNetworkLayer networkLayer, IProgress<float> progress, CancellationToken token)
		{
            using (var headeredNetworkLayer = networkLayer.Clone())
            {
                networkLayer.AddHeaders("User-Agent", BotUserAgent);
                return await networkLayer.Get(uri, token, progress, null, true);
            }
		}

		internal static string GetYouTubeId(string originalUrl)
		{
			var youtubeRegex = new Regex("youtu(?:\\.be|be\\.com)/(?:.*v(?:/|=)|(?:.*/)?)([a-zA-Z0-9-_]+)");
			if (youtubeRegex.IsMatch(originalUrl))
			{
				return youtubeRegex.Match(originalUrl).Groups[1].Value;
			}
			else
				return null;
		}

		internal static bool IsAPI(string originalUrl)
		{
			var youtubeRegex = new Regex("youtu(?:\\.be|be\\.com)/(?:.*v(?:/|=)|(?:.*/)?)([a-zA-Z0-9-_]+)");
			return youtubeRegex.IsMatch(originalUrl);
		}

		internal static IVideoResult GetVideoResult(string originalUrl, IResourceNetworkLayer networkLayer)
		{
			return new VideoResult(originalUrl);
		}

		private class VideoResult : IVideoResult
		{
			string _originalUrl;

			public VideoResult(string originalUrl)
			{
				_originalUrl = originalUrl;
			}

			public Task<string> PreviewUrl(IResourceNetworkLayer networkLayer, IProgress<float> progress, CancellationToken cancelToken)
			{
				return Task.FromResult(YouTube.GetThumbnailUri(YouTube.GetYouTubeId(_originalUrl)).ToString());
			}

			public async Task<IEnumerable<Tuple<string, string>>> PlayableStreams(IResourceNetworkLayer networkLayer, IProgress<float> progress, CancellationToken cancelToken)
			{
				var uris = await YouTube.GetUrisAsync(YouTube.GetYouTubeId(_originalUrl), networkLayer, progress, cancelToken);
				return uris.Select(uri => Tuple.Create(uri.Uri.ToString(), uri.Type)).ToList();
			}
		}
	}

}
