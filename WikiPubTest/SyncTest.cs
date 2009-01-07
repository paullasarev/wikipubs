using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using DotNetWikiBot;
using NUnit.Framework;
using WikiPubLib;

namespace WikiPubTest
{
	public class Utils
	{
		//public const string intwiki = "http://vangog/wiki/";
		public const string intwiki = "http://vangog/wiki/";
		public const string intwikiName = "KintWiki";
		public const string intwikiUser = "Rebot";
		public const string intwikiPass = "1752369876208566";


		public const string pubwiki = "http://wiki.kint.ru/";
		public const string pubwikiName = "KintWiki";
		public const string pubwikiUser = "Rebot";
		public const string pubwikiPass = "18340273413745";

		public static void ClearCategory(Site site, string category, string reason)
		{
			PageList publist = new PageList(site);
			publist.FillAllFromCategory(category);
			foreach (Page page in publist)
			{
				page.Delete(reason);
			}
		}

		public static void AddPage(Site site, string category, string title, string text)
		{
			Page newpage = new Page(site);
			newpage.title = title;
			newpage.text = String.Format("[[Категория: {0}]]\n{1}\n", category, text);
			newpage.Save();
		}

		public static void AddNewPage(Site site, string category, string title, string text)
		{
			Page page = new Page(site, title);
			if (page.LoadTry())
				page.Delete("test setup");
			AddPage(site, category, title, text);
		}

		public static void AddImage(Site site, string title, string imageFileName)
		{
			Page newpage = new Page(site);
			newpage.title = title;
			newpage.UploadImage(imageFileName, "", "", "", "");
		}
	}

	[TestFixture]
	public class SiteTests
	{

		[TestFixtureSetUp]
		public void FixtureSetUp()
		{
			if(System.IO.Directory.Exists("Cache"))
				System.IO.Directory.Delete("Cache", true);
		}

		[Test]
		public void IntSite()
		{
			Site intsite = new Site(Utils.intwiki, Utils.intwikiUser, Utils.intwikiPass);
			Assert.AreEqual(Utils.intwikiName, intsite.name);
		}

		[Test]
		public void PubSite()
		{
			Site pubsite = new Site(Utils.pubwiki, Utils.pubwikiUser, Utils.pubwikiPass);
			Assert.AreEqual(Utils.pubwikiName, pubsite.name);
		}
	
	}

	[TestFixture]
	public class PageListTests
	{
		private Site intsite;
		private Site pubsite;

		[TestFixtureSetUp]
		public void FixtureSetUp()
		{
			if (System.IO.Directory.Exists("Cache"))
				System.IO.Directory.Delete("Cache", true);

			intsite = new Site(Utils.intwiki, Utils.intwikiUser, Utils.intwikiPass);
			pubsite = new Site(Utils.pubwiki, Utils.pubwikiUser, Utils.pubwikiPass);
		}

		[Test]
		public void IntCategoryPageList()
		{
			const string category = "Public";
			PageList list = new PageList(intsite);
			list.FillAllFromCategory(category);
			Assert.Greater(list.Count(), 0);
			Console.Out.WriteLine("got {0} articles in category {1}", list.Count(), category);
			foreach (Page page in list)
			{
				Console.Out.WriteLine(page.title);
			}
		}

		[Test]
		public void PubCategoryPageList()
		{
			const string category = "Базы данных";
			PageList list = new PageList(pubsite);
			list.FillAllFromCategory(category);
			Assert.Greater(list.Count(), 0);
			Console.Out.WriteLine("got {0} articles in category {1}", list.Count(), category);
		}

		[Test]
		public void FillPageLists()
		{
			const string category = "Тесты синхронизации wiki";
			//Utils.AddPage(intsite, category, "Тесты синхронизации wiki: Тест1", "test page");
			PageList intlist = new PageList(intsite);
			intlist.FillAllFromCategory(category);
			//Assert.Greater(intlist.Count(), 0);

			PageList publist = new PageList(pubsite);
			publist.FillAllFromCategory(category);
			//Assert.AreEqual(0, publist.Count());
		}

