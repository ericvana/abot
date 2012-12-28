﻿using Abot.Core;
using Abot.Poco;
using NUnit.Framework;
using System;
using System.Collections.Concurrent;

namespace Abot.Tests.Unit.Core
{
    [TestFixture]
    public class CrawlDecisionMakerTest
    {
        CrawlDecisionMaker _unitUnderTest;
        CrawlContext _crawlContext;

        [SetUp]
        public void SetUp()
        {
            _crawlContext = new CrawlContext();
            _crawlContext.CrawlConfiguration = new CrawlConfiguration();
            _unitUnderTest = new CrawlDecisionMaker();
        }


        [Test]
        public void ShouldCrawlPage_NullPageToCrawl_ReturnsFalse()
        {
            CrawlDecision result = _unitUnderTest.ShouldCrawlPage(null, new CrawlContext());
            Assert.IsFalse(result.Allow);
            Assert.AreEqual("Null page to crawl", result.Reason);
        }

        [Test]
        public void ShouldCrawlPage_NullCrawlContext_ReturnsFalse()
        {
            CrawlDecision result = _unitUnderTest.ShouldCrawlPage(new PageToCrawl(new Uri("http://a.com/")), null);
            Assert.IsFalse(result.Allow);
            Assert.AreEqual("Null crawl context", result.Reason);
        }

        [Test]
        public void ShouldCrawlPage_NonDuplicate_ReturnsTrue()
        {
            CrawlDecision result = _unitUnderTest.ShouldCrawlPage(
                new PageToCrawl(new Uri("http://a.com/"))
                {
                    IsInternal = true
                },
                new CrawlContext
                {
                    CrawlConfiguration = new CrawlConfiguration(),
                    CrawlStartDate = DateTime.Now
                });
            Assert.IsTrue(result.Allow);
            Assert.AreEqual("", result.Reason);
        }

        [Test]
        public void ShouldCrawlPage_Duplicate_ReturnsFalse()
        {
            _crawlContext.CrawledUrls = new ConcurrentBag<string> { "http://a.com/" };
            CrawlDecision result = _unitUnderTest.ShouldCrawlPage(
                new PageToCrawl(
                    new Uri("http://a.com/")),
                    _crawlContext);
            Assert.IsFalse(result.Allow);
            Assert.AreEqual("Link already crawled", result.Reason);
        }

        [Test]
        public void ShouldCrawlPage_NonHttpOrHttpsSchemes_ReturnsFalse()
        {
            CrawlDecision result = _unitUnderTest.ShouldCrawlPage(new PageToCrawl(new Uri("file:///C:/Users/")), new CrawlContext());
            Assert.IsFalse(result.Allow);
            Assert.AreEqual("Scheme does not begin with http", result.Reason);

            result = _unitUnderTest.ShouldCrawlPage(new PageToCrawl(new Uri("mailto:user@yourdomainname.com")), new CrawlContext());
            Assert.IsFalse(result.Allow);
            Assert.AreEqual("Scheme does not begin with http", result.Reason);

            result = _unitUnderTest.ShouldCrawlPage(new PageToCrawl(new Uri("ftp://user@yourdomainname.com")), new CrawlContext());
            Assert.IsFalse(result.Allow);
            Assert.AreEqual("Scheme does not begin with http", result.Reason);

            result = _unitUnderTest.ShouldCrawlPage(new PageToCrawl(new Uri("callto:+1234567")), new CrawlContext());
            Assert.IsFalse(result.Allow);
            Assert.AreEqual("Scheme does not begin with http", result.Reason);

            result = _unitUnderTest.ShouldCrawlPage(new PageToCrawl(new Uri("tel:+1234567")), new CrawlContext());
            Assert.IsFalse(result.Allow);
            Assert.AreEqual("Scheme does not begin with http", result.Reason);
        }

