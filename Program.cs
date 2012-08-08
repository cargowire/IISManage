using System;
using System.IO;
using Microsoft.Web.Administration;

using NDesk.Options;

namespace IISManage
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var showHelp = false;

			var sitename = string.Empty;
			string branchsitename = null;
			var sitesfolder = string.Empty;
			var logsfolder = string.Empty;
			var apppoolname = System.Configuration.ConfigurationManager.AppSettings["DefaultAppPool"];
			var stringtoreplace = System.Configuration.ConfigurationManager.AppSettings["RemoveFromBranchName"];
			var appPoolDotNetVersion = System.Configuration.ConfigurationManager.AppSettings["AppPoolDotNetVersion"];

			var branch = string.Empty;
			var defaultbranch = string.Empty;

			var p = new OptionSet
				{
					{ "s|site=", "The name of the site to create (doubles as host header).", (v) => sitename = v },
					{ "sf|sitefolder=", "The physical folder to store the site (is string formatted with the sitename).", (v) => sitesfolder = v },
					{ "lf|logsfolder=", "The physical folder to store the logs of the site (is string formatted with the sitename).", (v) => logsfolder = v },
					{ "a|apppool=", "The name of the application pool to use/create.", (v) => apppoolname = v },
					{ "b|branch=", "The branch (assuming source control) that is being used for this site.", (v) => branch = v },
					{ "db|defaultbranch=", "The default branch (assuming source control) that is being used for this site.", (v) => defaultbranch = v },
					{ "apv|apppoolversion=", "The version of .NET which the application pool runs in", (v) => appPoolDotNetVersion = v },
					{ "h|help",  "show this message and exit", v => showHelp = v != null }
				};

			try
			{
				p.Parse(args);

				if (!string.IsNullOrEmpty(branch))
				{
					branch = branch.Replace(stringtoreplace, string.Empty);

					if (!branch.Equals(defaultbranch, StringComparison.InvariantCultureIgnoreCase))
						branchsitename = string.Concat(branch, ".", sitename);
				}
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

			if (string.IsNullOrEmpty(sitename))
				throw new ArgumentNullException("site");

			if (string.IsNullOrEmpty(sitesfolder))
				throw new ArgumentNullException("sitefolder");

			var sitelocation = CreateFolder(string.Format(sitesfolder, branchsitename ?? sitename));
			var logs = CreateFolder(string.Format(logsfolder, branchsitename ?? sitename));

			using (var serverManager = new ServerManager())
			{
				CreateApplicationPool(serverManager, apppoolname, appPoolDotNetVersion);
				var site = CreateWebsite(serverManager, sitename, sitelocation, logs, apppoolname);

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

		private static DirectoryInfo CreateFolder(string path)
		{
			if (!Directory.Exists(path))
				return Directory.CreateDirectory(path);
			
			return new DirectoryInfo(path);
		}

		private static ApplicationPool CreateApplicationPool(ServerManager serverManager, string appPool, string runtimeVersion)
		{
			var iisAppPool = serverManager.ApplicationPools[appPool];
			if (iisAppPool == null)
				iisAppPool = serverManager.ApplicationPools.Add(appPool);

			iisAppPool.ManagedRuntimeVersion = runtimeVersion;

			return iisAppPool;
		}

		private static Site CreateWebsite(ServerManager serverManager, string site, DirectoryInfo sitelocation, DirectoryInfo logs, string appPool)
		{
			var iisSite = serverManager.Sites[site];
			if (iisSite == null)
				iisSite = serverManager.Sites.Add(site, "http", string.Format("*:80:{0}", site), sitelocation.ToString());

			iisSite.ApplicationDefaults.ApplicationPoolName = appPool;

			iisSite.LogFile.Directory = logs.ToString();
			iisSite.ServerAutoStart = true;

			return iisSite;
		}
	}
}