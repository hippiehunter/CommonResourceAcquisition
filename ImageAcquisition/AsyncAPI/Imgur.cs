using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CommonResourceAcquisition.ImageAcquisition.AsyncAPI
{
    class Imgur : IAsyncAcquisitionAPI
    {
        internal static string _apiKey;
        //Transliterated from Reddit Enhancement Suite https://github.com/honestbleeps/Reddit-Enhancement-Suite/blob/master/lib/reddit_enhancement_suite.user.js
        private static Regex hashRe = new Regex(@"^https?:\/\/(?:i\.|m\.|edge\.|www\.)*imgur\.com\/(?:r\/\w+\/)*(?!gallery)(?!removalrequest)(?!random)(?!memegen)(\w{5,7}(?:[&,]\w{5,7})*)(?:#\d+)?[sbtmlh]?(\.(?:jpe?g|gif|png|gifv))?(\?.*)?$");
        private static Regex hostedHashRe = new Regex(@"^https?:(\/\/i\.\w+\.*imgur\.com\/)(\w{5,7}(?:[&,]\w{5,7})*)(?:#\d+)?[sbtmlh]?(\.(?:jpe?g|gif|png))?(\?.*)?$");
        private static Regex galleryHashRe = new Regex(@"^https?:\/\/(?:m\.|www\.)?imgur\.com\/gallery\/(\w+)(?:[/#]|$)");
        private static Regex albumHashRe = new Regex(@"^https?:\/\/(?:m\.|www\.)?imgur\.com\/a\/(\w+)(?:[/#]|$)");
		private static string apiPrefix = "https://api.imgur.com/3/";

        public bool IsMatch(Uri uri)
        {
            var href = uri.OriginalString;

			if (href.Contains("/gallery/"))
				return true;

            var groups = hashRe.Match(href).Groups;
            GroupCollection albumGroups = null;

            if (groups.Count == 0 || (groups.Count > 0 && string.IsNullOrWhiteSpace(groups[0].Value)))
                albumGroups = albumHashRe.Match(href).Groups;

            return ((albumGroups != null && albumGroups.Count > 1 && !string.IsNullOrWhiteSpace(albumGroups[1].Value)) ||
                groups.Count > 0 && !string.IsNullOrWhiteSpace(groups[0].Value));
            
        }

        public class ImgurImage
        {
            public string id { get; set; }
            public string title { get; set; }
            public string description { get; set; }
            public int datetime { get; set; }
            public string type { get; set; }
            public bool animated { get; set; }
            public int width { get; set; }
            public int height { get; set; }
            public int size { get; set; }
            public int views { get; set; }
            public object bandwidth { get; set; }
            public object vote { get; set; }
            public bool favorite { get; set; }
            public object nsfw { get; set; }
            public object section { get; set; }
            public object account_url { get; set; }
            public object account_id { get; set; }
            public object comment_preview { get; set; }
            public string gifv { get; set; }
            public string webm { get; set; }
            public string mp4 { get; set; }
            public string link { get; set; }
            public bool looping { get; set; }
        }

        public class ImgurData
        {
            public string id { get; set; }
            public object title { get; set; }
            public object description { get; set; }
            public int datetime { get; set; }
            public string cover { get; set; }
            public int cover_width { get; set; }
            public int cover_height { get; set; }
            public object account_url { get; set; }
            public object account_id { get; set; }
            public string privacy { get; set; }
            public string layout { get; set; }
            public int views { get; set; }
            public string link { get; set; }
            public bool favorite { get; set; }
            public bool nsfw { get; set; }
            public string section { get; set; }
            public int images_count { get; set; }
            public List<ImgurImage> images { get; set; }
        }

        public class ImgurInfo
        {
            public ImgurData data { get; set; }
            public bool success { get; set; }
            public int status { get; set; }
        }

        private async Task<ImgurInfo> GetResponse(string url, IResourceNetworkLayer networkLayer, IProgress<float> progress, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                throw new Exception("ApiKey not set for Imgur, please set it on ImageAquisition.ImgurAPIKey");
            }

            using (var network = networkLayer.Clone())
            {
                network.AddHeaders("Authorization", string.Format("Client-ID {0}", _apiKey));
                var responseString = await network.Get(url, token, progress, null, false);
                return JsonConvert.DeserializeObject<ImgurInfo>(responseString);
            }
        }

        private ImgurInfo HandleImageCollection(string[] hashes, string href)
        {
            var madeImages = hashes.Select(hash => new ImgurImage { id = hash, link = string.Format("http://i.imgur.com/{0}.jpg", hash) }).ToList();
            return new ImgurInfo
            {
                success = true,
                data = new ImgurData
                {
                    images = madeImages
                }
            };
        }

        private ImgurInfo HandleImage(string hash, string href)
        {
            return new ImgurInfo
            {
                success = true,
                data = new ImgurData
                {
                    images = new List<ImgurImage> { new ImgurImage { id = hash, link = string.Format("http://i.imgur.com/{0}.jpg", hash) } }
                }
            };
        }

        public async Task<IEnumerable<Tuple<string, string>>> GetImagesFromUri(string title, Uri uri, IResourceNetworkLayer networkLayer, IProgress<float> progress, CancellationToken token)
        {

            var href = uri.OriginalString.Split('?')[0];
            string hash = "";
            var gallaryMatch = galleryHashRe.Match(href);
            var albumsMatch = albumHashRe.Match(href);
            var hostedMatch = hostedHashRe.Match(href);
            var hashMatch = hashRe.Match(href);
            ImgurInfo info = null;
            if (gallaryMatch != null && gallaryMatch.Groups != null && gallaryMatch.Groups.Count > 0 && !string.IsNullOrWhiteSpace(gallaryMatch.Groups[0].Value))
            {
                hash = gallaryMatch.Groups[1].Value;
                info = await GetResponse(apiPrefix + "gallery/" + hash, networkLayer, progress, token);
            }
            else if (albumsMatch != null && albumsMatch.Groups != null && albumsMatch.Groups.Count > 0 && !string.IsNullOrWhiteSpace(albumsMatch.Groups[1].Value))
            {
                hash = albumsMatch.Groups[1].Value;
                info = await GetResponse(apiPrefix + "album/" + hash, networkLayer, progress, token);
            }
            else if (hostedMatch != null && hostedMatch.Groups != null && hostedMatch.Groups.Count > 2 && !string.IsNullOrWhiteSpace(hostedMatch.Groups[2].Value))
            {
                hash = hostedMatch.Groups[2].Value;
                info = HandleImage(hash, href);
            }
            else if (hashMatch != null && hashMatch.Groups != null && hashMatch.Groups.Count > 1 && !string.IsNullOrWhiteSpace(hashMatch.Groups[1].Value))
            {
                hash = hashMatch.Groups[1].Value;
                var splitHash = Regex.Split(hash, "[&,]");
                if (splitHash != null && splitHash.Length > 0)
                {
                    // handle separated list of IDs
                    info = HandleImageCollection(splitHash, href);
                }
                else {
                    info = HandleImage(hash, href);
                }
            }


            if (info != null && info.success == true && info.data.images != null)
            {
                return info.data.images.Select(image => Tuple.Create(image.title ?? title, image.link));
            }
            else if (info?.data?.link != null)
            {
                return new Tuple<string, string>[] { Tuple.Create(info.data.title as string ?? title, info.data.link) };
            }
            else if (info.success)
            {
                return Enumerable.Empty<Tuple<string, string>>();
            }
            else
            {
                throw new Exception("imgur returned status: " + info.status);
            }
            //        var href = uri.OriginalString;
            //        var groups = hashRe.Match(href).Groups;
            //        GroupCollection albumGroups = null;

            //        if (groups.Count == 0 || (groups.Count > 0 && string.IsNullOrWhiteSpace(groups[0].Value)))
            //            albumGroups = albumHashRe.Match(href).Groups;

            //        if (groups.Count > 2 && string.IsNullOrWhiteSpace(groups[2].Value))
            //        {
            //            if (Regex.IsMatch(groups[1].Value, "[&,]"))
            //            {
            //                var hashes = Regex.Split(groups[1].Value, "[&,]");
            //                //Imgur doesn't really care about the extension and the browsers don't seem to either.
            //                return hashes
            //                    .Select(hash => Tuple.Create(title, string.Format("http://i.imgur.com/{0}.gif", hash)));

            //            }
            //            else
            //            {
            //                if (uri.AbsolutePath.ToLower().StartsWith("/gallery"))
            //                {
            //                    return await GetImagesFromUri(title, new Uri("http://imgur.com/a/" + groups[1].Value), networkLayer, progress, token);
            //                }
            //                else
            //                {
            //                    //Imgur doesn't really care about the extension and the browsers don't seem to either.
            //                    return new Tuple<string, string>[] { Tuple.Create(title, string.Format("http://i.imgur.com/{0}.gif", groups[1].Value)) };
            //                }
            //            }
            //        }
            //        else if (albumGroups.Count > 2 && string.IsNullOrWhiteSpace(albumGroups[2].Value))
            //        {
            //            var apiURL = string.Format("{0}album/{1}.json", apiPrefix, albumGroups[1].Value);
            //            var jsonResult = await networkLayer.Get(apiURL, token, progress, null, true);
            //            if(string.IsNullOrWhiteSpace(jsonResult))
            //                return Enumerable.Empty<Tuple<string, string>>();

            //JObject result = null;
            //try
            //{
            //	result = JsonConvert.DeserializeObject(jsonResult) as JObject;
            //}
            //catch { }

            //            if (result != null && result.HasValues)
            //            {
            //                JToken errorToken;
            //                if (result.TryGetValue("error", out errorToken))
            //                {
            //                    if(((JObject)errorToken)["message"].Value<string>() == "Album not found")
            //                        return new Tuple<string, string>[] { Tuple.Create(title, string.Format("http://i.imgur.com/{0}.gif", albumGroups[1].Value)) };
            //                    else
            //                        return Enumerable.Empty<Tuple<string, string>>();
            //                }

            //                var albumTitleElement = (string)((JObject)result.GetValue("album")).GetValue("title");
            //                var albumTitle = string.IsNullOrWhiteSpace(albumTitleElement) ? title : albumTitleElement;

            //                var resultImages = ((IEnumerable)((JObject)result.GetValue("album")).GetValue("images"))
            //                    .Cast<JObject>()
            //                    .Select(e => 
            //                        {
            //                            var caption = (string)((JObject)e.GetValue("image")).GetValue("caption");

            //                            if (!string.IsNullOrWhiteSpace(caption))
            //                                caption = caption.Replace("&#039;", "'").Replace("&#038;", "&").Replace("&#034;", "\"");

            //                            return Tuple.Create(string.IsNullOrWhiteSpace(caption) ? albumTitle : caption, (string)((JObject)e.GetValue("links")).GetValue("original"));
            //                        })
            //		.ToArray();
            //	return resultImages;
            //            }
            //            else
            //	return new Tuple<string, string>[] { Tuple.Create(title, string.Format("http://i.imgur.com/{0}.gif", albumGroups[1].Value)) };
            //        }
            //        else
            //            return Enumerable.Empty<Tuple<string, string>>();
        }
	}
}