		[Test]
		public void AddToCategory()
		{
			const string category = "Тесты синхронизации wiki";

			Utils.ClearCategory(pubsite, category, "test");

			PageList publist = Sync.GetCategoryList(pubsite, category);
			int count = publist.Count();

			Utils.AddPage(pubsite, category, "Тесты синхронизации wiki: Тестовая страница", "test page");

			PageList newpublist = new PageList(pubsite);
			newpublist.FillAllFromCategory(category);
			int newcount = newpublist.Count();
			Assert.AreEqual(count + 1, newcount);
		}

		[Test]
		public void ClearCategory()
		{
			const string category = "Тесты синхронизации wiki";

			Utils.AddPage(pubsite, category, "Тесты синхронизации wiki: Тестовая страница", "test page");
			Utils.ClearCategory(pubsite, category, "test");

			PageList newpublist = Sync.GetCategoryList(pubsite, category);
			int newcount = newpublist.Count();

			Assert.AreEqual(0, newcount);
		}

		[Test]
		public void SubCategoryPageList()
		{
			const string category = "Public";
			PageList list = new PageList(intsite);
			list.FillAllFromCategory(category);
			Assert.IsTrue(list.Contains("Категория:Базы данных"));
		}

	}

	[TestFixture]
	public class MatchTests
	{
		[Test]
		public void SubCategoryMatch()
		{
			string text =
				@"<li><div class=""CategoryTreeSection""><div class=""CategoryTreeItem""><span class=""CategoryTreeBullet"">[<a href=""#"" onclick=""this.href='javascript:void(0)'; categoryTreeExpandNode('Базы_данных','0',this);"" title=""развернуть"">+</a>] </span><a class=""CategoryTreeLabel  CategoryTreeLabelNs14 CategoryTreeLabelCategory"" href=""/wiki/index.php/%D0%9A%D0%B0%D1%82%D0%B5%D0%B3%D0%BE%D1%80%D0%B8%D1%8F:%D0%91%D0%B0%D0%B7%D1%8B_%D0%B4%D0%B0%D0%BD%D0%BD%D1%8B%D1%85"">Базы данных</a></div>
		<div class=""CategoryTreeChildren"" style=""display:none""></div></div>
		</li>";
			//Regex linkToSubcategoryRE = new Regex("<li><div class=\"CategoryTreeSection\">.*[^\\[]<a.*>(?<category>[^<]+)</a>.*</li>");
			Regex linkToSubcategoryRE = new Regex("<li><div class=\"CategoryTreeSection\">.*[^\\[]<a.*>(?<category>[^<]+)</a>");
			MatchCollection subcategories = linkToSubcategoryRE.Matches(text);
			Assert.AreEqual(1, subcategories.Count);
			Console.Out.WriteLine("<{0}>", subcategories[0].Groups["category"].Value);
		}
		
	}


	[TestFixture]
	public class OneWaySync
	{
		private Site localSite;
		private Site publicSite;
		const string category = "Тесты синхронизации wiki OneWaySync";

		[TestFixtureSetUp]
		public void FixtureSetUp()
		{
			if (System.IO.Directory.Exists("Cache"))
				System.IO.Directory.Delete("Cache", true);

			localSite = new Site(Utils.intwiki, Utils.intwikiUser, Utils.intwikiPass);
			publicSite = new Site(Utils.pubwiki, Utils.pubwikiUser, Utils.pubwikiPass);

		}

		[SetUp]
		public void SetUp()
		{
			Utils.ClearCategory(localSite, category, "OneWaySync test");
			Utils.ClearCategory(publicSite, category, "OneWaySync test");
		}

		[Test]
		public void One_Zero()
		{
			Utils.AddPage(localSite, category, category + ": OnePage", "test 1");
			Sync.OneWaySync(localSite, publicSite, category);

			PageList newpublist = Sync.GetCategoryList(publicSite, category);
			Assert.AreEqual(1, newpublist.Count());
		}

		[Test]
		public void Two_Zero()
		{
			Utils.AddPage(localSite, category, category + ": TwoPages1", "test 1");
			Utils.AddPage(localSite, category, category + ": TwoPages2", "test 2");

			Sync.OneWaySync(localSite, publicSite, category);

			PageList newpublist = Sync.GetCategoryList(publicSite, category);
			Assert.AreEqual(2, newpublist.Count());
		}

