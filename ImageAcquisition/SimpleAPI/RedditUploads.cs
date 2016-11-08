using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonResourceAcquisition.ImageAcquisition.SimpleAPI
{
    //i.reddituploads.com
    class RedditUploads : IAcquisitionAPI
    {
        public string GetImageFromUri(Uri uri)
        {
            return uri.ToString();
        }
    }
}
