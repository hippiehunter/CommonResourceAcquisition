﻿using CommonResourceAcquisition.ImageAcquisition;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CommonResourceAcquisition.VideoAcquisition
{
	class Vimeo
	{
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

            private IResourceNetworkLayer _networkLayer = HttpClientUtility.NetworkLayer.Clone();
            Tuple<Task<RootVimeo>, IProgress<float>> _configRootPack;
            JoinableCancellationTokenSource _cancelTokenSource = new JoinableCancellationTokenSource();
            string _originalUrl;

            async Task<RootVimeo> LoadContentsImpl(IResourceNetworkLayer networkLayer, IProgress<float> progress, CancellationToken token)
            {
                using (var client = networkLayer.Clone())
                {
                    client.AddHeaders("User-Agent", "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:12.0) Gecko/20100101 Firefox/12.0");
                    client.AddHeaders("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
                    client.AddHeaders("Accept-Language", "ru,en;q=0.8,en-us;q=0.5,uk;q=0.3");
                    client.SetReferer("http://www.google.com");
                    var pageData = await client.Get(_originalUrl, token, progress, null, false);
                    string clipId = null;
                    if (Regex.Match(pageData, @"clip_id=(\d+)", RegexOptions.Singleline).Success)
                    {
                        clipId = Regex.Match(pageData, @"clip_id=(\d+)", RegexOptions.Singleline).Groups[1].ToString();
                    }
                    else if (Regex.Match(pageData, @"(\d+)", RegexOptions.Singleline).Success)
                    {
                        clipId = Regex.Match(pageData, @"(\d+)", RegexOptions.Singleline).Groups[1].ToString();
                    }

                    if (!string.IsNullOrWhiteSpace(clipId))
                    {
                        client.SetReferer(string.Format("http://vimeo.com/{0}", clipId));
                        var configResult = await client.Get(string.Format("http://player.vimeo.com/video/{0}/config", clipId), token, progress, null, false);
                        return JsonConvert.DeserializeObject<RootVimeo>(configResult);
                    }
                    else
                        return null;
                }
            }

            async Task<RootVimeo> LoadContents(IResourceNetworkLayer networkLayer, IProgress<float> progress, CancellationToken token)
            {
                var reset = _cancelTokenSource.AddToken(token);
                if (_configRootPack == null || reset)
                {
                    lock (this)
                    {
                        if (_configRootPack == null || reset)
                        {
                            _configRootPack = Tuple.Create(LoadContentsImpl(networkLayer, progress, _cancelTokenSource.Token), progress);
                        }
                    }

                }

                if (_configRootPack.Item2 != progress)
                {
                    HttpClientUtility.NetworkLayer.JoinProgress(_configRootPack.Item2, progress);
                }

                try
                {
                    return await _configRootPack.Item1;
                }
                finally
                {
                    _cancelTokenSource.Clear();
                }
            }

            public VideoResult(string originalUrl)
			{
                _originalUrl = originalUrl;
			}

			public async Task<string> PreviewUrl(IResourceNetworkLayer networkLayer, IProgress<float> progress, System.Threading.CancellationToken cancelToken)
			{
				var config = await LoadContents(networkLayer, progress, cancelToken);
				if (config != null)
				{
					return config.video.thumbs.medium;
				}
				else
					return null;
			}

			public async Task<IEnumerable<Tuple<string, string>>> PlayableStreams(IResourceNetworkLayer networkLayer, IProgress<float> progress, System.Threading.CancellationToken cancelToken)
			{
				var config = await LoadContents(networkLayer, progress, cancelToken);
				if (config != null)
				{
					var result = new List<Tuple<string, string>>();
					if(config.request.files.h264.mobile != null)
						result.Add(Tuple.Create(config.request.files.h264.mobile.url, config.request.files.h264.mobile.bitrate.ToString()));
					if(config.request.files.h264.hd != null)
						result.Add(Tuple.Create(config.request.files.h264.hd.url, config.request.files.h264.hd.bitrate.ToString()));
					if (config.request.files.h264.sd != null)
						result.Add(Tuple.Create(config.request.files.h264.sd.url, config.request.files.h264.sd.bitrate.ToString()));
					return result;
				}
				else
					return null;
			}
		}
	}

	public class Mobile
	{
		public int profile { get; set; }
		public string origin { get; set; }
		public string url { get; set; }
		public int height { get; set; }
		public int width { get; set; }
		public int id { get; set; }
		public int bitrate { get; set; }
		public int availability { get; set; }
	}

	public class Hd
	{
		public int profile { get; set; }
		public string origin { get; set; }
		public string url { get; set; }
		public int height { get; set; }
		public int width { get; set; }
		public int id { get; set; }
		public int bitrate { get; set; }
		public int availability { get; set; }
	}

	public class Sd
	{
		public int profile { get; set; }
		public string origin { get; set; }
		public string url { get; set; }
		public int height { get; set; }
		public int width { get; set; }
		public int id { get; set; }
		public int bitrate { get; set; }
		public int availability { get; set; }
	}

	public class H264
	{
		public Mobile mobile { get; set; }
		public Hd hd { get; set; }
		public Sd sd { get; set; }
	}

	public class Hls
	{
		public string origin { get; set; }
		public string cdn { get; set; }
		public string all { get; set; }
		public string hd { get; set; }
	}

	public class Files
	{
		public H264 h264 { get; set; }
		public Hls hls { get; set; }
		public List<string> codecs { get; set; }
	}

	public class Cookie
	{
		public int scaling { get; set; }
		public double volume { get; set; }
		public object hd { get; set; }
		public string captions { get; set; }
	}

	public class Flags
	{
		public int preload_video { get; set; }
		public int plays { get; set; }
		public int webp { get; set; }
		public int partials { get; set; }
		public int conviva { get; set; }
		public int login { get; set; }
	}

	public class Build
	{
		public string player { get; set; }
		public string js { get; set; }
	}

	public class Urls
	{
		public string zeroclip_swf { get; set; }
		public string js { get; set; }
		public string proxy { get; set; }
		public string conviva { get; set; }
		public string flideo { get; set; }
		public string canvas_js { get; set; }
		public string moog { get; set; }
		public string conviva_service { get; set; }
		public string moog_js { get; set; }
		public string zeroclip_js { get; set; }
		public string css { get; set; }
	}

	public class Request
	{
		public Files files { get; set; }
		public string ga_account { get; set; }
		public int timestamp { get; set; }
		public int expires { get; set; }
		public string session { get; set; }
		public Cookie cookie { get; set; }
		public string cookie_domain { get; set; }
		public object referrer { get; set; }
		public string conviva_account { get; set; }
		public Flags flags { get; set; }
		public Build build { get; set; }
		public Urls urls { get; set; }
		public string signature { get; set; }
	}

	public class Rating
	{
		public int id { get; set; }
	}

	public class Owner
	{
		public string account_type { get; set; }
		public string name { get; set; }
		public string img { get; set; }
		public string url { get; set; }
		public string img_2x { get; set; }
		public int id { get; set; }
	} 

	public class Thumbs
	{
		[JsonProperty("1280")]
		public string large { get; set; }
		[JsonProperty("960")]
		public string medium { get; set; }
		[JsonProperty("640")]
		public string small { get; set; }
		[JsonProperty("base")]
		public string plain { get; set; }
	}

	public class Video
	{
		public Rating rating { get; set; }
		public int allow_hd { get; set; }
		public int height { get; set; }
		public Owner owner { get; set; }
		public Thumbs thumbs { get; set; }
		public int duration { get; set; }
		public int id { get; set; }
		public int hd { get; set; }
		public string embed_code { get; set; }
		public int default_to_hd { get; set; }
		public string title { get; set; }
		public string url { get; set; }
		public string privacy { get; set; }
		public string share_url { get; set; }
		public int width { get; set; }
		public string embed_permission { get; set; }
		public double fps { get; set; }
	}

	public class Build2
	{
		public string player { get; set; }
		public string rpc { get; set; }
	}

	public class Embed
	{
		public object player_id { get; set; }
		public string outro { get; set; }
		public int api { get; set; }
		public string context { get; set; }
		public int time { get; set; }
		public string color { get; set; }
		public int on_site { get; set; }
		public int loop { get; set; }
		public int autoplay { get; set; }
	}

	public class User
	{
		public int liked { get; set; }
		public string account_type { get; set; }
		public int logged_in { get; set; }
		public int owner { get; set; }
		public int watch_later { get; set; }
		public int id { get; set; }
		public int mod { get; set; }
	}

	public class RootVimeo
	{
		public string cdn_url { get; set; }
		public int view { get; set; }
		public Request request { get; set; }
		public string player_url { get; set; }
		public Video video { get; set; }
		public Build2 build { get; set; }
		public Embed embed { get; set; }
		public string vimeo_url { get; set; }
		public User user { get; set; }
	}
}
