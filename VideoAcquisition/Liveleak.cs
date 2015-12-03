using CommonResourceAcquisition.ImageAcquisition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
            Tuple<Task<string>, IProgress<float>> _pageContentLoadPack;
            JoinableCancellationTokenSource _cancelTokenSource = new JoinableCancellationTokenSource();
            string _originalUrl;
            async Task<string> LoadContents(IResourceNetworkLayer networkLayer, IProgress<float> progress, CancellationToken token)
            {
                var reset = _cancelTokenSource.AddToken(token);
                if (_pageContentLoadPack == null || reset)
                {
                    lock(this)
                    {
                        if (_pageContentLoadPack == null || reset)
                        {
                            if (_originalUrl.Contains("&ajax=1"))
                                _pageContentLoadPack = Tuple.Create(networkLayer.Get(_originalUrl, _cancelTokenSource.Token, progress, null, false), progress);
                            else
                                _pageContentLoadPack = Tuple.Create(networkLayer.Get(_originalUrl + "&ajax=1", token, progress, null, false), progress);
                        }
                    }
                    
                }

                if (_pageContentLoadPack.Item2 != progress)
                {
                    HttpClientUtility.NetworkLayer.JoinProgress(_pageContentLoadPack.Item2, progress);
                }

                try
                {
                    return await _pageContentLoadPack.Item1;
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
                var pageContents = await LoadContents(networkLayer, progress, cancelToken);
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

			public async Task<IEnumerable<Tuple<string, string>>> PlayableStreams(IResourceNetworkLayer networkLayer, IProgress<float> progress, System.Threading.CancellationToken cancelToken)
			{
				var pageContents = await LoadContents(networkLayer, progress, cancelToken);
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
