﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using GitItGUI.Core;
using GitItGUI.Tools;
using LibGit2Sharp;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace GitItGUI
{
	public class ClickCommand : ICommand
	{
		#pragma warning disable CS0067
		public event EventHandler CanExecuteChanged;
		#pragma warning restore CS0067

		private FileItem sender;

		public ClickCommand(FileItem sender)
		{
			this.sender = sender;
		}

		public bool CanExecute(object parameter)
		{
			return true;
		}

		public void Execute(object parameter)
		{
			switch (sender.State)
			{
				case FileStates.NewInWorkdir:
				case FileStates.TypeChangeInWorkdir:
				case FileStates.RenamedInWorkdir:
				case FileStates.ModifiedInWorkdir:
				case FileStates.DeletedFromWorkdir:
					sender.Stage(true);
					break;

				case FileStates.NewInIndex:
				case FileStates.TypeChangeInIndex:
				case FileStates.RenamedInIndex:
				case FileStates.ModifiedInIndex:
				case FileStates.DeletedFromIndex:
					sender.Unstage(true);
					break;
			}
		}
	}

	public class OpenFile_ClickCommand : ICommand
	{
		#pragma warning disable CS0067
		public event EventHandler CanExecuteChanged;
		#pragma warning restore CS0067

		private FileItem sender;

		public OpenFile_ClickCommand(FileItem sender)
		{
			this.sender = sender;
		}

		public bool CanExecute(object parameter)
		{
			return true;
		}

		public void Execute(object parameter)
		{
			sender.OpenFile();
		}
	}

	public class OpenFileLocation_ClickCommand : ICommand
	{
		#pragma warning disable CS0067
		public event EventHandler CanExecuteChanged;
		#pragma warning restore CS0067

		private FileItem sender;

		public OpenFileLocation_ClickCommand(FileItem sender)
		{
			this.sender = sender;
		}

		public bool CanExecute(object parameter)
		{
			return true;
		}

		public void Execute(object parameter)
		{
			sender.OpenFileLocation();
		}
	}

	public class RevertFile_ClickCommand : ICommand
	{
		#pragma warning disable CS0067
		public event EventHandler CanExecuteChanged;
		#pragma warning restore CS0067

		private FileItem sender;

		public RevertFile_ClickCommand(FileItem sender)
		{
			this.sender = sender;
		}

		public bool CanExecute(object parameter)
		{
			return true;
		}

		public void Execute(object parameter)
		{
			sender.RevertFile();
		}
	}

	public class DeleteFile_ClickCommand : ICommand
	{
		#pragma warning disable CS0067
		public event EventHandler CanExecuteChanged;
		#pragma warning restore CS0067

		private FileItem sender;

		public DeleteFile_ClickCommand(FileItem sender)
		{
			this.sender = sender;
		}

		public bool CanExecute(object parameter)
		{
			return true;
		}

		public void Execute(object parameter)
		{
			sender.DeleteFile();
		}
	}

	public class DeleteUntrackedFiles_ClickCommand : ICommand
	{
		#pragma warning disable CS0067
		public event EventHandler CanExecuteChanged;
		#pragma warning restore CS0067

		private FileItem sender;

		public DeleteUntrackedFiles_ClickCommand(FileItem sender)
		{
			this.sender = sender;
		}

		public bool CanExecute(object parameter)
		{
			return true;
		}

		public void Execute(object parameter)
		{
			sender.DeleteUntrackedFiles();
		}
	}

	public class FileItem
	{
		private Bitmap icon;
		public Bitmap Icon {get {return icon;}}

		public FileState fileState;
		public string Filename {get {return fileState.filename;}}
		public FileStates State {get {return fileState.state;}}

		public FileItem()
		{
			fileState.filename = "ERROR";
		}

		public FileItem(Bitmap icon, FileState fileState)
		{
			this.icon = icon;
			this.fileState = fileState;
		}

		private ClickCommand clickCommand;
		public ClickCommand ClickCommand
		{
			get
			{
				clickCommand = new ClickCommand(this);
				return clickCommand;
			}
		}

		private OpenFile_ClickCommand openFileClickCommand;
		public OpenFile_ClickCommand OpenFileClickCommand
		{
			get
			{
				openFileClickCommand = new OpenFile_ClickCommand(this);
				return openFileClickCommand;
			}
		}

		private OpenFileLocation_ClickCommand openFileLocationClickCommand;
		public OpenFileLocation_ClickCommand OpenFileLocationClickCommand
		{
			get
			{
				openFileLocationClickCommand = new OpenFileLocation_ClickCommand(this);
				return openFileLocationClickCommand;
			}
		}

		private RevertFile_ClickCommand revertFileClickCommand;
		public RevertFile_ClickCommand RevertFileClickCommand
		{
			get
			{
				revertFileClickCommand = new RevertFile_ClickCommand(this);
				return revertFileClickCommand;
			}
		}

		private DeleteFile_ClickCommand deleteFileClickCommand;
		public DeleteFile_ClickCommand DeleteFileClickCommand
		{
			get
			{
				deleteFileClickCommand = new DeleteFile_ClickCommand(this);
				return deleteFileClickCommand;
			}
		}

		private DeleteUntrackedFiles_ClickCommand deleteUntrackedFilesClickCommand;
		public DeleteUntrackedFiles_ClickCommand DeleteUntrackedFilesClickCommand
		{
			get
			{
				deleteUntrackedFilesClickCommand = new DeleteUntrackedFiles_ClickCommand(this);
				return deleteUntrackedFilesClickCommand;
			}
		}

		public void Stage(bool refresh)
		{
			if (fileState.state == FileStates.Conflicted)
			{
				if (MessageBox.Show("File in conflicted state.\nAre you sure you want to stage un-resolved file?", MessageBoxTypes.YesNo)) ChangesManager.StageFile(fileState, refresh);
			}
			else
			{
				ChangesManager.StageFile(fileState, refresh);
			}
		}

		public void Unstage(bool refresh)
		{
			ChangesManager.UnstageFile(fileState, refresh);
		}

		public void OpenFile()
		{
			try
			{
				System.Diagnostics.Process.Start("explorer.exe", string.Format("{0}\\{1}", RepoManager.repoPath, fileState.filename));
			}
			catch (Exception ex)
			{
				Debug.LogError("Failed to open file: " + ex.Message, true);
			}
		}

		public void OpenFileLocation()
		{
			try
			{
				System.Diagnostics.Process.Start("explorer.exe", string.Format("/select, {0}\\{1}", RepoManager.repoPath, fileState.filename));
			}
			catch (Exception ex)
			{
				Debug.LogError("Failed to open folder location: " + ex.Message, true);
			}
		}

		public void RevertFile()
		{
			if (!MessageBox.Show("Are you sure you want to revert this file?", MessageBoxTypes.YesNo)) return;
			ChangesManager.RevertFile(fileState);
		}

		public void DeleteFile()
		{
			if (!MessageBox.Show("Are you sure you want to delete this file?", MessageBoxTypes.YesNo)) return;
			ChangesManager.DeleteUntrackedUnstagedFile(fileState, true);
		}

		public void DeleteUntrackedFiles()
		{
			if (!MessageBox.Show("Are you sure you want to delete all untracked/unstaged files?", MessageBoxTypes.YesNo)) return;
			ChangesManager.DeleteUntrackedUnstagedFiles(true);
		}
	}

	public class ChangesPage : UserControl
	{
		public static ChangesPage singleton;

		// ui objects
		Button refreshChangedButton, revertAllButton, stageAllButton, unstageAllButton, resolveSelectedButton, resolveAllButton;
		Button openDiffToolButton, syncChangesButton, commitStagedButton_Advanced, pullChangesButton_Advanced, pushChangesButton_Advanced;
		ListBox unstagedChangesListView, stagedChangesListView;
		TextBox diffTextBox;

		List<FileItem> unstagedChangesListViewItems, stagedChangesListViewItems;

		public ChangesPage()
		{
			singleton = this;
			AvaloniaXamlLoader.Load(this);

			// load ui items
			diffTextBox = this.Find<TextBox>("diffTextBox");
			unstagedChangesListView = this.Find<ListBox>("unstagedChangesListView");
			stagedChangesListView = this.Find<ListBox>("stagedChangesListView");
			stageAllButton = this.Find<Button>("stageAllButton");
			unstageAllButton = this.Find<Button>("unstageAllButton");
			refreshChangedButton = this.Find<Button>("refreshChangedButton");
			revertAllButton = this.Find<Button>("revertAllButton");
			resolveSelectedButton = this.Find<Button>("resolveSelectedButton");
			resolveAllButton = this.Find<Button>("resolveAllButton");
			openDiffToolButton = this.Find<Button>("openDiffToolButton");
			syncChangesButton = this.Find<Button>("syncChangesButton");
			commitStagedButton_Advanced = this.Find<Button>("commitStagedButton_Advanced");
			pullChangesButton_Advanced = this.Find<Button>("pullChangesButton_Advanced");
			pushChangesButton_Advanced = this.Find<Button>("pushChangesButton_Advanced");

			// apply bindings
			unstagedChangesListViewItems = new List<FileItem>();
			stagedChangesListViewItems = new List<FileItem>();
			unstagedChangesListView.Items = unstagedChangesListViewItems;
			stagedChangesListView.Items = stagedChangesListViewItems;
			
			unstagedChangesListView.SelectionChanged += UnstagedChangesListView_SelectionChanged;
			stagedChangesListView.SelectionChanged += StagedChangesListView_SelectionChanged;
			stageAllButton.Click += StageAllButton_Click;
			unstageAllButton.Click += UnstageAllButton_Click;
			refreshChangedButton.Click += RefreshChangedButton_Click;
			revertAllButton.Click += RevertAllButton_Click;
			resolveSelectedButton.Click += ResolveSelectedButton_Click;
			resolveAllButton.Click += ResolveAllButton_Click;
			openDiffToolButton.Click += OpenDiffToolButton_Click;
			commitStagedButton_Advanced.Click += CommitStagedButton_Click;
			syncChangesButton.Click += SyncChangesButton_Click;
			pullChangesButton_Advanced.Click += PullChangesButton_Advanced_Click;
			pushChangesButton_Advanced.Click += PushChangesButton_Advanced_Click;

			// bind event
			RepoManager.RepoRefreshedCallback += RepoManager_RepoRefreshedCallback;
			ChangesManager.AskUserToResolveBinaryFileCallback += ChangesManager_AskUserToResolveBinaryFileCallback;
			ChangesManager.AskUserIfTheyAcceptMergedFileCallback += ChangesManager_AskUserIfTheyAcceptMergedFileCallback;
		}

		private bool ChangesManager_AskUserToResolveBinaryFileCallback(FileState fileState, out MergeBinaryFileResults result)
		{
			string appResult;
			if (!CoreApps.LaunchBinaryConflicPicker(fileState.filename, out appResult))
			{
				result = MergeBinaryFileResults.Error;
				return false;
			}

			switch (appResult)
			{
				case "UseTheirs": result = MergeBinaryFileResults.UseTheirs; return true;
				case "KeepMine": result = MergeBinaryFileResults.KeepMine; return true;
				case "Canceled": result = MergeBinaryFileResults.Cancel; return true;
				case "RunMergeTool": result = MergeBinaryFileResults.RunMergeTool; return true;
				default: result = MergeBinaryFileResults.Error; return true;
			}
		}

		private bool ChangesManager_AskUserIfTheyAcceptMergedFileCallback(FileState fileState, out MergeFileAcceptedResults result)
		{
			result = MessageBox.Show("Do you accept the file as merged?", MessageBoxTypes.YesNo) ? MergeFileAcceptedResults.Yes : MergeFileAcceptedResults.No;
			return true;
		}

		private void CheckLocalBranchSyncErrors()
		{
			if (BranchManager.IsTracking()) return;


		}

		private void PushChangesButton_Advanced_Click(object sender, RoutedEventArgs e)
		{
			CheckLocalBranchSyncErrors();
			ProcessingPage.singleton.mode = ProcessingPageModes.Push;
			MainWindow.LoadPage(PageTypes.Processing);
		}

		private void PullChangesButton_Advanced_Click(object sender, RoutedEventArgs e)
		{
			CheckLocalBranchSyncErrors();
			ProcessingPage.singleton.mode = ProcessingPageModes.Pull;
			MainWindow.LoadPage(PageTypes.Processing);
		}

		private void SyncChangesButton_Click(object sender, RoutedEventArgs e)
		{
			CheckLocalBranchSyncErrors();

			// check if files need to be staged
			if (ChangesManager.FilesAreUnstaged())
			{
				MessageBox.Show("You must stage all files first!");
				return;
			}

			// check if files need to be commit
			if (ChangesManager.FilesAreStaged())
			{
				if (!CommitChanges()) return;
			}

			// sync changes
			ProcessingPage.singleton.mode = ProcessingPageModes.Sync;
			MainWindow.LoadPage(PageTypes.Processing);
		}

		private void CommitStagedButton_Click(object sender, RoutedEventArgs e)
		{
			CommitChanges();
		}

		private bool CommitChanges()
		{
			if (!ChangesManager.FilesAreStaged())
			{
				MessageBox.Show("No files have been staged to commit");
				return false;
			}

			string result;
			if (CoreApps.LaunchCommitEntry("", out result)) return ChangesManager.CommitStagedChanges(result);
			else return false;
		}

		private void OpenDiffToolButton_Click(object sender, RoutedEventArgs e)
		{
			var item = unstagedChangesListView.SelectedItem as FileItem;
			if (item == null)
			{
				Debug.Log("Unstaged file must be selected", true);
				return;
			}

			ChangesManager.OpenDiffTool(item.fileState);
		}

		private void ResolveAllButton_Click(object sender, RoutedEventArgs e)
		{
			if (ChangesManager.ResolveAllConflicts()) MessageBox.Show("Resolve All Conflices done.");
		}

		private void ResolveSelectedButton_Click(object sender, RoutedEventArgs e)
		{
			var item = unstagedChangesListView.SelectedItem as FileItem;
			if (item == null)
			{
				Debug.Log("Unstaged file must be selected", true);
				return;
			}

			ChangesManager.ResolveConflict(item.fileState, true);
		}

		private void RevertAllButton_Click(object sender, RoutedEventArgs e)
		{
			if (!MessageBox.Show("Are you sure you want to revert all files?", MessageBoxTypes.OkCancel)) return;
			ChangesManager.RevertAll();
			MessageBox.Show("Revert All done.");
		}

		private void RefreshChangedButton_Click(object sender, RoutedEventArgs e)
		{
			RepoManager.Refresh();
		}

		private void StageAllButton_Click(object sender, RoutedEventArgs e)
		{
			foreach (var item in unstagedChangesListViewItems)
			{
				item.Stage(false);
			}

			RepoManager.Refresh();
		}

		private void UnstageAllButton_Click(object sender, RoutedEventArgs e)
		{
			foreach (var item in stagedChangesListViewItems)
			{
				item.Unstage(false);
			}

			RepoManager.Refresh();
		}

		private void RepoManager_RepoRefreshedCallback()
		{
			if (Dispatcher.UIThread.CheckAccess())
			{
				RepoManager_RepoRefreshedCallback_UIThread();
			}
			else
			{
				bool isDone = false;
				Dispatcher.UIThread.InvokeAsync(delegate
				{
					RepoManager_RepoRefreshedCallback_UIThread();
					isDone = true;
				});

				while (!isDone) Thread.Sleep(1);
			}
		}

		private void RepoManager_RepoRefreshedCallback_UIThread()
		{
			diffTextBox.Text = "";
			unstagedChangesListViewItems.Clear();
			stagedChangesListViewItems.Clear();
			unstagedChangesListView.Items = null;
			stagedChangesListView.Items = null;
			foreach (var fileState in ChangesManager.GetFileChanges())
			{
				var item = new FileItem(ResourceManager.GetResource(fileState.state), fileState);
				if (!fileState.IsStaged()) unstagedChangesListViewItems.Add(item);
				else stagedChangesListViewItems.Add(item);
			}
			unstagedChangesListView.Items = unstagedChangesListViewItems;
			stagedChangesListView.Items = stagedChangesListViewItems;
		}

		private void UpdateDiffPanel(FileItem item)
		{
			var data = ChangesManager.GetQuickViewData(item.fileState);
			if (data == null)
			{
				diffTextBox.Text = "<<< ERROR >>>";
			}
			else if (data.GetType() == typeof(string))
			{
				diffTextBox.Text = data.ToString();
			}
			else
			{
				diffTextBox.Text = "<<< Unsported Binary Format >>>";
			}
		}

		private void UnstagedChangesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var item = unstagedChangesListView.SelectedItem as FileItem;
			if (item != null)
			{
				UpdateDiffPanel(item);
				stagedChangesListView.SelectedIndex = -1;
			}
		}

		private void StagedChangesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var item = stagedChangesListView.SelectedItem as FileItem;
			if (item != null)
			{
				UpdateDiffPanel(item);
				unstagedChangesListView.SelectedIndex = -1;
			}
		}
	}
}
