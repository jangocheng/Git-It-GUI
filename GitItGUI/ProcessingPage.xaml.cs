﻿using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GitItGUI.Core;
using System.Threading.Tasks;
using System.Threading;
using Avalonia.Threading;

namespace GitItGUI
{
	public enum ProcessingPageModes
	{
		None,
		Clone,
		Pull,
		Push,
		Sync,
		Merge,
		Switch
	}

	public class ProcessingPage : UserControl, NavigationPage
	{
		public static ProcessingPage singleton;

		public ProcessingPageModes mode = ProcessingPageModes.None;
		public string clonePath, cloneURL, cloneUsername, clonePassword;
		public bool cloneSucceeded;

		public BranchState mergeOtherBranch;
		public BranchState switchOtherBranch;
		
		private Thread thread;

		// ui
		private TextBox statusTextBox;

		public ProcessingPage()
		{
			singleton = this;
			AvaloniaXamlLoader.Load(this);

			// load ui
			statusTextBox = this.Find<TextBox>("statusTextBox");
		}

		public void NavigatedFrom()
		{
			mode = ProcessingPageModes.None;
		}

		public async void NavigatedTo()
		{
			statusTextBox.Text = "Waiting...";
			await Task.Delay(500);
			thread = new Thread(Process);
			thread.Start();
		}

		private void StatusUpdateCallback(string status)
		{
			if (Dispatcher.UIThread.CheckAccess())
			{
				statusTextBox.Text = status;
			}
			else
			{
				bool isDone = false;
				Dispatcher.UIThread.InvokeAsync(delegate
				{
					statusTextBox.Text = status;
					isDone = true;
				});

				while (!isDone) Thread.Sleep(1);
			}
		}

		private void HandleMergeConflicts()
		{
			const string warning = "\nIf you notice extra files in your staged area (its OK),\nthis is common after a merge conflic.";
			const string resolveFailWarning = "Please resolve conflicts then sync your changes with the server!" + warning;
			if (MessageBox.Show("Conflicts detected! Resolve now?", MessageBoxTypes.YesNo))
			{
				if (ChangesManager.ResolveAllConflicts()) MessageBox.Show("Now sync your changes with the server!" + warning);
				else MessageBox.Show(resolveFailWarning);
			}
			else
			{
				MessageBox.Show(resolveFailWarning);
			}

			MainContent.singleton.tabControlNavigateIndex = 0;
		}

		private void Process()
		{
			// pull
			if (mode == ProcessingPageModes.Pull)
			{
				if (ChangesManager.Pull(StatusUpdateCallback) == SyncMergeResults.Conflicts)
				{
					HandleMergeConflicts();
				}
			}

			// push
			else if (mode == ProcessingPageModes.Push)
			{
				ChangesManager.Push(StatusUpdateCallback);
			}

			// sync
			else if (mode == ProcessingPageModes.Sync)
			{
				var result = ChangesManager.Sync(StatusUpdateCallback);
				if (result == SyncMergeResults.Succeeded)
				{
					string size;
					int count = RepoManager.UnpackedObjectCount(out size);
					if (count >= 1000 && MessageBox.Show(string.Format("Would you like to run git optimizers?\nYou have {0} from {1} unpacked files.\nThis can take over 10 sec to complete!", size, count), MessageBoxTypes.YesNo))
					{
						RepoManager.Optimize();
					}
				}
				else if (result == SyncMergeResults.Conflicts)
				{
					HandleMergeConflicts();
				}
			}

			// clone
			else if (mode == ProcessingPageModes.Clone)
			{
				// clone repo
				cloneSucceeded = RepoManager.Clone(cloneURL, clonePath, cloneUsername, clonePassword, out clonePath);
				if (!cloneSucceeded)
				{
					MessageBox.Show("Failed to clone repo: " + clonePath);
					MainWindow.LoadPage(PageTypes.Clone);
					return;
				}

				// open repo
				if (!RepoManager.OpenRepo(clonePath))
				{
					MessageBox.Show("Failed to open repo: " + clonePath);
					MainWindow.LoadPage(PageTypes.Start);
					return;
				}

				// update credentials
				RepoManager.UpdateCredentialValues(cloneUsername, clonePassword);
				RepoManager.SaveSettings();
				RepoManager.Refresh();
			}

			// merge
			else if (mode == ProcessingPageModes.Merge)
			{
				var result = BranchManager.MergeBranchIntoActive(mergeOtherBranch, StatusUpdateCallback);
				if (result == MergeResults.Succeeded)
				{
					MessageBox.Show("Merge Succedded!\n(Remember to sync with the server!)");
					MainContent.singleton.tabControlNavigateIndex = 0;
					MainWindow.LoadPage(PageTypes.MainContent);
				}
				else if (result == MergeResults.Conflicts)
				{
					HandleMergeConflicts();
				}
			}

			// switch
			else if (mode == ProcessingPageModes.Switch)
			{
				if (!switchOtherBranch.isRemote) BranchManager.Checkout(switchOtherBranch, StatusUpdateCallback);
				else if (MessageBox.Show("Cannot checkout to remote branch.\nDo you want to create a local one that tracks this remote instead?", MessageBoxTypes.YesNo))
				{
					string fullName = switchOtherBranch.branchName;
					if (BranchManager.AddNewBranch(fullName))
					{
						BranchManager.Checkout(fullName, StatusUpdateCallback);
						BranchManager.AddUpdateTracking(switchOtherBranch.fullName);
					}
				}
			}

			// error
			else
			{
				MessageBox.Show("Unsuported Processing mode: " + mode);
			}

			// finish
			MainWindow.LoadPage(PageTypes.MainContent);
		}
	}
}
