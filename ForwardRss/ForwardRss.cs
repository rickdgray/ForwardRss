using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.ServiceModel.Syndication;
using System.Xml;

namespace ForwardRss
{
    internal class ForwardRss : BackgroundService
    {
        private readonly Settings _settings;
        private readonly ILogger _logger;

        public ForwardRss(IOptions<Settings> settings,
            ILogger<ForwardRss> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting application.");

            using var client = new HttpClient();

            var lastPoll = DateTime.Now;

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Pulling Feed.");

                var rssReader = XmlReader.Create(_settings.RssFeedUrl);
                var rssFeed = SyndicationFeed.Load(rssReader);
                rssReader.Close();

                var items = rssFeed.Items
                    .Where(i => i.PublishDate > lastPoll)
                    .ToList();
                lastPoll = DateTime.Now;

                _logger.LogInformation("Found {count} new items.", items.Count);

                var notifications = new List<Dictionary<string, string>>();

                foreach (var item in items)
                {
                    if (item == null)
                    {
                        continue;
                    }

                    var data = new Dictionary<string, string>
                    {
                        { "token", _settings.PushoverAppKey },
                        { "user", _settings.PushoverUserKey }
                    };

                    if (item.Summary != null)
                    {
                        data.Add("message", item.Summary.Text);

                        if (item.Title != null)
                        {
                            data.Add("title", item.Title.Text);
                        }
                    }
                    else if (item.Title != null)
                    {
                        data.Add("message", item.Title.Text);
                    }
                    else
                    {
                        continue;
                    }

                    if (item.Links.Any())
                    {
                        var link = item.Links.First();
                        data.Add("url", link.Uri.ToString());
                        data.Add("url_title", "Read more");
                    }

                    notifications.Add(data);
                    _logger.LogDebug("Queued notification. Count: {count}", notifications.Count);
                }

                _logger.LogInformation("Queued all notifications. Count: {count}", notifications.Count);

                foreach (var notification in notifications)
                {
                    await client.PostAsync(
                        "https://api.pushover.net/1/messages.json",
                        new FormUrlEncodedContent(notification),
                        stoppingToken);

                    _logger.LogDebug("Pushed notification; delaying for 3 seconds...");

                    await Task.Delay(3000, stoppingToken);
                }

                _logger.LogInformation(
                    "Pushed all notifications. Waiting until next poll time: {pollTime}",
                    DateTime.Now.AddMinutes(_settings.PollRateInMinutes));

                await Task.Delay(_settings.PollRateInMinutes * 60000, stoppingToken);
            }
        }
    }
}
