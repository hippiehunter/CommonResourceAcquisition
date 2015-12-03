using CommonResourceAcquisition.ImageAcquisition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CommonResourceAcquisition.VideoAcquisition
{
    public interface IVideoResult
    {
        Task<string> PreviewUrl(IResourceNetworkLayer networkLayer, IProgress<float> progress, CancellationToken cancelToken);
		Task<IEnumerable<Tuple<string, string>>> PlayableStreams(IResourceNetworkLayer networkLayer, IProgress<float> progress, CancellationToken cancelToken);
    }
}