        [Test]
        public void ShouldCrawlPage_OverMaxPageToCrawlLimit_ReturnsFalse()
        {
            CrawlDecision result = _unitUnderTest.ShouldCrawlPage(
                new PageToCrawl(new Uri("http://a.com/")),
                new CrawlContext
                {
                    CrawlConfiguration = new CrawlConfiguration
                    {
                        MaxPagesToCrawl = 0
                    }
                });
            Assert.IsFalse(result.Allow);
            Assert.AreEqual("MaxPagesToCrawl limit of [0] has been reached", result.Reason);
        }

        [Test]
        public void ShouldCrawlPage_OverCrawlTimeoutSeconds_ReturnsFalse()
        {
            CrawlDecision result = _unitUnderTest.ShouldCrawlPage(
                new PageToCrawl(new Uri("http://a.com/")),
                new CrawlContext
                {
                    CrawlStartDate = DateTime.Now.AddSeconds(-100),
                    CrawlConfiguration = new CrawlConfiguration
                    {
                        CrawlTimeoutSeconds = 99
                    }
                });
            Assert.IsFalse(result.Allow);
            Assert.AreEqual("Crawl timeout of [99] seconds has been reached", result.Reason);
        }

        [Test]
        public void ShouldCrawlPage_CrawlTimeoutSecondsZero_ReturnsTrue()
        {
            CrawlDecision result = _unitUnderTest.ShouldCrawlPage(
                new PageToCrawl(new Uri("http://a.com/")) { IsInternal = true },
                new CrawlContext
                {
                    CrawlStartDate = DateTime.Now.AddSeconds(-100),
                    CrawlConfiguration = new CrawlConfiguration
                    {
                        CrawlTimeoutSeconds = 0 //equivalent to infinity
                    }
                });
            Assert.IsTrue(result.Allow);
            Assert.AreEqual("", result.Reason);
        }

        [Test]
        public void ShouldCrawlPage_IsExternalPageCrawlingEnabledFalse_PageIsExternal_ReturnsFalse()
        {
            CrawlDecision result = _unitUnderTest.ShouldCrawlPage(
                new PageToCrawl(new Uri("http://a.com/"))
                {
                    IsInternal = false
                },
                new CrawlContext
                {
                    CrawlConfiguration = new CrawlConfiguration
                    {
                        IsExternalPageCrawlingEnabled = false
                    },
                    CrawlStartDate = DateTime.Now,
                });
            Assert.IsFalse(result.Allow);
            Assert.AreEqual("Link is external", result.Reason);
        }

        [Test]
        public void ShouldCrawlPage_IsExternalPageCrawlingEnabledTrue_PageIsExternal_ReturnsTrue()
        {
            CrawlDecision result = _unitUnderTest.ShouldCrawlPage(
                new PageToCrawl(new Uri("http://a.com/"))
                {
                    IsInternal = false
                },
                new CrawlContext
                {
                    CrawlConfiguration = new CrawlConfiguration
                    {
                        IsExternalPageCrawlingEnabled = true
                    },
                    CrawlStartDate = DateTime.Now,
                });
            Assert.IsTrue(result.Allow);
            Assert.AreEqual("", result.Reason);
        }

        [Test]
        public void ShouldCrawlPage_IsExternalPageCrawlingEnabledFalse_PageIsInternal_ReturnsTrue()
        {
            CrawlDecision result = _unitUnderTest.ShouldCrawlPage(
                new PageToCrawl(new Uri("http://a.com/"))
                {
                    IsInternal = true
                },
                new CrawlContext
                {
                    CrawlConfiguration = new CrawlConfiguration
                    {
                        IsExternalPageCrawlingEnabled = false
                    },
                    CrawlStartDate = DateTime.Now,
                });
            Assert.IsTrue(result.Allow);
            Assert.AreEqual("", result.Reason);
        }

        [Test]
        public void ShouldCrawlPage_IsExternalPageCrawlingEnabledTrue_PageIsInternal_ReturnsTrue()
        {
            CrawlDecision result = _unitUnderTest.ShouldCrawlPage(
                new PageToCrawl(new Uri("http://a.com/"))
                {
                    IsInternal = true
                },
                new CrawlContext
                {
                    CrawlConfiguration = new CrawlConfiguration
                    {
                        IsExternalPageCrawlingEnabled = true
                    },
                    CrawlStartDate = DateTime.Now,
                });
            Assert.IsTrue(result.Allow);
            Assert.AreEqual("", result.Reason);
        }

