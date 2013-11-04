﻿using BaconographyPortable.Services;
using BaconographyPortable.ViewModel;
using BaconographyWP8.Common;
using Microsoft.Practices.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

<<<<<<< HEAD
namespace BaconographyWP8.Common
=======
namespace BaconographyWP8Core.Common
>>>>>>> 4c5e7082cc57dc2fbaa85dc2c8e2d94383549579
{
    public class LinkTypeTemplateSelector : TypedTemplateSelector
    {
        ISettingsService _settingsService;
        public LinkTypeTemplateSelector()
        {
            _settingsService = ServiceLocator.Current.GetInstance<ISettingsService>();
        }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            if (item is LinkViewModel)
            {

                string key = item != null ? string.Format("LinkType:{0}", _settingsService.OneTouchVoteMode ? "OneTouch" : "Normal") : DefaultTemplateKey;
                return ApplyKey(container, key);
            }
            else
                return base.SelectTemplateCore(item, container);
        }
    }
}
