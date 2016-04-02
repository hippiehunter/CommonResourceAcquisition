using CommonResourceAcquisition.ImageAcquisition.AsyncAPI;
using CommonResourceAcquisition.ImageAcquisition.SimpleAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CommonResourceAcquisition.ImageAcquisition
{
    public enum ImageAPIType
    {
        None,
        Simple,
        Async
    }

    public class ImageAcquisition
    {
		private static Dictionary<string, IAcquisitionAPI> _simpleAPIs = new Dictionary<string,IAcquisitionAPI>();
		private static Dictionary<string, IAsyncAcquisitionAPI> _asyncAPIs = new Dictionary<string,IAsyncAcquisitionAPI>();

        public static string ImgurAPIKey
        {
            set
            {
                Imgur._apiKey = value;
            }
        }

		static ImageAcquisition()
		{
			var quickmeme = new Quickmeme();
			var picsarus = new Picsarus();
			var memecrunch = new Memecrunch();
			var livememe = new Livememe();
			var memedad = new Memedad();
			var memegen = new Memegen();
			var memefive = new Memefive();
			var ehost = new Ehost();
			var makeameme = new Makeameme();
			var imgflip = new Imgflip();
			var crtlv = new Ctrlv();
			var picshd = new Picshd();

			_simpleAPIs.Add("qkme.me", quickmeme);
			_simpleAPIs.Add("quickmeme.com", quickmeme);
			_simpleAPIs.Add("picsarus.com", picsarus);
			_simpleAPIs.Add("memecrunch.com", memecrunch);
			_simpleAPIs.Add("livememe.com", livememe);
			_simpleAPIs.Add("memedad.com", memedad);
			_simpleAPIs.Add("memefive.com", memefive);
			_simpleAPIs.Add("eho.st", ehost);
			_simpleAPIs.Add("makeameme.org", makeameme);
			_simpleAPIs.Add("imgflip.com", imgflip);
			_simpleAPIs.Add("ctrlv.in", crtlv);
			_simpleAPIs.Add("picshd.com", picshd);

			_simpleAPIs.Add("memegen.com", memegen);
			_simpleAPIs.Add("memegen.de", memegen);
			_simpleAPIs.Add("memegen.nl", memegen);
			_simpleAPIs.Add("memegen.fr", memegen);
			_simpleAPIs.Add("memegen.it", memegen);
			_simpleAPIs.Add("memegen.es", memegen);
			_simpleAPIs.Add("memegen.se", memegen);
			_simpleAPIs.Add("memegen.pl",  memegen);

			var imgur = new Imgur();
			var flickr = new Flickr();
			var minus = new Minus();


			_asyncAPIs.Add("imgur.com", imgur);
			_asyncAPIs.Add("flickr.com", flickr);
			_asyncAPIs.Add("min.us", minus);
		}

        public static string GetSimpleImageFromUrl(string url)
        {
            try
            {
                var uri = new Uri(url);

                string filename = Path.GetFileName(uri.LocalPath);

                if (filename.EndsWith(".jpg") || filename.EndsWith(".png") || filename.EndsWith(".jpeg") || filename.EndsWith(".gif"))
                    return url;
                else
                {
                    var domain = HttpClientUtility.GetDomainFromUrl(url).ToLower();
                    IAcquisitionAPI simpleApi = null;
                    if (_simpleAPIs.TryGetValue(domain, out simpleApi))
                    {
                        return simpleApi.GetImageFromUri(uri);
                    }
                    else
                        return string.Empty;
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        public static async Task<IEnumerable<Tuple<string, string>>> GetImagesFromUrl(string title, string url, IResourceNetworkLayer networkLayer, IProgress<float> progress, CancellationToken token)
        {
            try
            {
                var uri = new Uri(url);

                string filename = Path.GetFileName(uri.LocalPath);

                if (filename.EndsWith(".jpg") || filename.EndsWith(".png") || filename.EndsWith(".jpeg") || filename.EndsWith(".gif"))
                    return new Tuple<string, string>[] { Tuple.Create(title, url) };
                else
                {
					var domain = HttpClientUtility.GetDomainFromUrl(url).ToLower();
					IAsyncAcquisitionAPI asyncApi = null;
					IAcquisitionAPI simpleApi = null;
					if (_simpleAPIs.TryGetValue(domain, out simpleApi))
					{
						return new Tuple<string, string>[] { Tuple.Create(title, simpleApi.GetImageFromUri(uri)) };
					}
					else if (_asyncAPIs.TryGetValue(domain, out asyncApi))
					{
						return await asyncApi.GetImagesFromUri(title, uri, networkLayer, progress, token);
					}
					else
						return Enumerable.Empty<Tuple<string, string>>();
                }
            }
            catch
            {
                return Enumerable.Empty<Tuple<string, string>>();
            }
        }

        public static bool IsImage(string url)
        {
            try
            {
                var uri = new Uri(url);

                string filename = Path.GetFileName(uri.LocalPath);

                if (filename.EndsWith(".jpg") || url.EndsWith(".png") || url.EndsWith(".jpeg") || filename.EndsWith(".gif"))
                    return true;
            }
            catch
            {
                //ignore failure here, we're going to return false anyway
            }
            return false;
        }

        public static ImageAPIType ImageAPIType(string url)
        {
            try
            {
                var uri = new Uri(url);

                string filename = Path.GetFileName(uri.LocalPath);

                if (filename.EndsWith(".jpg") || url.EndsWith(".png") || url.EndsWith(".jpeg") || filename.EndsWith(".gif"))
                    return CommonResourceAcquisition.ImageAcquisition.ImageAPIType.Simple;
                else
                {
					var domain = HttpClientUtility.GetDomainFromUrl(url).ToLower();
					IAsyncAcquisitionAPI asyncApi = null;
					IAcquisitionAPI simpleApi = null;
					if (_simpleAPIs.TryGetValue(domain, out simpleApi))
					{
						return CommonResourceAcquisition.ImageAcquisition.ImageAPIType.Simple;
					}
					else if (_asyncAPIs.TryGetValue(domain, out asyncApi))
					{
						return asyncApi.IsMatch(uri) ? CommonResourceAcquisition.ImageAcquisition.ImageAPIType.Async : CommonResourceAcquisition.ImageAcquisition.ImageAPIType.None;
					}
					else
						return CommonResourceAcquisition.ImageAcquisition.ImageAPIType.None;
                }

            }
            catch
            {
                //ignore failure here, we're going to return false anyway
            }
            return CommonResourceAcquisition.ImageAcquisition.ImageAPIType.None;
        }
    }
}
