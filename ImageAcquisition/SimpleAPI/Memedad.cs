﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CommonResourceAcquisition.ImageAcquisition.SimpleAPI
{
	class Memedad : IAcquisitionAPI
	{
		private static Regex hashRe = new Regex(@"^http:\/\/memedad.com\/meme\/([0-9]+)");

		public string GetImageFromUri(Uri uri)
		{
			var href = uri.OriginalString;
			var groups = hashRe.Match(href).Groups;

			if (groups != null)
				return string.Format("http://memedad.com/memes/{0}.jpg", groups[1].Value);

			else
				return null;
		}
	}
}
