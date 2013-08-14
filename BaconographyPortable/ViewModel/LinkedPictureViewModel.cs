﻿using BaconographyPortable.Messages;
using BaconographyPortable.Services;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Practices.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.ViewModel
{
    public class LinkedPictureViewModel : ViewModelBase
    {
        public class LinkedPicture : ViewModelBase
        {
			private object _imageSource;
            public object ImageSource
			{
				get
				{
					return _imageSource;
				}
				set
				{
					_imageSource = value;
					RaisePropertyChanged("ImageSource");
				}
			}
            public string Url { get; set; }
            public string Title { get; set; }
            public bool IsAlbum { get; set; }
            public int PositionInAlbum { get; set; }
            public int AlbumSize { get; set; }
        }

        public string LinkId { get; set; }

        public IEnumerable<LinkedPicture> _pictures;
        public IEnumerable<LinkedPicture> Pictures
        {
            get
            {
                return _pictures;
            }
            set
            {
                if (value != null)
                {
                    var refiedValue = value.ToList();
                    if (refiedValue.Count > 1)
                    {
                        int i = 1;
                        foreach (var picture in refiedValue)
                        {
                            picture.IsAlbum = true;
                            picture.PositionInAlbum = i++;
                            picture.AlbumSize = refiedValue.Count;
                        }
                    }
                    else
                    {
                        foreach (var picture in refiedValue)
                        {
                            picture.IsAlbum = false;
                            picture.PositionInAlbum = 1;
                            picture.AlbumSize = 1;
                        }
                    }
                    _pictures = refiedValue;
                }
                else
                    _pictures = null;
            }
        }
        public string ImageTitle
        {
            get
            {
                var firstPicture = Pictures.FirstOrDefault();
                if (firstPicture != null)
                    return firstPicture.Title;
                else
                    return "";
            }
        }

        public string LinkTitle
        {
            get;
            set;
        }

        public bool IsAlbum
        {
            get
            {
                return Pictures != null && Pictures.Count() > 1;
            }
        }

        LinkViewModel _parentLink;
        private LinkViewModel ParentLink
        {
            get
            {
                if (_parentLink == null)
                {
                    if (string.IsNullOrWhiteSpace(LinkId))
                        return null;

                    var viewModelContextService = ServiceLocator.Current.GetInstance<IViewModelContextService>();
                    var firstRedditViewModel = viewModelContextService.ContextStack.FirstOrDefault(context => context is RedditViewModel) as RedditViewModel;
                    if (firstRedditViewModel != null)
                    {
                        for (int i = 0; i < firstRedditViewModel.Links.Count; i++)
                        {
                            var linkViewModel = firstRedditViewModel.Links[i] as LinkViewModel;
                            if (linkViewModel != null)
                            {
                                if (linkViewModel.LinkThing.Data.Id == LinkId)
                                {
                                    _parentLink = linkViewModel;
                                    break;
                                }
                            }
                        }
                    }
                }

                return _parentLink;
            }
        }

        public bool HasContext
        {
            get
            {
                return ParentLink != null;
            }
        }

        public async Task<LinkedPictureViewModel> Previous()
        {
            var parentLink = ParentLink;
            if (parentLink != null)
            {
                var viewModelContextService = ServiceLocator.Current.GetInstance<IViewModelContextService>();
                var firstRedditViewModel = viewModelContextService.ContextStack.FirstOrDefault(context => context is RedditViewModel) as RedditViewModel;
                if (firstRedditViewModel != null)
                {
                    var imagesService = ServiceLocator.Current.GetInstance<IImagesService>();
                    var currentLinkPos = firstRedditViewModel.Links.IndexOf(parentLink);
                    var linksEnumerator = firstRedditViewModel.Links.Take(currentLinkPos).Reverse();
                    return await MakeContextedImageTuple(imagesService, linksEnumerator);

                }
            }
            return null;
        }

        private static async Task<LinkedPictureViewModel> MakeContextedImageTuple(IImagesService imagesService, IEnumerable<ViewModelBase> linksEnumerator)
        {
            var targetViewModel = linksEnumerator.FirstOrDefault(vm => vm is LinkViewModel && imagesService.MightHaveImagesFromUrl(((LinkViewModel)vm).Url)) as LinkViewModel;

            if (targetViewModel != null)
            {
                var smartOfflineService = ServiceLocator.Current.GetInstance<ISmartOfflineService>();
                smartOfflineService.NavigatedToOfflineableThing(targetViewModel.LinkThing, false);
                Messenger.Default.Send<LoadingMessage>(new LoadingMessage { Loading = true });
                await ServiceLocator.Current.GetInstance<IOfflineService>().StoreHistory(targetViewModel.Url);
                var imageResults = await ServiceLocator.Current.GetInstance<IImagesService>().GetImagesFromUrl(targetViewModel.LinkThing == null ? "" : targetViewModel.LinkThing.Data.Title, targetViewModel.Url);
                Messenger.Default.Send<LoadingMessage>(new LoadingMessage { Loading = false });

                if (imageResults != null && imageResults.Count() > 0)
                {
                    var imageTuple = new Tuple<string, IEnumerable<Tuple<string, string>>, string>(targetViewModel.LinkThing != null ? targetViewModel.LinkThing.Data.Title : "", imageResults, targetViewModel.LinkThing != null ? targetViewModel.LinkThing.Data.Id : "");
                    Messenger.Default.Send<LongNavigationMessage>(new LongNavigationMessage { Finished = true, TargetUrl = targetViewModel.Url });
                    return new LinkedPictureViewModel
                    {
                        LinkTitle = imageTuple.Item1.Replace("&amp;", "&").Replace("&lt;", "<").Replace("&gt;", ">").Replace("&quot;", "\"").Replace("&apos;", "'").Trim(),
                        LinkId = imageTuple.Item3,
                        Pictures = imageTuple.Item2.Select(tpl => new LinkedPictureViewModel.LinkedPicture
                        {
                            Title = tpl.Item1.Replace("&amp;", "&").Replace("&lt;", "<").Replace("&gt;", ">").Replace("&quot;", "\"").Replace("&apos;", "'").Trim(),
                            ImageSource = tpl.Item2,
                            Url = tpl.Item2
                        })
                    };
                }
            }

            return null;
        }

        public async Task<LinkedPictureViewModel> Next()
        {
            var parentLink = ParentLink;
            if (parentLink != null)
            {
                var viewModelContextService = ServiceLocator.Current.GetInstance<IViewModelContextService>();
                var firstRedditViewModel = viewModelContextService.ContextStack.FirstOrDefault(context => context is RedditViewModel) as RedditViewModel;
                if (firstRedditViewModel != null)
                {
                    var imagesService = ServiceLocator.Current.GetInstance<IImagesService>();
                    var currentLinkPos = firstRedditViewModel.Links.IndexOf(parentLink);
                    var linksEnumerator = firstRedditViewModel.Links.Skip(currentLinkPos);
                    return await MakeContextedImageTuple(imagesService, linksEnumerator);
                }
            }
            return null;
        }
        
    }
}
