using System;
using TweetR.Models;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Channels;
using System.Threading;
using LinqToTwitter;
using System.Linq;

namespace TweetR.Hubs
{
    public class StreamHub : Hub
    {
        public ChannelReader<string> StartTweetStream(CancellationToken cancellationToken)
        {
            var channel = Channel.CreateUnbounded<string>();
            return channel.Reader;
        }

        private async Task WriteTweetAsync(ChannelWriter<string> writer, CancellationToken cancellationToken)
        {
            try
            {
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var apiSettings = new TwitterApiSetting();
                    var auth = new SingleUserAuthorizer
                    {

                        CredentialStore = new InMemoryCredentialStore()
                        {

                            ConsumerKey = apiSettings.ConsumerKey,
                            ConsumerSecret = apiSettings.ConsumerSecrect,
                            OAuthToken = apiSettings.OAuthToken,
                            OAuthTokenSecret = apiSettings.OAuthTokenSecrect
                        }
                    };
                    var twitterCtx = new TwitterContext(auth);

                    await
                       (from strm in twitterCtx.Streaming
                        where strm.Type == StreamingType.Filter &&
                              strm.Track.ToLower() == "azure"
                        select strm)
                       .StartAsync(async strm =>
                       {
                           var content = strm.Content;
                           await writer.WriteAsync(strm.Content);
                       });
                }
            }
            catch (Exception)
            {
                writer.TryComplete();

            }
            writer.TryComplete();
        }

    }
}
