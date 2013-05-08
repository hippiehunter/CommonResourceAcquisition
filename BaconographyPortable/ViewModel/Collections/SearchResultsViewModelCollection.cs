﻿using BaconographyPortable.Model.Reddit;
using BaconographyPortable.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.ViewModel.Collections
{
    public class SearchResultsViewModelCollection : ThingViewModelCollection
    {
        public SearchResultsViewModelCollection(IBaconProvider baconProvider, string query, bool reddits = false) :
            base(baconProvider, 
                new BaconographyPortable.Model.Reddit.ListingHelpers.SearchResults(baconProvider, query, reddits),
                new BaconographyPortable.Model.KitaroDB.ListingHelpers.SearchResults(baconProvider, query, reddits)) { }

        
    }
}