        [Test]
        public void ShouldCrawlPage_OverMaxPagesToCrawlPerDomain_ReturnsFalse()
        {
            Uri uri = new Uri("http://a.com/");
            CrawlConfiguration config = new CrawlConfiguration
            {
                MaxPagesToCrawlPerDomain = 100
            };
            ConcurrentDictionary<string,int> countByDomain = new ConcurrentDictionary<string,int>();
            countByDomain.TryAdd(uri.Authority, 100);

            CrawlDecision result = _unitUnderTest.ShouldCrawlPage(
                new PageToCrawl(new Uri(uri.AbsoluteUri + "anotherpage"))
                {
                    IsInternal = true
                },
                new CrawlContext
                {
                    CrawlConfiguration = config,
                    CrawlStartDate = DateTime.Now,
                    CrawlCountByDomain = countByDomain
                });

            Assert.IsFalse(result.Allow);
            Assert.AreEqual("MaxPagesToCrawlPerDomain limit of [100] has been reached for domain [a.com]", result.Reason);
        }


        [Test]
        public void ShouldCrawlPageLinks_NullCrawledPage_ReturnsFalse()
        {
            CrawlDecision result = _unitUnderTest.ShouldCrawlPageLinks(null, new CrawlContext());
            Assert.IsFalse(result.Allow);
            Assert.AreEqual("Null crawled page", result.Reason);
        }

        [Test]
        public void ShouldCrawlPageLinks_NullCrawlContext_ReturnsFalse()
        {
            CrawlDecision result = _unitUnderTest.ShouldCrawlPageLinks(new CrawledPage(new Uri("http://a.com/a.html")){ RawContent = "aaaa" }, null);

            Assert.IsFalse(result.Allow);
            Assert.AreEqual("Null crawl context", result.Reason);
        }

        [Test]
        public void ShouldCrawlPageLinks_NullHtmlContent_ReturnsFalse()
        {
            CrawlDecision result = _unitUnderTest.ShouldCrawlPageLinks(new CrawledPage(new Uri("http://a.com/")) { RawContent = null }, new CrawlContext());
            Assert.IsFalse(result.Allow);
            Assert.AreEqual("Page has no content", result.Reason);
        }

        [Test]
        public void ShouldCrawlPageLinks_WhitespaceHtmlContent_ReturnsFalse()
        {
            CrawlDecision result = _unitUnderTest.ShouldCrawlPageLinks(new CrawledPage(new Uri("http://a.com/")) { RawContent = "     " }, new CrawlContext());
            Assert.IsFalse(result.Allow);
            Assert.AreEqual("Page has no content", result.Reason);
        }

        [Test]
        public void ShouldCrawlPageLinks_EmptyHtmlContent_ReturnsFalse()
        {
            CrawlDecision result = _unitUnderTest.ShouldCrawlPageLinks(new CrawledPage(new Uri("http://a.com/")) { RawContent = "" }, new CrawlContext());
            Assert.IsFalse(result.Allow);
            Assert.AreEqual("Page has no content", result.Reason);            
        }

        [Test]
        public void ShouldCrawlPageLinks_IsExternalPageLinksCrawlingEnabledFalse_ExternalLink_ReturnsFalse()
        {
            CrawlDecision result = _unitUnderTest.ShouldCrawlPageLinks(
                new CrawledPage(new Uri("http://b.com/a.html"))
                {
                    RawContent = "aaaa",
                    IsInternal = false
                },
                new CrawlContext
                {
                    RootUri = new Uri("http://a.com/ "),
                    CrawlConfiguration = new CrawlConfiguration
                    {
                        IsExternalPageLinksCrawlingEnabled = false
                    }
                });
            Assert.AreEqual(false, result.Allow);
            Assert.AreEqual("Link is external", result.Reason);
        }

