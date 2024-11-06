using CefSharp;
using CefSharp.OffScreen;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace ArchiveBookScrape
{
    public class CustomBrowser : ChromiumWebBrowser
    {
        public CustomBrowser(CefSharp.Web.HtmlString html, IBrowserSettings browserSettings = null, IRequestContext requestContext = null, bool automaticallyCreateBrowser = true, Action<IBrowser> onAfterBrowserCreated = null) :
            base(html, browserSettings, requestContext, automaticallyCreateBrowser, onAfterBrowserCreated)
        {
        }
        public CustomBrowser(string address = "", IBrowserSettings browserSettings = null, IRequestContext requestContext = null, bool automaticallyCreateBrowser = true, Action<IBrowser> onAfterBrowserCreated = null) :
            base(address, browserSettings, requestContext, automaticallyCreateBrowser, onAfterBrowserCreated)
        {
        }
    }
    public class Scraper
    {
        private static readonly Dictionary<string, BorrowState> StringStateDict =
            new Dictionary<string, BorrowState>
            {
                { "ia-button primary initial", BorrowState.Unborrowed },
                { "ia-button danger initial", BorrowState.Borrowed }
            };

        public enum BorrowState
        {
            Unborrowed,
            Borrowed,
            Unavailable
        }

        //ReusableAwaiter<bool> reusableTCS = null;
        EventHandler<LoadingStateChangedEventArgs> lsc = null;
        List<KeyValuePair<ChromiumWebBrowser, ReusableAwaiter<bool>>> browsers = null;
        public FiddlerProxy proxy = null;

        public IEnumerable<ConcurrentDictionary<string, Rule>> UrlRules
        {
            get
            { 
                return browsers.Select(browser => ((CustomRequestHandler)browser.Key.RequestHandler).urlRules); 
            }
        }

        static readonly string loginScript = @"
        document.getElementsByClassName('form-element input-email')[0].value = '{0}';
        document.getElementsByClassName('form-element input-password')[0].value = '{1}';
        document.getElementsByClassName('btn btn-primary btn-submit input-submit js-submit-login')[0].click();
";

        static readonly string borrowButtonScript = @"document.getElementsByClassName('BookReaderMessage focus-on-child-only')[0].firstChild.shadowRoot.childNodes[1].childNodes[3].shadowRoot.childNodes[2].childNodes[4]";
        static readonly string borrowButtonContentScript = borrowButtonScript + ".childNodes[4].className";
        static readonly string borrowButtonClickScript = borrowButtonScript + ".childNodes[4].click()";

        static readonly string defaultPageSrc = "document.getElementsByClassName('BRpageimage')[0].src";
        static readonly string currentPageScript = @"document.getElementsByClassName('BRcurrentpage')[0].textContent";

        static readonly string flipPageNext = @"document.getElementsByClassName('BRicon book_right book_flip_next')[0].click()";
        static readonly string flipPageBack = @"document.getElementsByClassName('BRicon book_left book_flip_prev')[0].click()";

        static readonly string zoomScript = @"document.getElementsByClassName('BRicon zoom_in')[0].click();";

        public async Task Initialize(int instances = 1)
        {
            //reusableTCS = new ReusableAwaiter<bool>();

            lsc =
            new EventHandler<LoadingStateChangedEventArgs>((sender, args) =>
            {
                //Wait for the Page to finish loading
                if (args.IsLoading == false)
                {
                    foreach (KeyValuePair<ChromiumWebBrowser, ReusableAwaiter<bool>> tab in browsers)
                        if (object.ReferenceEquals(tab.Key.GetBrowser(), args.Browser))
                            tab.Value.TrySetResult(true);
                    //reusableTCS.TrySetResult(true);
                    Debug.WriteLine("LoadingStateChanged invoked");
                }
            });

            if (instances < 1)
                throw new Exception("Initialize: Number of instances cannot be less than 1");

            proxy = new FiddlerProxy();
            string proxyAddress = await proxy.Initialize();
            Debug.WriteLine($"Proxy: {proxyAddress}");

            browsers = new List<KeyValuePair<ChromiumWebBrowser, ReusableAwaiter<bool>>>();
            for (int i = 0; i < instances; i++)
            {
                ChromiumWebBrowser browser = new ChromiumWebBrowser()
                { RequestHandler = new CustomRequestHandler() };
                browser.LoadingStateChanged += lsc;

                while (!browser.IsBrowserInitialized)
                {
                    Debug.WriteLine("Initializing");
                    await Task.Delay(1000);
                }
                await SetProxy(browser, proxyAddress);
                browsers.Add(new KeyValuePair<ChromiumWebBrowser, ReusableAwaiter<bool>>
                    (browser, new ReusableAwaiter<bool>()));
            }
        }

        public static Task SetProxy(IWebBrowser webBrowser, string address)
        {
            return Cef.UIThreadTaskFactory.StartNew(() =>
            {
                var context = webBrowser.GetBrowser().GetHost().RequestContext;

                context.SetPreference("proxy", new Dictionary<string, object>
                {
                    ["mode"] = "fixed_servers",
                    ["server"] = address
                }, out _);
            });
        }

        private string getPageUrlFormatted(string url)
        {
            Uri uri = new Uri(url);
            string firstPageUrl = url.Replace(uri.AbsolutePath, "");

            string newPath = "";
            foreach (string segment in uri.Segments)
            {
                newPath += segment;
                if (segment == "page/")
                    break;
            }

            if (!uri.Segments.Contains("page/"))
                newPath += "/page/";

            newPath += "n{0}/";

            return firstPageUrl + newPath;
        }

        private (string urlFormatted, int zeroPad) PageEnumeratorUrl(string src)
        {
            Uri uri = new Uri(src);
            NameValueCollection queries = HttpUtility.ParseQueryString(uri.Query);

            string file = queries.Get("file");
            //Because the last occurence of "_" is the start of the enumarator
            int enumatorIndex = file.LastIndexOf("_") + 1;

            int zeropad = 0;
            int temp = enumatorIndex;
            while (file[temp] >= '0' && file[temp] <= '9')
            {
                zeropad++;
                temp++;
            }

            file = file.Remove(enumatorIndex, temp - enumatorIndex);
            file = file.Insert(enumatorIndex, "{*}");

            queries.Set("file", file);

            string url = uri.GetLeftPart(UriPartial.Path) + "?" + queries;
            url = Uri.UnescapeDataString(url);

            return (url, zeropad);
        }

        private int GetPage(string src)
        {
            Uri uri = new Uri(src);
            NameValueCollection queries = HttpUtility.ParseQueryString(uri.Query);

            string file = queries.Get("file");
            //Because the last occurence of "_" is the start of the enumarator
            int enumatorIndex = file.LastIndexOf("_") + 1;

            int zeropad = 0;
            int temp = enumatorIndex;
            while (file[temp] >= '0' && file[temp] <= '9')
            {
                zeropad++;
                temp++;
            }

            return int.Parse(file.Substring(enumatorIndex, temp - enumatorIndex));
        }

        private string GetMaxZoomUrl(string url)
        {
            Uri uri = new Uri(url);
            var nameValues = HttpUtility.ParseQueryString(uri.Query);
            nameValues.Set("scale", "1");

            return uri.GetLeftPart(UriPartial.Path) + "?" + nameValues;
        }

        private string TrimSpecialCharacters(string str)
        {
            int start = 0;
            int end = 0;

            for (int i = 0; i < str.Length; i++)
                if (char.IsLetter(str[i]))
                {
                    start = i;
                    break;
                }
            for (int i = str.Length - 1; i >= 0; i--)
                if (char.IsLetter(str[i]))
                {
                    end = i;
                    break;
                }

            return str.Substring(start, end - start + 1);
        }

        private int MaxNumber(int numOfDigits)
        {
            return (int)Math.Pow(10, numOfDigits) - 1;
        }

        private async Task<string> GetBRImageSrc(ChromiumWebBrowser browser, int interval)
        {
            var result = await TryEvaluteScript(browser, defaultPageSrc);
            if (!result.jr.Success)
                throw new Exception("GetBRImageSrc: Can't get the source of default BGImage");
            return Uri.UnescapeDataString((string)result.jr.Result);
        }

        private async Task<BorrowState> GetBorrowState(ChromiumWebBrowser browser)
        {
            var buttonClassName = await TryEvaluteScript(browser, borrowButtonContentScript, attempts: 5);
            if (!buttonClassName.isSuccess)
                throw new Exception($"Error at GetBorrowState: {buttonClassName.jr.Message}");

            //Just in case
            string stateStr = TrimSpecialCharacters((string)buttonClassName.jr.Result);
            Debug.WriteLine(stateStr);
            BorrowState state = BorrowState.Unavailable;
            if (!StringStateDict.TryGetValue(stateStr, out state))
                return BorrowState.Unavailable;
            return state;
        }

        private async Task<int[]> GetPageInfo(ChromiumWebBrowser browser)
        {
            var pageInfo = await TryEvaluteScript(browser, currentPageScript, attempts: 5);
            if (!pageInfo.isSuccess)
                throw new Exception($"Error at IsOnLastPage: {pageInfo.jr.Message}");

            string info = String.Join("", ((string)pageInfo.jr.Result).Where(char.IsLetterOrDigit));

            string[] split = info.Split(new string[] { "of" }, StringSplitOptions.None);
            int[] pages = new int[2];

            foreach (string s in split)
                Debug.WriteLine(s);

            pages[0] = int.Parse(split[0]);
            pages[1] = int.Parse(split[1]);

            return pages;
        }

        private CefSharp.Cookie CloneCookie(CefSharp.Cookie cookie)
        {
            CefSharp.Cookie result = new CefSharp.Cookie()
            {
                Name = cookie.Name,
                Value = cookie.Value,
                Domain = cookie.Domain,
                Path = cookie.Path,
                Secure = cookie.Secure,
                HttpOnly = cookie.HttpOnly,
                Expires = cookie.Expires,
                SameSite = cookie.SameSite,
                Priority = cookie.Priority
            };

            return result;
        }

        private async Task CopyCookies(ChromiumWebBrowser source, ChromiumWebBrowser dest)
        {
            ICookieManager cookieManager = source.GetCookieManager();

            if (cookieManager == null)
                throw new Exception("Can't get cookies. Source browser is null.");

            TaskCookieVisitor tcv = new TaskCookieVisitor();
            cookieManager.VisitUrlCookies("https://archive.org/", true, tcv);

            List<CefSharp.Cookie> cookieList = await tcv.Task;

            ICookieManager cookieManagerTarget = dest.GetCookieManager();

            if (cookieManager == null)
                throw new Exception("Can't set cookies. Destination browser is null.");

            foreach (CefSharp.Cookie cookie in cookieList)
            {
                await cookieManagerTarget.SetCookieAsync("https://archive.org/", CloneCookie(cookie));
            }
        }
        
        private async Task<(bool isSuccess, JavascriptResponse jr)> TryEvaluteScript(ChromiumWebBrowser browser, string script, int interval = 1000, int attempts = -1)
        {
            int counter = 0;
            while (true)
            {
                JavascriptResponse jr = await browser.EvaluateScriptAsync(script);
                if (jr.Message == null)
                    return (true, jr);

                counter++;
                Debug.WriteLine(counter);
                if (counter != -1 && counter == attempts)
                    return (false, jr);

                await Task.Delay(1000);
            }
        }

        private async Task Login(ChromiumWebBrowser browser, ReusableAwaiter<bool> reusableTCS,
            string email, string password)
        {
            browser.Load("https://archive.org/account/login");
            await reusableTCS.Reset();

            string script = string.Format(loginScript, email, password);
            await TryEvaluteScript(browser, script);
        }

        private async Task<bool> Borrow(ChromiumWebBrowser browser)
        {
            BorrowState state = await GetBorrowState(browser);
            Debug.WriteLine(state.ToString());
            switch (state)
            {
                case BorrowState.Unborrowed:
                    browser.ExecuteScriptAsync(borrowButtonClickScript);
                    return true;
            }
            return false;
        }

        private async Task ZoomToMax(ChromiumWebBrowser browser)
        {
            string src = await GetBRImageSrc(browser, 1000);

            //Zoom to get maximum resolution
            while (!src.Contains("&scale=1&"))
            {
                Debug.WriteLine("Zooming");
                browser.ExecuteScriptAsync(zoomScript);
                src = await GetBRImageSrc(browser, 1000);
            }

            Debug.WriteLine("Done Zooming");
            Debug.WriteLine(src);
        }

        private async Task DownloadAll(ChromiumWebBrowser browser, ReusableAwaiter<bool> reusableTCS,
            string startUrl, string filenameFormatted, int startPage, int maxPageCount, int skipPageCount)
        {
            //string startUrl = string.Format(url, startPage.ToString());
            Debug.WriteLine($"Downloading: {startUrl}");
            browser.Load(startUrl);
            await reusableTCS.Reset();

            //int currentPage = startPage;
            int stopPage = startPage + maxPageCount;
            Debug.WriteLine($"Stop page: {stopPage}");

            //Get the accurate BRImageSrc after borrowing
            string src = await GetBRImageSrc(browser, 1000);
            string maxZoomSrc = GetMaxZoomUrl(src);

            var pe = PageEnumeratorUrl(maxZoomSrc);

            Debug.WriteLine("Done Getting Enumerator Url");
            Debug.WriteLine(pe.urlFormatted);
            Debug.WriteLine(pe.zeroPad);

            CustomRequestHandler handler = (CustomRequestHandler)browser.RequestHandler;

            Rule rule = new Rule(filenameFormatted, startPage, maxPageCount, false);

            if (!handler.urlRules.TryAdd(pe.urlFormatted, rule))
                Debug.WriteLine("AddRule: Url clash");

            await ZoomToMax(browser);
            await TryEvaluteScript(browser, flipPageNext, attempts: 5);

            Debug.WriteLine($"Starting at {startPage}, maximum page download of {maxPageCount} page(s)");

            while (true)
            {
                await Task.Delay(500);

                //Debug.WriteLine($"Page count: {currentPage - startPage}");

                //if (maxPageCount != -1 && rule.RequestedPages.Contains(stopPage))
                //    break;

                //We're using Mapper.Values instead because only when the page is requested is it added to our Mapper
                if (maxPageCount != -1 && rule.Mapper.Values.Contains(stopPage))
                    break;

                    //int temp = rule.requestedPages.Last();

                    //while (currentPage == temp)
                    //{
                    //    temp = rule.requestedPages.Last();
                    //    await Task.Delay(500);
                    //}

                    //currentPage = temp;

                    //Debug.WriteLine($"Downloading at page {currentPage}, {currentPage + 1}");

                    //Flip the page
                    var result = await TryEvaluteScript(browser, flipPageNext, attempts: 5);

                if (!result.isSuccess)
                    throw new Exception($"DownloadAll: Can't flip page ({result.jr.Message})");
            }

            Debug.WriteLine("Finished Flipping");

            while (true)
            {
                int[] status = rule.OutputPages();
                if (status.Count(i => i == 1 | i == 2) >= (maxPageCount - skipPageCount))
                {
                    for (int i = 0; i < status.Length - 1; i++)
                    {
                        int index = i;
                        if (status[index] == 0)
                            rule.SkippedPages.Add(startPage + index);
                    }
                    break;
                }
                await Task.Delay(3000);
            }

            while ((rule.DownloadedPages.Count - rule.FailedPages.Count) != maxPageCount)
                await Task.Delay(3000);

            Debug.WriteLine("Finished Downloading");
        }

        public async void Download(string email, string password, string url, string folderPath)
        {
            Debug.WriteLine("Stage 0");
            string urlFormatted = getPageUrlFormatted(url);

            await Login(browsers[0].Key, browsers[0].Value, email, password);
            await browsers[0].Value.Reset();
            Debug.WriteLine("Stage 1");

            string firstPageUrl = string.Format(urlFormatted, 1);
            browsers[0].Key.Load(firstPageUrl);

            await browsers[0].Value.Reset();
            //Assuming when BRImage appears everything has loaded
            string src = await GetBRImageSrc(browsers[0].Key, 1500);
            Debug.WriteLine("Stage 2-1");

            //Borrow the book
            if (!src.Contains(@"BookReader/BookReaderImages.php"))
                if (await Borrow(browsers[0].Key))
                {
                    await browsers[0].Value.Reset();
                    Debug.WriteLine("Borrowed");
                }
            Debug.WriteLine("Stage 2");

            for (int i = 1; i < browsers.Count; i++)
                await CopyCookies(browsers[0].Key, browsers[i].Key);

            src = await GetBRImageSrc(browsers[0].Key, 1000);
            int zeroPad = PageEnumeratorUrl(src).zeroPad;

            //Load the maximum page number to get redirected to the last page
            browsers[0].Key.Load(string.Format(urlFormatted, MaxNumber(zeroPad)));
            await browsers[0].Value.Reset();

            //int maxPage = GetPage(await GetBRImageSrc(browsers[0].Key, 1000));
            int maxPage = (await GetPageInfo(browsers[0].Key))[1];
            int pagesPerBrowser = maxPage / browsers.Count;
            int remainder = maxPage % browsers.Count;

            int[,] skipPageCounts = new int[browsers.Count, 3];
            
            Debug.WriteLine("Stage 3");
            int previousEndIndex = 0;
            for (int i = 0; i < browsers.Count; i++)
            {
                //startPage is index, so it's lowest value is 0
                int startPage = i * pagesPerBrowser;
                int pages = i == browsers.Count - 1 ? pagesPerBrowser + remainder : pagesPerBrowser;

                //endPage is index, so it's lowest value is 0
                browsers[0].Key.Load(string.Format(urlFormatted, startPage));
                await browsers[0].Value.Reset();
                int startIndex = GetPage(await GetBRImageSrc(browsers[0].Key, 1000));

                int endPage = startPage + pages - 1;
                browsers[0].Key.Load(string.Format(urlFormatted, endPage));
                await browsers[0].Value.Reset();
                int endIndex = GetPage(await GetBRImageSrc(browsers[0].Key, 1000));

                int startSkipPage = startIndex - previousEndIndex - 1;
                int skipPage = endIndex - startIndex + 1 - pages;
                int totalSkipPage = startSkipPage + skipPage;

                skipPageCounts[i, 0] = previousEndIndex;
                //Because previousEndIndex is one unit behind
                skipPageCounts[i, 1] = endIndex - previousEndIndex;
                skipPageCounts[i, 2] = totalSkipPage;

                previousEndIndex = endIndex;
            }

            List<Task> tasks = new List<Task>();
            for (int i = 0; i < browsers.Count; i++)
            {
                //int startPage = i * pagesPerBrowser;
                //int pages = i == browsers.Count - 1 ? pagesPerBrowser + remainder : pagesPerBrowser;

                int startPageUrl = i * pagesPerBrowser;
                int startPage = skipPageCounts[i, 0];
                int pages = skipPageCounts[i, 1];
                int skipPage = skipPageCounts[i, 2];

                tasks.Add(DownloadAll(browsers[i].Key, browsers[i].Value, string.Format(urlFormatted, startPageUrl)
                    , @"D:\ArchiveBooks\test{*}", startPage, pages, skipPage));

                Debug.WriteLine($"Added browser {i}, at url {string.Format(urlFormatted, startPage)}, start page {startPage} and page count of {pages}");
            }

            await Task.WhenAll(tasks);
            Debug.WriteLine("Stage 4");
        }
    }
}
