﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace GitItGUI.Core
{
	class VersionNumber
	{
		public int major, minor, patch, build;
	}

	public delegate void CheckForUpdatesCallbackMethod(bool succeeded);

	/// <summary>
	/// Handles preliminary features
	/// </summary>
	public static class AppManager
	{
		public static bool gitlfsInstalled {get; private set;}
		private static CheckForUpdatesCallbackMethod checkForUpdatesCallback;
		private static string checkForUpdatesURL, checkForUpdatesOutOfDateURL;

		internal static XML.AppSettings settings;

		private static WebClient client;
		#if WINDOWS
		private const string platform = "Windows";
		#elif MAC
		private const string platform = "Mac";
		#elif LINUX
		private const string platform = "Linux";
		#endif

		/// <summary>
		/// Must be called before using any other API feature
		/// </summary>
		/// <returns>True if succeeded</returns>
		public static bool Init()
		{
			try
			{
				// load settings
				string rootAppSettingsPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
				settings = Settings.Load<XML.AppSettings>(rootAppSettingsPath + "\\" + Settings.appSettingsFolderName + "\\" + Settings.appSettingsFilename);

				// apply default lfs ignore types
				if (settings.defaultGitLFS_Exts.Count == 0)
				{
					settings.defaultGitLFS_Exts.AddRange(new List<string>()
					{
						".psd", ".jpg", ".jpeg", ".png", ".bmp", ".tga",// image types
						".mpeg", ".mov", ".avi", ".mp4", ".wmv",// video types
						".wav", ".mp3", ".ogg", ".wma", ".acc",// audio types
						".zip", ".7z", ".rar", ".tar", ".gz",// compression types
						".fbx", ".obj", ".3ds", ".blend", ".ma", ".mb", ".dae",// 3d formats
						".pdf",// doc types
						".bin", ".data", ".raw", ".hex",// unknown binary types
					});
				}
			}
			catch (Exception e)
			{
				Debug.LogError("AppManager.Init Failed: " + e.Message);
				Dispose();
				return false;
			}

			return true;
		}

		/// <summary>
		/// Disposes all manager objects (Call before app exit)
		/// </summary>
		public static void Dispose()
		{
			RepoManager.Dispose();
			BranchManager.Dispose();
		}

		public static bool CheckForUpdates(string url, string outOfDateURL, CheckForUpdatesCallbackMethod checkForUpdatesCallback)
		{
			try
			{
				AppManager.checkForUpdatesCallback = checkForUpdatesCallback;
				checkForUpdatesURL = url;
				checkForUpdatesOutOfDateURL = outOfDateURL;

				client = new WebClient();
				client.DownloadStringCompleted += Client_DownloadStringCompleted;
				client.DownloadStringAsync(new Uri(url));
			}
			catch (Exception e)
			{
				Debug.LogError("Failed to check for updates: " + e.Message, true);
				if (checkForUpdatesCallback != null) checkForUpdatesCallback(false);
			}

			return true;
		}

		private static VersionNumber GetVersionNumber(string version)
		{
			var result = new VersionNumber();
			var values = version.Split('.');
			int i = 0;
			foreach (var value in values)
			{
				int num = 0;
				int.TryParse(value, out num);
				if (i == 0) result.major = num;
				else if (i == 1) result.minor = num;
				else if (i == 2) result.patch = num;
				else if (i == 3) result.build = num;
				else break;

				++i;
			}

			return result;
		}

		private static bool IsValidVersion(string currentVersion, string requiredVersion)
		{
			var v1 = GetVersionNumber(currentVersion);
			var v2 = GetVersionNumber(requiredVersion);
			if (v1.major > v2.major)
			{
				return true;
			}
			else if (v1.major < v2.major)
			{
				return false;
			}
			else if (v1.major == v2.major)
			{
				if (v1.minor > v2.minor)
				{
					return true;
				}
				else if (v1.minor < v2.minor)
				{
					return false;
				}
				else
				{
					if (v1.patch > v2.patch)
					{
						return true;
					}
					else if (v1.patch < v2.patch)
					{
						return false;
					}
					else
					{
						if (v1.build >= v2.build)
						{
							return true;
						}
					}
				}
			}

			return false;
		}

		private static void Client_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
		{
			if (e.Error != null)
			{
				Debug.LogError("Failed to check for updates: " + e.Error.Message, true);
				client.Dispose();
				if (checkForUpdatesCallback != null) checkForUpdatesCallback(false);
				return;
			}

			if (e.Cancelled)
			{
				Debug.LogError("Update check canceled!", true);
				client.Dispose();
				if (checkForUpdatesCallback != null) checkForUpdatesCallback(false);
				return;
			}

			try
			{
				// get git and git-lfs version
				bool canCheckGit = true, canCheckGitLFS = true;
				string gitVersion = null, gitlfsVersion = null;
				string gitlfsRequiredVersion = "0.0.0.0";
				try
				{
					gitVersion = Tools.RunExeOutput("git", "version", null);
				}
				catch
				{
					Debug.LogError("git is not installed correctly. (Make sure git is usable in the cmd/terminal)", true);
					client.Dispose();
					if (checkForUpdatesCallback != null) checkForUpdatesCallback(false);
					return;
				}

				try
				{
					gitlfsVersion = Tools.RunExeOutput("git-lfs", "version", null);
					gitlfsInstalled = true;
				}
				catch
				{
					canCheckGitLFS = false;
					gitlfsInstalled = false;
				}

				var match = Regex.Match(gitVersion, @"git version (.*)\.windows");
				if (match.Success && match.Groups.Count == 2) gitVersion = match.Groups[1].Value;
				else canCheckGit = false;

				if (canCheckGitLFS)
				{
					match = Regex.Match(gitlfsVersion, @"git-lfs/(.*) \(GitHub; windows amd64; go (.*); git ");
					if (match.Success && match.Groups.Count == 3)
					{
						gitlfsVersion = match.Groups[1].Value;
						gitlfsRequiredVersion = match.Groups[2].Value;
					}
					else canCheckGitLFS = false;
				}

				// make sure the git version installed is supporeted by lfs
				if (!IsValidVersion(gitVersion, gitlfsRequiredVersion))
				{
					Debug.LogError(string.Format("'git-lfs' version is not compatible with 'git' version installed!"), true);
					client.Dispose();
					if (checkForUpdatesCallback != null) checkForUpdatesCallback(false);
					return;
				}

				// check versions
				bool canCheckAppVersion = true;
				using (var reader = new StringReader(e.Result))
				using (var xmlReader = new XmlTextReader(reader))
				{
					while (xmlReader.Read())
					{
						if (canCheckAppVersion && xmlReader.Name == "AppVersion")
						{
							canCheckAppVersion = false;
							if (!IsValidVersion(VersionInfo.version, xmlReader.ReadInnerXml()))
							{
								Debug.LogError("Your 'Git-Game-GUI' version is out of date.", true);
								using (var process = Process.Start(checkForUpdatesOutOfDateURL))
								{
									process.WaitForExit();
								}
							}
						}
						else if (canCheckGit && xmlReader.Name == "GitVersion")
						{
							while (xmlReader.Read())
							{
								if (canCheckGit && xmlReader.Name == platform)
								{
									canCheckGit = false;
									if (!IsValidVersion(gitVersion, xmlReader.ReadInnerXml()))
									{
										Debug.LogError("Your 'git' version is out of date.", true);
										using (var process = Process.Start("https://git-scm.com/downloads"))
										{
											process.WaitForExit();
										}
									}
								}

								if (xmlReader.Name == "GitVersion") break;
							}
						}
						else if (canCheckGitLFS && xmlReader.Name == "Git_LFS_Version")
						{
							while (xmlReader.Read())
							{
								if (canCheckGitLFS && xmlReader.Name == platform)
								{
									canCheckGitLFS = false;
									if (!IsValidVersion(gitlfsVersion, xmlReader.ReadInnerXml()))
									{
										Debug.LogError("Your 'git-lfs' version is out of date.", true);
										using (var process = Process.Start("https://git-lfs.github.com/"))
										{
											process.WaitForExit();
										}
									}
								}

								if (xmlReader.Name == "GitVersion") break;
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				Debug.LogError("Failed to get version info!\nMake sure git and git-lfs are installed\nAlso make sure you're connected to the internet: \n\n" + ex.Message, true);
			}

			client.Dispose();
			if (checkForUpdatesCallback != null) checkForUpdatesCallback(true);
		}
	}
}
