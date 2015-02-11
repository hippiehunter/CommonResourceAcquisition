using CommonResourceAcquisition.ImageAcquisition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonResourceAcquisition.VideoAcquisition
{
    public class VideoAcquisition
    {
        
        //someday we will support other video providers
        public static bool IsAPI(string originalUrl)
        {
            var isYoutube = YouTube.IsAPI(originalUrl);
			if (!isYoutube)
			{
				switch (HttpClientUtility.GetDomainFromUrl(originalUrl).ToLower())
				{
					case "gfycat.com":
						return !string.IsNullOrWhiteSpace(Gfycat.GetGfyName(originalUrl));
					case "liveleak.com":
					case "vimeo.com":
						return true;
					default:
						return false;
				}
			}
			else
				return true;
        }

        public static bool IsGifType(string originalUrl)
        {
            string fileName = "";

            if (Uri.IsWellFormedUriString(originalUrl, UriKind.Absolute))
            {
                var uri = new Uri(originalUrl);
                fileName = uri.AbsolutePath;
            }
            return fileName.EndsWith(".gifv") || HttpClientUtility.GetDomainFromUrl(originalUrl).ToLower() == "gfycat.com";
        }

        public static IVideoResult GetVideo(string originalUrl)
        {
			string targetHost = null;
			string fileName = null;

			if (Uri.IsWellFormedUriString(originalUrl, UriKind.Absolute))
			{
				var uri = new Uri(originalUrl);
				targetHost = uri.DnsSafeHost.ToLower();
				fileName = uri.AbsolutePath;
			}

			if (fileName.EndsWith(".gifv"))
				return new DummyVideoResult(originalUrl.Replace(".gifv", ".mp4"), originalUrl.Replace(".gifv", "l.jpg"));
			else if (fileName.EndsWith(".mp4") || fileName.EndsWith(".mpg"))
				return new DummyVideoResult(originalUrl, null);
			else if (YouTube.IsAPI(originalUrl))
				return YouTube.GetVideoResult(originalUrl);
			else
			{
				switch (HttpClientUtility.GetDomainFromUrl(originalUrl).ToLower())
				{
					case "liveleak.com":
						return Liveleak.GetVideoResult(originalUrl);
					case "gfycat.com":
						return Gfycat.GetVideoResult(originalUrl);
					case "vimeo.com":
						return Vimeo.GetVideoResult(originalUrl);
				}
			}
			return null;
        }

		class DummyVideoResult : IVideoResult
		{
			string _videoUrl;
			string _previewUrl;
			public DummyVideoResult(string videoUrl, string previewUrl)
			{
				_videoUrl = videoUrl;
				_previewUrl = previewUrl;
			}
			public Task<string> PreviewUrl(System.Threading.CancellationToken cancelToken)
			{
				return Task.FromResult(_previewUrl);
			}

			public Task<IEnumerable<Tuple<string, string>>> PlayableStreams(System.Threading.CancellationToken cancelToken)
			{
				return Task.FromResult((IEnumerable<Tuple<string, string>>)new List<Tuple<string, string>> { new Tuple<string, string> ( _videoUrl, "mp4" )});
			}
		}
    }
}
