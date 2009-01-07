using System;
using System.Collections.Generic;
using Commons.GetOptions;
using WikiPubLib;
using DotNetWikiBot;

namespace WikiPubConsole
{
	class WikiPubOptions: Options
	{
		[Option("Local wiki URL", "local")] 
		public string localWiki = null;

		[Option("Local wiki user", "localUser")]
		public string localWikiUser = null;

		[Option("Local wiki password", "localPassword")]
		public string localWikiPassword = null;

		[Option("Public wiki URL", "public")]
		public string publicWiki = null;

		[Option("Public wiki user", "publicUser")]
		public string publicWikiUser = null;

		[Option(1, "Public wiki password", "publicPassword")]
		public string publicWikiPassword = null;

		[Option("Category to synchronize", "category")]
		public string category = null;

		[Option("Page with category list to synchronize", "categoryPage")]
		public string categoryPage = null;
				
		public bool Validate()
		{
			return localWiki != null && localWikiUser != null && localWikiPassword != null
			       && publicWiki != null && publicWikiUser != null && publicWikiPassword != null
				   && ((category != null) && (categoryPage == null) || (category == null) && (categoryPage != null));
		}
	}

	class Program
	{
		static void Main(string[] args)
		{
			WikiPubOptions options = new WikiPubOptions();
			options.ProcessArgs(args);
			if (!options.Validate())
			{
				options.DoHelp();
				return;
			}

			Site localWiki = new Site(options.localWiki, options.localWikiUser, options.localWikiPassword);
			Site publicWiki = new Site(options.publicWiki, options.publicWikiUser, options.publicWikiPassword);
			if (options.category != null)
				Sync.OneWaySync(localWiki, publicWiki, options.category);
			else
				Sync.SyncCategoryTree(localWiki, publicWiki, options.categoryPage);
		}

	}
}