		[Test]
		public void Two_One()
		{
			Utils.AddPage(localSite, category, category + ": TwoPages1", "test 1");
			Utils.AddPage(localSite, category, category + ": TwoPages2", "test 2");
			Utils.AddPage(publicSite, category, category + ": TwoPages2", "test 2");

			Sync.OneWaySync(localSite, publicSite, category);

			PageList newpublist = Sync.GetCategoryList(publicSite, category);
			Assert.AreEqual(2, newpublist.Count());
		}

		[Test]
		public void One_Two()
		{
			Utils.AddPage(localSite, category, category + ": TwoPages1", "test 1");
			Utils.AddPage(publicSite, category, category + ": TwoPages1", "test 1");
			Utils.AddPage(publicSite, category, category + ": TwoPages2", "test 2");

			Sync.OneWaySync(localSite, publicSite, category);

			PageList newpublist = Sync.GetCategoryList(publicSite, category);
			Assert.AreEqual(1, newpublist.Count());
		}


		[Test]
		public void PageContent()
		{
			const string pagename = category + ": TwoPages1";
			const string localtext = "test 1";
			Utils.AddPage(localSite, category, pagename , localtext);
			Utils.AddPage(publicSite, category, pagename, "test 2");

			Sync.OneWaySync(localSite, publicSite, category);

			Page localpage = new Page(localSite, pagename);
			Page publicpage = new Page(publicSite, pagename);

			localpage.Load();
			publicpage.Load();

			Assert.AreEqual(localpage.text + Sync.SyncTemplate, publicpage.text);
		}


	}

	[TestFixture]
	public class ImageOneWaySync
	{
		private Site localSite;
		private Site publicSite;
		const string category = "Тесты синхронизации изображений wiki OneWaySync";
		private const string test1Image = @"..\..\Images\test1.png";

		[TestFixtureSetUp]
		public void FixtureSetUp()
		{
			if (System.IO.Directory.Exists("Cache"))
				System.IO.Directory.Delete("Cache", true);

			localSite = new Site(Utils.intwiki, Utils.intwikiUser, Utils.intwikiPass);
			publicSite = new Site(Utils.pubwiki, Utils.pubwikiUser, Utils.pubwikiPass);

		}

		[SetUp]
		public void SetUp()
		{
			Utils.ClearCategory(localSite, category, "OneWaySync test");
			Utils.ClearCategory(publicSite, category, "OneWaySync test");
		}

		[Test]
		public void One_Zero()
		{
			const string imageName = "ImageOneWaySync_1.png";
			const string title = category + ": OnePage";
			Utils.AddPage(localSite, category, title, String.Format("test 1\n[[Изображение:{0}]]\n", imageName));
			Utils.AddImage(localSite, imageName, test1Image);

			Sync.OneWaySync(localSite, publicSite, category);

			PageList newpublist = Sync.GetCategoryList(publicSite, category);
			Assert.AreEqual(1, newpublist.Count());

			Page page = newpublist[0];
			Assert.AreEqual(title, page.title);

			page.Load();
			
			string[] images = page.GetImages(false);
			Assert.AreEqual(1, images.Length);
			Assert.AreEqual(imageName, images[0]);

			string fullImagename = "Изображение:" + imageName;
			Page image = new Page(publicSite, fullImagename);
			Assert.AreEqual(fullImagename, image.title);
			Assert.IsTrue(image.LoadTry());
			
			string tmpfile = System.IO.Path.GetTempFileName();
			image.DownloadImage(tmpfile);

			FileAssert.AreEqual(test1Image, tmpfile);
			System.IO.File.Delete(tmpfile);
		}
	
	}

	[TestFixture]
	public class GetCategoryList
	{
		//private Site localSite;
		////private Site publicSite;
		//const string pageName = "Тест Список публикуемых категорий";

