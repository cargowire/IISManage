namespace IISManage
{
    using System;
    using System.IO;
    using System.Linq;

    using Microsoft.Web.Administration;

    using NDesk.Options;

    public class Program
	{
		public static void Main(string[] args)
		{
			if (args != null && args.Any())
                RunFromArgs(args);
            else
                CreateIISSite(@"C:\Tools\IISDemo\Sites\{0}\", "master.test.me.com", @"C:\Tools\IISDemo\Logs\{0}\", "Templates", "v4.0", "test.me.com", "master");
		}

        public static void RunFromArgs(string[] args)
        {
            var showHelp = false;

            var baseSiteUrl = string.Empty;
            string branchSiteUrl = null;
            var sitesFolder = string.Empty;
            var logsFolder = string.Empty;
            var appPoolName = System.Configuration.ConfigurationManager.AppSettings["DefaultAppPool"];
            var stringToReplace = System.Configuration.ConfigurationManager.AppSettings["RemoveFromBranchName"];
            var appPoolDotNetVersion = System.Configuration.ConfigurationManager.AppSettings["AppPoolDotNetVersion"];

            var branch = string.Empty;
            var defaultBranch = string.Empty;

            var p = new OptionSet
				{
					{ "s|site=", "The name of the site to create (doubles as host header).", (v) => baseSiteUrl = v },
					{ "sf|sitefolder=", "The physical folder to store the site (is string formatted with the sitename).", (v) => sitesFolder = v },
					{ "lf|logsfolder=", "The physical folder to store the logs of the site (is string formatted with the sitename).", (v) => logsFolder = v },
					{ "a|apppool=", "The name of the application pool to use/create.", (v) => appPoolName = v },
					{ "b|branch=", "The branch (assuming source control) that is being used for this site.", (v) => branch = v },
					{ "db|defaultbranch=", "The default branch (assuming source control) that is being used for this site.", (v) => defaultBranch = v },
					{ "apv|apppoolversion=", "The version of .NET which the application pool runs in", (v) => appPoolDotNetVersion = v },
					{ "h|help",  "show this message and exit", v => showHelp = v != null }
				};

            try
            {
                p.Parse(args);

                if (!string.IsNullOrEmpty(branch))
                {
                    branch = branch.Replace(stringToReplace, string.Empty);
                    branchSiteUrl = string.Concat(branch.ToLower(), ".", baseSiteUrl);
                }
                else
                    branchSiteUrl = string.Concat(defaultBranch.ToLower(), ".", baseSiteUrl);
            }
            catch (OptionException e)
            {
                Console.Write("IISManage: ");
                Console.WriteLine(e.Message);
                Console.WriteLine("Try 'IISManage --help' for more information.");
                return;
            }

            if (showHelp)
            {
                ShowHelp(p);
                return;
            }

            if (string.IsNullOrEmpty(baseSiteUrl))
                throw new ArgumentNullException("site");

            if (string.IsNullOrEmpty(sitesFolder))
                throw new ArgumentNullException("sitefolder");

            CreateIISSite(sitesFolder, branchSiteUrl, logsFolder, appPoolName, appPoolDotNetVersion, baseSiteUrl, defaultBranch);
        }

        public static void CreateIISSite(string sitesFolder, string branchSiteName, string logsFolder, string appPoolName, string appPoolDotNetVersion, string baseSiteUrl, string defaultBranchName)
        {
            var siteDirectory = string.Format(sitesFolder, branchSiteName);
            var logDirectory = string.Format(logsFolder, branchSiteName);

            Console.WriteLine("Site Directory: {0}", siteDirectory);
            Console.WriteLine("Log Directory: {0}", logDirectory);

            CreateFolder(siteDirectory);
            CreateFolder(logDirectory);

            using (var serverManager = new ServerManager())
            {
                CreateApplicationPool(serverManager, appPoolName, appPoolDotNetVersion);
                var site = CreateWebsite(serverManager, branchSiteName, defaultBranchName, baseSiteUrl, siteDirectory, logDirectory, appPoolName);

                serverManager.CommitChanges();
            }
        }

		private static void ShowHelp(OptionSet p)
		{
			Console.WriteLine("Usage: IISManage [OPTIONS]");
			Console.WriteLine();
			Console.WriteLine("Options:");
			p.WriteOptionDescriptions(Console.Out);
		}

		private static void CreateFolder(string path)
		{
			if (!Directory.Exists(path))
			    Directory.CreateDirectory(path);
		}

		private static ApplicationPool CreateApplicationPool(ServerManager serverManager, string appPool, string runtimeVersion)
		{
			var iisAppPool = serverManager.ApplicationPools[appPool];
			if (iisAppPool == null)
				iisAppPool = serverManager.ApplicationPools.Add(appPool);

			iisAppPool.ManagedRuntimeVersion = runtimeVersion;

			return iisAppPool;
		}

		private static Site CreateWebsite(ServerManager serverManager, string branchSiteName, string defaultBranchName, string baseSiteUrl, string siteLocation, string logDirectory, string appPool)
		{
            var iisSite = serverManager.Sites[branchSiteName];
			if (iisSite == null)
			{
			    iisSite = serverManager.Sites.Add(branchSiteName, "http", string.Format("*:80:{0}", branchSiteName), siteLocation);
                if (!string.IsNullOrWhiteSpace(defaultBranchName) && branchSiteName.StartsWith(defaultBranchName, StringComparison.InvariantCultureIgnoreCase))
                    iisSite.Bindings.Add(string.Format("*:80:{0}", baseSiteUrl), "http");
			}

			iisSite.ApplicationDefaults.ApplicationPoolName = appPool;

            iisSite.LogFile.Directory = logDirectory;
			iisSite.ServerAutoStart = true;

			return iisSite;
		}
	}
}