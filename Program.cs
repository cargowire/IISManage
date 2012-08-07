using System;
using System.IO;
using Microsoft.Web.Administration;

using NDesk.Options;

namespace IISManage
{
	class Program
	{
		static void Main(string[] args)
		{
			bool showHelp = false;

			string sitename = string.Empty;
			string sitesfolder = string.Empty;
			string logsfolder = string.Empty;
			string apppoolname = "Sites";

			var p = new OptionSet() 
				{
					{ "s|site=", "The name of the site to create (doubles as host header).", (v) => sitename = v },
					{ "sf|sitefolder=", "The physical folder to store the site (is string formatted with the sitename).", (v) => sitesfolder = v },
					{ "lf|logsfolder=", "The physical folder to store the logs of the site (is string formatted with the sitename).", (v) => logsfolder = v },
					{ "a|apppool=", "The name of the application pool to use/create.", (v) => apppoolname = v },
					{ "h|help",  "show this message and exit", v => showHelp = v != null }
				};

			try
			{
				p.Parse(args);
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

			var sitelocation = CreateFolder(string.Format(sitesfolder, sitename));
			var logs = CreateFolder(string.Format(logsfolder, sitename));

			using (var serverManager = new ServerManager())
			{
				CreateApplicationPool(serverManager, apppoolname, "v4.0");
				var site = CreateWebsite(serverManager, sitename, sitelocation, logs, apppoolname);

				serverManager.CommitChanges();
			}
		}

		private static void ShowHelp(OptionSet p)
		{
			System.Console.WriteLine("Usage: IISManage [OPTIONS]");
			System.Console.WriteLine();
			System.Console.WriteLine("Options:");
			p.WriteOptionDescriptions(System.Console.Out);
		}

		private static DirectoryInfo CreateFolder(string path)
		{
			if (!Directory.Exists(path))
				return Directory.CreateDirectory(path);
			else
				return new DirectoryInfo(path);
		}

		private static ApplicationPool CreateApplicationPool(ServerManager serverManager, string appPool, string runtimeVersion)
		{
			ApplicationPool iisAppPool = serverManager.ApplicationPools[appPool];
			if (iisAppPool == null)
				iisAppPool = serverManager.ApplicationPools.Add(appPool);

			iisAppPool.ManagedRuntimeVersion = "v4.0";

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