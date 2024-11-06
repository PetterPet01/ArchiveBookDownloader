using CefSharp;
using CefSharp.OffScreen;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;
using System.Net;
using System.Collections.Specialized;
using System.IO;
using System.Drawing.Imaging;
using System.Net.Mime;
using DiffMatchPatch;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Collections.Concurrent;

namespace ArchiveBookScrape
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            CefSettings Settings = new CefSettings();
            //Settings.CachePath = @"D:\ArchiveBooks";

            CefSharp.Cef.Initialize(Settings);

            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //chromiumWebBrowser1.Load("https://archive.org/account/login");
            bool isElevated;
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                isElevated = principal.IsInRole(WindowsBuiltInRole.Administrator);
            }

            Debug.WriteLine($"Elevated: {isElevated}");
        }

        #region Former
        //        private static readonly Dictionary<string, BorrowState> StringStateDict =
        //            new Dictionary<string, BorrowState>
        //            {
        //                { "ia-button primary initial", BorrowState.Unborrowed },
        //                { "ia-button danger initial", BorrowState.Borrowed }
        //            };

        //        public enum BorrowState
        //        {
        //            Unborrowed,
        //            Borrowed,
        //            Unavailable
        //        }

        //        public class BitmapReader
        //        {
        //            byte[] data;
        //            int width;
        //            int height;
        //            int stride;

        //            public BitmapReader(byte[] data, int width, int height)
        //            {
        //                this.data = data;
        //                this.width = width;
        //                this.height = height;
        //                Bitmap source = new Bitmap(width, height);
        //                BitmapData bd = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

        //                this.stride = bd.Stride;
        //                source.Dispose();
        //            }

        //            public Color GetPixel(int x, int y)
        //            {
        //                int lineStart = (y + 1) * stride;
        //                int offset = lineStart + x * 4;

        //                return Color.FromArgb(data[offset + 0],
        //                    data[offset + 1],
        //                    data[offset + 2],
        //                    data[offset + 3]);
        //            }
        //        }

        //        public class PageEnumerator
        //        {
        //            public string urlFormatted;
        //            public int zeroPad;

        //            public PageEnumerator(string urlFormatted, int zeroPad)
        //            {
        //                this.urlFormatted = urlFormatted;
        //                this.zeroPad = zeroPad;
        //            }

        //            public string PageAt(int page)
        //            {
        //                string formatPattern = "D" + zeroPad.ToString();
        //                return string.Format(urlFormatted, page.ToString(formatPattern));
        //            }
        //        }

        //        ReusableAwaiter<bool> reusableTCS = null;
        //        EventHandler<LoadingStateChangedEventArgs> lsc = null;
        //        CustomRequestHandler handler = null;

        //        static readonly string loginScript = @"
        //        document.getElementsByClassName('form-element input-email')[0].value = '{0}';
        //        document.getElementsByClassName('form-element input-password')[0].value = '{1}';
        //        document.getElementsByClassName('btn btn-primary btn-submit input-submit js-submit-login')[0].click();
        //";

        //        static readonly string borrowButtonScript = @"document.getElementsByClassName('BookReaderMessage focus-on-child-only')[0].firstChild.shadowRoot.childNodes[1].childNodes[3].shadowRoot.childNodes[2].childNodes[4]";
        //        static readonly string borrowButtonContentScript = borrowButtonScript + ".childNodes[4].className";
        //        static readonly string borrowButtonClickScript = borrowButtonScript + ".childNodes[4].click()";

        //        //        static readonly string borrowScript = @"(function () {
        //        //    var ele = document.getElementById('IABookReaderMessageWrapper');
        //        //    var rect = ele.getBoundingClientRect();
        //        //    return rect.y + rect.height / 2;
        //        //})();";

        //        static readonly string currentPageScript = @"document.getElementsByClassName('BRcurrentpage')[0].textContent";

        //        static readonly string flipPageNext = @"document.getElementsByClassName('BRicon book_right book_flip_next')[0].click()";
        //        static readonly string flipPageBack = @"document.getElementsByClassName('BRicon book_left book_flip_prev')[0].click()";

        //        static readonly string zoomScript = @"document.getElementsByClassName('BRicon zoom_in')[0].click();";
        //        private static IEnumerable<HtmlNode> GetElementsByClassName(HtmlAgilityPack.HtmlDocument htmlDoc, string name)
        //        {
        //            return htmlDoc.DocumentNode.Descendants(0)
        //        .Where(n => n.HasClass(name));
        //        }

        //        private void Initialize()
        //        {
        //            reusableTCS = new ReusableAwaiter<bool>();

        //            var newsettings = new BrowserSettings();



        //            handler = new CustomRequestHandler();
        //            chromiumWebBrowser1.RequestHandler = handler;

        //            lsc =
        //            new EventHandler<LoadingStateChangedEventArgs>((sender, args) =>
        //            {
        //                //Wait for the Page to finish loading
        //                if (args.IsLoading == false)
        //                {
        //                    reusableTCS.TrySetResult(true);
        //                    Debug.WriteLine("LoadingStateChanged invoked");
        //                }
        //            });
        //            chromiumWebBrowser1.LoadingStateChanged += lsc;
        //        }

        //        private async Task<(bool isSuccess, JavascriptResponse jr)> TryEvaluteScript(string script, int interval = 1000, int attempts = -1)
        //        {
        //            int counter = 0;
        //            while (true)
        //            {
        //                JavascriptResponse jr = await chromiumWebBrowser1.EvaluateScriptAsync(script);
        //                if (jr.Message == null)
        //                    return (true, jr);

        //                counter++;
        //                Debug.WriteLine(counter);
        //                if (counter != -1 && counter == attempts)
        //                    return (false, jr);

        //                await Task.Delay(1000);
        //            }
        //        }

        //        private async Task Login(string email, string password)
        //        {
        //            chromiumWebBrowser1.Load("https://archive.org/account/login");
        //            await reusableTCS.Reset();

        //            string script = string.Format(loginScript, email, password);
        //            chromiumWebBrowser1.ExecuteScriptAsync(script);
        //        }

        //        private async Task<string> WaitUntilContains(string value, int interval)
        //        {
        //            string src;
        //            while (true)
        //            {
        //                Debug.WriteLine("Waiting...");
        //                await Task.Delay(interval);
        //                src = WebUtility.HtmlDecode(await chromiumWebBrowser1.GetSourceAsync());
        //                if (src.Contains(value))
        //                    break;
        //            }
        //            return src;
        //        }

        //        private async Task<string> GetBRImageSrc(int interval)
        //        {
        //            string src = await WaitUntilContains(@"class=""BRpageimage""", interval);

        //            HtmlAgilityPack.HtmlDocument htmlDoc = new HtmlAgilityPack.HtmlDocument();
        //            htmlDoc.LoadHtml(src);
        //            IEnumerable<HtmlNode> nodes = GetElementsByClassName(htmlDoc, "BRpageimage");
        //            return Uri.UnescapeDataString(nodes.First().Attributes["src"].Value);
        //        }

        //        private int GetXPos(byte[] img)
        //        {
        //            int target = Color.FromArgb(25, 72, 128).ToArgb();
        //            using (var ms = new MemoryStream(img))
        //            {
        //                Bitmap bmp = (Bitmap)Image.FromStream(ms);

        //                int buttonXPos = 0;
        //                int width = 0;
        //                using (var fastBitmap = bmp.FastLock())
        //                {
        //                    bool done = false;
        //                    for (int y = 0; y < bmp.Height; y++)
        //                    {
        //                        if (done)
        //                            break;
        //                        for (int x = 0; x < bmp.Width; x++)
        //                        {
        //                            Color c = fastBitmap.GetPixel(x, y);
        //                            if (c == Color.FromArgb(25, 72, 128))
        //                            {
        //                                if (width == 0)
        //                                    buttonXPos = x - 1;
        //                                width++;
        //                            }
        //                            if (width > 0 && c == Color.FromArgb(197, 209, 223))
        //                            {
        //                                done = true;
        //                                break;
        //                            }
        //                        }
        //                    }
        //                }
        //                return buttonXPos + width / 2;
        //            }
        //        }

        //        //        private async void Borrow()
        //        //        {
        //        //            int xPosition = GetXPos(await chromiumWebBrowser1.CaptureScreenshotAsync());
        //        //            Debug.WriteLine(xPosition);

        //        //            Debug.WriteLine("Borrowing");
        //        //            // get button's position
        //        //            int yPosition = -1;
        //        //            var jsReponse = await chromiumWebBrowser1.EvaluateScriptAsync(borrowScript);
        //        //            if (jsReponse.Success && jsReponse.Result != null)
        //        //                yPosition = (int)Math.Ceiling((double)jsReponse.Result);
        //        //            else
        //        //                Debug.WriteLine(jsReponse.Message);
        //        //            Debug.WriteLine("Initiated");
        //        //            Debug.WriteLine(yPosition);
        //        //            //send mouse click event
        //        //            if (yPosition != -1)
        //        //            {
        //        //                Debug.WriteLine(xPosition);
        //        //                Debug.WriteLine(yPosition);

        //        //                using (var ms = new MemoryStream(await chromiumWebBrowser1.CaptureScreenshotAsync()))
        //        //                {
        //        //                    Image img = Image.FromStream(ms);
        //        //                    using (Graphics grf = Graphics.FromImage(img))
        //        //                    {
        //        //                        using (Brush brsh = new SolidBrush(ColorTranslator.FromHtml("#ff00ffff")))
        //        //                        {
        //        //                            grf.FillEllipse(brsh, xPosition, yPosition, 19, 19);
        //        //                        }
        //        //                    }
        //        //                    img.Save(@"D:\ArchiveBooks\test.png");
        //        //                }

        //        //                var host = chromiumWebBrowser1.GetBrowser().GetHost();
        //        //        host.SendMouseMoveEvent(xPosition, yPosition, false, CefEventFlags.None);
        //        //                await Task.Delay(50);
        //        //        host.SendMouseClickEvent(xPosition, yPosition, MouseButtonType.Left, false, 1, CefEventFlags.None);
        //        //                await Task.Delay(50);
        //        //        host.SendMouseClickEvent(xPosition, yPosition, MouseButtonType.Left, true, 1, CefEventFlags.None);
        //        //            }
        //        //}

        //        private string TrimSpecialCharacters(string str)
        //        {
        //            int start = 0;
        //            int end = 0;

        //            for (int i = 0; i < str.Length; i++)
        //                if (char.IsLetter(str[i]))
        //                {
        //                    start = i;
        //                    break;
        //                }
        //            for (int i = str.Length - 1; i >= 0; i--)
        //                if (char.IsLetter(str[i]))
        //                {
        //                    end = i;
        //                    break;
        //                }

        //            return str.Substring(start, end - start + 1);
        //        }

        //        private async Task<BorrowState> GetBorrowState()
        //        {
        //            var buttonClassName = await TryEvaluteScript(borrowButtonContentScript, attempts: 5);
        //            if (!buttonClassName.isSuccess)
        //                throw new Exception($"Error at GetBorrowState: {buttonClassName.jr.Message}");

        //            //Just in case
        //            string stateStr = TrimSpecialCharacters((string)buttonClassName.jr.Result);
        //            Debug.WriteLine(stateStr);
        //            BorrowState state = BorrowState.Unavailable;
        //            if (!StringStateDict.TryGetValue(stateStr, out state))
        //                return BorrowState.Unavailable;
        //            return state;
        //        }

        //        private async Task<bool> Borrow()
        //        {
        //            BorrowState state = await GetBorrowState();
        //            Debug.WriteLine(state.ToString());
        //            switch (state)
        //            {
        //                case BorrowState.Unborrowed:
        //                    chromiumWebBrowser1.ExecuteScriptAsync(borrowButtonClickScript);
        //                    return true;
        //            }
        //            return false;
        //        }

        //        private async Task<int[]> GetPageInfo()
        //        {
        //            var pageInfo = await TryEvaluteScript(currentPageScript, attempts: 5);
        //            if (!pageInfo.isSuccess)
        //                throw new Exception($"Error at IsOnLastPage: {pageInfo.jr.Message}");

        //            string info = String.Join("", ((string)pageInfo.jr.Result).Where(char.IsLetterOrDigit));

        //            string[] split = info.Split(new string[] { "of" }, StringSplitOptions.None);
        //            int[] pages = new int[2];
        //            pages[0] = int.Parse(split[0]);
        //            pages[1] = int.Parse(split[1]);

        //            return pages;
        //        }

        //        private bool IsOnLastPage(string info)
        //        {
        //            if (info.Length % 2 != 0)
        //                return false;

        //            int halfLen = info.Length / 2;
        //            if (info.Substring(0, halfLen) == info.Substring(halfLen, halfLen))
        //                return true;

        //            return false;
        //        }

        //        //private PageEnumerator PageEnumeratorUrl(string src)
        //        //{
        //        //    Uri uri = new Uri(src);
        //        //    NameValueCollection queries = HttpUtility.ParseQueryString(uri.Query);

        //        //    //+ 1 to compensate for the "=" sign in &file=
        //        //    //int fileIndex = src.IndexOf("&file=") + 6;

        //        //    //string id = queries.Get("id");
        //        //    string file = queries.Get("file");
        //        //    //Because the last occurence of "_" is the start of the enumarator
        //        //    int enumatorIndex = file.LastIndexOf("_") + 1;


        //        //    int zeropad = 0;
        //        //    int temp = enumatorIndex;
        //        //    while (file[temp] == '0')
        //        //    {
        //        //        zeropad++;
        //        //        temp++;
        //        //    }
        //        //    //Add 1 more because the last number wasn't 0
        //        //    zeropad++;
        //        //    temp++;

        //        //    file = file.Remove(enumatorIndex, temp - enumatorIndex);
        //        //    file = file.Insert(enumatorIndex, "{0}");

        //        //    queries.Set("file", file);

        //        //    string url = uri.GetLeftPart(UriPartial.Path) + "?" + queries;
        //        //    url = Uri.UnescapeDataString(url);

        //        //    return new PageEnumerator(url, zeropad);
        //        //}

        //        private (string urlFormatted, int zeroPad) PageEnumeratorUrl(string src)
        //        {
        //            Uri uri = new Uri(src);
        //            NameValueCollection queries = HttpUtility.ParseQueryString(uri.Query);

        //            //+ 1 to compensate for the "=" sign in &file=
        //            //int fileIndex = src.IndexOf("&file=") + 6;

        //            //string id = queries.Get("id");
        //            string file = queries.Get("file");
        //            //Because the last occurence of "_" is the start of the enumarator
        //            int enumatorIndex = file.LastIndexOf("_") + 1;


        //            int zeropad = 0;
        //            int temp = enumatorIndex;
        //            while (file[temp] == '0')
        //            {
        //                zeropad++;
        //                temp++;
        //            }
        //            //Add 1 more because the last number wasn't 0
        //            zeropad++;
        //            temp++;

        //            file = file.Remove(enumatorIndex, temp - enumatorIndex);
        //            file = file.Insert(enumatorIndex, "{*}");

        //            queries.Set("file", file);

        //            string url = uri.GetLeftPart(UriPartial.Path) + "?" + queries;
        //            url = Uri.UnescapeDataString(url);

        //            return (url, zeropad);
        //        }

        //        string getFirstPageUrl(string url)
        //        {
        //            Uri uri = new Uri(url);  
        //            string firstPageUrl = url.Replace(uri.AbsolutePath, "");

        //            string newPath = "";
        //            foreach (string segment in uri.Segments)
        //            {
        //                newPath += segment;
        //                if (segment == "page/")
        //                    break;
        //            }

        //            if (!uri.Segments.Contains("page/"))
        //                newPath += "/page/";

        //            newPath += "1/";

        //            return firstPageUrl + newPath;
        //        }

        //        //page is of the set [1; positive infinity) 
        //        private void AddRule_(PageEnumerator pe, string filenameFormatted, int page)
        //        {
        //            string formatPattern = "D" + pe.zeroPad.ToString();

        //            handler.AddRule(pe.PageAt(page),
        //                string.Format(filenameFormatted, page.ToString(formatPattern)));
        //            handler.AddRule(pe.PageAt(page + 1),
        //                string.Format(filenameFormatted, (page + 1).ToString(formatPattern)));
        //        }

        //        //strippedUrl has the format: https://archive.org/details/*/page/1/ where * is the book ID
        //        //private async void DownloadAll(string firstPageUrl, string filenameFormatted)
        //        //{
        //        //    Debug.WriteLine($"Downloading: {firstPageUrl}");

        //        //    int[] oriPageInfo = await GetPageInfo();

        //        //    int[] prevPageInfo = new int[2];
        //        //    Array.Copy(oriPageInfo, prevPageInfo, 2);
        //        //    int currentPage = oriPageInfo[0];

        //        //    //Get the accurate BRImageSrc after borrowing
        //        //    string src = await GetBRImageSrc(1000);
        //        //    string maxZoomSrc = GetMaxZoomUrl(src);

        //        //    //string urlFormatted;
        //        //    //int zeropad;
        //        //    PageEnumerator pe = PageEnumeratorUrl(maxZoomSrc);

        //        //    Debug.WriteLine("Done Getting Enumerator Url");
        //        //    Debug.WriteLine(pe.urlFormatted);
        //        //    Debug.WriteLine(pe.zeroPad);

        //        //    AddRule(pe, filenameFormatted, currentPage);

        //        //    Debug.WriteLine(pe.PageAt(1));

        //        //    await ZoomToMax();

        //        //    while (true)
        //        //    {
        //        //        int[] curPageInfo = await GetPageInfo();

        //        //        if (prevPageInfo[0] == curPageInfo[0] && curPageInfo[0] != oriPageInfo[0])
        //        //        {
        //        //            await TryEvaluteScript(flipPageNext, attempts: 5);
        //        //            continue;
        //        //        }

        //        //        if (oriPageInfo[0] == curPageInfo[0] && currentPage > curPageInfo[1])
        //        //            break;

        //        //        if (currentPage == 1)
        //        //            currentPage++;
        //        //        else
        //        //            currentPage += 2;

        //        //        if (currentPage > curPageInfo[1])
        //        //        {
        //        //            currentPage = 1;
        //        //        }

        //        //        Debug.WriteLine($"Downloading at page {currentPage + 1} - {curPageInfo[0]}, {currentPage + 2} - {curPageInfo[0] + 1}");

        //        //        //Add the rule
        //        //        AddRule(pe, filenameFormatted, currentPage);

        //        //        //Flip the page
        //        //        var result = await TryEvaluteScript(flipPageNext, attempts: 5);

        //        //        if (!result.isSuccess)
        //        //            throw new Exception($"DownloadAll: Can't flip page ({result.jr.Message})");

        //        //        prevPageInfo = curPageInfo;
        //        //    }

        //        //    Debug.WriteLine("Finished Downloading");
        //        //}
        //        private async void DownloadAll(string firstPageUrl, string filenameFormatted)
        //        {
        //            Debug.WriteLine($"Downloading: {firstPageUrl}");

        //            int[] oriPageInfo = await GetPageInfo();

        //            int[] prevPageInfo = new int[2];
        //            Array.Copy(oriPageInfo, prevPageInfo, 2);
        //            int currentPage = oriPageInfo[0];

        //            //Get the accurate BRImageSrc after borrowing
        //            string src = await GetBRImageSrc(1000);
        //            string maxZoomSrc = GetMaxZoomUrl(src);

        //            //string urlFormatted;
        //            //int zeropad;
        //            var pe = PageEnumeratorUrl(maxZoomSrc);

        //            Debug.WriteLine("Done Getting Enumerator Url");
        //            Debug.WriteLine(pe.urlFormatted);
        //            Debug.WriteLine(pe.zeroPad);

        //            handler.AddRule(pe.urlFormatted, filenameFormatted, false);
        //            await ZoomToMax();
        //            await TryEvaluteScript(flipPageNext, attempts: 5);

        //            bool loopedOver = false;
        //            while (true)
        //            {
        //                await Task.Delay(500);

        //                int[] curPageInfo = await GetPageInfo();

        //                if (prevPageInfo[0] == curPageInfo[0])
        //                {
        //                    await TryEvaluteScript(flipPageNext, attempts: 5);
        //                    continue;
        //                }

        //                if (curPageInfo[0] >= oriPageInfo[0] && loopedOver)
        //                    break;

        //                if (currentPage == 1)
        //                    currentPage++;
        //                else
        //                    currentPage += 2;

        //                if (currentPage > curPageInfo[1])
        //                {
        //                    loopedOver = true;
        //                    currentPage = 2;
        //                }

        //                Debug.WriteLine($"Downloading at page {currentPage + 1} - {curPageInfo[0]}, {currentPage + 2} - {curPageInfo[0] + 1}");

        //                //Flip the page
        //                var result = await TryEvaluteScript(flipPageNext, attempts: 5);

        //                if (!result.isSuccess)
        //                    throw new Exception($"DownloadAll: Can't flip page ({result.jr.Message})");

        //                prevPageInfo = curPageInfo;
        //            }

        //            Debug.WriteLine("Finished Downloading");
        //        }
        //        private string GetMaxZoomUrl(string url)
        //        {
        //            Uri uri = new Uri(url);
        //            var nameValues = HttpUtility.ParseQueryString(uri.Query);
        //            nameValues.Set("scale", "1");

        //            return uri.GetLeftPart(UriPartial.Path) + "?" + nameValues;
        //        }

        //        private async Task ZoomToMax()
        //        {
        //            string src = await GetBRImageSrc(1000);

        //            //Zoom to get maximum resolution
        //            while (!src.Contains("&scale=1&"))
        //            {
        //                Debug.WriteLine("Zooming");
        //                chromiumWebBrowser1.ExecuteScriptAsync(zoomScript);
        //                src = await GetBRImageSrc(1000);
        //            }

        //            Debug.WriteLine("Done Zooming");
        //            Debug.WriteLine(src);
        //        }

        //        private async void Download(string email, string password, string url, string folderPath)
        //        {
        //            string firstPageUrl = getFirstPageUrl(url);

        //            await Login(email, password);

        //            await reusableTCS.Reset();
        //            chromiumWebBrowser1.Load(firstPageUrl);

        //            await reusableTCS.Reset();
        //            //Assuming when BRImage appears everything has loaded
        //            string src = await GetBRImageSrc(1500);

        //            //Borrow the book
        //            if (!src.Contains(@"BookReader/BookReaderImages.php"))
        //                if (await Borrow())
        //                {
        //                    await reusableTCS.Reset();
        //                    Debug.WriteLine("Borrowed");
        //                }

        //            DownloadAll(firstPageUrl, @"D:\ArchiveBooks\test{*}");
        //        }
        #endregion

        Scraper scraper = new Scraper();

        private async void button1_Click(object sender, EventArgs e)
        {
            await scraper.Initialize(2);
            timer1.Start();
            scraper.Download("cejale8108@tagbert.com", "cejale8108",
                "https://archive.org/details/procrastinatione00stee/page/2/mode/2up", @"D:\ArchiveBooks");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //string urlFormatted;
            //int zeropad;

            //var pe = PageEnumeratorUrl("https://ia802308.us.archive.org/BookReader/BookReaderImages.php?zip=/21/items/vocabularyworksh0000john/vocabularyworksh0000john_jp2.zip&file=vocabularyworksh0000john_jp2/vocabularyworksh0000john_0001.jp2&id=vocabularyworksh0000john&scale=16&rotate=0");

            //Debug.WriteLine("Done!");
            //Debug.WriteLine(pe.urlFormatted);
            //Debug.WriteLine(pe.zeroPad);
        }

        private async void button3_Click(object sender, EventArgs e)
        {
            byte[] imgBytes = await chromiumWebBrowser1.CaptureScreenshotAsync();
            using (Image image = Image.FromStream(new MemoryStream(imgBytes)))
            {
                Debug.WriteLine(image.Size);
            }
        }
        public byte[] ImageToByteArray(System.Drawing.Image imageIn)
        {
            using (var ms = new MemoryStream())
            {
                imageIn.Save(ms, imageIn.RawFormat);
                return ms.ToArray();
            }
        }
        private void button4_Click(object sender, EventArgs e)
        {
            //Debug.WriteLine(GetXPos(ImageToByteArray(Bitmap.FromFile(@"D:\ArchiveBooks\test.png"))));
            string cpString = "inline; filename*=UTF-8''cambridgeielts6e0006unse_0061.jpg";
            ContentDisposition contentDisposition = new ContentDisposition(cpString);
            string filename = contentDisposition.FileName;
            StringDictionary parameters = contentDisposition.Parameters;
            // You have got parameters now
            foreach (KeyValuePair<string, string> kvp in parameters)
            {
                Debug.WriteLine(kvp.Key + ": " + kvp.Value);
            }
        }

        
        private void button5_Click(object sender, EventArgs e)
        {
            scraper.proxy.Stop();
        }

        public Image byteArrayToImage(byte[] byteArrayIn)
        {
            MemoryStream ms = new MemoryStream(byteArrayIn);
            Image returnImage = Image.FromStream(ms);
            return returnImage;
        }

        IEnumerable<string> GetUrls()
        {
            if (scraper == null || scraper.UrlRules == null)
                return default;
            return scraper.UrlRules.Select((dict) => dict.Keys).
                SelectMany(sl => sl).Distinct().ToList();
        }

        int[] OutputPages(IEnumerable<Rule> rules)
        {
            int totalPageCount = rules.Select((p) => p.MaxPage).Sum();

            int[] result = new int[totalPageCount];
            foreach (Rule rule in rules)
            {
                IEnumerable<Guid> downloaded = rule.DownloadedPages.Except(rule.FailedPages);
                IEnumerable<Guid> failed = rule.FailedPages.Except(rule.DownloadedPages);

                foreach (Guid id in rule.RequestedPages)
                {
                    int page = rule.Mapper[id] - 1;
                    result[page] = 0;
                }
                foreach (Guid id in downloaded)
                {
                    int page = rule.Mapper[id] - 1;
                    result[page] = 1;
                }
                foreach (Guid id in failed)
                {
                    int page = rule.Mapper[id] - 1;
                    result[page] = 2;
                }
            }
            return result;
        }

        static Color defaultColor = Color.Gray;
        static Color requestedColor = Color.Blue;
        static Color downloadedColor = Color.Green;
        static Color failedColor = Color.Red;
        static Color skippedColor = Color.Yellow;
        void UpdateResults()
        {
            /*
             * Using .Where(), we can select all dictionary which has the specfied url
             * Using .Select(), we can select only the .Values (the Rules)
             * Using .SelectMany(), we can flatten out our IEnumerable of IEnumerable of Rules
             */
            IEnumerable<Rule> rules = scraper.UrlRules.Select((dict) => dict.Values).
                SelectMany(item => item);

            int totalPageCount = rules.Select((p) => p.MaxPage).Sum();
            if (listView1.Items.Count != totalPageCount)
            {
                listView1.Items.Clear();
                for (int i = 0; i < totalPageCount; i++)
                    listView1.Items.Add($"Page: {i + 1}");
            }

            IEnumerable<int> status = rules.Select((rule) => rule.OutputPages())
                .SelectMany(p => p);
            for (int i = 0; i < totalPageCount; i++)
            {
                switch (status.ElementAt(i))
                {
                    case 0:
                        listView1.Items[i].BackColor = defaultColor;
                        break;
                    case 1:
                        listView1.Items[i].BackColor = requestedColor;
                        break;
                    case 2:
                        listView1.Items[i].BackColor = downloadedColor;
                        break;
                    case 3:
                        listView1.Items[i].BackColor = failedColor;
                        break;
                    case 4:
                        listView1.Items[i].BackColor = skippedColor;
                        break;
                }
            }

            //foreach (Rule rule in rules)
            //{
            //    listBox1.Items.Clear();
            //    foreach (Guid page in rule.RequestedPages)
            //        listBox1.Items.Add(rule.Mapper[page]);

            //    listBox2.Items.Clear();
            //    foreach (Guid page in rule.DownloadedPages)
            //        listBox2.Items.Add(rule.Mapper[page]);

            //    listBox3.Items.Clear();
            //    foreach (Guid page in rule.FailedPages)
            //        listBox3.Items.Add(rule.Mapper[page]);
            //}
        }

        IEnumerable<string> urls = new List<string>();
        private void timer1_Tick(object sender, EventArgs e)
        {
            UpdateResults();

            //var bitmap = await scraper.browsers[0].CaptureScreenshotAsync();

            //pictureBox1.Image = byteArrayToImage(bitmap);
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }
    }
}
