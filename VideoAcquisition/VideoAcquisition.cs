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

        public static IVideoResult GetVideo(string originalUrl)
        {
			if (YouTube.IsAPI(originalUrl))
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
    }
}
