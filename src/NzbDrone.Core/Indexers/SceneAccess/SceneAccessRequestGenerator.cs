using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.IndexerSearch.Definitions;

namespace NzbDrone.Core.Indexers.SceneAccess
{
    public class SceneAccessRequestGenerator : IIndexerRequestGenerator
    {
        public SceneAccessSettings Settings { get; set; }

        public Int32 MaxPages { get; set; }
        public Int32 PageSize { get; set; }

        private NameValueCollection MagicMethodStrings;
        private int[] RssCategories;

        public SceneAccessRequestGenerator()
        {
            MaxPages = 1;
            PageSize = 25;
            MagicMethodStrings = new NameValueCollection {
                {"browse", "method=2&c27=27&c17=17&c11=11"},
                {"archive", "method=1&c26=26"},
                {"nonscene", "method=2&c44=44&c45=44"}
            };
            RssCategories = new int[] {26, 27, 17, 11};
        }

        public virtual IndexerPageableRequestChain GetRecentRequests()
        {
            var pageableRequests = new IndexerPageableRequestChain();
            pageableRequests.Add(GetRssRequests());

            return pageableRequests;
        }

        public virtual IndexerPageableRequestChain GetSearchRequests(SingleEpisodeSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            foreach (var queryTitle in searchCriteria.QueryTitles)
            {
                pageableRequests.Add(GetPagedRequests(MaxPages, "browse",
                    PrepareQuery(queryTitle),
                    String.Format("S{0:00}E{1:00}", searchCriteria.SeasonNumber, searchCriteria.EpisodeNumber)));
            }

            return pageableRequests;
        }

        public virtual IndexerPageableRequestChain GetSearchRequests(SeasonSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            foreach (var queryTitle in searchCriteria.QueryTitles)
            {
                pageableRequests.Add(GetPagedRequests(MaxPages, "archive",
                    PrepareQuery(queryTitle),
                    String.Format("S{0:00}", searchCriteria.SeasonNumber)));

                pageableRequests.Add(GetPagedRequests(MaxPages, "browse",
                    PrepareQuery(queryTitle),
                    String.Format("S{0:00}", searchCriteria.SeasonNumber)));
            }

            return pageableRequests;
        }

        public virtual IndexerPageableRequestChain GetSearchRequests(DailyEpisodeSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            foreach (var queryTitle in searchCriteria.QueryTitles)
            {
                pageableRequests.Add(GetPagedRequests(MaxPages, "browse",
                    PrepareQuery(queryTitle),
                    String.Format("{0:yyyy-MM-dd}", searchCriteria.AirDate)));
            }

            return pageableRequests;
        }

        public virtual IndexerPageableRequestChain GetSearchRequests(AnimeEpisodeSearchCriteria searchCriteria)
        {
            return new IndexerPageableRequestChain();
        }

        public virtual IndexerPageableRequestChain GetSearchRequests(SpecialEpisodeSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            foreach (var queryTitle in searchCriteria.EpisodeQueryTitles)
            {
                pageableRequests.Add(GetPagedRequests(MaxPages, "browse",
                    PrepareQuery(queryTitle)));
            }

            return pageableRequests;
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(Int32 maxPages, String searchType, params String[] searchParameters)
        {
            String searchString = null;

            if (searchParameters.Any())
            {
                searchString = String.Join(" ", searchParameters).Trim();
            }

            searchString = System.Web.HttpUtility.UrlEncode(searchString);

            for (var page = 0; page < maxPages; page++)
            {
                var request = new IndexerRequest(String.Format("{0}/{1}.php?{2}&search={3}&page={4}", 
                    Settings.BaseUrl.TrimEnd('/'), searchType, MagicMethodStrings[searchType], searchString, page),
                    new HttpAccept("application/x-httpd-php"));
                request.HttpRequest.AddCookie(Settings.CookieUid);
                request.HttpRequest.AddCookie(Settings.CookiePass);

                yield return request;
            }

        }

        private IEnumerable<IndexerRequest> GetRssRequests()
        {
            foreach (var category in RssCategories)
            {
                var request = new IndexerRequest(String.Format("{0}/rss.php?feed=dl&cat={1}&passkey={2}",
                    Settings.BaseUrl.TrimEnd('/'), category, Settings.RssKey), HttpAccept.Rss);

                yield return request;
            }
        }

        private String PrepareQuery(String query)
        {
            return query.Replace('+', ' ');
        }
    }
}
