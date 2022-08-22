using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace Turkey.Tests
{
    public class SourceBuildTest
    {
        private static readonly string FAKE_FEED = "https://myget.org/my/secret/3.1/feed.json";

        public class ProdConHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage message, CancellationToken token)
            {
                if (message.Method == HttpMethod.Get)
                {
                    if (message.RequestUri.AbsoluteUri.Equals("https://raw.githubusercontent.com/dotnet/source-build/release/3.1/ProdConFeed.txt"))
                    {
                        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(FAKE_FEED)
                        });

                    }
                    else if (message.RequestUri.AbsoluteUri.Equals(FAKE_FEED))
                    {
                        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent("ignore")
                        });
                    }
                }
                Assert.True(false);
                return null;
            }
        }

        public class NoProdConHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage message, CancellationToken token)
            {
                Assert.True(false);
                return null;
            }
        }

        [Fact]
        public async Task VerifyProdConFeedIsLookedUpAndThenTheFeedIsVerifiedToResolve()
        {
            var messageHandler = new ProdConHandler();
            var client = new HttpClient(messageHandler);
            var sourceBuild = new SourceBuild(client);

            var feed = await sourceBuild.GetProdConFeedAsync(Version.Parse("3.1"));

            Assert.Equal(FAKE_FEED, feed);
        }

        [Fact]
        public async Task VerifyProdConFeedIsNotUsedForNewReleases()
        {
            var messageHandler = new NoProdConHandler();
            var client = new HttpClient(messageHandler);
            var sourceBuild = new SourceBuild(client);

            await Assert.ThrowsAsync<ArgumentException>(() => sourceBuild.GetProdConFeedAsync(Version.Parse("6.1")));
        }

    }
}
