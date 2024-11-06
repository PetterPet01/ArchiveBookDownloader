using CefSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.IO;
using MimeTypes;
using DiffMatchPatch;
using CefSharp.ResponseFilter;

namespace ArchiveBookScrape
{
	public class CustomResourceRequestHandler : CefSharp.Handler.ResourceRequestHandler
	{
		private readonly MemoryStream memoryStream;
		private StreamResponseFilter filter;
		private string filename;

		public CustomResourceRequestHandler(string filename, MemoryStream memoryStream)
        {
			this.filename = filename;
			this.memoryStream = memoryStream;
		}

		protected override IResponseFilter GetResourceResponseFilter(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IResponse response)
		{
			filter = new StreamResponseFilter(memoryStream);
			return filter;
		}

		protected override void OnResourceLoadComplete(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IResponse response, UrlRequestStatus status, long receivedContentLength)
		{
			Debug.WriteLine($"Downloading {filename}");

			//Validate response
			if (response.StatusCode != 200 || !response.Headers.AllKeys.Contains("Content-Type") ||
				response.Headers.Get("Content-Type") != "image/jpeg")
			{
				foreach (string header in response.Headers)
                {
					Debug.WriteLine(header + ":" + response.Headers[header]);
                }

				//In the case the browser is disposed yet responses are still coming
				if (browser != null)
					browser.MainFrame.LoadRequest(request);

				if (filter != null)
					filter.Dispose();

				//Notify that the download is complete (or rather, we got the response)
				memoryStream.Dispose();
				//Notify that the download, in fact, failed
				memoryStream.Dispose();

				return;
			}

			filename = Path.ChangeExtension(filename,
				MimeTypeMap.GetExtension(response.Headers.Get("Content-Type").Replace(" ", "")));

			using (FileStream stream = new FileStream(filename, FileMode.Create))
            {
				if (memoryStream.Length == 0)
					Debug.WriteLine($"WARNING: MEMORYSTREAM RECEIVED 0 BYTE for FILENAME {filename}");
				memoryStream.WriteTo(stream);
			}
			
			if (filter != null)
				filter.Dispose();

			//Notify that the download is complete (or rather, we got the response)
			memoryStream.Dispose();
		}
	}

	public class Rule
    {
		public bool OneTime { get; private set; }
		public string Filename { get; private set; }
		public int StartPage { get; private set; }
		public int MaxPage { get; private set; }
		public ConcurrentBag<Guid> RequestedPages { get; private set; }
		public ConcurrentBag<Guid> DownloadedPages { get; private set; }
		public ConcurrentBag<Guid> FailedPages { get; private set; }
		public ConcurrentBag<int> SkippedPages { get; private set; }
		public ConcurrentDictionary<Guid, int> Mapper { get; private set; }

		public int[] OutputPages()
        {
			int totalPageCount = MaxPage;

			int[] result = new int[totalPageCount];
			IEnumerable<Guid> downloaded = DownloadedPages.Except(FailedPages);
			IEnumerable<Guid> failed = FailedPages.Except(DownloadedPages);

			foreach (Guid id in RequestedPages)
			{
				int page = Mapper[id] - 1;
				result[page - StartPage] = 1;
			}
			foreach (Guid id in downloaded)
			{
				int page = Mapper[id] - 1;
				result[page - StartPage] = 2;
			}
			foreach (Guid id in failed)
			{
				int page = Mapper[id] - 1;
				result[page - StartPage] = 3;
			}
			foreach (int page in SkippedPages)
			{
				result[page - StartPage] = 4;
			}
			return result;
		}

		public Rule(string filename, int startPage, int maxPage, bool oneTime = true)
        {
			this.Filename = filename;
			this.StartPage = startPage;
			this.MaxPage = maxPage;
			this.OneTime = oneTime;
			RequestedPages = new ConcurrentBag<Guid>();
			DownloadedPages = new ConcurrentBag<Guid>();
			FailedPages = new ConcurrentBag<Guid>();
			SkippedPages = new ConcurrentBag<int>();
			Mapper = new ConcurrentDictionary<Guid, int>();
		}
	}