        [Test]
        public void ShouldCrawlPageLinks_IsExternalPageLinksCrawlingEnabledTrue_InternalLink_ReturnsTrue()
        {
            CrawlDecision result = _unitUnderTest.ShouldCrawlPageLinks(
                new CrawledPage(new Uri("http://b.com/a.html"))
                {
                    RawContent = "aaaa",
                    IsInternal = true
                },
                new CrawlContext
                {
                    RootUri = new Uri("http://a.com/ "),
                    CrawlConfiguration = new CrawlConfiguration
                    {
                        IsExternalPageLinksCrawlingEnabled = true
                    }
                });
            Assert.AreEqual(true, result.Allow);
            Assert.AreEqual("", result.Reason);
        }

        [Test]
        public void ShouldCrawlPageLinks_IsExternalPageLinksCrawlingEnabledFalse_InternalLink_ReturnsTrue()
        {
            CrawlDecision result = _unitUnderTest.ShouldCrawlPageLinks(
                new CrawledPage(new Uri("http://b.com/a.html"))
                {
                    RawContent = "aaaa",
                    IsInternal = true
                },
                new CrawlContext
                {
                    RootUri = new Uri("http://a.com/ "),
                    CrawlConfiguration = new CrawlConfiguration
                    {
                        IsExternalPageLinksCrawlingEnabled = false
                    }
                });
            Assert.AreEqual(true, result.Allow);
            Assert.AreEqual("", result.Reason);
        }


        [Test]
        public void ShouldDownloadPageContent_DownloadablePage_ReturnsTrue()
        {
            Uri valid200StatusUri = new Uri("http://localhost:1111/");

            CrawlDecision result = _unitUnderTest.ShouldDownloadPageContent(new PageRequester("someuseragentstring").MakeRequest(valid200StatusUri), _crawlContext);

            Assert.AreEqual(true, result.Allow);
            Assert.AreEqual("", result.Reason);
        }

        [Test]
        public void ShouldDownloadPageContent_NullCrawledPage_ReturnsFalse()
        {
            CrawlDecision result = _unitUnderTest.ShouldDownloadPageContent(null, new CrawlContext());
            Assert.AreEqual(false, result.Allow);
            Assert.AreEqual("Null crawled page", result.Reason);
        }

        [Test]
        public void ShouldDownloadPageContent_NullCrawlContext_ReturnsFalse()
        {
            CrawlDecision result = _unitUnderTest.ShouldDownloadPageContent(new CrawledPage(new Uri("http://a.com/a.html")), null);

            Assert.IsFalse(result.Allow);
            Assert.AreEqual("Null crawl context", result.Reason);
        }

        [Test]
        public void ShouldDownloadPageContent_NullHttpWebResponse_ReturnsFalse()
        {
            CrawlDecision result = _unitUnderTest.ShouldDownloadPageContent(
                new CrawledPage(new Uri("http://b.com/a.html"))
                {
                    HttpWebResponse = null
                },
                new CrawlContext
                {
                    RootUri = new Uri("http://a.com/ ")
                });
            Assert.AreEqual(false, result.Allow);
            Assert.AreEqual("Null HttpWebResponse", result.Reason);
        }

        [Test]
        public void ShouldDownloadPageContent_HttpStatusNon200_ReturnsFalse()
        {
            Uri non200Uri = new Uri("http://localhost:1111/HttpResponse/Status403");

            CrawlDecision result = _unitUnderTest.ShouldDownloadPageContent(new PageRequester("someuseragentstring").MakeRequest(non200Uri), new CrawlContext());

            Assert.AreEqual(false, result.Allow);
            Assert.AreEqual("HttpStatusCode is not 200", result.Reason);
        }

        [Test]
        public void ShouldDownloadPageContent_NonHtmlPage_ReturnsFalse()
        {
            Uri imageUrl = new Uri("http://localhost:1111/Content/themes/base/images/ui-bg_flat_0_aaaaaa_40x100.png");

            CrawlDecision result = _unitUnderTest.ShouldDownloadPageContent(new PageRequester("someuseragentstring").MakeRequest(imageUrl), _crawlContext);

            Assert.AreEqual(false, result.Allow);
            Assert.AreEqual("Content type is not any of the following: text/html", result.Reason);
        }
    }
}