using System;
using System.Collections.Generic;
using System.IO;
using DotNetWikiBot;
using System.Text.RegularExpressions;

namespace WikiPubLib
{
	public class Sync
	{
		public const string SyncTemplate = "\n{{Template:Replicate-from-kintwiki}}";
		public static PageList GetCategoryList(Site site, string category)
		{
			PageList publist = new PageList(site);
			publist.FillAllFromCategory(category);
			return publist;
		}

		public static void OneWaySync(Site localsite, Site pubsite, string category)
		{
			PageList localpages = GetCategoryList(localsite, category);
			PageList pubpages = GetCategoryList(pubsite, category);

			syncNewPages(pubsite, localpages, pubpages);
			syncOldPages(pubsite, localpages, pubpages);
			syncDeletedPages(localpages, pubpages);
			syncImages(localpages, localsite, pubsite);
		}

		private static void syncImages(PageList localpages, Site localsite, Site pubsite)
		{
			string localTempFile = Path.GetTempFileName();
			string publicTempFile = Path.GetTempFileName();
			foreach (Page page in localpages)
			{
				page.Load();
				string[] images = page.GetImages(true);
				foreach (string imageName in images)
				{
					Page localImage = new Page(localsite, imageName);
					if (!localImage.TryDownloadImage(localTempFile))
					    continue;

					Page publicImage = new Page(pubsite, imageName);
					if (publicImage.TryDownloadImage(publicTempFile))
					{
						if (FilesAreIdentical(localTempFile, publicTempFile))
							continue;
					}
					publicImage.UploadImage(localTempFile, "", "", "", "");
				}
			}
			File.Delete(localTempFile);
			File.Delete(publicTempFile);
		}

		public static bool FilesAreIdentical(string file1, string file2)
		{
			FileStream fs1 = new FileStream(file1, FileMode.Open);
			FileStream fs2 = new FileStream(file2, FileMode.Open);
			bool result = false;
			if (fs1.Length == fs2.Length)
			{           
				result = true;
				while(result)
				{
					int file1byte = fs1.ReadByte();
					if (file1byte == -1)
						break;
					int file2byte = fs2.ReadByte();
					if (file1byte != file2byte)
						result = false;
				}
			}

			fs1.Close();
			fs2.Close();
			return result;
		}

		private static void syncDeletedPages(PageList localpages, PageList pubpages)
		{
			foreach (Page page in pubpages)
			{
				if (localpages.Contains(page))
					continue;
				page.Delete("wiki synchronization");
			}
		}

		private static void syncOldPages(Site pubsite, PageList localpages, PageList pubpages)
		{
			foreach (Page page in localpages)
			{
				if (!pubpages.Contains(page))
					continue;
				page.LoadSilent();
				Page pubpage = pubpages[page.title];
				pubpage.LoadSilent();
				if (pubpage.text != getPageText(page))
					copyPage(page, pubsite);
			}
		}

		private static void copyPage(Page page, Site pubsite)
		{
			Page outPage = new Page(pubsite);
			outPage.title = page.title;
			outPage.text = getPageText(page);
			outPage.timestamp = page.timestamp;
			outPage.Save();
		}

		private static string getPageText(Page page)
		{
			if (page.site.RemoveNSPrefix(page.title, 10) != page.title)
			    return page.text;
			return page.text + SyncTemplate;
		}

		private static void syncNewPages(Site pubsite, PageList localpages, PageList pubpages)
		{
			foreach (Page page in localpages)
			{
				if (pubpages.Contains(page))
					continue;
				page.Load();
				copyPage(page, pubsite);
			}
		}

		public static List<string> GetPublicCategoryList(string text)
		{
			Regex regex = new Regex("(?m)^[*](?<category>[^\n\r]+)\r*$");
			MatchCollection matches = regex.Matches(text);
			List<string> result = new List<string>();
			foreach (Match match in matches)
			{
				result.Add(match.Groups["category"].Value);
			}
			return result;
		}

		public static List<string> ExpandCaterory(Site site, string category)
		{
			PageList pageList = new PageList(site);
			pageList.FillSubsFromCategoryTree(category);
			List<string> result = new List<string>();
			result.Add(category);
			foreach (Page page in pageList)
				result.Add(site.RemoveNSPrefix(page.title, 14));
			return result;
		}

		public static void SyncCategoryTree(Site localWiki, Site publicWiki, string categoryPage)
		{
			List<string> categoriesList = MakePublicCateroryList(localWiki, categoryPage);
			foreach (string category in categoriesList)
			{
				Console.Out.WriteLine("Синхронизация категории {0}", category);
				OneWaySync(localWiki, publicWiki, category);
			}
		}

		public static List<string> MakePublicCateroryList(Site localWiki, string categoryPage)
		{
			List<string> resultCategoriesList = new List<string>();

			Page page = new Page(localWiki, categoryPage);
			page.Load();
			List<string> categoryList = GetPublicCategoryList(page.text);
			foreach (string category in categoryList)
			{
				if (category.StartsWith("+"))
				{
					List<string> expandedList = ExpandCaterory(localWiki, category.Substring(1));
					foreach (string s in expandedList)
					{
						if (!resultCategoriesList.Contains(s))
							resultCategoriesList.Add(s);
					}
				}
				else
				{
					if (!resultCategoriesList.Contains(category))
						resultCategoriesList.Add(category);
				}
			}
			return resultCategoriesList;
		}
	}
}