	public class CustomRequestHandler : CefSharp.Handler.RequestHandler
	{
		public ConcurrentDictionary<string, Rule> urlRules = null;
		private RecyclableMemoryStreamManager memoryManager = null;

		public CustomRequestHandler()
        {
			urlRules = new ConcurrentDictionary<string, Rule>();
			memoryManager = new RecyclableMemoryStreamManager();

			memoryManager.StreamDisposed += (object sender, RecyclableMemoryStreamManager.StreamDisposedEventArgs args) =>
			{
				foreach (string url in urlRules.Keys)
					if (url == args.Tag)
						urlRules[url].DownloadedPages.Add(args.Id);
			};

			memoryManager.StreamDoubleDisposed += (object sender, RecyclableMemoryStreamManager.StreamDoubleDisposedEventArgs args) =>
			{
				foreach (string url in urlRules.Keys)
					if (url == args.Tag)
						urlRules[url].FailedPages.Add(args.Id);
			};
		}

        //public bool AddRule(string url, string filename, bool oneTime = true)
        //{
        //    Debug.WriteLine("AddRule: " + url);
        //    if (!urlRules.TryAdd(url, new Rule()
        //    {
        //        filename = filename,
        //        oneTime = oneTime
        //    }))
        //    {
        //        Debug.WriteLine("AddRule: Url clash");
        //        return false;
        //    }
        //    return true;
        //}
        //public bool RemoveRule(string url)
        //{
        //    return urlRules.TryRemove(url, out _);
        //}

        //TODO: Check if this is thread-safe
        static diff_match_patch dmp = new diff_match_patch();
		private static bool WildcardDiff(string url1, string url2, out string diffOutput)
		{
			bool valid = false;
			diffOutput = "";

			List<Diff> diffs = dmp.diff_main(url1, url2);
			dmp.diff_cleanupSemantic(diffs);

			//Debug.WriteLine(diffs.Count);

			if (diffs.Count != 4)
			{
				//Debug.WriteLine("WildcardDiff: Invalid diff count");
				return false;
			}

			foreach (Diff diff in diffs)
			{
				if (diff.operation != Operation.EQUAL)
					if (diff.text == "{*}")
						valid = true;
					else
						diffOutput = diff.text.ToClrString();
			}

			if (!valid)
            {
				//Debug.WriteLine("WildcardDiff: Invalid urls");
				return false;
			}

			Debug.WriteLine("WildcardDiff");
			return true;
		}
		private static Guid Int2Guid(int value)
		{
			byte[] bytes = new byte[16];
			BitConverter.GetBytes(value).CopyTo(bytes, 0);
			return new Guid(bytes);
		}
		private static int Guid2Int(Guid value)
		{
			byte[] b = value.ToByteArray();
			int bint = BitConverter.ToInt32(b, 0);
			return bint;
		}
		protected override IResourceRequestHandler GetResourceRequestHandler(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool isNavigation, bool isDownload, string requestInitiator, ref bool disableDefaultHandling)
		{
			//Only intercept specific Url's
			string diff;
			foreach (string url in urlRules.Keys)
				if (WildcardDiff(request.Url, url, out diff))
				{
					int page = int.Parse(diff);
					Guid guid = Guid.NewGuid();

					//Pick the rule where the page fits
					string urlCopy = urlRules.Where((item) => 
					(item.Value.StartPage + 1 <= page &&
						page <= item.Value.StartPage + item.Value.MaxPage))
						.FirstOrDefault()
						.Key;
					//If it's null, reject it
					if (urlCopy == null)
						return null;

					urlRules[urlCopy].Mapper.TryAdd(guid, page);
					urlRules[urlCopy].RequestedPages.Add(guid);

					Debug.WriteLine(request.Url);

					string filename = urlRules[urlCopy].Filename;
					filename = filename.Replace("{*}", diff);
					Debug.WriteLine(filename);

					if (urlRules[urlCopy].OneTime)
						urlRules.TryRemove(urlCopy, out _);

					return new CustomResourceRequestHandler(filename,
						memoryManager.GetStream(guid, urlCopy));
				}

			//Default behaviour, url will be loaded normally.
			return null;
		}
		
	}
}
