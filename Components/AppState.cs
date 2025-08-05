using System.Diagnostics;
using System.Xml;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using Tumbleweed.Data;
using Tumbleweed.Data.Models;

namespace Tumbleweed.Components;

public class AppState(AppDbContext context, IHttpClientFactory factory)
{
    public event Action? OnChange;

    private bool _isLoading;
    public event Action? OnIsLoadingChange;
    public bool IsLoading
    {
        get => _isLoading;
        private set { _isLoading = value; OnIsLoadingChange?.Invoke(); OnChange?.Invoke(); }
    }

    private IEnumerable<(Feed Feed, int UnreadCount)> _feeds = [];
    public event Action? OnFeedsChange;
    public IEnumerable<(Feed Feed, int UnreadCount)> Feeds
    {
        get => _feeds;
        private set { _feeds = value; OnFeedsChange?.Invoke(); OnChange?.Invoke(); }
    }

    private Feed? _selectedFeed;
    public event Action? OnSelectedFeedChange;
    public Feed? SelectedFeed
    {
        get => _selectedFeed;
        private set { _selectedFeed = value; OnSelectedFeedChange?.Invoke(); OnChange?.Invoke(); }
    }

    private IEnumerable<Episode> _episodes = [];
    public event Action? OnEpisodesChange;
    public IEnumerable<Episode> Episodes
    {
        get => _episodes;
        private set { _episodes = value; OnEpisodesChange?.Invoke(); OnChange?.Invoke(); }
    }

    private Episode? _selectedEpisode;
    public event Action? OnSelectedEpisodeChange;
    public Episode? SelectedEpisode
    {
        get => _selectedEpisode;
        private set { _selectedEpisode = value; OnSelectedEpisodeChange?.Invoke(); OnChange?.Invoke(); }
    }

    private bool _useIFrame;
    public event Action? OnUseIFrameChange;
    public bool UseIFrame
    {
        get => _useIFrame;
        private set { _useIFrame = value; OnUseIFrameChange?.Invoke(); OnChange?.Invoke(); }
    }

    public async Task ToggleUseIFrameAsync()
    {
        if (SelectedFeed is not null)
        {
            UseIFrame = !UseIFrame;
            SelectedFeed.UseIFrame = UseIFrame;
            await context.Feeds.Where(x => x.Id == SelectedFeed.Id).ExecuteUpdateAsync(c => c.SetProperty(x => x.UseIFrame, UseIFrame));
        }
    }

