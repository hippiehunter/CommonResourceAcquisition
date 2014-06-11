using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonResourceAcquisition.ImageAcquisition
{
	interface IAcquisitionAPI
	{
		string GetImageFromUri(Uri uri);
	}
}