		//[TestFixtureSetUp]
		//public void FixtureSetUp()
		//{
		//    if (System.IO.Directory.Exists("Cache"))
		//        System.IO.Directory.Delete("Cache", true);

		//    localSite = new Site(Utils.intwiki, Utils.intwikiUser, Utils.intwikiPass);
		//    //publicSite = new Site(Utils.pubwiki, Utils.pubwikiUser, Utils.pubwikiPass);

		//}

		//[SetUp]
		//public void SetUp()
		//{
		//    Page page = new Page(localSite, pageName);
		//    page.Delete("test setup");
		//}

		[Test]
		public void OneCategory()
		{
			string text = "*Public";
			List<string> list = Sync.GetPublicCategoryList(text);
			Assert.AreEqual(1, list.Count);
			Assert.AreEqual("Public", list[0]);
		}

		[Test]
		public void TwoCategory()
		{
			string text = @"*Public1
*Public2";
			List<string> list = Sync.GetPublicCategoryList(text);
			Assert.AreEqual(2, list.Count);
			Assert.AreEqual("Public1", list[0]);
			Assert.AreEqual("Public2", list[1]);
		}

		[Test]
		public void TwoCategoryWithSpaces()
		{
			string text = @"

*Public1
*Public2

";
			List<string> list = Sync.GetPublicCategoryList(text);
			Assert.AreEqual(2, list.Count);
			Assert.AreEqual("Public1", list[0]);
			Assert.AreEqual("Public2", list[1]);
		}

		[Test]
		public void TwoCategoryWithCategory()
		{
			string text = @"[[Категория:Wiki]]

*Public1
*Public2

";
			List<string> list = Sync.GetPublicCategoryList(text);
			Assert.AreEqual(2, list.Count);
			Assert.AreEqual("Public1", list[0]);
			Assert.AreEqual("Public2", list[1]);
		}

		[Test]
		public void TwoCategoryWithPlus()
		{
			string text = @"asdfasdf
asdfdf
*Public1
*+Public2
asdf
";
			List<string> list = Sync.GetPublicCategoryList(text);
			Assert.AreEqual(2, list.Count);
			Assert.AreEqual("Public1", list[0]);
			Assert.AreEqual("+Public2", list[1]);
		}

		[Test]
		public void TwoCategoryWithNewline()
		{
			string text = "asdfasdf\nasdfdf\n*Public1\n*+Public2\nasdf";
			List<string> list = Sync.GetPublicCategoryList(text);
			Assert.AreEqual(2, list.Count);
			Assert.AreEqual("Public1", list[0]);
			Assert.AreEqual("+Public2", list[1]);
		}
	
	}

	[TestFixture]
	public class ExpandCategory
	{
		private Site localSite;
		const string category = "Тест разворот категорий";
		const string subcategory = "Тест разворот категорий подкатегория";
		const string categoryPage = "Тест Список катгорий";

		[TestFixtureSetUp]
		public void FixtureSetUp()
		{
			if (System.IO.Directory.Exists("Cache"))
				System.IO.Directory.Delete("Cache", true);

			localSite = new Site(Utils.intwiki, Utils.intwikiUser, Utils.intwikiPass);
		}

		[SetUp]
		public void SetUp()
		{
			Utils.AddNewPage(localSite, "Test", "Категория:" + category, "test1");
			Utils.AddNewPage(localSite, category, "Категория:" + subcategory, "test2");
			Utils.AddNewPage(localSite, "Wiki", categoryPage, String.Format("*+{0}\n*Public", category));
		}

		[Test]
		public void ExpandCategoryTest()
		{
			List<string> list = Sync.ExpandCaterory(localSite, category);
			//Assert.AreEqual(2, list.Count);
			Assert.IsTrue(list.Contains(category));
			Assert.IsTrue(list.Contains(subcategory));
		}

		[Test]
		public void MakePublicCateroryList()
		{
			List<string> list = Sync.MakePublicCateroryList(localSite, categoryPage);
			Assert.AreEqual(3, list.Count);
			Assert.IsTrue(list.Contains(category));
			Assert.IsTrue(list.Contains(subcategory));
			Assert.IsTrue(list.Contains("Public"));
		}
	
	}

}