    public async Task UseLoadingAsync(Func<Task> func)
    {
        IsLoading = true;
        try
        {
            await func();
        }
        catch
        {
            throw;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task LoadFeedsAsync()
    {
        var items = await context.Feeds
            .GroupJoin(context.Episodes, f => f.Id, e => e.FeedId, (f, e) => new { Feed = f, Count = e.Where(x => !x.IsRead).Count() })
            .ToListAsync();

        Feeds = items.Select(x => (x.Feed, x.Count)).ToList();
        SelectedFeed = Feeds.Where(x => x.Feed.Id == SelectedFeed?.Id).SingleOrDefault().Feed;
    }

    public async Task AddFeedAsync(string feedUrl)
    {
        await FetchAddStoreFeed(feedUrl);
        await LoadFeedsAsync();
    }

    public async Task ImportOpmlAsync(Stream opml)
    {
        try
        {
            var feeds = await ResolveFeedUrls(opml);
            await Task.WhenAll(feeds.Select(FetchAddStoreFeed));
        }
        catch (Exception)
        {
            throw;
        }

        static async Task<IEnumerable<string>> ResolveFeedUrls(Stream opml)
        {
            var xmlReader = XmlReader.Create(opml, new XmlReaderSettings { Async = true });
            var opmlDoc = await XDocument.LoadAsync(xmlReader, LoadOptions.None, CancellationToken.None);
            var feedUrls = opmlDoc.Descendants("outline")
                .Where(x => x.Attribute("type")?.Value == "rss")
                .Select(x => x.Attribute("xmlUrl")!.Value)
                .ToList();
            return feedUrls;
        }
    }

    public string ExportOpml()
    {
        var opml = new XElement("opml",
            new XAttribute("version", "2.0"),
            new XElement("head",
                new XElement("title", "My RSS Subscriptions"),
                new XElement("dateCreated", DateTime.UtcNow.ToString("r"))),
            new XElement("body",
                new XElement("outline",
                    new XAttribute("text", "RSS Feeds"),
                    new XAttribute("title", "RSS Feeds"),
                    Feeds.Select((it) => new XElement("outline",
                        new XAttribute("type", "rss"),
                        new XAttribute("text", it.Feed.Title),
                        new XAttribute("title", it.Feed.Title),
                        new XAttribute("xmlUrl", it.Feed.Id))))));
        return opml.ToString();
    }

    public async Task RemoveFeedAsync(string feedId)
    {
        using var tx = await context.Database.BeginTransactionAsync();
        try
        {
            await context.Feeds.Where(x => x.Id == feedId).ExecuteDeleteAsync();
            await context.Episodes.Where(x => x.FeedId == feedId && !x.IsStarred).ExecuteDeleteAsync();
            await tx.CommitAsync();
            await LoadFeedsAsync();
        }
        catch (Exception)
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task SelectFeedAsync(Feed feed)
    {
        SelectedFeed = feed;
        Episodes = await context.Episodes.Where(x => x.FeedId == SelectedFeed.Id).ToListAsync();
    }

    public async Task LoadEpisodesAsync()
    {
        if (SelectedFeed is not null)
            Episodes = await context.Episodes.Where(x => x.FeedId == SelectedFeed.Id).ToListAsync();
        SelectedEpisode = Episodes.Where(x => x.Id == SelectedEpisode?.Id).SingleOrDefault();
    }

    public async Task SelectEpisodeAsync(Episode? episode)
    {
        SelectedEpisode = episode;
        if (SelectedFeed is not null) UseIFrame = SelectedFeed.UseIFrame;

        Debug.WriteLine($"SelectedEpisode: {SelectedEpisode?.Title}");

        if (episode is not null) await MarkCurrentEpisodeAsReadAsync();
    }

    public async Task MarkCurrentEpisodeAsReadAsync()
    {
        if (SelectedEpisode is not null && !SelectedEpisode.IsRead)
        {
            SelectedEpisode.IsRead = true;
            await context.Episodes.Where(x => x.Id == SelectedEpisode.Id).ExecuteUpdateAsync(c => c.SetProperty(x => x.IsRead, true));
            SelectedEpisode.IsRead = true;
            OnSelectedEpisodeChange?.Invoke();
        }
    }

    public async Task MarkCurrentEpisodeAsUnreadAsync()
    {
        if (SelectedEpisode is not null && SelectedEpisode.IsRead)
        {
            SelectedEpisode.IsRead = false;
            await context.Episodes.Where(x => x.Id == SelectedEpisode.Id).ExecuteUpdateAsync(c => c.SetProperty(x => x.IsRead, false));
            SelectedEpisode.IsRead = false;
            OnSelectedEpisodeChange?.Invoke();
        }
    }

    public async Task MarkAllAsReadAsync(Feed? feed = null)
    {
        IQueryable<Episode> episodes = context.Episodes;
        if (feed is not null) episodes = episodes.Where(x => x.FeedId == feed.Id);
        await episodes.ExecuteUpdateAsync(c => c.SetProperty(x => x.IsRead, true));

        await LoadFeedsAsync();
        await LoadEpisodesAsync();
    }

    public async Task StarCurrentEpisodeAsync()
    {
        if (SelectedEpisode is not null && !SelectedEpisode.IsStarred)
        {
            SelectedEpisode.IsStarred = true;
            await context.Episodes.Where(x => x.Id == SelectedEpisode.Id).ExecuteUpdateAsync(c => c.SetProperty(x => x.IsStarred, true));
            SelectedEpisode.IsStarred = true;
            OnSelectedEpisodeChange?.Invoke();
        }
    }

    public async Task UnstarCurrentEpisodeAsync()
    {
        if (SelectedEpisode is not null && SelectedEpisode.IsStarred)
        {
            SelectedEpisode.IsStarred = false;
            await context.Episodes.Where(x => x.Id == SelectedEpisode.Id).ExecuteUpdateAsync(c => c.SetProperty(x => x.IsStarred, false));
            SelectedEpisode.IsStarred = false;
            OnSelectedEpisodeChange?.Invoke();
        }
    }

    private async Task FetchAddStoreFeed(string feedUrl)
    {
        var (feed, episodes) = await FetchFeedAsync(feedUrl);
        if (!await context.Feeds.AnyAsync(x => x.Id == feed.Id))
        {
            await context.Feeds.AddAsync(feed);
        }

        var current = await context.Episodes.Where(x => x.FeedId == feed.Id).Select(x => x.Id).ToListAsync();
        var toAdd = episodes.ExceptBy(current, x => x.Id).ToList();
        await context.Episodes.AddRangeAsync(toAdd);
        await context.SaveChangesAsync();
    }

    private async Task<(Feed Feed, IEnumerable<Episode> Episodes)> FetchFeedAsync(string feedUrl)
    {
        var client = factory.CreateClient("Default");
        var response = await client.GetAsync(feedUrl);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream, System.Text.Encoding.UTF8);
        using var xmlReader = XmlReader.Create(reader);

        var feed = new Feed { Id = feedUrl };
        var episodes = new List<Episode>();
        var flag = xmlReader.Read();

        while (flag)
        {
            if (!TryReadFeedMetaData(xmlReader, feed))
            {
                if (TryReadEpisode(xmlReader, feed.Id, out var episode)) episodes.Add(episode!);
                else flag = xmlReader.Read();
            }
        }

        return (feed, episodes);

        static bool TryReadFeedMetaData(XmlReader xmlReader, Feed feed)
        {
            if (xmlReader.Depth != 2 || xmlReader.NodeType != XmlNodeType.Element) return false;

            string[] sourceArray = ["title", "description", "link"];
            if (sourceArray.Contains(xmlReader.Name))
            {
                if (xmlReader.Name == "title") feed.Title = xmlReader.ReadElementContentAsString();
                if (xmlReader.Name == "description") feed.Description = xmlReader.ReadElementContentAsString();
                if (xmlReader.Name == "link") feed.Link = xmlReader.ReadElementContentAsString();
                return true;
            }

            return false;
        }

        static bool TryReadEpisode(XmlReader xmlReader, string feedId, out Episode? episode)
        {
            episode = null;

            if (xmlReader.Name == "item" && xmlReader.NodeType == XmlNodeType.Element)
            {
                episode = new() { FeedId = feedId };
                using var itemReader = xmlReader.ReadSubtree();

                var flag = itemReader.Read();

                while (flag)
                {
                    if (itemReader.NodeType != XmlNodeType.Element)
                    {
                        flag = itemReader.Read();
                        continue;
                    }

                    switch (itemReader.Name)
                    {
                        case "guid":
                            episode.Id = itemReader.ReadElementContentAsString();
                            break;
                        case "title":
                            episode.Title = itemReader.ReadElementContentAsString();
                            break;
                        case "link":
                            episode.Link = itemReader.ReadElementContentAsString();
                            break;
                        case "pubDate":
                            episode.PublishedDate = DateOnly.FromDateTime(DateTime.Parse(itemReader.ReadElementContentAsString()));
                            break;
                        case "description":
                            episode.Description = itemReader.ReadElementContentAsString();
                            break;
                        case "content:encoded":
                            episode.Content = itemReader.ReadElementContentAsString();
                            break;
                        default:
                            flag = itemReader.Read();
                            break;
                    }
                }

                return true;
            }

            return false;
        }
    }
}
