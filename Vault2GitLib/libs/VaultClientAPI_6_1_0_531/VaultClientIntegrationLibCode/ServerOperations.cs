/*
	SourceGear Vault
	Copyright 2002-2012 SourceGear LLC
	All Rights Reserved.
	
	You may not distribute this code, or any portion thereof, 
	or any derived work thereof, neither in source code form 
	nor in compiled form, to anyone outside your organization.
	
	This file is meant as an example to show how to call into 
	the SourceGear VaultClientIntegrationLib, and how to process 
	results that you get from it.

	Please go to http://support.sourcegear.com/index.php?c=8 to 
	ask questions and look at other examples.
*/

using System;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;
using System.Web.Services.Protocols;
using VaultLib;
using VaultClientOperationsLib;
using VaultClientNetLib;


namespace VaultClientIntegrationLib
{
	/// <summary>
	/// This class encapsulates all the options related to a login operation.
	/// </summary>
	public class LoginOptions
	{
		/// <summary>
		/// Controls whether to attempt an admin-level connection.  Some operations, such as creating a repository
		/// require Global Admin permissions.
		/// </summary>
		public VaultConnection.AccessLevelType AccessLevel = VaultConnection.AccessLevelType.Client;
		/// <summary>
		/// The URL of the server to connect to.  For example: "http://hostname/VaultService/"
		/// </summary>
		public string URL = null;
		/// <summary>
		/// The user name used to connect
		/// </summary>
		public string User = null;
		/// <summary>
		/// The repository name to use to validate the user's access.
		/// </summary>
		public string Password = null;
		/// <summary>
		/// The repository that will set as the active repository.
		/// </summary>
		public string Repository = null;
		/// <summary>
		/// 
		/// </summary>
		public string ProxyServer = null;
		/// <summary>
		/// 
		/// </summary>
		public string ProxyPort = null;
		/// <summary>
		/// 
		/// </summary>
		public string ProxyUser = null;
		/// <summary>
		/// 
		/// </summary>
		public string ProxyPassword = null;
		/// <summary>
		/// 
		/// </summary>
		public string ProxyDomain = null;
	}

	/// <summary>
	/// This class encapsulates one Vault server connection.
	/// </summary>
	public class ClientConnection
	{
		private ClientInstance _client = new ClientInstance();

		/// <summary>
		/// Get or set the ClientInstance object.
		/// </summary>
		public ClientInstance ClientInstance
		{
			get { return _client; }
		}

		public void DeleteClientInstance()
		{
			if (_client != null)
			{
				try { _client.Logout(); }
				catch { }

				// just in case remove active repo id
				if (_client.ActiveRepositoryID != -1)
				{
					try { _client.SetActiveRepositoryID(-1, null, null, false, true); }
					catch { }
				}

				try { _client.ClientInstance_Shutdown(); }
				catch { }

				_client = null;
			}
		}

		public ClientInstance CreateClientInstance(bool bUseFileSystemWatchers)
		{
			DeleteClientInstance();

			_client = new ClientInstance();
			_client.UseFileSystemWatchers = bUseFileSystemWatchers;
			return _client;
		}


		/// <summary>
		/// A comment to be included on checkin operations.
		/// </summary>
		public string Comment;

		/// <summary>
		/// An array of integers which can be set to include bug ids in a checkin transaction.
		/// </summary>
		public int[] bugIDs;

		/// <summary>
		/// A boolean which determines if bugs included in a checkin will be marked completed/fixed.
		/// </summary>
		public bool markBugFixed = false;

		/// <summary>
		/// A boolean which determines if bug information will be appended to the checkin comments.
		/// </summary>
		public bool addBugComment = false;

		/// <summary>
		/// The login options for this connection.
		/// </summary>
		public LoginOptions LoginOptions = new LoginOptions();

		/// <summary>
		/// Controls if operations are auto-commited (if possible).
		/// </summary>
		public bool AutoCommit = false;

		/// <summary>
		/// Controls if all messages are written (true) or just errors (false).
		/// </summary>
		public bool Verbose = false;

		/// <summary>
		/// Get/set the collection of cloaks.
		/// </summary>
		public VaultNameValueCollection Cloaks
		{
			get { return ClientInstance.TreeCache.Cloaks; }
			set { ClientInstance.TreeCache.Cloaks = value; }
		}

		/// <summary>
		/// Get or set option that determines if the server makes backups of files when changes are undone.
		/// </summary>
		public bool MakeBackups
		{
			get { return ClientInstance.WorkingFolderOptions.MakeBackups; }
			set { ClientInstance.WorkingFolderOptions.MakeBackups = value; }
		}

		/// <summary>
		/// MessageEvent handler.  Writes the message if Verbose is true or the message level is error.
		/// </summary>
		/// <param name="e"></param>
		public void HandleEvent(MessageEvent e)
		{
			ServerOperations.NewMessageHandler(this, e.Message);
		}

		/// <summary>
		/// BulkMessageEvent handler.  Writes the message if Verbose is true or the message level is error.
		/// </summary>
		/// <param name="e"></param>
		public void HandleEvent(BulkMessageEvent e)
		{
			ServerOperations.NewBulkMessagesHandler(this, (ProgressMessage[])e.Messages.ToArray(typeof(ProgressMessage)));
		}
	}

	/// <summary>
	/// Controls how unchanged files are handled.
	/// </summary>
	public enum UnchangedHandler
	{
		/// <summary>
		/// Leave unchanged files checked out.
		/// </summary>
		LeaveCheckedOut,

		/// <summary>
		/// Checkin unchanged files.
		/// </summary>
		Checkin,

		/// <summary>
		/// Undo the checkout on unchanged files.
		/// </summary>
		UndoCheckout
	};

	/// <summary>
	/// Summary description for ServerOperations.
	/// </summary>
	public class ServerOperations
	{
		private static object _lockInst = new object();
		private static ServerOperations _instance = null;

		/// <summary>
		/// Returns an instance of ServerOperations.
		/// </summary>
		/// <returns>A ServerOperations object.</returns>
		[Hidden]
		public static ServerOperations GetInstance()
		{
			lock (_lockInst)
			{
				if (_instance == null) { _instance = new ServerOperations(); }
			}
			return _instance;
		}

		// Note:  if any of the following three values change, they should also be
		// changed in the CC.Net plugin in the FortressClient.cs file in the
		// RetrieveSession method
		const string SESSION_FILENAME = "vault_cmdline_client_session.txt";
		private static string _cryptVector = "lWW1nOh5RUY=";
		private static string _cryptKey = "lXTnY5DKE9/x/5EAL98OKUqV8GA+icuF";


#if !JAVA && !_MS_IDE // Mainsoft will turn this property into get_client(), which will break
			// pre-existing builds.
			// This is ONLY A MAINSOFT fix.  Mono checks out.

		private readonly ClientConnection _clientconn = null;
		private ServerOperations()
		{
			_clientconn = new ClientConnection();
		}

		/// <summary>
		/// The ClientConnection object.  An encapsulation of the server connection.
		/// </summary>
		public static ClientConnection client
		{
			get
			{
				ClientConnection cc = null;
				ServerOperations so = GetInstance();
				if (so != null) { cc = so._clientconn; }
				return cc;
			}
		}

		public ClientConnection ClientConn
		{
			get { return _clientconn; }
		}

#else

		public static readonly ClientConnection client = new ClientConnection();

		// empty constructor
		private ServerOperations()
		{
		}

		public ClientConnection ClientConn
		{
			get { return client; }
		}

#endif // !JAVA

		/// <summary>
		/// Add local items to the Vault repository into the given folder
		/// </summary>
		/// <param name="folderPath">The folder where the items should be added.  This can be either a local path or a server path.</param>
		/// <param name="localItemsToAdd">An array of local paths that should be added to the folder given.</param>
		[LocalPathOnly("localItemsToAdd"), LocalOrRemotePath("folderPath")]
		public static ChangeSetItemColl ProcessCommandAdd(string folderPath, string[] localItemsToAdd)
		{
			try
			{
				client.ClientInstance.Refresh();

				string rFolderPath = RepositoryUtil.CleanUpPathAndReturnRepositoryPath(folderPath);

				ChangeSetItemColl csic = new ChangeSetItemColl();

				foreach (string strItem in localItemsToAdd)
				{
					string repositoryPath = rFolderPath + "/" + Path.GetFileName(strItem);

					if (File.Exists(strItem))
					{
						ArrayList folders = new ArrayList();
						string folderpath = rFolderPath;
						while (RepositoryUtil.PathExists(folderpath) == false)
						{
							folders.Add(folderpath);
							folderpath = RepositoryPath.GetFolder(folderpath);
						}

						folders.Reverse();
						foreach (string folder in folders)
						{
							ChangeSetItem_CreateFolder cscf = new ChangeSetItem_CreateFolder(
								VaultDateTime.Now,
								client.Comment,
								String.Empty,
								folder);
							if (!csic.Contains(cscf))
								csic.Add(cscf);
						}

						ChangeSetItem_AddFile csaf = new ChangeSetItem_AddFile(
							VaultDateTime.Now,
							client.Comment,
							String.Empty,
							strItem,
							repositoryPath);
						csic.Add(csaf);
					}
					else if (Directory.Exists(strItem))
					{
						ChangeSetItem_AddFolder csaf = new ChangeSetItem_AddFolder(
							VaultDateTime.Now,
							client.Comment,
							String.Empty,
							strItem,
							repositoryPath);
						if (!csic.Contains(csaf))
							csic.Add(csaf);
					}
					else
					{
						throw new Exception(string.Format("{0} does not exist", strItem));
					}
				}

				return commitTransaction(csic);
			}
			catch (Exception e)
			{
				int errorcode = VaultConnection.GetSoapExceptionStatusCodeInt(e);
				if (errorcode == VaultLib.VaultStatusCode.FailInvalidSessionToken)
				{
					ServerOperations.Logout();
				}
				throw e;
			}
		}
		/// <summary>
		/// Return details about the last time a line was changed in a region of a file.
		/// </summary>
		/// <param name="objectPath">The file whose history will be examined.  This can be either a local path or a server path.</param>
		/// <param name="version">The newest version of the file that will be examined for changes.  Passing -1 for this parameter chooses the latest version in the repository.</param>
		/// <param name="startlinenumber">The first line of the region of interest in the file.</param>
		/// <param name="endlinenumber">The last line of the region of interest in the file. Passing zero or a value less than startlinenumber is equivalant to passing in the same value as startlinenumber.</param>
		/// <returns>A VaultBlameRegionResponse describing the last transaction which changed the described region of the file.</returns>
		[RecommendedOptionDefault("endlinenumber", "0"), RecommendedOptionDefault("version", "-1"), LocalOrRemotePath("objectPath")]
		public static VaultBlameRegionResponse ProcessCommandBlame(string objectPath, long version, int startlinenumber, int endlinenumber)
		{
			try
			{
				VaultClientFile file = RepositoryUtil.FindVaultFileAtReposOrLocalPath(objectPath);
				if (version == -1)
					version = file.Version;
				if (endlinenumber < startlinenumber)
					endlinenumber = startlinenumber;
				VaultBlameRegionResponse response = null;
				client.ClientInstance.Connection.BlameRegion(client.ClientInstance.ActiveRepositoryID, file.FullPath, version, startlinenumber, endlinenumber, ref response);
				return response;
			}
			catch (Exception e)
			{
				int errorcode = VaultConnection.GetSoapExceptionStatusCodeInt(e);
				if (errorcode == VaultLib.VaultStatusCode.FailInvalidSessionToken)
				{
					ServerOperations.Logout();
				}
				throw e;
			}
		}

		/// <summary>
		/// Branch an item from one location to another.
		/// </summary>
		/// <param name="objectPath_From">The path to the object that will be branched.  This can be either a local path or a server path.</param>
		/// <param name="objectPath_To">The path to the new location for the branch.  This path should not exist in the repository.  The last segment of this path will be the new name of the branched folder.</param>
		[LocalOrRemotePath("objectPath_From"), LocalOrRemotePath("objectPath_To")]
		public static ChangeSetItemColl ProcessCommandBranch(string objectPath_From, string objectPath_To)
		{
			try
			{
				ChangeSetItemColl csic = new ChangeSetItemColl();

				VaultClientTreeObject treeobj = null;
				treeobj = RepositoryUtil.FindVaultTreeObjectAtReposOrLocalPath(objectPath_From);
				string tmpstr = RepositoryUtil.CleanUpPathAndReturnRepositoryPath(objectPath_To);
				if (tmpstr == null)
					throw new Exception("Could not determine path: " + objectPath_To);
				objectPath_To = tmpstr;
				if (treeobj is VaultClientFolder)
				{
					// ok, this is a folder
					ChangeSetItem_CopyBranch csi = new ChangeSetItem_CopyBranch(
						VaultDateTime.Now,
						client.Comment,
						String.Empty,
						treeobj.FullPath,
						objectPath_To,
						treeobj.ObjVerID);
					csic.Add(csi);
				}
				else
				{
					throw new UsageException(string.Format("{0} exists, but this command may not be used to branch individual files.", treeobj.FullPath));
				}

				return commitTransaction(csic);
			}
			catch (Exception e)
			{
				int errorcode = VaultConnection.GetSoapExceptionStatusCodeInt(e);
				if (errorcode == VaultLib.VaultStatusCode.FailInvalidSessionToken)
				{
					ServerOperations.Logout();
				}
				throw e;
			}
		}

		/// <summary>
		/// Branch an item from one location to another (based on a label).
		/// </summary>
		/// <param name="objectPath_From">The path to the object that will be branched.  This can be either a local path or a server path.</param>
		/// <param name="objectPath_To">The path to the new location for the branch.  This path should not exist in the repository.  The last segment of this path will be the new name of the branched folder.</param>
		/// <param name="label">The label to use as the branch point.  Only the initial version of the label will be branched.  No label promotions will be branched.</param>
		[LocalOrRemotePath("objectPath_From"), LocalOrRemotePath("objectPath_To")]
		public static ChangeSetItemColl ProcessCommandBranchFromLabel(string objectPath_From, string objectPath_To, string label)
		{
			try
			{
				ChangeSetItemColl csic = new ChangeSetItemColl();

				VaultClientTreeObject treeobj = null;
				treeobj = RepositoryUtil.FindVaultTreeObjectAtReposOrLocalPath(objectPath_From);
				string tmpstr = RepositoryUtil.CleanUpPathAndReturnRepositoryPath(objectPath_To);
				if (tmpstr == null)
					throw new Exception("Could not determine path: " + objectPath_To);
				objectPath_To = tmpstr;
				if (treeobj is VaultClientFolder)
				{
					bool bSuccess = false;
					long labelID = 0;
					string[] discoveredPaths = null;
					long rootID = 0;
					VaultClientTreeObject labelStructure = null;
					try
					{
						bSuccess = client.ClientInstance.GetByLabel_GetStructure(treeobj.FullPath, label, ref labelID, "", out discoveredPaths, out labelStructure, out rootID);
					}
					catch (Exception e)
					{
						if (labelStructure == null)
						{
							throw new Exception(string.Format("Could not find label \"{0}\" created at item \"{1}\".  {2}", label, treeobj.FullPath, e.Message));
						}
						else
						{
							throw;
						}
					}
					if (bSuccess == false)
						throw new Exception(string.Format("Could not find label \"{0}\" created at item \"{1}\".", label, treeobj.FullPath));

					// ok, this is a folder
					ChangeSetItem_CopyBranch csi = new ChangeSetItem_CopyBranch(
						VaultDateTime.Now,
						client.Comment,
						String.Empty,
						treeobj.FullPath,
						objectPath_To,
						labelStructure.ObjVerID);
					csic.Add(csi);
				}
				else
				{
					throw new UsageException(string.Format("{0} exists, but this command may not be used to branch individual files.", treeobj.FullPath));
				}

				return commitTransaction(csic);
			}
			catch (Exception e)
			{
				int errorcode = VaultConnection.GetSoapExceptionStatusCodeInt(e);
				if (errorcode == VaultLib.VaultStatusCode.FailInvalidSessionToken)
				{
					ServerOperations.Logout();
				}
				throw e;
			}
		}

		/// <summary>
		/// Checkout a collection of Vault objects
		/// </summary>
		/// <param name="objectPaths">An array of paths to Vault objects.  These paths can be either local or repository paths.  If they are repository paths, wildcards can be included see <see cref="RepositoryUtil.MatchWildcardToTreeObjects"/></param>
		/// <param name="checkoutExclusive">If this parameter is true, exclusive checkout will be requested for all objects.</param>
		/// <param name="getLatest">
		///    If true, the latest version of the file(s) will be got after the checkout.
		///    You can't just set getOptions to null; getOptions.Recursive is used during the checkout.
		/// </param>
		/// <param name="getOptions">The options that will control how the get is performed.</param>
		[LocalOrRemotePath("objectPaths"), RecommendedOptionDefault("checkoutExclusive", "true"), RecommendedOptionDefault("getLatest", "true")]
		public static void ProcessCommandCheckout(string[] objectPaths, bool checkoutExclusive, bool getLatest, GetOptions getOptions)
		{
			try
			{
				VaultResponseItem[] vriResponses = null;

				ClientInstance ci = client.ClientInstance;

				ci.Refresh();

				if (getOptions.Merge == MergeType.Unspecified)
				{
					// use a default merge type if one was not specified
					getOptions.Merge = MergeType.AttemptAutomaticMerge;
				}

				byte checkOutType = (checkoutExclusive ? VaultCheckOutType.Exclusive : VaultCheckOutType.CheckOut);

				// Collect all the specified files/folders into a single list of files
				ArrayList objects = new ArrayList();
				foreach (string objectPath in objectPaths)
				{
					objects.AddRange(RepositoryUtil.MatchWildcardToTreeObjects(objectPath));
				}

				VaultClientTreeObject[] treeobjects = (VaultClientTreeObject[])objects.ToArray(typeof(VaultClientTreeObject));

				// check for working folder before checking out
				foreach (VaultClientTreeObject vcto in treeobjects)
				{
					RepositoryUtil.CheckForWorkingFolder(vcto, true);
				}

				vriResponses = ci.CheckOut(treeobjects, getOptions.Recursive, checkOutType, string.Empty);
				if (vriResponses == null)
				{
					throw new Exception(string.Format("The checkout request did not return a response."));
				}
				foreach (VaultResponseItem vri in vriResponses)
				{
					if (vri.Status != VaultStatusCode.Success && vri.Status != VaultStatusCode.SuccessRequireFileDownload)
						throw new Exception("Checkout failed.  " + VaultConnection.GetSoapExceptionMessage(vri.Status));
				}

				// Get the items that were checked out.
				if (getLatest)
				{
					foreach (VaultClientTreeObject treeobj in treeobjects)
						ci.PerformPendingServerNamespaceChanges(treeobj.FullPath);
					ci.Get(treeobjects, getOptions.Recursive, false, MakeWritableType.MakeAllFilesWritable, getOptions.SetFileTime, getOptions.Merge, null);
					foreach (VaultClientTreeObject treeobj in treeobjects)
					{
						if (treeobj is VaultClientFolder)
						{
							ci.PerformPendingLocalDeletions(treeobj.FullPath, getOptions.PerformDeletions);
						}
					}
				}

				ci.Refresh();
			}
			catch (Exception e)
			{
				int errorcode = VaultConnection.GetSoapExceptionStatusCodeInt(e);
				if (errorcode == VaultLib.VaultStatusCode.FailInvalidSessionToken)
				{
					ServerOperations.Logout();
				}
				throw e;
			}
		}

		/// <summary>
		/// Cloak the given paths.
		/// </summary>
		/// <param name="objectPaths">An array of folder paths.  These can be either local paths or repository paths</param>
		[LocalOrRemotePath("objectPaths")]
		public static void ProcessCommandCloak(string[] objectPaths)
		{
			try
			{
				ClientInstance ci = client.ClientInstance;
				ci.Refresh();

				foreach (string folderPath in objectPaths)
				{
					VaultClientFolder vcfolder = RepositoryUtil.FindVaultFolderAtReposOrLocalPath(folderPath);

					ci.CloakOrUnCloak(vcfolder.FullPath, true);
					WriteUserMessage(string.Format("Cloaked: {0}", vcfolder.FullPath));
				}
			}
			catch (Exception e)
			{
				int errorcode = VaultConnection.GetSoapExceptionStatusCodeInt(e);
				if (errorcode == VaultLib.VaultStatusCode.FailInvalidSessionToken)
				{
					ServerOperations.Logout();
				}
				throw e;
			}
		}

		/// <summary>
		/// This method is for Eclipse.  Mainwin can't handle out params so we are just wrapping the call and passing a dummy ChangeSetItemColl 
		/// since the out param was added to fix a clc bug.
		/// </summary>
		/// <param name="csic"></param>
		/// <param name="unchanged"></param>
		/// <param name="keepCheckedOut"></param>
		/// <param name="localCopy"></param>
		/// <param name="removeLocalCopy"></param>
		[Hidden]
		public static void ProcessCommandCommit(ChangeSetItemColl csic, UnchangedHandler unchanged, bool keepCheckedOut, LocalCopyType localCopy, bool removeLocalCopy)
		{
			try
			{
				ChangeSetItemColl dummyCsic = new ChangeSetItemColl();
				ProcessCommandCommit(csic, unchanged, keepCheckedOut, localCopy, removeLocalCopy, out dummyCsic);
			}
			catch (Exception e)
			{
				int errorcode = VaultConnection.GetSoapExceptionStatusCodeInt(e);
				if (errorcode == VaultLib.VaultStatusCode.FailInvalidSessionToken)
				{
					ServerOperations.Logout();
				}
				throw e;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="csic"></param>
		/// <param name="unchanged"></param>
		/// <param name="keepCheckedOut"></param>
		/// <param name="localCopy"></param>
		/// <param name="removeLocalCopy"></param>
		/// <param name="csicRemove"></param>
		[Hidden]
		public static void ProcessCommandCommit(ChangeSetItemColl csic, UnchangedHandler unchanged, bool keepCheckedOut, LocalCopyType localCopy, bool removeLocalCopy, out ChangeSetItemColl csicRemove)
		{
			ProcessCommandCommit(csic, unchanged, keepCheckedOut, localCopy, removeLocalCopy, false, out csicRemove);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="csic"></param>
		/// <param name="unchanged"></param>
		/// <param name="keepCheckedOut"></param>
		/// <param name="localCopy"></param>
		/// <param name="removeLocalCopy"></param>
		/// <param name="resolveMerge"></param>
		/// <param name="csicRemove"></param>
		[Hidden]
		public static void ProcessCommandCommit(ChangeSetItemColl csic, UnchangedHandler unchanged, bool keepCheckedOut, LocalCopyType localCopy, bool removeLocalCopy, bool resolveMerge, out ChangeSetItemColl csicRemove)
		{
			// of the finalized change set, locate files which need to be "un-checked out"
			VaultClientFileColl vcfcUndoCheckouts = new VaultClientFileColl();
			VaultClientFile vcfile = null;

			// set the array of items removed from the change set.
			csicRemove = new ChangeSetItemColl();

			// make collections for resolveMerge work
			ChangeSetItemColl csicRMOldItems = new ChangeSetItemColl();
			ChangeSetItemColl csicRMNewItems = new ChangeSetItemColl();

			ClientInstance ci = client.ClientInstance;

			ci.Refresh();

			ci.UpdateKnownChanges_RefreshKnown(true);

			// Dev note:  GuiClientInstance, GuiClientInstanceComWrapper, GuiClientInstanceDWWrapper, and VaultWrapper
			// contain a variation of this code.  Any changes made here should also be implemented there as well.

			foreach (ChangeSetItem csiItem in csic)
			{
				vcfile = ci.TreeCache.Repository.Root.FindFileRecursive(csiItem.DisplayRepositoryPath);

#if JAVA
				if (vcfile != null) 
				{
					string vaultFileMessage = "Vault File Info:\nFile:  " + vcfile.FullPath + "\n" + "VaultModifiedDate:  " + vcfile.ModifiedDate.ToString() + "\n";
					string systemFileMessage = "System File Info:\nFile:  " + vcfile.FullPath + "\n" + "Last Write Time:  " + File.GetLastWriteTime(vcfile.FullPath).ToString() + "\n";
					SimpleLogger.Log.WriteLine(null, "File information before commit:\n" + vaultFileMessage + systemFileMessage);
				}
#endif

				if (csiItem.Type == ChangeSetItemType.Unmodified)
				{
					if (
						(vcfile != null) &&
						(((ChangeSetItem_Unmodified)csiItem).FileID == vcfile.ID)
						)
					{
						// this file is unmodified, what do we do about it?
						// default is to leave checked out

						if (unchanged == UnchangedHandler.LeaveCheckedOut)
						{
							// this change set item needs to be 
							// removed from the items to commit
							// since it should be left checked out.
							csicRemove.Add(csiItem);
						}
						else if (unchanged == UnchangedHandler.UndoCheckout)
						{
							// regardless of .KeepChecked out, this 
							// change set item needs to be removed 
							// from the items to commit
							csicRemove.Add(csiItem);

							// when keep checked out has not been specified...
							if (keepCheckedOut == false)
							{
								// ...we undo checkouts
								if (ci.GetWorkingFolder(vcfile.Parent) == null)
								{
									throw new Exception(string.Format("{0} does not have a working folder set", vcfile.Name));
								}
								vcfcUndoCheckouts.Add(vcfile);
							}
						}
					}
					else
					{
						throw new Exception(String.Format("{0} does not exist", csiItem.DisplayRepositoryPath));
					}
				}
				else if (csiItem.Type == ChangeSetItemType.CheckedOutMissing)
				{
					// Can't check in a file if it is missing
					csicRemove.Add(csiItem);
				}
				else if (csiItem.Type == ChangeSetItemType.Modified)
				{
					// Don't check in file if it is Needs Merge, or is Renegade
					if (!ci.IsCheckedOutByMeOnThisMachine(vcfile) && ci.WorkingFolderOptions.RequireCheckOutBeforeCheckIn)
					{
						csicRemove.Add(csiItem);
					}
					else
					{
						if (((ChangeSetItem_Modified)csiItem).NeedsMerge == false)
						{
							if ((unchanged == UnchangedHandler.LeaveCheckedOut) || ((unchanged == UnchangedHandler.UndoCheckout) && (keepCheckedOut == true)))
							{	// this case occurs when a "modified" item really had no change (unchanged file).
								// If keep checked out is true, we don't want to undo the item (because then we'd need to
								// check it right back out).  So put the item in the "removal" list.
								if (ci.IsModifiedItemReallyModifed((ChangeSetItem_Modified)csiItem) == false) { csicRemove.Add(csiItem); }
							}
						}
						else
						{
							if (resolveMerge)
							{
								ProcessCommandResolveMerge(vcfile);
								csicRMOldItems.Add(csiItem);
								csicRMNewItems.Add(ci.MakeChangeSetItemForKnownChange(vcfile, ci.GetWorkingFolder(vcfile.Parent), false));
							}
							else
							{
								csicRemove.Add(csiItem);
							}
						}
					}
				}
			}

			// set the comment
			ci.InternalChangeSet_SetComment(client.Comment);

			// replace items if we resolved merge status on any files
			for (int i = 0; i < csicRMOldItems.Count; i++)
			{
				csic.Remove(csicRMOldItems[i]);
				// just to be sure
				if (csicRMNewItems.Count > i)
					csic.Add(csicRMNewItems[i]);
			}

			// remove any change set items which will not be committed
			// with the changeset.
			for (int i = 0; i < csicRemove.Count; i++)
			{
				csic.Remove(csicRemove[i]);
			}

			// write out the change set after the items have been removed.
			WriteChangeSet(csic);

			// commit the transaction
			long nRevID = 0;
			bool bRet = ci.Commit(csic, keepCheckedOut, removeLocalCopy, ref nRevID);
			if ((bRet == true) && (vcfcUndoCheckouts.Count > 0))
			{
				// the commit was successful, now undo checkouts.
				ci.UndoCheckOut((VaultClientFile[])vcfcUndoCheckouts.ToArray(typeof(VaultClientFile)), localCopy);
			}

			ClientConnection cc = client;
			if ((bRet == true) && (cc.bugIDs != null) && (cc.bugIDs.Length > 0))
			{
				UpdateBugURLCommand ucmd = new UpdateBugURLCommand(csic, cc.bugIDs, cc.markBugFixed, cc.addBugComment, cc.Comment, nRevID);
				ci.UpdateBugTracking(ucmd);
			}

			if (bRet == false)
			{
				throw new Exception("Commit failed");
			}
		}
		/// <summary>
		/// Commit pending operations at or underneath an array of Vault object paths.
		/// </summary>
		/// <param name="objectPaths">An array of paths to Vault objects.  These paths can be either local or repository paths.  If they are repository paths, wildcards can be included see <see cref="RepositoryUtil.MatchWildcardToTreeObjects"/></param>
		/// <param name="unchanged">Controls how unchanged files are treated.  Possible values are:  &quot;LeaveCheckedOut&quot;, &quot;Checkin&quot;, and &quot;UndoCheckout&quot;.</param>
		/// <param name="keepCheckedOut">Inform the server that you wish to retain the checkout on items.</param>
		/// <param name="localCopy">If unchanged is UnchangedHandler.UndoCheckOut, this parameter controls what is done with any modified local files that are present in the working folder.  Possible values are:  &quot;Replace&quot;, &quot;Delete&quot;, and &quot;Leave&quot;.</param>
		/// <param name="resolveMerge">If true, will resolve the merge status on all files with Needs Merge status before check in.</param>
		[LocalOrRemotePath("objectPaths"), RecommendedOptionDefault("unchanged", "\"UndoCheckout\""), RecommendedOptionDefault("keepCheckedOut", "false"), RecommendedOptionDefault("localCopy", "\"Replace\""), RecommendedOptionDefault("resolveMerge", "false")]
		public static void ProcessCommandCheckIn(string[] objectPaths, UnchangedHandler unchanged, bool keepCheckedOut, LocalCopyType localCopy, bool resolveMerge)
		{
			ServerOperations.ProcessCommandCheckin(objectPaths, unchanged, keepCheckedOut, localCopy, resolveMerge);
		}

		/// <summary>
		/// Commit pending operations at or underneath an array of Vault object paths.
		/// </summary>
		/// <param name="objectPaths">An array of paths to Vault objects.  These paths can be either local or repository paths.  If they are repository paths, wildcards can be included see <see cref="RepositoryUtil.MatchWildcardToTreeObjects"/></param>
		/// <param name="unchanged">Controls how unchanged files are treated.  Possible values are:  &quot;LeaveCheckedOut&quot;, &quot;Checkin&quot;, and &quot;UndoCheckout&quot;.</param>
		/// <param name="keepCheckedOut">Inform the server that you wish to retain the checkout on items.</param>
		/// <param name="localCopy">If unchanged is UnchangedHandler.UndoCheckOut, this parameter controls what is done with any modified local files that are present in the working folder.  Possible values are:  &quot;Replace&quot;, &quot;Delete&quot;, and &quot;Leave&quot;.</param>
		[Hidden]
		public static void ProcessCommandCheckIn(string[] objectPaths, UnchangedHandler unchanged, bool keepCheckedOut, LocalCopyType localCopy)
		{
			ProcessCommandCheckin(objectPaths, unchanged, keepCheckedOut, localCopy, false);
		}

		/// <summary>
		/// Commit pending operations at or underneath an array of Vault object paths.
		/// </summary>
		/// <param name="objectPaths">An array of paths to Vault objects.  These paths can be either local or repository paths.  If they are repository paths, wildcards can be included see <see cref="RepositoryUtil.MatchWildcardToTreeObjects"/></param>
		/// <param name="unchanged">Controls how unchanged files are treated.  Possible values are:  &quot;LeaveCheckedOut&quot;, &quot;Checkin&quot;, and &quot;UndoCheckout&quot;.</param>
		/// <param name="keepCheckedOut">Inform the server that you wish to retain the checkout on items.</param>
		/// <param name="localCopy">If unchanged is UnchangedHandler.UndoCheckOut, this parameter controls what is done with any modified local files that are present in the working folder.  Possible values are:  &quot;Replace&quot;, &quot;Delete&quot;, and &quot;Leave&quot;.</param>
		/// <param name="resolveMerge">If true, will resolve the merge status on all files with Needs Merge status before check in.</param>
		[Hidden]
		public static void ProcessCommandCheckin(string[] objectPaths, UnchangedHandler unchanged, bool keepCheckedOut, LocalCopyType localCopy, bool resolveMerge)
		{
			try
			{
				ChangeSetItemColl csicRemove = null;
				ServerOperations.ProcessCommandCommit(objectPaths, unchanged, keepCheckedOut, localCopy, resolveMerge, out csicRemove);
			}
			catch (Exception e)
			{
				int errorcode = VaultConnection.GetSoapExceptionStatusCodeInt(e);
				if (errorcode == VaultLib.VaultStatusCode.FailInvalidSessionToken)
				{
					ServerOperations.Logout();
				}
				throw e;
			}
		}
		/// <summary>
		/// Commit pending operations at or underneath an array of Vault object paths.
		/// </summary>
		/// <param name="objectPaths">An array of paths to Vault objects.  These paths can be either local or repository paths.  If they are repository paths, wildcards can be included see <see cref="RepositoryUtil.MatchWildcardToTreeObjects"/></param>
		/// <param name="unchanged">Controls how unchanged files are treated.</param>
		/// <param name="keepCheckedOut">Inform the server that you wish to retain the checkout on items.</param>
		/// <param name="localCopy">If unchanged is UnchangedHandler.UndoCheckOut, this parameter controls what is done with any modified local files that are present in the working folder.</param>
		/// <param name="csicRemove"></param>
		[Hidden] //Hide the Commit commands from Nant, but expose the Checkin, for historical reasons.
		public static void ProcessCommandCommit(string[] objectPaths, UnchangedHandler unchanged, bool keepCheckedOut, LocalCopyType localCopy, out ChangeSetItemColl csicRemove)
		{
			ProcessCommandCommit(objectPaths, unchanged, keepCheckedOut, localCopy, false, out csicRemove);
		}

		/// <summary>
		/// Commit pending operations at or underneath an array of Vault object paths.
		/// </summary>
		/// <param name="objectPaths">An array of paths to Vault objects.  These paths can be either local or repository paths.  If they are repository paths, wildcards can be included see <see cref="RepositoryUtil.MatchWildcardToTreeObjects"/></param>
		/// <param name="unchanged">Controls how unchanged files are treated.</param>
		/// <param name="keepCheckedOut">Inform the server that you wish to retain the checkout on items.</param>
		/// <param name="localCopy">If unchanged is UnchangedHandler.UndoCheckOut, this parameter controls what is done with any modified local files that are present in the working folder.</param>
		/// <param name="resolveMerge">If true, will resolve the merge status on all files with Needs Merge status before check in.</param>
		/// <param name="csicRemove"></param>
		[Hidden] //Hide the Commit commands from Nant, but expose the Checkin, for historical reasons.
		public static void ProcessCommandCommit(string[] objectPaths, UnchangedHandler unchanged, bool keepCheckedOut, LocalCopyType localCopy, bool resolveMerge, out ChangeSetItemColl csicRemove)
		{
			ChangeSetItem csiItem = null;

			csicRemove = null;

			ClientInstance ci = client.ClientInstance;

			if (ci.WorkingFolderOptions.RequireCheckOutBeforeCheckIn == false)
			{
				// do a scan to update the change set list
				ci.UpdateKnownChanges_All(false);
			}
			else
			{
				ci.UpdateKnownChanges_RefreshKnown(false);
			}

			// get the internal change set
			ChangeSetItemColl csic = ci.InternalChangeSet_GetItems(true);
			if ((csic != null) && (csic.Count > 0))
			{
				// a sub set of the change set is requested... build that collection
				if (objectPaths.Length > 0)
				{
					int nPos = 0;

					// set the old change set list.
					ChangeSetItemColl csicOld = csic;

					// the new list of change set items
					csic = new ChangeSetItemColl();

					// find the subset of change sets to use
					foreach (string objectPath in objectPaths)
					{
						// see if the item is numeric based
						try
						{
							nPos = Convert.ToInt32(objectPath);
						}
						catch
						{
							nPos = -1;
						}

						if (nPos == -1)
						{
							// a string based subset item
							VaultClientTreeObject[] objects = RepositoryUtil.MatchWildcardToTreeObjects(objectPath);
							// find this item in the old change set
							bool bFoundItem = false;
							foreach (VaultClientTreeObject treeobj in objects)
							{

								for (int j = 0; j < csicOld.Count; j++)
								{
									csiItem = csicOld[j];
									if (csiItem.DisplayRepositoryPath.ToLower().Equals(treeobj.FullPath.ToLower()) ||
										csiItem.DisplayRepositoryPath.ToLower().StartsWith(treeobj.FullPath.ToLower() + "/"))
									{
										// if not already there, add the item
										if (csic.Contains(csiItem) == false)
										{
											csic.Add(csiItem);
										}

										bFoundItem = true;
										//break;
									}
								}
							}
							if (bFoundItem == false)
							{
								// throw here
								throw new Exception(string.Format("The current change set does not have any items for {0}", objectPath));
							}
						}
						else
						{
							if (ValidateChangeSetItemID(nPos, csicOld) == true)
							{
								csiItem = csicOld[nPos];
								// if not already there, add the item
								if (csic.Contains(csiItem) == false)
								{
									csic.Add(csiItem);
								}
							}
							else
							{
								throw new UsageException(string.Format("Invalid ChangeSetItem ID: {0}.  Please use the LISTCHANGESET command to retrieve a valid ID.", nPos));
							}
						}
					}
				}
				ProcessCommandCommit(csic, unchanged, keepCheckedOut, localCopy, false, resolveMerge, out csicRemove);
			}
			else
			{
				// nothing to do but... 
				// write out the change set
				WriteChangeSet(csic);
			}
		}

		/// <summary>
		/// Create a new folder in the given location.
		/// </summary>
		/// <param name="newFolderPath">The path to the location for the new folder.  This can be either a local path or a repository path.</param>
		[LocalOrRemotePath("newFolderPath")]
		public static ChangeSetItemColl ProcessCommandCreateFolder(string newFolderPath)
		{
			try
			{
				string tmpstr = RepositoryUtil.CleanUpPathAndReturnRepositoryPath(newFolderPath);
				if (tmpstr == null)
					throw new Exception("Could not determine path: " + newFolderPath);
				newFolderPath = tmpstr;
				ChangeSetItem_CreateFolder csaf = new ChangeSetItem_CreateFolder(
					VaultDateTime.Now,
					client.Comment,
					String.Empty,
					newFolderPath);
				ChangeSetItemColl csic = new ChangeSetItemColl();
				csic.Add(csaf);

				return commitTransaction(csic);
			}
			catch (Exception e)
			{
				int errorcode = VaultConnection.GetSoapExceptionStatusCodeInt(e);
				if (errorcode == VaultLib.VaultStatusCode.FailInvalidSessionToken)
				{
					ServerOperations.Logout();
				}
				throw e;
			}
		}

		/// <summary>
		/// Delete objects from the Vault tree.
		/// </summary>
		/// <param name="objectPaths">An array of object paths.  These can be either local or repository paths.</param>
		[LocalOrRemotePath("objectPaths")]
		public static ChangeSetItemColl ProcessCommandDelete(string[] objectPaths)
		{
			try
			{
				client.ClientInstance.Refresh();

				ChangeSetItemColl csic = new ChangeSetItemColl();

				ArrayList objects = new ArrayList();
				foreach (string objectPath in objectPaths)
				{
					objects.AddRange(RepositoryUtil.MatchWildcardToTreeObjects(objectPath));
				}
				if (objects.Count == 0)
					throw new Exception("No objects were found in the paths that you supplied.");
				VaultClientTreeObject[] treeobjects = (VaultClientTreeObject[])objects.ToArray(typeof(VaultClientTreeObject));

				foreach (VaultClientTreeObject treeobj in treeobjects)
				{
					if (treeobj is VaultClientFolder)
					{
						// OK, this is a folder
						ChangeSetItem_DeleteFolder csdf = new ChangeSetItem_DeleteFolder(
							VaultDateTime.Now,
							client.Comment,
							String.Empty,
							treeobj.ID,
							treeobj.FullPath);
						csic.Add(csdf);
					}
					else
					{
						// OK, this is a file
						ChangeSetItem_DeleteFile csdf = new ChangeSetItem_DeleteFile(
							VaultDateTime.Now,
							client.Comment,
							String.Empty,
							treeobj.ID,
							treeobj.FullPath);
						csic.Add(csdf);
					}
				}

				return commitTransaction(csic);
			}
			catch (Exception e)
			{
				int errorcode = VaultConnection.GetSoapExceptionStatusCodeInt(e);
				if (errorcode == VaultLib.VaultStatusCode.FailInvalidSessionToken)
				{
					ServerOperations.Logout();
				}
				throw e;
			}
		}

		/// <summary>
		/// Delete a label that has been applied to a Vault object
		/// </summary>
		/// <param name="objectPath">The path to the object that has been labeled.  This can be either a repository or a local path.</param>
		/// <param name="labelName">The label that will be deleted from the object.</param>
		[LocalOrRemotePath("objectPath")]
		public static void ProcessCommandDeleteLabel(string objectPath, string labelName)
		{
			try
			{
				long labelID = 0;
				long rootID = 0;
				string[] discoveredPaths;
				VaultClientTreeObject labelStructure = null;

				client.ClientInstance.Refresh();

				VaultClientTreeObject reposTreeObj = RepositoryUtil.FindVaultTreeObjectAtReposOrLocalPath(objectPath);

				try
				{
					// There isn't a good API to get a label ID based on a label name, so just get the whole structure
					client.ClientInstance.GetByLabel_GetStructure(reposTreeObj.FullPath, labelName, ref labelID, "", out discoveredPaths, out labelStructure, out rootID);

					if (reposTreeObj.ID == rootID && labelID != 0)
					{
						int iRet = client.ClientInstance.DeleteLabel(reposTreeObj.FullPath, labelID);
						if (iRet != VaultStatusCode.Success)
						{
							throw new Exception("Delete Label error: " + VaultConnection.GetSoapExceptionMessage(iRet));
						}
					}
					else
					{
						throw new Exception(string.Format("Could not find label \"{0}\" created at item \"{1}\".  ", labelName, reposTreeObj.FullPath));
					}
				}
				catch (Exception /*e*/)
				{
					if (labelStructure == null)
					{
						throw new Exception(string.Format("Could not find label \"{0}\" created at item \"{1}\".  ", labelName, reposTreeObj.FullPath));
					}
					else
					{
						throw;
					}
				}
			}
			catch (Exception e)
			{
				int errorcode = VaultConnection.GetSoapExceptionStatusCodeInt(e);
				if (errorcode == VaultLib.VaultStatusCode.FailInvalidSessionToken)
				{
					ServerOperations.Logout();
				}
				throw e;
			}
		}

		/// <summary>
		/// Launch a diff program to compare a file or folder.
		/// </summary>
		/// <param name="diffProgram">The path to the diff program that will be launched.  If null or an empty string is passed, the VAULTDIFF environment variable will be read.</param>
		/// <param name="diffArguments">The arguments that will be passed into the diff program.</param>
		/// <param name="compareToOption">The type of diff that will be performed.  Possible values are:  &quot;current&quot;, &quot;label&quot;, &quot;lastget&quot;, &quot;local&quot;, and &quot;repository&quot;.</param>
		/// <param name="recursive">Recursively diff folders.</param>
		/// <param name="objectPathLeft">The path to the first object that will be diffed.  This can be either a repository or a local path.</param>
		/// <param name="objectPathRight">The value of this argument will depend on the CompareToOption that is specified.
		///<ul>
		///<li>CompareToOption.current: This parameter is not needed (pass null).</li>
		///<li>CompareToOption.label:  This parameter is the label that was applied to the object at objectPathLeft.</li>
		///<li>CompareToOption.lastget: This parameter is not needed (pass null).</li>
		///<li>CompareToOption.local:  This parameter is the path to the local folder or file.</li>
		///<li>CompareToOption.repository:  This parameter is the path to the repository file or folder.</li>
		///</ul>
		///</param>
		[LocalOrRemotePath("objectPathLeft")]
		public static void ProcessCommandDiff(string diffProgram, string diffArguments, CompareToOption compareToOption, bool recursive, string objectPathLeft, string objectPathRight)
		{
			DiffAgainstType diffType = DiffAgainstType.CurrentRepositoryVersion;
			string leftDescription = "", rightDescription = "";
			if (objectPathLeft == null || objectPathLeft == String.Empty)
				throw new UsageException("Diff requires that at least one object is specified.");
			switch (compareToOption)
			{
				case CompareToOption.current:
					diffType = DiffAgainstType.CurrentRepositoryVersion;
					// TODO - move into resource for globalization
					leftDescription = "Working: {0}";
					rightDescription = "Repository: {0}";
					break;

				case CompareToOption.label:

					if (objectPathRight != null)
					{
						diffType = DiffAgainstType.Label;

						// TODO - move into resource for globalization
						leftDescription = "Label: {0}";
						rightDescription = "Working: {0}";
					}
					else
					{
						throw new UsageException("When diffing against a label, you must include the label text.");
					}
					break;

				case CompareToOption.lastget:
					diffType = DiffAgainstType.PreviousRepositoryVersion;

					// TODO - move into resource for globalization
					leftDescription = "Baseline: {0}";
					rightDescription = "Working Version: {0}";
					break;

				case CompareToOption.local:

					if (objectPathRight != null)
					{
						diffType = DiffAgainstType.AnyLocalItem;

						// TODO - move into resource for globalization
						leftDescription = "{0}";
						rightDescription = "Working: {0}";
					}
					else
					{
						throw new UsageException("When diffing against a local item, you must include the path to the local item.");
					}
					break;

				case CompareToOption.repository:

					if (objectPathRight != null)
					{
						diffType = DiffAgainstType.AnyRepositoryItem;

						// TODO - move into resource for globalization
						leftDescription = "Repository: {0}";
						rightDescription = "Working: {0}";
					}
					else
					{
						throw new UsageException("When diffing against a repository item, you must include the path to the repository item.");
					}
					break;

				default:
					diffType = DiffAgainstType.CurrentRepositoryVersion;

					// TODO - move into resource for globalization
					leftDescription = "Working: {0}";
					rightDescription = "Repository: {0}";
					break;
			}

			int nDoDiffError = DoDiffError.Success;

			// determine the diff command
			if ((diffProgram == null) || (diffProgram.Length == 0))
			{
				// nothing specified on the command line, what about the environment variable.
#if ! JAVA
				diffProgram = Environment.GetEnvironmentVariable(DiffDefines.DiffEnv);
#endif

				if (diffProgram == null)
				{
					// just try plain diff
					diffProgram = DiffDefines.DiffBin;
				}
			}

			// add parameters for left/right to the args
			if (diffArguments.IndexOf(DiffDefines.DiffLeftItem) < 0)
			{
				diffArguments += string.Format("\"{0}\" ", DiffDefines.DiffLeftItem);
			}
			if (diffArguments.IndexOf(DiffDefines.DiffRightItem) < 0)
			{
				diffArguments += string.Format("\"{0}\" ", DiffDefines.DiffRightItem);
			}

			string strLItem = null, strRItem = null;
			Exception e = null;

			// get the repository item.
			VaultClientTreeObject treeobj = RepositoryUtil.FindVaultTreeObjectAtReposOrLocalPath(objectPathLeft);
			if (treeobj is VaultClientFile)
			{
				VaultClientFile vcfile = (VaultClientFile)treeobj;
				// the working folder must be set
				if (client.ClientInstance.TreeCache.GetBestWorkingFolder(vcfile.Parent) != null)
				{
					nDoDiffError = client.ClientInstance.DoWorkingFileDiff(vcfile, diffType, objectPathRight,
						diffProgram, diffArguments, leftDescription, rightDescription, out strLItem, out strRItem, out e);
				}
				else
				{
					nDoDiffError = DoDiffError.NoValidWorkingFolder;
				}
			}
			else
			{
				VaultClientFolder vcfolder = (VaultClientFolder)treeobj;
				if (client.ClientInstance.TreeCache.GetBestWorkingFolder(vcfolder) != null)
				{
					nDoDiffError = client.ClientInstance.DoWorkingFolderDiff(vcfolder, recursive, diffType, objectPathRight,
						diffProgram, diffArguments, leftDescription, rightDescription, out strLItem, out strRItem, out e);
				}
				else
				{
					nDoDiffError = DoDiffError.NoValidWorkingFolder;
				}
			}

			// handle the diff error
			if (nDoDiffError != DoDiffError.Success)
			{
				if (objectPathLeft == null)
				{
					objectPathLeft = "Unknown Item";
				}
				if (objectPathRight == null)
				{
					objectPathRight = "Unknown Item";
				}

				string strMessage = null;
				switch (nDoDiffError)
				{
					case DoDiffError.RepositoryItemNotFound:
					case DoDiffError.LeftItemDoesNotExist:
						strMessage = string.Format("Item {0} could not be found.", objectPathLeft);
						break;
					case DoDiffError.RightItemDoesNotExist:
						strMessage = string.Format("Item {0} could not be found.", objectPathRight);
						break;
					case DoDiffError.DiffBinaryError:
						strMessage = "The Diff utility encountered an error during execution.  Please verify the use of VAULTDIFF or the \"diff\" utility.";
						break;
					case DoDiffError.TempPathNotFound:
						strMessage = "Could not find temp path.";
						break;
					case DoDiffError.TempFileNotFound:
						strMessage = "Could not find temp file.";
						break;
					case DoDiffError.LabelNotRetrieved:
						strMessage = string.Format("Could not retrieve label {0}.", objectPathRight);
						break;
					case DoDiffError.NoValidWorkingFolder:
						strMessage = string.Format("The working folder has not been set for {0}.", objectPathLeft);
						break;
					default:
						if (e != null)
						{
							strMessage = e.Message;
						}
						else
						{
							// some other error
							// TODO - move this to a resource.
							strMessage = "An unknown error occurred executing the diff utility.";
						}
						break;
				}

				throw new Exception(strMessage);
			}
		}

		/// <summary>
		/// enum used to control if user/action filters are include or exclude
		/// </summary>
		private enum HistoryFilterType
		{
			Indeterminate,
			FilterIsExclude,
			FilterIsInclude
		}

		private static HistoryFilterType ConvertCharToHistoryFilterType(char c)
		{
			HistoryFilterType hft = HistoryFilterType.Indeterminate;
			switch (c)
			{
				case 'e':
				case 'E':
					hft = HistoryFilterType.FilterIsExclude;
					break;

				case 'i':
				case 'I':
					hft = HistoryFilterType.FilterIsInclude;
					break;
			}

			return hft;
		}

		/// <summary>
		/// Perform a full history query on the server.
		/// </summary>
		/// <param name="objectPath">The path to the object to use for the root as the history query.  This can be either a repository or a local path.</param>
		/// <param name="recursive">Recursively act on folders.</param>
		/// <param name="dateSort">Specify the sort ordering of the history results.  Possible values are:  &quot;asc&quot; and &quot;desc&quot;.</param>
		/// <param name="filterUserType">Specify if the user filter list will be an &quot;include&quot; ('i' | 'I') or &quot;exclude&quot; ('e' | 'E') type.</param>
		/// <param name="filteredUsers">A comma-separated list of users to filter within the history request.  For example, &quot;admin,builduser,bob&quot;.  Pass null to not filter out users.</param>
		/// <param name="filterActionType">Specify if the action filter list will be an &quot;include&quot; ('i' | 'I') or &quot;exclude&quot; ('e' | 'E') type.</param>
		/// <param name="filteredActions">A comma-separated list of actions to filter within the history request.  All actions are: &quot;add,branch,checkin,create,delete,label,move,obliterate,pin,propertychange,rename,rollback,share,snapshot,undelete&quot;.</param>
		/// <param name="beginDate">The date that will be used as the starting point for the history query.  Pass VaultDate.EmptyDate() to include all history.</param>
		/// <param name="endDate">The date that will be used as the ending point for the history query.  Pass VaultDate.EmptyDate() to include all history.</param>
		/// <param name="beginLabel">The label that will be used as the starting point for the history query.  Pass null to include all history.</param>
		/// <param name="endLabel">The label that will be used as the ending point for the history query.  Pass null to include all history.</param>
		/// <param name="beginVersion">The version that will be used as the starting point for the history query.  Pass -1 to include all history.</param>
		/// <param name="endVersion">The version that will be used as the ending point for the history query.  Pass -1 to include all history.</param>
		/// <param name="rowLimit">The maximum number of history items will be returned.</param>
		/// <param name="commentFilter">The substring to search for in comments.  If this is null or empty, no comment filtering will be performed.</param>
		/// <returns>An array of VaultHistoryItem references for each history action that the history query returned.</returns>
		/// , RecommendedOptionDefault("", "")
		[LocalOrRemotePath("objectPath"), RecommendedOptionDefault("recursive", "false"), RecommendedOptionDefault("dateSort", "\"desc\""), RecommendedOptionDefault("filterUserType", "'x'"), RecommendedOptionDefault("filteredUsers", "null"), RecommendedOptionDefault("filterActionType", "'x'"), RecommendedOptionDefault("filteredActions", "null"), RecommendedOptionDefault("beginDate", "null"), RecommendedOptionDefault("endDate", "null"), RecommendedOptionDefault("beginLabel", "null"), RecommendedOptionDefault("endLabel", "null"), RecommendedOptionDefault("beginVersion", "-1"), RecommendedOptionDefault("endVersion", "-1"), RecommendedOptionDefault("rowLimit", "1000"), RecommendedOptionDefault("commentFilter", "null")]
		public static VaultHistoryItem[] ProcessCommandHistoryEx(string objectPath, bool recursive, DateSortOption dateSort, char filterUserType, string filteredUsers, char filterActionType, string filteredActions, string beginDate, string endDate, string beginLabel, string endLabel, long beginVersion, long endVersion, int rowLimit, string commentFilter)
		{
			try
			{
				ClientInstance ci = client.ClientInstance;

				// prepare the object used to filter the history results
				VaultHistoryQueryRequest hq = new VaultHistoryQueryRequest();
				hq.RepID = ci.ActiveRepositoryID;
				if (commentFilter != null && commentFilter != "")
				{
					hq.CommentFilter = VaultQueryRequestComments.FilteredComment;
					hq.CommentSubstring = commentFilter;
				}

				hq.Recursive = recursive;

				VaultClientTreeObject treeobj = RepositoryUtil.FindVaultTreeObjectAtReposOrLocalPath(objectPath);
				if (treeobj is VaultClientFolder)
				{
					hq.TopName = treeobj.FullPath;
					hq.TopID = treeobj.ID;
					hq.IsFolder = true;
				}
				else
				{
					hq.TopName = treeobj.FullPath;
					hq.TopID = treeobj.ID;
					hq.IsFolder = false;
				}


				// set the sort order
				hq.Sorts = new long[1];
				hq.Sorts[0] = (dateSort == DateSortOption.asc) ?
				(long)(VaultQueryRequestSort.DateSort | VaultQueryRequestSort.AscSort) :
				(long)(VaultQueryRequestSort.DateSort | VaultQueryRequestSort.DescSort);


				// filter users
				HistoryFilterType hft = ConvertCharToHistoryFilterType(filterUserType);
				if ((string.IsNullOrEmpty(filteredUsers) == false) && (hft != HistoryFilterType.Indeterminate))
				{
					// need the entire list of users for either include/exclude case.
					VaultUser[] serverUsers = null;
					ci.Connection.GetUserList(ref serverUsers);
					if (serverUsers == null)
					{
						WriteUserMessage("Could not get user list for history query.");
						return null;
					}

					// this is the array of vaultusers passed to the server.
					System.Collections.Generic.List<VaultLib.VaultUser> listServerUsers = null;

					// the arg list.
					string[] arUserArgs = filteredUsers.Split(",".ToCharArray());

					if (hft == HistoryFilterType.FilterIsExclude)
					{
						// initialize a FULL list.
						listServerUsers = new System.Collections.Generic.List<VaultLib.VaultUser>(serverUsers);

						foreach (string excludedUser in arUserArgs)
						{
							for (int i = serverUsers.Length - 1; i >= 0; i--)
							{
								if (string.Compare(serverUsers[i].Login, excludedUser.Trim(), true) == 0)
								{
									listServerUsers.Remove(serverUsers[i]);
								}
							}
						}
					}
					else // if (hft == HistoryFilterType.FilterIsInclude)
					{
						// create an empty list
						listServerUsers = new System.Collections.Generic.List<VaultLib.VaultUser>();

						foreach (string includedUser in arUserArgs)
						{
							for (int i = serverUsers.Length - 1; i >= 0; i--)
							{
								if (string.Compare(serverUsers[i].Login, includedUser.Trim(), true) == 0)
								{
									listServerUsers.Add(serverUsers[i]);
								}
							}
						}
					}

					if ((listServerUsers != null) && (listServerUsers.Count > 0)) { hq.Users = listServerUsers.ToArray(); }
				}

				// set the date ranges.
				bool bBegDateNull = beginDate == null || beginDate.Length == 0;
				bool bEndDateNull = endDate == null || endDate.Length == 0;
				VaultDateTime beginVaultDateTime = VaultDate.EmptyDate();
				VaultDateTime endVaultDateTime = VaultDate.EmptyDate();
				if (bBegDateNull == false)
				{
					beginVaultDateTime = VaultDateTime.Parse(beginDate);
					bBegDateNull = VaultDate.IsEmptyDate(beginVaultDateTime);
				}
				if (bEndDateNull == false)
				{
					endVaultDateTime = VaultDateTime.Parse(endDate);
					bEndDateNull = VaultDate.IsEmptyDate(endVaultDateTime);
				}
				//Fill this list once here, so that we don't hit the server twice for 
				//the same basic information.
				VaultObjectVersionInfo[] vovi = null;
				if (beginVersion >= 1)
				{
					ci.Connection.GetObjectVersionList(treeobj.ID, beginVersion, ref vovi, false);
				}
				else if (endVersion >= 1)
				{
					ci.Connection.GetObjectVersionList(treeobj.ID, endVersion, ref vovi, false);
				}

				#region "Use the Label Dates if no begin or end dates were specified"

				if (bBegDateNull == true && beginLabel != null && beginLabel != string.Empty)
				{//Try to find the label that specified.
					VaultDateTime dtBegin = VaultDateTime.MinValue;
					string strQryTokenBegin = "";
					int nRowsRecursive = 0, nRowsInherited = 0;
					ci.BeginLabelQuery(treeobj.FullPath, treeobj.ID, false, true, false, true, int.MaxValue, out nRowsInherited, out nRowsRecursive, out strQryTokenBegin);

					VaultLabelItemX[] vlx = null;
					int current = 0;
					while (dtBegin == VaultDateTime.MinValue && current < nRowsInherited)
					{
						ci.GetLabelQueryItems_Main(strQryTokenBegin, current, current + 5, out vlx);

						if (vlx != null)
							foreach (VaultLabelItemX vli in vlx)
							{
								if (string.Compare(vli.Label, beginLabel, true) == 0)
								{
									if (vli.LabelType == VaultLabelResultType.MainLabel
									|| vli.LabelType == VaultLabelResultType.InheritedLabel)
									{
										//This is a hack, looking only at the date the label is applied.
										//You can break this by labeling a historical version of an object.
										dtBegin = vli.LabelDate;
									}
								}
							}
						current = current + 5;
					}
					ci.EndLabelQuery(strQryTokenBegin);
					if (dtBegin == VaultDateTime.MinValue)
					{
						WriteUserMessage("The label " + beginLabel + " could not be found.");
						return null;
					}
					else
					{
						bBegDateNull = false;
						beginVaultDateTime = dtBegin;
					}
				}
				else if (bBegDateNull == true && beginLabel == null && beginVersion != -1)
				{
					if (vovi != null)
					{
						foreach (VaultObjectVersionInfo vi in vovi)
						{
							if (vi.Version == beginVersion)
							{
								beginVaultDateTime = vi.TxDate;
								bBegDateNull = false;
								break;
							}
						}
					}
				}
				if (bEndDateNull == true && endLabel != null && endLabel != string.Empty)
				{//Try to find the label that specified.
					VaultDateTime dtEnd = VaultDateTime.MinValue;
					string strQryTokenEnd = "";
					int nRowsRecursive = 0, nRowsInherited = 0;
					ci.BeginLabelQuery(treeobj.FullPath, treeobj.ID, false, true, false, true, int.MaxValue, out nRowsInherited, out nRowsRecursive, out strQryTokenEnd);

					VaultLabelItemX[] vlx = null;
					int current = 0;
					while (dtEnd == VaultDateTime.MinValue && current < nRowsInherited)
					{
						ci.GetLabelQueryItems_Main(strQryTokenEnd, current, current + 5, out vlx);

						if (vlx != null)
							foreach (VaultLabelItemX vli in vlx)
							{
								if (string.Compare(vli.Label, endLabel, true) == 0)
								{
									if (vli.LabelType == VaultLabelResultType.MainLabel
									|| vli.LabelType == VaultLabelResultType.InheritedLabel)
									{
										//This is a hack, looking only at the date the label is applied.
										//You can break this by labeling a historical version of an object.
										dtEnd = vli.LabelDate;
									}
								}
							}
						current = current + 5;
					}
					ci.EndLabelQuery(strQryTokenEnd);
					if (dtEnd == VaultDateTime.MinValue)
					{
						WriteUserMessage("The label " + endLabel + " could not be found.");
						return null;
					}
					else
					{
						bEndDateNull = false;
						endVaultDateTime = dtEnd;
					}
				}
				else if (bEndDateNull == true && endLabel == null && endVersion != -1)
				{
					if (vovi != null)
					{
						foreach (VaultObjectVersionInfo vi in vovi)
						{
							if (vi.Version == endVersion)
							{
								endVaultDateTime = vi.TxDate;
								bEndDateNull = false;
								break;
							}
						}
					}
				}
				#endregion

				if ((bBegDateNull == true) &&
				(bEndDateNull == true))
				{
					// no date range
					hq.DateFilterMask = VaultQueryRequestDates.DoNotFilter;
					hq.BeginDate = hq.EndDate = VaultDate.EmptyDate();
				}
				else if ((bBegDateNull == false) &&
				(bEndDateNull == false))
				{
					// a range of dates has been requested
					hq.DateFilterMask = VaultQueryRequestDates.HistoryBefore | VaultQueryRequestDates.HistoryAfter;
					hq.BeginDate = beginVaultDateTime;
					hq.EndDate = endVaultDateTime;
				}
				else if (bBegDateNull == false)
				{
					// when -begindate (floor) has been specified,
					// and -enddate has not, the user
					// is asking for all dates after the  date.

					// q query of this type should be defined so the
					// end date is valid and the begindate is empty.
					hq.DateFilterMask = VaultQueryRequestDates.HistoryAfter;
					hq.BeginDate = VaultDate.EmptyDate();
					hq.EndDate = beginVaultDateTime;
				}
				else // bEndDateNull will be false
				{
					// when -enddate (ceiling) has been specified,
					// and -begindate has not, the user
					// is asking for all dates before the date.

					// q query of this type should be defined so the
					// begin date is valid and the enddate is empty.
					hq.DateFilterMask = VaultQueryRequestDates.HistoryBefore;
					hq.BeginDate = endVaultDateTime;
					hq.EndDate = VaultDate.EmptyDate();
				}

				#region Exclude Actions

				hft = ConvertCharToHistoryFilterType(filterActionType);
				if ((string.IsNullOrEmpty(filteredActions) == false) && (hft != HistoryFilterType.Indeterminate))
				{
					// the list to filter against.
					System.Collections.Generic.List<long> listActions = new System.Collections.Generic.List<long>();

					string[] arActionArgs = filteredActions.Split(",".ToCharArray());

					if (hft == HistoryFilterType.FilterIsExclude)
					{
						// fill the list with 24 default actions to filter against.
						for (long action = 1; action <= 24; action++) { listActions.Add(action); }

						// exclude these actions.
						foreach (string excludedAction in arActionArgs)
						{
							switch (excludedAction.ToLower().Trim())
							{
								case "add":
								case "create":
									listActions.Remove((long)VaultRequestType.AddFile);
									listActions.Remove((long)VaultRequestType.AddFolder);
									break;
								case "branch":
									listActions.Remove((long)VaultRequestType.CopyBranch);
									listActions.Remove((long)VaultRequestType.ShareBranch);
									break;
								case "checkin":
									listActions.Remove((long)VaultRequestType.CheckIn);
									break;
								case "delete":
									listActions.Remove((long)VaultRequestType.Delete);
									break;
								case "label":
									listActions.Remove((long)VaultRequestType.LabelItem);
									break;
								case "move":
									listActions.Remove((long)VaultRequestType.Move);
									break;
								case "obliterate":
									listActions.Remove((long)VaultRequestType.Obliterate);
									break;
								case "pin":
									listActions.Remove((long)VaultRequestType.Pin);
									listActions.Remove((long)VaultRequestType.Unpin);
									break;
								case "propertychange":
									listActions.Remove((long)VaultRequestType.PropertyChanged);
									listActions.Remove((long)VaultRequestType.ExtPropertyChanged);
									break;
								case "rename":
									listActions.Remove((long)VaultRequestType.Rename);
									break;
								case "rollback":
									listActions.Remove((long)VaultRequestType.Rollback);
									break;
								case "share":
									listActions.Remove((long)VaultRequestType.Share);
									break;
								case "snapshot":
									listActions.Remove((long)VaultRequestType.Snapshot);
									break;
								case "undelete":
									listActions.Remove((long)VaultRequestType.Undelete);
									break;
							}
						}
					}
					else // if (hft == HistoryFilterType.FilterIsInclude)
					{
						// include these actions.
						foreach (string includedAction in arActionArgs)
						{
							switch (includedAction.ToLower().Trim())
							{
								case "add":
								case "create":
									listActions.Add((long)VaultRequestType.AddFile);
									listActions.Add((long)VaultRequestType.AddFolder);
									break;
								case "branch":
									listActions.Add((long)VaultRequestType.CopyBranch);
									listActions.Add((long)VaultRequestType.ShareBranch);
									break;
								case "checkin":
									listActions.Add((long)VaultRequestType.CheckIn);
									break;
								case "delete":
									listActions.Add((long)VaultRequestType.Delete);
									break;
								case "label":
									listActions.Add((long)VaultRequestType.LabelItem);
									break;
								case "move":
									listActions.Add((long)VaultRequestType.Move);
									break;
								case "obliterate":
									listActions.Add((long)VaultRequestType.Obliterate);
									break;
								case "pin":
									listActions.Add((long)VaultRequestType.Pin);
									listActions.Add((long)VaultRequestType.Unpin);
									break;
								case "propertychange":
									listActions.Add((long)VaultRequestType.PropertyChanged);
									listActions.Add((long)VaultRequestType.ExtPropertyChanged);
									break;
								case "rename":
									listActions.Add((long)VaultRequestType.Rename);
									break;
								case "rollback":
									listActions.Add((long)VaultRequestType.Rollback);
									break;
								case "share":
									listActions.Add((long)VaultRequestType.Share);
									break;
								case "snapshot":
									listActions.Add((long)VaultRequestType.Snapshot);
									break;
								case "undelete":
									listActions.Add((long)VaultRequestType.Undelete);
									break;
							}
						}
					}

					if ((listActions != null) && (listActions.Count > 0)) { hq.Actions = listActions.ToArray(); }
				}

				#endregion

				// ///////////////////////////////
				//   execute the query and get results.
				int nRowsRetrieved = 0;
				string strQryToken = null;
				VaultHistoryItem[] histitems = new VaultHistoryItem[0];

				ci.Connection.HistoryBegin(hq, rowLimit, ref nRowsRetrieved, ref strQryToken);
				if (nRowsRetrieved > 0)
				{
					ci.Connection.HistoryFetch(strQryToken, 0, nRowsRetrieved - 1, ref histitems);
				}
				ci.Connection.HistoryEnd(strQryToken);

				return histitems;
			}
			catch (Exception e)
			{
				int errorcode = VaultConnection.GetSoapExceptionStatusCodeInt(e);
				if (errorcode == VaultLib.VaultStatusCode.FailInvalidSessionToken)
				{
					ServerOperations.Logout();
				}
				throw e;
			}
		}

		/// <summary>
		/// Perform a exclusion only history query on the server
		/// </summary>
		/// <param name="objectPath">The path to the object to use for the root as the history query.  This can be either a repository or a local path.</param>
		/// <param name="recursive">Recursively act on folders.</param>
		/// <param name="dateSort">Specify the sort ordering of the history results.  Possible values are:  &quot;asc&quot; and &quot;desc&quot;.</param>
		/// <param name="excludedUsers">A comma-separated list of users to filter out of the history request.  For example, &quot;admin,builduser,bob&quot;.  Pass null to return history items for all users.</param>
		/// <param name="excludedActions">A comma-separated list of actions to filter out of the history request.  All actions are: &quot;add,branch,checkin,create,delete,label,move,obliterate,pin,propertychange,rename,rollback,share,snapshot,undelete&quot;.  Pass null to return history for all actions.</param>
		/// <param name="beginDate">The date that will be used as the starting point for the history query.  Pass VaultDate.EmptyDate() to include all history.</param>
		/// <param name="endDate">The date that will be used as the ending point for the history query.  Pass VaultDate.EmptyDate() to include all history.</param>
		/// <param name="beginLabel">The label that will be used as the starting point for the history query.  Pass null to include all history.</param>
		/// <param name="endLabel">The label that will be used as the ending point for the history query.  Pass null to include all history.</param>
		/// <param name="beginVersion">The version that will be used as the starting point for the history query.  Pass -1 to include all history.</param>
		/// <param name="endVersion">The version that will be used as the ending point for the history query.  Pass -1 to include all history.</param>
		/// <param name="rowLimit">The maximum number of history items will be returned.</param>
		/// <param name="commentFilter">The substring to search for in comments.  If this is null or empty, no comment filtering will be performed.</param>
		/// <returns>An array of VaultHistoryItem references for each history action that the history query returned.</returns>
		[LocalOrRemotePath("objectPath"), RecommendedOptionDefault("recursive", "false"), RecommendedOptionDefault("dateSort", "\"desc\""), RecommendedOptionDefault("excludedUsers", "null"), RecommendedOptionDefault("excludedActions", "null"), RecommendedOptionDefault("beginDate", "null"), RecommendedOptionDefault("endDate", "null"), RecommendedOptionDefault("beginLabel", "null"), RecommendedOptionDefault("endLabel", "null"), RecommendedOptionDefault("beginVersion", "-1"), RecommendedOptionDefault("endVersion", "-1"), RecommendedOptionDefault("rowLimit", "1000"), RecommendedOptionDefault("commentFilter", "null")]
		public static VaultHistoryItem[] ProcessCommandHistory(string objectPath, bool recursive, DateSortOption dateSort, string excludedUsers, string excludedActions, string beginDate, string endDate, string beginLabel, string endLabel, long beginVersion, long endVersion, int rowLimit, string commentFilter)
		{
			return ProcessCommandHistoryEx(objectPath, recursive, dateSort, 'E', excludedUsers, 'E', excludedActions, beginDate, endDate, beginLabel, endLabel, beginVersion, endVersion, rowLimit, commentFilter);
		}

		/// <summary>
		/// Perform an exclusion history query without comments on the server.
		/// </summary>
		/// <param name="objectPath">The path to the object to use for the root as the history query.  This can be either a repository or a local path.</param>
		/// <param name="recursive">Recursively act on folders.</param>
		/// <param name="dateSort">Specify the sort ordering of the history results.  Possible values are:  &quot;asc&quot; and &quot;desc&quot;.</param>
		/// <param name="excludedUsers">A comma-separated list of users to filter out of the history request.  For example, &quot;admin,builduser,bob&quot;.  Pass null to return history items for all users.</param>
		/// <param name="excludedActions">A comma-separated list of actions to filter out of the history request.  All actions are: &quot;add,branch,checkin,create,delete,label,move,obliterate,pin,propertychange,rename,rollback,share,snapshot,undelete&quot;.  Pass null to return history for all actions.</param>
		/// <param name="beginDate">The date that will be used as the starting point for the history query.  Pass VaultDate.EmptyDate() to include all history.</param>
		/// <param name="endDate">The date that will be used as the ending point for the history query.  Pass VaultDate.EmptyDate() to include all history.</param>
		/// <param name="beginLabel">The label that will be used as the starting point for the history query.  Pass null to include all history.</param>
		/// <param name="endLabel">The label that will be used as the ending point for the history query.  Pass null to include all history.</param>
		/// <param name="beginVersion">The version that will be used as the starting point for the history query.  Pass -1 to include all history.</param>
		/// <param name="endVersion">The version that will be used as the ending point for the history query.  Pass -1 to include all history.</param>
		/// <param name="rowLimit">The maximum number of history items will be returned.</param>
		/// <returns>An array of VaultHistoryItem references for each history action that the history query returned.</returns>
		[Hidden, LocalOrRemotePath("objectPath"), RecommendedOptionDefault("recursive", "false"), RecommendedOptionDefault("dateSort", "\"desc\""), RecommendedOptionDefault("excludedUsers", "null"), RecommendedOptionDefault("excludedActions", "null"), RecommendedOptionDefault("beginDate", "null"), RecommendedOptionDefault("endDate", "null"), RecommendedOptionDefault("beginLabel", "null"), RecommendedOptionDefault("endLabel", "null"), RecommendedOptionDefault("beginVersion", "-1"), RecommendedOptionDefault("endVersion", "-1"), RecommendedOptionDefault("rowLimit", "1000")]
		public static VaultHistoryItem[] ProcessCommandHistory(string objectPath, bool recursive, DateSortOption dateSort, string excludedUsers, string excludedActions, string beginDate, string endDate, string beginLabel, string endLabel, long beginVersion, long endVersion, int rowLimit)
		{
			return ProcessCommandHistoryEx(objectPath, recursive, dateSort, 'E', excludedUsers, 'E', excludedActions, beginDate, endDate, beginLabel, endLabel, beginVersion, endVersion, rowLimit, null);
		}

		/// <summary>
		/// Apply a label to a specific version of a Vault object.
		/// </summary>
		/// <param name="objectPath">The path to the object to label.  This can be either a repository or a local path.</param>
		/// <param name="labelName">The label that will be applied.  If the label has already been applied, an exception will be thrown.</param>
		/// <param name="versionID">The version that will have the label applied.  Pass -1 to apply a label to the latest version.</param>
		[LocalOrRemotePath("objectPath"), RecommendedOptionDefault("versionID", "-1")]
		public static void ProcessCommandLabel(string objectPath, string labelName, long versionID)
		{
			try
			{
				int ret = 0;
				VaultClientTreeObject vctreeobj = RepositoryUtil.FindVaultTreeObjectAtReposOrLocalPath(objectPath);
				VaultLabelResult labelResult = null;
				long objVerID = -1;

				VaultObjectVersionInfo[] ovis = null;
				client.ClientInstance.Connection.GetObjectVersionList(vctreeobj.ID, ref ovis, false);

				if ((ovis != null) && (ovis.Length > 0))
				{
					if (versionID == VaultDefine.Latest)
					{
						// use the last version, since the list is in ascending order.
						objVerID = ovis[ovis.Length - 1].ObjVerID;
					}
					else
					{
						// find the version number specified for this tree object
						foreach (VaultObjectVersionInfo ovi in ovis)
						{
							if (versionID == ovi.Version)
							{
								objVerID = ovi.ObjVerID;
								break;
							}
						}
					}
				}

				if (objVerID == -1)
				{
					throw new Exception(string.Format("{0} does not exist at version {1}", vctreeobj.FullPath, versionID));
				}

				ret = client.ClientInstance.AddLabel(vctreeobj.FullPath, objVerID, labelName, client.Comment, ref labelResult);
				switch (ret)
				{
					case VaultStatusCode.Success:
						break;

					case VaultStatusCode.FailDuplicateLabel:
						string strErrorMsg = null;
						if (String.Compare(labelResult.ExistingRootPath, vctreeobj.FullPath, true) == 0)
						{
							strErrorMsg = string.Format("{0} already has the label {1} applied", vctreeobj.FullPath, labelName);
						}
						else
						{
							strErrorMsg = string.Format("{0} has inherited the label {1} already", vctreeobj.FullPath, labelName);
						}
						throw new Exception(strErrorMsg);

					default:
						throw new Exception("Label error: " + VaultConnection.GetSoapExceptionMessage(ret));
				}
			}
			catch (Exception e)
			{
				int errorcode = VaultConnection.GetSoapExceptionStatusCodeInt(e);
				if (errorcode == VaultLib.VaultStatusCode.FailInvalidSessionToken)
				{
					ServerOperations.Logout();
				}
				throw e;
			}
		}

		/// <summary>
		/// Return the collection of change set items that are pending.
		/// </summary>
		/// <param name="objectPaths">An array of paths to recursively search for changes.  These can be either local paths or repository paths.</param>
		/// <returns>The collection of change set items that are pending.</returns>
		[LocalOrRemotePath("objectPaths"), RecommendedOptionDefault("objectPaths", "null")]
		public static ChangeSetItemColl ProcessCommandListChangeSet(string[] objectPaths)
		{
			try
			{
				ClientInstance ci = client.ClientInstance;

				if (!ci.HasActiveRepository)
				{
					return new ChangeSetItemColl();
				}
				ci.Refresh();
				if (ci.WorkingFolderOptions.RequireCheckOutBeforeCheckIn == false)
				{
					// do a scan to update the change set list
					if (objectPaths != null)
					{
						foreach (string objectPath in objectPaths)
						{
							VaultClientTreeObject treeobj = null;
							try
							{
								treeobj = RepositoryUtil.FindVaultTreeObjectAtReposOrLocalPath(objectPath);
							}
							catch (Exception)
							{
								continue;
							}
							if (treeobj != null)
							{
								if (treeobj is VaultClientFolder)
									ci.UpdateKnownChanges_Folder((VaultClientFolder)treeobj, false, true);
								else
									ci.UpdateKnownChanges_File(ci.GetWorkingFolder((VaultClientFile)treeobj), (VaultClientFile)treeobj);
							}
						}
					}
					else
						ci.UpdateKnownChanges_All(true);
				}
				else
				{
					ci.UpdateKnownChanges_RefreshKnown(true);
				}

				ChangeSetItemColl orig = ci.InternalChangeSet_GetItems(true);
				if (objectPaths == null || objectPaths.Length == 0)
					return orig;
				ChangeSetItemColl filtered = new ChangeSetItemColl();
				foreach (string objectPath in objectPaths)
				{
					try
					{
						VaultClientTreeObject treeobj = RepositoryUtil.FindVaultTreeObjectAtReposOrLocalPath(objectPath);
						for (int i = 0; i < orig.Count; i++)
						{
							ChangeSetItem csi = orig[i];
							if (csi.DisplayRepositoryPath == treeobj.FullPath || RepositoryPath.IsAncestorOf(treeobj.FullPath, csi.DisplayRepositoryPath))
								filtered.Add(csi);
						}
					}
					catch (Exception /*e*/)
					{
						bool found = false;
						for (int i = 0; i < orig.Count; i++)
						{
							ChangeSetItem csi = orig[i];
							if (csi.DisplayRepositoryPath == objectPath ||
								objectPath.StartsWith(csi.DisplayRepositoryPath + VaultDefine.PathSeparator) ||
								csi.DisplayName == objectPath ||
								objectPath.StartsWith(csi.DisplayName + Path.PathSeparator))
							{
								filtered.Add(csi);
								found = true;
							}
							else if (csi is ChangeSetItem_CreateFolder)
							{
								// CSI_CreateFolder only stores the repo path, we need to compute a disk path to check
								string strWFPath = ci.TreeCache.GetBestWorkingFolder(csi.DisplayRepositoryPath);
								if ((string.Compare(strWFPath, objectPath, true) == 0) || (objectPath.StartsWith(strWFPath + Path.PathSeparator) == true))
								{
									filtered.Add(csi);
									found = true;
								}
							}
						}
						if (found == false)
							WriteUserMessage("Could not find a tree object at: " + objectPath);
					}
				}
				return filtered;
			}
			catch (Exception e)
			{
				int errorcode = VaultConnection.GetSoapExceptionStatusCodeInt(e);
				if (errorcode == VaultLib.VaultStatusCode.FailInvalidSessionToken)
				{
					ServerOperations.Logout();
				}
				throw e;
			}
		}

		/// <summary>
		/// Returns the collection of items that are checked out by all users in the repository
		/// </summary>
		/// <returns>The collection of items that are checked out by all users in the repository</returns>
		public static VaultClientCheckOutList ProcessCommandListCheckOuts()
		{
			try
			{
				ClientInstance ci = client.ClientInstance;
				ci.Refresh();
				return ci.TreeCache.CheckOuts;
			}
			catch (Exception e)
			{
				int errorcode = VaultConnection.GetSoapExceptionStatusCodeInt(e);
				if (errorcode == VaultLib.VaultStatusCode.FailInvalidSessionToken)
				{
					ServerOperations.Logout();
				}
				throw e;
			}
		}


		/// <summary>
		/// List the properties of a folder or file in the repositories.
		/// </summary>
		/// <param name="objectPath">The path to the object whose properties.   This can be either a repository or a local path.</param>
		/// <returns></returns>
		[LocalOrRemotePath("objectPath")]
		public static VaultClientTreeObject ProcessCommandListObjectProperties(string objectPath)
		{
			try
			{
				VaultClientTreeObject vcforig = RepositoryUtil.FindVaultTreeObjectAtReposOrLocalPath(objectPath);
				return vcforig;
			}
			catch (Exception e)
			{
				int errorcode = VaultConnection.GetSoapExceptionStatusCodeInt(e);
				if (errorcode == VaultLib.VaultStatusCode.FailInvalidSessionToken)
				{
					ServerOperations.Logout();
				}
				throw e;
			}
		}
		/// <summary>
		/// List the contents of a folder.
		/// </summary>
		/// <param name="folderPath">The path to the folder whose contents you would like displayed.   This can be either a repository or a local path.</param>
		/// <param name="recursive">Return information about items inside this folder's children.</param>
		/// <returns>A VaultClientFolder object with the requested contents.</returns>
		[LocalOrRemotePath("folderPath"), RecommendedOptionDefault("recursive", "false")]
		public static VaultClientFolder ProcessCommandListFolder(string folderPath, bool recursive)
		{
			try
			{
				VaultClientFolder vcforig = RepositoryUtil.FindVaultFolderAtReposOrLocalPath(folderPath);
				if (vcforig == null)
					return null;
				VaultClientFolder clone = vcforig.Clone(vcforig.Parent);
				if (recursive == false)
				{
					foreach (VaultClientFolder sub in clone.Folders)
					{
						sub.Folders.Clear();
						sub.Files.Clear();
					}
				}
				return clone;
			}
			catch (Exception e)
			{
				int errorcode = VaultConnection.GetSoapExceptionStatusCodeInt(e);
				if (errorcode == VaultLib.VaultStatusCode.FailInvalidSessionToken)
				{
					ServerOperations.Logout();
				}
				throw e;
			}
		}
		/// <summary>
		/// Return the collection of repository information for the server that is currently connected.
		/// </summary>
		/// <returns>The collection of repository information for the server that is currently connected.</returns>
		[DoesNotRequireRepository]
		public static VaultRepositoryInfo[] ProcessCommandListRepositories()
		{
			try
			{
				VaultRepositoryInfo[] reps = null;
				client.ClientInstance.ListRepositories(ref reps);
				return reps;
			}
			catch (Exception e)
			{
				int errorcode = VaultConnection.GetSoapExceptionStatusCodeInt(e);
				if (errorcode == VaultLib.VaultStatusCode.FailInvalidSessionToken)
				{
					ServerOperations.Logout();
				}
				throw e;
			}
		}

		/// <summary>
		/// Move a vault object from one location
		/// </summary>
		/// <param name="objectPath_From">The path to the object that will be moved.  This can be either a repository or a local path.</param>
		/// <param name="objectPath_To">The path to the folder into which the item is to be moved.  If the folder already exists, no object with the same name as the moving object can exist therein already.  This can be either a repository or a local path.</param>
		[LocalOrRemotePath("objectPath_From"), LocalOrRemotePath("repositoryPath_To")]
		public static ChangeSetItemColl ProcessCommandMove(string objectPath_From, string objectPath_To)
		{
			try
			{
				ChangeSetItemColl csic = new ChangeSetItemColl();

				ArrayList folders = new ArrayList();
				string folderpath = RepositoryUtil.CleanUpPathAndReturnRepositoryPath(objectPath_To);
				objectPath_To = folderpath;
				while (RepositoryUtil.PathExists(folderpath) == false)
				{
					folders.Add(folderpath);
					folderpath = RepositoryPath.GetFolder(folderpath);
				}

				folders.Reverse();
				foreach (string folder in folders)
				{
					csic.Add(new ChangeSetItem_CreateFolder(
					VaultDateTime.Now,
					client.Comment,
					String.Empty,
					folder));
				}

				VaultClientTreeObject treeobj = RepositoryUtil.FindVaultTreeObjectAtReposOrLocalPath(objectPath_From);

				ChangeSetItem_Move csi = new ChangeSetItem_Move(
					VaultDateTime.Now,
					client.Comment,
					String.Empty,
					treeobj.ID,
					treeobj.FullPath,
					objectPath_To);
				csic.Add(csi);

				return commitTransaction(csic);
			}
			catch (Exception e)
			{
				int errorcode = VaultConnection.GetSoapExceptionStatusCodeInt(e);
				if (errorcode == VaultLib.VaultStatusCode.FailInvalidSessionToken)
				{
					ServerOperations.Logout();
				}
				throw e;
			}
		}

		/// <summary>
		/// Obliterate an object which has been deleted.  This will throw an Exception if there are multiple deleted objects at the specified path.
		/// Obliterate is will permanently remove a deleted folder or file (and all 
		/// its history) from the repository.  You must be logged in as a administrator 
		/// user to use this command.  This command should not be used lightly, as there 
		/// is no way to undo it.
		/// </summary>
		/// <param name="repositoryPath">The repository path to the object which will be obliterate.  This object must already be deleted.</param>
		[RemotePathOnly("repositoryPath")]
		public static void ProcessCommandObliterate(string repositoryPath)
		{
			Login(VaultConnection.AccessLevelType.Admin, true, false);
			ArrayList fileList = new ArrayList();
			string txtID = null;
			VaultDeletedObject[] vDeletedObjects = null;
			client.ClientInstance.Connection.ListDeletedObjects(client.ClientInstance.ActiveRepositoryID, "$/", true, ref vDeletedObjects);

			VaultRequestItem[] vRequests = new VaultRequestItem[1];
			VaultRequestObliterate r = null;
			if (vDeletedObjects != null && vDeletedObjects.Length > 0)
			{
				foreach (VaultDeletedObject item in vDeletedObjects)
				{
					if (String.Compare(item.FullPath, repositoryPath, true) == 0)
					{
						if (r != null)
						{
							throw new Exception("There are multiple deleted objects at the specified path.  Please use the admin tool to choose between the items.");
						}
						r = new VaultRequestObliterate();
						r.ObjID = item.ID;
						r.ItemPath = item.FullPath;
						r.DeletionID = item.DeletionID;
					}
				}
			}
			else
			{
				throw new Exception("There are no deleted items in the repository.");
			}
			if (r == null)
			{
				throw new Exception("No deleted item was found at " + repositoryPath);
			}
			else
				vRequests[0] = r;

			client.ClientInstance.Connection.BeginTx(client.ClientInstance.ActiveRepositoryID, ref vRequests, ref txtID, "");
			VaultResponseObliterate resp = null;

			foreach (VaultRequestItem req in vRequests)
			{
				if (req.Response.Status == VaultLib.VaultStatusCode.Success)
				{
					resp = (VaultResponseObliterate)req.Response;
					if (resp != null && resp.ObliteratedObjects != null)
					{
						foreach (string s in resp.ObliteratedObjects)
						{
							WriteUserMessage("Obliterating: " + s);
						}
					}
				}
				else if (req.Response.Status == VaultLib.VaultStatusCode.FailObliterateBranchExists)
				{
					string conflictlist = "";
					resp = (VaultResponseObliterate)req.Response;
					foreach (string s in resp.BranchedConflicts)
					{
						conflictlist += s + "\n";
					}
					//If one of the requests failed, the the TxID isn't valid, and we don't need to worry about
					// aborting the operation.
					throw new Exception(string.Format("Unable to Obliterate item: {0} All branches of an item must be obliterated before the item itself can be obliterated. It is possible that some of the branched items have been deleted, but not obliterated. You must obliterate all branches before you can obliterate this item. This item has branches at: {1}", req.ItemPath + "\n\n", "\n\n" + conflictlist + "\n"));
				}
				else if (req.Response.Status == VaultLib.VaultStatusCode.FailObliterateItemNotDeleted)
				{
					throw new Exception(string.Format("Unable to Obliterate the following item because it is not deleted: {0}", req.ItemPath));
				}
				else
				{
					throw new Exception("Unable to obliterate objects " + VaultClientNetLib.VaultConnection.GetSoapExceptionMessage(req.Response.Status));
					//If one of the requests failed, the the TxID isn't valid, and we don't need to worry about
					// aborting the operation.
				}
			}

			VaultResponseItem[] responses = new VaultResponseItem[vRequests.Length];
			for (int i = 0; i < vRequests.Length; i++)
				responses[i] = vRequests[i].Response;

			long newRevision = 0;
			VaultDateTime serverCheckInTime = VaultDateTime.Now;
			int action = VaultLib.VaultTxAction.Commit;

			client.ClientInstance.Connection.EndTx(txtID, ref newRevision, ref responses, action, ref serverCheckInTime);
		}

		/// <summary>
		/// Pin an object at a specific version.
		/// </summary>
		/// <param name="objectPath">The path to the object that will be pinned.  This can be either a repository or a local path.</param>
		/// <param name="version">The version number that the object will be pinned at.  If the object doesn't have this version, an exception will be thrown.  Passing -1 will pin the object to the current version.</param>
		[RecommendedOptionDefault("version", "-1"), LocalOrRemotePath("objectPath")]
		public static ChangeSetItemColl ProcessCommandPin(string objectPath, int version)
		{
			try
			{
				ChangeSetItemColl csic = new ChangeSetItemColl();

				VaultClientTreeObject treeobj = RepositoryUtil.FindVaultTreeObjectAtReposOrLocalPath(objectPath);
				long objverid = version;
				if (objverid != VaultDefine.Latest)
				{
					VaultObjectVersionInfo[] ovis = null;
					client.ClientInstance.Connection.GetObjectVersionList(treeobj.ID, ref ovis, false);
					bool bFound = false;
					foreach (VaultObjectVersionInfo ovi in ovis)
					{
						if (ovi.Version == version)
						{
							objverid = ovi.ObjVerID;
							bFound = true;
							break;
						}
					}
					if (!bFound)
					{
						throw new Exception(string.Format("Version {0} of {1} does not exist", version, treeobj.FullPath));
					}
				}


				ChangeSetItem_Pin csi = new ChangeSetItem_Pin(
					VaultDateTime.Now,
					client.Comment,
					String.Empty,
					objverid,
					treeobj.FullPath);
				csic.Add(csi);

				return commitTransaction(csic);
			}
			catch (Exception e)
			{
				int errorcode = VaultConnection.GetSoapExceptionStatusCodeInt(e);
				if (errorcode == VaultLib.VaultStatusCode.FailInvalidSessionToken)
				{
					ServerOperations.Logout();
				}
				throw e;
			}
		}
		/// <summary>
		/// Rename a Vault object in the repository.
		/// </summary>
		/// <param name="objectPath">The path to the object that will be renamed.  This can be either a repository or a local path.</param>
		/// <param name="newName">The new name for the object.</param>
		[LocalOrRemotePath("objectPath")]
		public static ChangeSetItemColl ProcessCommandRename(string objectPath, string newName)
		{
			try
			{
				ChangeSetItemColl csic = new ChangeSetItemColl();

				VaultClientTreeObject treeobj = RepositoryUtil.FindVaultTreeObjectAtReposOrLocalPath(objectPath);
				ChangeSetItem_Rename csi = new ChangeSetItem_Rename(
					VaultDateTime.Now,
					client.Comment,
					String.Empty,
					treeobj.ID,
					treeobj.FullPath,
					newName);
				csic.Add(csi);
				return commitTransaction(csic);
			}
			catch (Exception e)
			{
				int errorcode = VaultConnection.GetSoapExceptionStatusCodeInt(e);
				if (errorcode == VaultLib.VaultStatusCode.FailInvalidSessionToken)
				{
					ServerOperations.Logout();
				}
				throw e;
			}
		}

		/// <summary>
		/// Rename a label that has been applied to an Vault object.
		/// </summary>
		/// <param name="objectPath">The path to the object that has been labeled with the oldLabelName.  This can be either a repository or a local path.</param>
		/// <param name="oldLabelName">The label that will be renamed.  If this label hasn't been applied to the object, an exception will be thrown.</param>
		/// <param name="newLabelName">The new label that will given to the object.  If this label has already been applied to the object, an exception will be thrown.</param>
		[LocalOrRemotePath("objectPath")]
		public static void ProcessCommandRenameLabel(string objectPath, string oldLabelName, string newLabelName)
		{
			try
			{
				long labelID = 0;
				long rootID = 0;
				string[] discoveredPaths;
				VaultClientTreeObject labelStructure = null;

				client.ClientInstance.Refresh();

				VaultClientTreeObject reposTreeObj = RepositoryUtil.FindVaultTreeObjectAtReposOrLocalPath(objectPath);

				try
				{
					// There isn't a good API to get a label ID based on a label name, so just get the whole structure
					client.ClientInstance.GetByLabel_GetStructure(objectPath, oldLabelName, ref labelID, "", out discoveredPaths, out labelStructure, out rootID);

					if (reposTreeObj.ID == rootID && labelID != 0)
					{
						// We found the label ID.  Now rename it.
						VaultDateTime lastModified = VaultDateTime.Now;
						int indexFailed;
						string rootPathConflict;

						int ret = client.ClientInstance.PromoteLabelItems(objectPath, labelID, newLabelName, ref lastModified,
							null, out indexFailed, out rootPathConflict);

						if (ret == VaultStatusCode.FailDuplicateLabel)
						{
							// FailDuplicateLabel requires some string formatting.
							throw new Exception(
								String.Format(VaultConnection.GetSoapExceptionMessage(ret), rootPathConflict));
						}
						else if (ret != VaultStatusCode.Success)
						{
							throw new Exception(VaultConnection.GetSoapExceptionMessage(ret));
						}
					}
					else
					{
						throw new Exception(string.Format("Could not find label \"{0}\" created at item \"{1}\".  ", oldLabelName, objectPath));
					}
				}
				catch
				{
					if (labelStructure == null)
					{
						throw new Exception(string.Format("Could not find label \"{0}\" created at item \"{1}\".  ", oldLabelName, objectPath));
					}
					else
					{
						throw;
					}
				}
			}
			catch (Exception e)
			{
				int errorcode = VaultConnection.GetSoapExceptionStatusCodeInt(e);
				if (errorcode == VaultLib.VaultStatusCode.FailInvalidSessionToken)
				{
					ServerOperations.Logout();
				}
				throw e;
			}
		}

		/// <summary>
		/// Share a Vault object from one location to another.
		/// </summary>
		/// <param name="objectPath_From">The path to the file or folder that will be shared.  This can be either a repository or a local path.</param>
		/// <param name="folderPath_To">The path to the folder where the shared object will be put.  This folder must already exist.  This can be either a repository or a local path.</param>
		[LocalOrRemotePath("objectPath_From"), LocalOrRemotePath("folderPath_To")]
		public static ChangeSetItemColl ProcessCommandShare(string objectPath_From, string folderPath_To)
		{
			try
			{
				ChangeSetItemColl csic = new ChangeSetItemColl();

				VaultClientTreeObject treeobj = RepositoryUtil.FindVaultTreeObjectAtReposOrLocalPath(objectPath_From);
				VaultClientFolder targetFolder = RepositoryUtil.FindVaultFolderAtReposOrLocalPath(folderPath_To);
				ChangeSetItem_Share csi = new ChangeSetItem_Share(
					VaultDateTime.Now,
					client.Comment,
					String.Empty,
					treeobj.ID,
					treeobj.FullPath,
					targetFolder.FullPath);
				csic.Add(csi);
				return commitTransaction(csic);
			}
			catch (Exception e)
			{
				int errorcode = VaultConnection.GetSoapExceptionStatusCodeInt(e);
				if (errorcode == VaultLib.VaultStatusCode.FailInvalidSessionToken)
				{
					ServerOperations.Logout();
				}
				throw e;
			}
		}

		/// <summary>
		/// Returns the working folder status for the paths that have been passed in.
		/// Folders will be ignored.
		/// </summary>
		/// <param name="objectPaths"></param>
		/// <returns></returns>
		[Hidden]
		public static WorkingFolderFileStatus[] ProcessCommandStatus(string[] objectPaths)
		{
			try
			{
				ClientInstance ci = client.ClientInstance;

				ArrayList objects = new ArrayList();
				foreach (string path in objectPaths)
				{
					objects.AddRange(RepositoryUtil.MatchWildcardToTreeObjects(path));
				}
				ArrayList statuses = new ArrayList();
				foreach (VaultClientTreeObject treeobj in objects)
				{
					if (treeobj is VaultClientFile)
					{
						WorkingFolder wf = ci.GetWorkingFolder(treeobj.Parent);
						if (wf == null)
						{
							statuses.Add(WorkingFolderFileStatus.None);
						}
						else
						{
							statuses.Add(wf.GetStatus((VaultClientFile)treeobj));
						}
					}
				}
				return (WorkingFolderFileStatus[])statuses.ToArray(typeof(WorkingFolderFileStatus));
			}
			catch (Exception e)
			{
				int errorcode = VaultConnection.GetSoapExceptionStatusCodeInt(e);
				if (errorcode == VaultLib.VaultStatusCode.FailInvalidSessionToken)
				{
					ServerOperations.Logout();
				}
				throw e;
			}
		}
		/// <summary>
		/// Remove the cloak property on a folder.
		/// </summary>
		/// <param name="objectPaths">An array of paths to uncloak.  These can be either local paths or repository paths.</param>
		[LocalOrRemotePath("objectPaths")]
		public static void ProcessCommandUncloak(string[] objectPaths)
		{
			try
			{
				ClientInstance ci = client.ClientInstance;
				ci.Refresh();

				foreach (string objectPath in objectPaths)
				{
					VaultClientFolder vcfolder = RepositoryUtil.FindVaultFolderAtReposOrLocalPath(objectPath);
					ci.CloakOrUnCloak(vcfolder.FullPath, false);
					WriteUserMessage(string.Format("Cloaked: {0}", vcfolder.FullPath));
				}
			}
			catch (Exception e)
			{
				int errorcode = VaultConnection.GetSoapExceptionStatusCodeInt(e);
				if (errorcode == VaultLib.VaultStatusCode.FailInvalidSessionToken)
				{
					ServerOperations.Logout();
				}
				throw e;
			}
		}

		/// <summary>
		/// Undo the Nth item in the internal change set.  This will undo change set items such as add, delete or rename.
		/// Use ListChangeSetItems to determine the id of each change set item.
		/// </summary>
		/// <param name="changeSetItemId">The index of the change set item that will be removed from the internal change set item collection.</param>
		[Hidden]
		public static void ProcessCommandUndoChangeSetItem(int changeSetItemId)
		{
			try
			{
				if (ValidateChangeSetItemID(changeSetItemId) == false)
				{
					throw new UsageException(string.Format("Invalid ChangeSetItem ID: {0}.  Please use the LISTCHANGESET command to retrieve a valid ID.", changeSetItemId));
				}

				// get the change set item by index - note
				// error checking should have been done at this point.
				ChangeSetItemColl csic = client.ClientInstance.InternalChangeSet_GetItems(true);
				ChangeSetItem csi = csic[changeSetItemId];

				// remove this item from the set.
				client.ClientInstance.InternalChangeSet_Undo(csi);
			}
			catch (Exception e)
			{
				int errorcode = VaultConnection.GetSoapExceptionStatusCodeInt(e);
				if (errorcode == VaultLib.VaultStatusCode.FailInvalidSessionToken)
				{
					ServerOperations.Logout();
				}
				throw e;
			}
		}

		/// <summary>
		/// Undo any checkouts for a given Vault object.
		/// </summary>
		/// <param name="objectPaths">An array of paths to the objects whose pending changes will be undone.  This can be either a repository path or a local path.  If they are repository paths, wildcards can be included see <see cref="RepositoryUtil.MatchWildcardToTreeObjects"/></param>
		/// <param name="recursive">Recusively undo the checkouts for subfolders.</param>
		/// <param name="localCopy">Controls what is done with any modified local files that are present in the working folder.  Possible values are:  &quot;Replace&quot;, &quot;Delete&quot;, and &quot;Leave&quot;.</param>
		[LocalOrRemotePath("objectPaths"), RecommendedOptionDefault("recursive", "false"), RecommendedOptionDefault("localCopy", "\"Replace\"")]
		public static void ProcessCommandUndoCheckout(string[] objectPaths, bool recursive, LocalCopyType localCopy)
		{
			try
			{
				client.ClientInstance.Refresh();
				ArrayList objects = new ArrayList();
				foreach (string objectPath in objectPaths)
				{
					objects.AddRange(RepositoryUtil.MatchWildcardToTreeObjects(objectPath));
				}
				if (objects.Count == 0)
					throw new Exception("No objects were found in the paths that you supplied.");
				VaultClientTreeObject[] treeobjects = (VaultClientTreeObject[])objects.ToArray(typeof(VaultClientTreeObject));
				client.ClientInstance.UndoCheckOut(treeobjects, recursive, localCopy);
			}
			catch (Exception e)
			{
				int errorcode = VaultConnection.GetSoapExceptionStatusCodeInt(e);
				if (errorcode == VaultLib.VaultStatusCode.FailInvalidSessionToken)
				{
					ServerOperations.Logout();
				}
				throw e;
			}
		}

		/// <summary>
		/// Unpin a Vault object.
		/// </summary>
		/// <param name="objectPath">The path to the file or folder that will be unpinned.  This can be either a repository or a local path.</param>
		[LocalOrRemotePath("objectPath")]
		public static ChangeSetItemColl ProcessCommandUnPin(string objectPath)
		{
			try
			{
				ChangeSetItemColl csic = new ChangeSetItemColl();

				VaultClientTreeObject treeobj = RepositoryUtil.FindVaultTreeObjectAtReposOrLocalPath(objectPath);
				if (treeobj.PinFromObjVerID <= 0)
					return null;  //The object is not pinned.
				ChangeSetItem_Unpin csi = new ChangeSetItem_Unpin(
					VaultDateTime.Now,
					client.Comment,
					String.Empty,
					treeobj.ID,
					treeobj.FullPath);
				csic.Add(csi);

				return commitTransaction(csic);
			}
			catch (Exception e)
			{
				int errorcode = VaultConnection.GetSoapExceptionStatusCodeInt(e);
				if (errorcode == VaultLib.VaultStatusCode.FailInvalidSessionToken)
				{
					ServerOperations.Logout();
				}
				throw e;
			}
		}


		/// <summary>
		/// Lists the transactions that have been applied to a folder.
		/// </summary>
		/// <param name="folderPath">The path to the folder to query for.  This can be either a repository or a local path.</param>
		/// <param name="versionHistoryBeginVersion">The version number to start with.  Pass 0 to retrieve all versions.</param>
		/// <param name="beginDate">The earliest date/time that will be returned in the results.  Pass VaultDate.EmptyDate() to return all versions.</param>
		/// <param name="endDate">The latest date/time that will be returned in the results.  Pass VaultDate.EmptyDate() to return all versions.</param>
		/// <param name="rowLimit">The maximum number of history items will be returned.</param>
		/// <param name="commentFilter">The substring to search for in comments.  If this is null or empty, no comment filtering will be performed.</param>
		/// <returns>An array of VaultTxHistoryItems describing all of the versions that have been applied to the object in the given time frame.</returns>
		[LocalOrRemotePath("folderPath"), Hidden, RecommendedOptionDefault("commentFilter", null)]
		public static VaultTxHistoryItem[] ProcessCommandVersionHistory(string folderPath, long versionHistoryBeginVersion, VaultDateTime beginDate, VaultDateTime endDate, int rowLimit, string commentFilter)
		{
			try
			{
				VaultClientFolder vcfolder = RepositoryUtil.FindVaultFolderAtReposOrLocalPath(folderPath);

				int rowsRetrieved = 0;
				string strQryToken = null;
				VaultTxHistoryItem[] histitems = new VaultTxHistoryItem[0];

				VaultHistoryQueryRequest hqr = new VaultHistoryQueryRequest();
				hqr.TopID = vcfolder.ID;
				if (commentFilter != null && commentFilter != "")
				{
					hqr.CommentFilter = VaultQueryRequestComments.FilteredComment;
					hqr.CommentSubstring = commentFilter;
				}
				hqr.BeginDate = beginDate;
				hqr.EndDate = endDate;

				// when one of the dates is empty, 
				// the meaning of the variable is switched
				// because you are looking for dates AFTER the endDate
				// or BEFORE the beginDate
				if (VaultDate.IsEmptyDate(beginDate) || VaultDate.IsEmptyDate(endDate))
				{
					hqr.BeginDate = endDate;
					hqr.EndDate = beginDate;
				}

				try
				{
					ClientInstance ci = client.ClientInstance;

					ci.Connection.VersionHistoryBegin(rowLimit, ci.ActiveRepositoryID, versionHistoryBeginVersion, hqr, ref rowsRetrieved, ref strQryToken);
					if (rowsRetrieved > 0)
					{
						ci.Connection.VersionHistoryFetch(strQryToken, 0, rowsRetrieved - 1, ref histitems);
					}
					ci.Connection.VersionHistoryEnd(strQryToken);
				}
				catch (Exception e)
				{
					string strMsg = null;
					int nStatCode = VaultClientNetLib.VaultConnection.GetSoapExceptionStatusCodeInt(e);

					if (nStatCode != -1)
					{
						switch (nStatCode)
						{
							case VaultStatusCode.FailInvalidRange:
								strMsg = "Version History failed due to an invalid date range.  Please check the range of dates is in the correct order and retry the Version History command.";
								break;
							default:
								// Get the soap exception message based on the status code.
								strMsg = VaultConnection.GetSoapExceptionMessage(nStatCode);
								break;
						}
						throw new Exception(strMsg);
					}
					else
					{
						throw;
					}
				}

				return histitems;
			}
			catch (Exception e)
			{
				int errorcode = VaultConnection.GetSoapExceptionStatusCodeInt(e);
				if (errorcode == VaultLib.VaultStatusCode.FailInvalidSessionToken)
				{
					ServerOperations.Logout();
				}
				throw e;
			}
		}
		/// <summary>
		/// Lists the transactions that have been applied to a folder.
		/// </summary>
		/// <param name="folderPath">The path to the folder to query for.  This can be either a repository or a local path.</param>
		/// <param name="versionHistoryBeginVersion">The version number to start with.  Pass 0 to retrieve all versions.</param>
		/// <param name="beginDate">The earliest date/time that will be returned in the results.  Pass VaultDate.EmptyDate() to return all versions.</param>
		/// <param name="endDate">The latest date/time that will be returned in the results.  Pass VaultDate.EmptyDate() to return all versions.</param>
		/// <param name="rowLimit">The maximum number of history items will be returned.</param>
		/// <returns>An array of VaultTxHistoryItems describing all of the versions that have been applied to the object in the given time frame.</returns>
		[LocalOrRemotePath("folderPath"), Hidden]
		public static VaultTxHistoryItem[] ProcessCommandVersionHistory(string folderPath, long versionHistoryBeginVersion, VaultDateTime beginDate, VaultDateTime endDate, int rowLimit)
		{
			return ProcessCommandVersionHistory(folderPath, versionHistoryBeginVersion, beginDate, endDate, rowLimit, null);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="repID"></param>
		/// <param name="fullPath"></param>
		/// <param name="objID"></param>
		/// <param name="version"></param>
		/// <param name="bWithCurrentNames"></param>
		/// <returns></returns>
		[Hidden]
		public static VaultFolderDelta ProcessCommandGetBranchStructure(int repID, string fullPath, long objID, long version, bool bWithCurrentNames)
		{
			try
			{
				VaultFolderDelta vfd = null;
				client.ClientInstance.Connection.GetBranchStructure(repID, fullPath, objID, version, ref vfd, bWithCurrentNames);
				return vfd;
			}
			catch (Exception e)
			{
				int errorcode = VaultConnection.GetSoapExceptionStatusCodeInt(e);
				if (errorcode == VaultLib.VaultStatusCode.FailInvalidSessionToken)
				{
					ServerOperations.Logout();
				}
				throw e;
			}
		}

		/// <summary>
		/// Gets the transaction information for a specific vault transaction.
		/// </summary>
		/// <param name="nTxID">The transaction id</param>
		/// <returns>A TxInfo object containing the transaction information.</returns>
		public static TxInfo ProcessCommandTxDetail(long nTxID)
		{
			try
			{
				int userid = 0;
				string userlogin = string.Empty;
				VaultTxDetailHistoryItem[] items = null;
				string comment = null;

				client.ClientInstance.Connection.GetTxDetail(client.ClientInstance.ActiveRepositoryID, nTxID, out userid, out userlogin, out comment, out items);
				return new TxInfo(userid, userlogin, comment, items);
			}
			catch (Exception e)
			{
				int errorcode = VaultConnection.GetSoapExceptionStatusCodeInt(e);
				if (errorcode == VaultLib.VaultStatusCode.FailInvalidSessionToken)
				{
					ServerOperations.Logout();
				}
				throw e;
			}

		}

		/// <summary>
		/// Updates the comment of a vault transaction.
		/// </summary>
		/// <param name="nTxID">the transaction id</param>
		/// <param name="nObjverID">the obj version id</param>
		/// <param name="newComment">the new comment</param>
		public static void ProcessCommandUpdateTxComment(long nTxID, long nObjverID, string newComment)
		{
			try
			{
				client.ClientInstance.Connection.UpdateTxComment(client.ClientInstance.ActiveRepositoryID, nTxID, nObjverID, newComment);
			}
			catch (Exception e)
			{
				int errorcode = VaultConnection.GetSoapExceptionStatusCodeInt(e);
				if (errorcode == VaultLib.VaultStatusCode.FailInvalidSessionToken)
				{
					ServerOperations.Logout();
				}
				throw e;
			}
		}

		/// <summary>
		/// Commits given changes only if AutoCommit is enabled.
		/// throws Exception if commit fails.
		/// </summary>
		/// <param name="csic">Collection of changes to commit.</param>
		/// <returns>null if changes were autocommited, else the original csic.</returns>
		private static ChangeSetItemColl commitTransaction(ChangeSetItemColl csic)
		{
			bool bSuccess = true;

			ClientConnection cc = client;
			ClientInstance ci = cc.ClientInstance;

			ci.InternalChangeSet_Append(csic);

			if (cc.AutoCommit)
			{
				ci.InternalChangeSet_SetComment(cc.Comment);
				WriteChangeSet(csic);
				//bSuccess = ci.Commit(csic);
				long nRevID = 0;
				bSuccess = ci.Commit(csic, false, false, ref nRevID);
				if (bSuccess == false)
				{
					throw new Exception("Commit failed.");
				}
				else if (cc.bugIDs != null && cc.bugIDs.Length > 0)
				{
					UpdateBugURLCommand ucmd = new UpdateBugURLCommand(csic, cc.bugIDs, cc.markBugFixed, cc.addBugComment, cc.Comment, nRevID);

					ci.UpdateBugTracking(ucmd);
				}
				return null;
			}
			else
			{
				WriteChangeSet();
				return csic;
			}
		}

		/// <summary>
		/// Add a new repository.  You must be logged on as a user with admin rights to create a new repository.
		/// </summary>
		/// <param name="newReposName">The name of the repository that will be created.</param>
		/// <param name="enableSecurity">If true, enable folder security for the new repository.</param>
		[DoesNotRequireRepository, RecommendedOptionDefault("enableSecurity", "true")]
		public static void ProcessCommandAddRepository(string newReposName, bool enableSecurity)
		{
			Login(VaultConnection.AccessLevelType.Admin, true, false);

			int id = 0;
			try
			{
				id = GetRepositoryId(newReposName);
			}
			catch (Exception)
			{
				client.ClientInstance.Connection.AddRepository(newReposName, enableSecurity);

				WriteUserMessage(string.Format("Added repository: {0}", newReposName));
			}
			WriteUserMessage(newReposName + "already exists");
		}

		/// <summary>
		/// Delete a repository.  You must be logged on as a user with admin rights to delete a repository.
		/// </summary>
		/// <param name="repositoryName">The name of the repository that will be deleted.</param>
		[DoesNotRequireRepository]
		public static void ProcessCommandDeleteRepository(string repositoryName)
		{
			Login(VaultConnection.AccessLevelType.Admin, true, false);

			int id = 0;
			try
			{
				id = GetRepositoryId(repositoryName);
			}
			catch (Exception)
			{
				WriteUserMessage(repositoryName + "does not exist");
				return;
			}
			client.ClientInstance.Connection.DeleteRepository(id);

			WriteUserMessage(string.Format("Deleted repository: {0}", repositoryName));
		}

		/// <summary>
		/// Add a new user to Vault.  You must be logged on as a user with admin rights to add a user.
		/// </summary>
		/// <param name="login">The login name for the user.</param>
		/// <param name="password">The initial password for the user.</param>
		/// <param name="email">The email address for the user.</param>
		[DoesNotRequireRepository]
		public static void ProcessCommandAddUser(string login, string password, string email)
		{
			ArrayList groupList = new ArrayList();

			Login(VaultConnection.AccessLevelType.Admin, true, false);

			VaultUser newUser = new VaultUser();
			newUser.Login = login;
			newUser.Password = VaultLib.VaultUserCrypt.HashPassword(login, password);
			newUser.Name = login;
			newUser.Email = email;
			newUser.isActive = true;
			newUser.DefaultRights = 7;
			newUser.BelongToGroups = (VaultGroup[])groupList.ToArray(typeof(VaultGroup));

			try
			{
				client.ClientInstance.Connection.AddUser(ref newUser);
			}
			catch (Exception e)
			{
				bool bRet = true;
				string strMsg = null;
				if (newUser.UserID == 0)
				{
					strMsg = string.Format("{0} was not created - {1}", login, e.Message);
					bRet = false;
				}
				else
				{
					strMsg = string.Format("{0} was created.  But, there was a small problem - {1}", login, e.Message);
				}
				WriteUserMessage(strMsg);
				if (bRet == false)
					throw new Exception(strMsg);
			}
		}
		/// <summary>
		/// Resolve Merge Status on a path.  Path can be either local or repository.  
		/// For repository paths, wilcards are accepted.
		/// </summary>
		/// <param name="path"></param>
		[Hidden]
		public static void ProcessCommandResolveMerge(string path)
		{
			try
			{
				VaultClientTreeObject[] objs = null;
				objs = RepositoryUtil.MatchWildcardToTreeObjects(path);
				//vcf = RepositoryUtil.FindVaultFileAtReposOrLocalPath(path);
				foreach (VaultClientTreeObject o in objs)
				{
					if (o is VaultClientFile)
						ProcessCommandResolveMerge((VaultClientFile)o);
				}
			}
			catch (Exception e)
			{
				int errorcode = VaultConnection.GetSoapExceptionStatusCodeInt(e);
				if (errorcode == VaultLib.VaultStatusCode.FailInvalidSessionToken)
				{
					ServerOperations.Logout();
				}
				throw e;
			}
		}

		[Hidden]
		public static void ProcessCommandResolveMerge(VaultClientFile vcf)
		{
			try
			{
				if (vcf != null)
				{
					try
					{
						WorkingFolder wf = ServerOperations.client.ClientInstance.GetWorkingFolder(vcf);
						if ((wf != null) && (wf.GetStatus(vcf) == WorkingFolderFileStatus.NeedsMerge))
						{
							if (wf.CanResolveMergeStatus(vcf.ID, vcf.ObjVerID))
								wf.ResolveMergeStatus(vcf);
							else
							{
								wf.RetrieveLatestVersionForDiffOrMerge(vcf);
								wf.ResolveMergeStatus(vcf);
							}
						}
					}
					catch (Exception e)
					{
						throw new Exception(String.Format("Resolve Merge Status failed.  File:  {0}  Exception:  {1}", vcf.FullPath, e.Message));
					}
				}
			}
			catch (Exception e)
			{
				int errorcode = VaultConnection.GetSoapExceptionStatusCodeInt(e);
				if (errorcode == VaultLib.VaultStatusCode.FailInvalidSessionToken)
				{
					ServerOperations.Logout();
				}
				throw e;
			}
		}

		/// <summary>
		/// List all the users.
		/// note:  this method uses the adminService, use ProcessCommandGetUsers if the logged in user may not be admin
		/// </summary>
		/// <returns>An array of VaultUser objects</returns>
		[Hidden]
		public static VaultUser[] ProcessCommandListUsers()
		{
			try
			{
				VaultUser[] users = new VaultUser[0];
				client.ClientInstance.Connection.ListUsers(ref users);
				return users;
			}
			catch (Exception e)
			{
				int errorcode = VaultConnection.GetSoapExceptionStatusCodeInt(e);
				if (errorcode == VaultLib.VaultStatusCode.FailInvalidSessionToken)
				{
					ServerOperations.Logout();
				}
				throw e;
			}
		}

		/// <summary>
		/// List all the users.
		/// </summary>
		/// <returns>An array of VaultUser objects</returns>
		[Hidden]
		public static VaultUser[] ProcessCommandGetUsers()
		{
			try
			{
				VaultUser[] users = new VaultUser[0];
				client.ClientInstance.Connection.GetUserList(ref users);
				return users;
			}
			catch (Exception e)
			{
				int errorcode = VaultConnection.GetSoapExceptionStatusCodeInt(e);
				if (errorcode == VaultLib.VaultStatusCode.FailInvalidSessionToken)
				{
					ServerOperations.Logout();
				}
				throw e;
			}
		}

		/// <summary>
		/// Shelve a set of changes.
		/// </summary>
		/// <param name="name">Shelveset name</param>
		/// <param name="csic">ChangeSetItemColl of changes to be shelved</param>
		/// <param name="comment">comment for the shelveset</param>
		/// <param name="bugIDs">an int[] of bugIDs to associate with the shelveset</param>
		/// <param name="ideInfos">an array of ShelvesetItemIDEInfo objects describing all the open ide editors (not just items passed to shelve, ClientInstance will filter list)</param>
		/// <param name="undoChanges">true to undo changes in the working folder, false to leave them</param>
		/// <param name="replace">true to automatically replace if a shelveset with the given name already exists in the database</param>
		/// <returns>-1 for duplicate name (if replace == false), 0 for other failure, 1 for success</returns>
		[Hidden]
		public static int ProcessCommandShelve(string name, ChangeSetItemColl csic, string comment, int[] bugIDs, ShelvesetItemIDEInfo[] ideInfos, bool undoChanges, bool replace)
		{
			try
			{
				bool success = client.ClientInstance.Shelve(name, csic, comment, bugIDs, ideInfos, undoChanges, replace);
				if (success && undoChanges)
				{
					client.ClientInstance.UndoChangeSetItems(csic, (LocalCopyType)client.ClientInstance.UserOptions.GetInt(VaultOptions.UndoLocalCopy));
				}

				if (success)
					return 1;
				else
					return 0;
			}
			catch (SoapException ex)
			{
				int errorcode = VaultConnection.GetSoapExceptionStatusCodeInt(ex);
				if (errorcode == VaultStatusCode.FailShelveSetExists)
					return -1;
				else if (errorcode == VaultStatusCode.FailInvalidSessionToken)
				{
					ServerOperations.Logout();
					throw ex;
				}
				else
					throw ex;
			}
		}

		/// <summary>
		/// Get an array of ShelvesetSummary objects for the given user and repository.
		/// </summary>
		/// <param name="userid"></param>
		/// <param name="repid"></param>
		/// <returns>an array of ShelvesetSummary objects for the given user and repository</returns>
		[Hidden]
		public static ShelvesetSummary[] ProcessCommandGetShelvesetList(int userid, int repid)
		{
			try
			{
				ShelvesetSummary[] summaries = new ShelvesetSummary[0];
				client.ClientInstance.Connection.GetShelvesetList(userid, repid, ref summaries);
				return summaries;
			}
			catch (Exception e)
			{
				int errorcode = VaultConnection.GetSoapExceptionStatusCodeInt(e);
				if (errorcode == VaultLib.VaultStatusCode.FailInvalidSessionToken)
				{
					ServerOperations.Logout();
				}
				throw e;
			}
		}

		/// <summary>
		/// Get the ShelvesetDetails object given a user id, repository id, and the Shelveset name.
		/// </summary>
		/// <param name="userid">id for the creator of the Shelveset</param>
		/// <param name="repid">id for the repository the Shelveset was created on</param>
		/// <param name="ssName">name of the Shelveset</param>
		/// <returns>a ShelvesetDetails object</returns>
		[Hidden]
		public static ShelvesetDetails ProcessCommandGetShelvesetDetails(int userid, int repid, string ssName)
		{
			try
			{
				ShelvesetDetails details = new ShelvesetDetails();
				client.ClientInstance.Connection.GetShelvesetDetails(userid, repid, ssName, ref details);
				return details;
			}
			catch (Exception e)
			{
				int errorcode = VaultConnection.GetSoapExceptionStatusCodeInt(e);
				if (errorcode == VaultLib.VaultStatusCode.FailInvalidSessionToken)
				{
					ServerOperations.Logout();
				}
				throw e;
			}
		}

		/// <summary>
		/// Rename a Shelveset.
		/// </summary>
		/// <param name="userid">userid of the Shelveset creator</param>
		/// <param name="repid">repid for the repository the Shelveset was created in</param>
		/// <param name="ssName">current Shelveset name</param>
		/// <param name="ssNewName">new Shelveset name</param>
		[Hidden]
		public static ShelvesetSummary ProcessCommandRenameShelveset(int userid, int repid, string ssName, string ssNewName)
		{
			try
			{
				ShelvesetSummary ss = null;
				client.ClientInstance.Connection.RenameShelveset(userid, repid, ssName, ssNewName, ref ss);
				return ss;
			}
			catch (Exception e)
			{
				int errorcode = VaultConnection.GetSoapExceptionStatusCodeInt(e);
				if (errorcode == VaultLib.VaultStatusCode.FailInvalidSessionToken)
				{
					ServerOperations.Logout();
				}
				throw e;
			}
		}

		/// <summary>
		/// Delete a Shelveset.
		/// </summary>
		/// <param name="userid">userid of the Shelveset creator</param>
		/// <param name="repid">repid for the repository the Shelveset was created in</param>
		/// <param name="ssName">Shelveset name</param>
		public static void ProcessCommandDeleteShelveset(int userid, int repid, string ssName)
		{
			try
			{
				client.ClientInstance.Connection.DeleteShelveset(userid, repid, ssName);
			}
			catch (Exception e)
			{
				int errorcode = VaultConnection.GetSoapExceptionStatusCodeInt(e);
				if (errorcode == VaultLib.VaultStatusCode.FailInvalidSessionToken)
				{
					ServerOperations.Logout();
				}
				throw e;
			}
		}

		/// <summary>
		/// Prepare 2 files to diff, based on a selected ShelvesetItem and a DiffShelvedAgainstType.
		/// </summary>
		/// <param name="diffType">DiffShelvedAgainstType, describing the type of diff to perform</param>
		/// <param name="si">The ShelvesetItem the diff was initiated from.</param>
		/// <param name="vcf">The VaultClientFile the ShelvesetItem refers to.</param>
		/// <returns>A ShelveDiffInfo object containing the paths to the diff items and an int describing errors, if any.</returns>
		[Hidden]
		public static ShelveDiffInfo ProcessCommandPrepareShelveFilesForDiff(DiffShelvedAgainstType diffType, ShelvesetItem si, VaultClientFile vcf)
		{
			try
			{
				String left = "", right = "";
				int diffE = client.ClientInstance.PrepareFilesForShelveDiff(diffType, si, vcf, ref left, ref right);
				ShelveDiffInfo sdi = new ShelveDiffInfo(left, right, diffE);
				return sdi;
			}
			catch (Exception e)
			{
				int errorcode = VaultConnection.GetSoapExceptionStatusCodeInt(e);
				if (errorcode == VaultLib.VaultStatusCode.FailInvalidSessionToken)
				{
					ServerOperations.Logout();
				}
				throw;
			}
		}

		[Hidden]
		public static FindInFilesData[] ProcessCommandFindInFilesByFileList(string strSearchString, string[] arFiles, bool bMatchCase, bool bMatchWord, VaultFindInFilesDefine.PatternMatch pm)
		{
			FindInFilesData[] arFifData = null;

			// verify the file paths
			if ((arFiles != null) && (arFiles.Length > 0))
			{
				ClientInstance ci = client.ClientInstance;

				foreach (string strFilePath in arFiles)
				{
					VaultClientTreeObject to = ci.TreeCache.Repository.Root.FindTreeObjectRecursive(strFilePath);
					if (to == null)
						throw new Exception(string.Format("No object was found at the repository path: {0}", strFilePath));
					else if (to is VaultClientFolder)
						throw new Exception(string.Format("The object at the repository path {0} is not a valid file.", strFilePath));
				}

				try
				{
					FindInFilesByFileListQuery q = new FindInFilesByFileListQuery(ci.ActiveRepositoryID, strSearchString);
					q.Files = arFiles;
					q.MatchCase = bMatchCase;
					q.MatchWord = bMatchWord;
					q.PatternMatch = (int)pm;

					if (ci.Connection != null)
					{
						ci.Connection.FindInFilesByFileList(q, true, out arFifData);
					}
				}
				catch (Exception e)
				{
					int nErrCode = VaultConnection.GetSoapExceptionStatusCodeInt(e);
					if (nErrCode == VaultLib.VaultStatusCode.FailInvalidSessionToken)
					{
						ServerOperations.Logout();
					}

					// nice message handling
					throw new Exception(string.Format("[{0}] : {1}", nErrCode, VaultConnection.GetSoapExceptionMessage(e)));
				}
			}

			if (arFifData == null) { arFifData = new FindInFilesData[0]; }

			return arFifData;
		}

		/// <summary>
		/// Method will run a find in files using a folder path
		/// </summary>
		/// <param name="strSearchString"></param>
		/// <param name="strFolderPath"></param>
		/// <param name="bRecursive"></param>
		/// <param name="arIncludedFiles"></param>
		/// <param name="arExcludedFiles"></param>
		/// <param name="bMatchCase"></param>
		/// <param name="bMatchWord"></param>
		/// <param name="pm"></param>
		/// <returns></returns>
		[Hidden]
		public static FindInFilesData[] ProcessCommandFindInFilesByFolder(string strSearchString, string strFolderPath, bool bRecursive, string[] arIncludedFiles, string[] arExcludedFiles, bool bMatchCase, bool bMatchWord, VaultFindInFilesDefine.PatternMatch pm)
		{
			FindInFilesData[] arFifData = null;

			if (string.IsNullOrEmpty(strFolderPath) == false)
			{
				ClientInstance ci = client.ClientInstance;

				VaultClientTreeObject to = ci.TreeCache.Repository.Root.FindTreeObjectRecursive(strFolderPath);
				if (to == null)
					throw new Exception("No object was found at the repository path: " + strFolderPath);
				else if (to is VaultClientFile)
					throw new Exception(string.Format("The object at the repository path {0} is not a valid folder.", strFolderPath));

				try
				{
					FindInFilesByFolderQuery q = new FindInFilesByFolderQuery(ci.ActiveRepositoryID, strSearchString, strFolderPath, bRecursive);
					q.IncludeFiles = arIncludedFiles;
					q.ExcludeFiles = arExcludedFiles;
					q.MatchCase = bMatchCase;
					q.MatchWord = bMatchWord;
					q.PatternMatch = (int)pm;

					if (ci.Connection != null)
					{
						ci.Connection.FindInFilesByFolder(q, true, out arFifData);
					}
				}
				catch (Exception e)
				{
					int nErrCode = VaultConnection.GetSoapExceptionStatusCodeInt(e);
					if (nErrCode == VaultLib.VaultStatusCode.FailInvalidSessionToken)
					{
						ServerOperations.Logout();
					}

					// nice message handling
					throw new Exception(string.Format("[{0}] : {1}", nErrCode, VaultConnection.GetSoapExceptionMessage(e)));
				}
			}

			if (arFifData == null) { arFifData = new FindInFilesData[0]; }
			return arFifData;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="shelveset"></param>
		/// <param name="path"></param>
		/// <returns></returns>
		[Hidden]
		public static string[] BuildTreeForShelvesetDiff(ShelvesetDetails shelveset, string path)
		{
			try
			{
				string[] errors = new string[0];
				client.ClientInstance.BuildTreeForDiff_Shelveset(shelveset, path, ref errors);
				return errors;
			}
			catch (Exception e)
			{
				int errorcode = VaultConnection.GetSoapExceptionStatusCodeInt(e);
				if (errorcode == VaultLib.VaultStatusCode.FailInvalidSessionToken)
				{
					ServerOperations.Logout();
				}
				throw e;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="shelveset"></param>
		/// <param name="path"></param>
		/// <returns></returns>
		[Hidden]
		public static string[] BuildTreeForShelvesetDiffBaseline(ShelvesetDetails shelveset, string path)
		{
			try
			{
				string[] errors = new string[0];
				client.ClientInstance.BuildTreeForDiff_ShelvesetBaselines(shelveset, path, ref errors);
				return errors;
			}
			catch (Exception e)
			{
				int errorcode = VaultConnection.GetSoapExceptionStatusCodeInt(e);
				if (errorcode == VaultLib.VaultStatusCode.FailInvalidSessionToken)
				{
					ServerOperations.Logout();
				}
				throw e;
			}
		}

		/// <summary>
		/// This event is fired every time that a string should be output to the user.
		/// </summary>
		public event UserMessageEventHandler UserMessage;
		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="message"></param>
		public delegate void UserMessageEventHandler(object sender, string message);
		private static void WriteUserMessage(string message)
		{
			ServerOperations so = GetInstance();
			if (so.UserMessage != null) { so.UserMessage(so, message); }

			if ((so.ClientConn != null) && (so.ClientConn.ClientInstance != null) && (so.ClientConn.ClientInstance.EventEngine != null))
				so.ClientConn.ClientInstance.EventEngine.fireEvent(new UserMessageEvent(message));
		}

		/// <summary>
		/// 
		/// </summary>
		public event ChangesetOutputEventHandler ChangesetOutput;
		/// <summary>
		/// 
		/// </summary>
		/// <param name="changeset"></param>
		public delegate void ChangesetOutputEventHandler(ChangeSetItemColl changeset);
		private static void WriteChangeSet()
		{
			//Only go through the pain of scanning everything if someone is listening, which is unlikely.
			if (ServerOperations.GetInstance().ChangesetOutput != null)
			{
				ClientInstance ci = client.ClientInstance;

				if (ci.WorkingFolderOptions.RequireCheckOutBeforeCheckIn == false)
				{
					// do a scan to update the change set list
					ci.UpdateKnownChanges_All(false);
				}
				else
				{
					ci.UpdateKnownChanges_RefreshKnown(false);
				}
				ChangeSetItemColl csic = ci.InternalChangeSet_GetItems(true);
				WriteChangeSet(csic);
			}
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="changeset"></param>
		private static void WriteChangeSet(ChangeSetItemColl changeset)
		{
			if (ServerOperations.GetInstance().ChangesetOutput != null)
				ServerOperations.GetInstance().ChangesetOutput(changeset);
		}

		/// <summary>
		/// Return information about the working folder assignments for the currently logged in user.
		/// </summary>
		/// <returns>A sorted list whose keys will be the repository paths and values will be the corresponding disk path mapping.</returns>
		public static SortedList GetWorkingFolderAssignments()
		{
			try
			{
				SortedList hash = new SortedList();
				string[] fullPaths = null, diskPaths = null;
				client.ClientInstance.TreeCache.GetWorkingFolderAssignments(ref fullPaths, ref diskPaths);
				for (int i = 0; i < fullPaths.Length; i++)
				{
					hash[fullPaths[i]] = diskPaths[i];
				}
				return hash;
			}
			catch (Exception e)
			{
				int errorcode = VaultConnection.GetSoapExceptionStatusCodeInt(e);
				if (errorcode == VaultLib.VaultStatusCode.FailInvalidSessionToken)
				{
					ServerOperations.Logout();
				}
				throw e;
			}
		}



		/// <summary>
		/// Delete a working folder assignment.
		/// </summary>
		/// <param name="repositoryFolderPath">The path to the repository folder whose working folder association will be deleted.</param>
		/// <param name="recursive">True to recursively remove working folder settings from subfolders.</param>
		[RecommendedOptionDefault("recursive", "false")]
		public static void RemoveWorkingFolder(string repositoryFolderPath, bool recursive)
		{
			try
			{
				RepositoryUtil.ValidateReposPath(repositoryFolderPath);
				client.ClientInstance.TreeCache.RemoveWorkingFolder(repositoryFolderPath, recursive);
			}
			catch (Exception e)
			{
				int errorcode = VaultConnection.GetSoapExceptionStatusCodeInt(e);
				if (errorcode == VaultLib.VaultStatusCode.FailInvalidSessionToken)
				{
					ServerOperations.Logout();
				}
				throw e;
			}
		}
		/// <summary>
		/// Delete a working folder assignment.
		/// </summary>
		/// <param name="repositoryFolderPath">The path to the repository folder whose working folder association will be deleted.</param>
		[Hidden]
		public static void RemoveWorkingFolder(string repositoryFolderPath)
		{
			try
			{
				RepositoryUtil.ValidateReposPath(repositoryFolderPath);
				client.ClientInstance.TreeCache.RemoveWorkingFolder(repositoryFolderPath);
			}
			catch (Exception e)
			{
				int errorcode = VaultConnection.GetSoapExceptionStatusCodeInt(e);
				if (errorcode == VaultLib.VaultStatusCode.FailInvalidSessionToken)
				{
					ServerOperations.Logout();
				}
				throw e;
			}

		}
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		[Hidden]
		public static bool isConnected()
		{
			ClientInstance ci = client.ClientInstance;
			return ((ci != null) && (ci.ConnectionStateType == ConnectionStateType.Connected));
		}
		/// <summary>
		/// Set the login options that will be used when connecting to Vault.  This command does not trigger a login.
		/// </summary>
		/// <param name="URL">The URL to the Vault server.  For example &quot;http://localhost/VaultService&quot;</param>
		/// <param name="user">The username to use to log in to Vault.</param>
		/// <param name="password">The password to use for authentication.</param>
		/// <param name="repository">The name of the repository to connect to.  If this is null, then no repository will be connected to.</param>
		/// <param name="saveSession">If saveSession is true, then the information will be stored on disk and used to automatically connect in the future (until the PurgeSession command is invoked).</param>
		[RecommendedOptionDefault("repository", "null"), RecommendedOptionDefault("saveSession", "false"), DoesNotRequireLogin, DoesNotRequireRepository]
		public static void SetLoginOptions(string URL, string user, string password, string repository, bool saveSession)
		{
			ClientConnection cc = client;
			cc.LoginOptions.URL = URL;

			if (cc.LoginOptions.URL.IndexOf("VaultService") < 0)
			{
				if (!cc.LoginOptions.URL.EndsWith("/"))
					cc.LoginOptions.URL = cc.LoginOptions.URL + "/";

				cc.LoginOptions.URL = cc.LoginOptions.URL + "VaultService";
			}

			cc.LoginOptions.User = user;
			cc.LoginOptions.Password = password;
			cc.LoginOptions.Repository = repository;
			if (saveSession)
			{
				StoreSession(cc.LoginOptions.URL, cc.LoginOptions.User, cc.LoginOptions.Password, cc.LoginOptions.Repository);
			}
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="altCommand"></param>
		/// <param name="bAllowAuto"></param>
		/// <param name="bSaveSession"></param>
		[Hidden]
		public static void Login(VaultConnection.AccessLevelType altCommand, bool bAllowAuto, bool bSaveSession)
		{
			bool bResult = false;

			// get the static client connect.
			ClientConnection cc = client;
			// and the connections current client instance.
			ClientInstance ci = cc.ClientInstance;
			if (ci != null)
			{
				if ((ci.ConnectionStateType == ConnectionStateType.Connected) &&
					(ci.AccessLevel == altCommand))
				{
					// already logged in
					if (bSaveSession)
					{
						StoreSession(cc.LoginOptions.URL, cc.LoginOptions.User, cc.LoginOptions.Password, cc.LoginOptions.Repository);
					}
					return;
				}

				// clear out the client instance.
				Logout();
				ci = null;
			}

			// init a new client instance (not using file system watchers)
			ci = cc.CreateClientInstance(false);
			ci.Init(altCommand);

			if (
				(bAllowAuto == true) &&
				((cc.LoginOptions.URL == null) || (cc.LoginOptions.User == null))
				)
			{
				string tmpURL = null, tmpUser = null, tmpPassword = null, tmpRepository = null;
				RetrieveSession(ref tmpURL, ref tmpUser, ref tmpPassword, ref tmpRepository);

				// Override from cmd line 
				cc.LoginOptions.URL = cc.LoginOptions.URL != null ? cc.LoginOptions.URL : tmpURL;
				cc.LoginOptions.User = cc.LoginOptions.User != null ? cc.LoginOptions.User : tmpUser;
				cc.LoginOptions.Password = cc.LoginOptions.Password != null ? cc.LoginOptions.Password : tmpPassword;
				cc.LoginOptions.Repository = cc.LoginOptions.Repository != null && cc.LoginOptions.Repository.Length > 0 ? cc.LoginOptions.Repository : tmpRepository;
			}

			if ((cc.LoginOptions.URL != null) && (cc.LoginOptions.User != null))
			{
				if (cc.LoginOptions.URL.IndexOf("VaultService") < 0)
				{
					if (!cc.LoginOptions.URL.EndsWith("/"))
						cc.LoginOptions.URL = cc.LoginOptions.URL + "/";

					cc.LoginOptions.URL = cc.LoginOptions.URL + "VaultService";
				}

				if (cc.LoginOptions.Password == null)
				{
					cc.LoginOptions.Password = string.Empty;
				}

				try
				{
					if (cc.LoginOptions.ProxyServer != null && cc.LoginOptions.ProxyServer != string.Empty)
						ci.Connection.ResetProxy(1, cc.LoginOptions.ProxyServer, cc.LoginOptions.ProxyPort);
					if (cc.LoginOptions.ProxyUser != null && cc.LoginOptions.ProxyUser != string.Empty)
					{
						if (cc.LoginOptions.ProxyDomain != null && cc.LoginOptions.ProxyDomain != string.Empty)
							ci.Connection.Proxy.Credentials = new System.Net.NetworkCredential(cc.LoginOptions.ProxyUser, cc.LoginOptions.ProxyPassword, cc.LoginOptions.ProxyDomain);
						else
							ci.Connection.Proxy.Credentials = new System.Net.NetworkCredential(cc.LoginOptions.ProxyUser, cc.LoginOptions.ProxyPassword);
					}

					ci.Login(cc.LoginOptions.URL, cc.LoginOptions.User, cc.LoginOptions.Password);
				}
				catch (Exception e)
				{
					string message = string.Empty;

					if (VaultClientNetLib.VaultConnection.GetSoapExceptionStatusCodeInt(e) != -1)
						message = SoapExceptions.GetSoapExceptionMessage(e);
					else
						message = string.Format("The connection to the server failed: server cannot be contacted or uses a protocol that is not supported by this client. {0}", e.Message);

					throw new Exception(message, e);
				}

				if (ci.ConnectionStateType == ConnectionStateType.Connected)
				{
					bResult = true;

					SetRepository(cc.LoginOptions.Repository);

					// Note: 9/9/04 Cautiously removing these overrides.  If the user doesn't specify
					// these options on the command line, then the clc program defaults end up overriding
					// the user options.  Explicitly specified input options should override user options from
					// the database, and CLC programatic options should not override anything.
					// set working folder options
					//ci.WorkingFolderOptions.RequireCheckOutBeforeCheckIn = client.args.RequireCheckOut;
					//ci.WorkingFolderOptions.DefaultLocalCopyType = cc.args.LocalCopy;

					if ((bAllowAuto == false) &&
						(bSaveSession))
					{
						StoreSession(cc.LoginOptions.URL, cc.LoginOptions.User, cc.LoginOptions.Password, cc.LoginOptions.Repository);
					} 
				}
				else
				{
					throw new Exception("Login failed.");
				}
			}
			else
			{
				throw new UsageException("Please specify -user, -password, and -host.");
			}

			// recheck the client instance for adding new listeners.
			ci = cc.ClientInstance;
			if ((ci != null) && (ci.EventEngine != null))
			{
				ci.EventEngine.addListener(cc, typeof(MessageEvent));
				ci.EventEngine.addListener(cc, typeof(BulkMessageEvent));
			}

			if (bResult == false)
			{
				throw new UsageException("Please verify that you have specified -user, -password, -host, and -repository.");
			}
		}

		/// <summary>
		/// Connect to the specified Vault repository.
		/// </summary>
		/// <param name="repositoryName">The name of the repository to connect to.</param>
		[DoesNotRequireRepository]
		public static void SetRepository(string repositoryName)
		{
			try
			{
				if (repositoryName != null && repositoryName.Length > 0)
				{
					VaultRepositoryInfo theRep = null;

					VaultRepositoryInfo[] reps = null;
					client.ClientInstance.ListRepositories(ref reps);

					foreach (VaultRepositoryInfo r in reps)
					{
						if (string.Compare(r.RepName, repositoryName, true) == 0)
						{
							theRep = r;
							break;
						}
					}

					if (theRep != null)
					{
						SetRepository(theRep);
					}
					else
					{
						throw new UsageException(string.Format("Repository {0} not found", client.LoginOptions.Repository));
					}
				}
			}
			catch (Exception e)
			{
				int errorcode = VaultConnection.GetSoapExceptionStatusCodeInt(e);
				if (errorcode == VaultLib.VaultStatusCode.FailInvalidSessionToken)
				{
					ServerOperations.Logout();
				}
				throw e;
			}
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="repositoryInfo"></param>
		[Hidden]
		public static void SetRepository(VaultRepositoryInfo repositoryInfo)
		{
			try
			{
				if (repositoryInfo != null)
				{
					ClientConnection cc = client;
					cc.ClientInstance.SetActiveRepositoryID(repositoryInfo.RepID, cc.LoginOptions.User, repositoryInfo.UniqueRepID, true, true);
				}
			}
			catch (Exception e)
			{
				int errorcode = VaultConnection.GetSoapExceptionStatusCodeInt(e);
				if (errorcode == VaultLib.VaultStatusCode.FailInvalidSessionToken)
				{
					ServerOperations.Logout();
				}
				throw e;
			}
		}
		/// <summary>
		/// 
		/// </summary>
		[Hidden]
		public static void Login()
		{
			Login(VaultConnection.AccessLevelType.Client, true, false);
		}

		/// <summary>
		/// Disconnect from the Vault server.
		/// </summary>
		public static void Logout()
		{
			// get rid of any listeners
			ClientConnection cc = client;

			// stop listening
			if ((cc.ClientInstance != null) && (cc.ClientInstance.EventEngine != null))
			{
				cc.ClientInstance.EventEngine.removeListener(cc, typeof(MessageEvent));
				cc.ClientInstance.EventEngine.removeListener(cc, typeof(BulkMessageEvent));
			}

			// then clean up the rest of the client connection's client instance.
			cc.DeleteClientInstance();
		}

		private static bool ValidateChangeSetItemID(int nID)
		{
			ChangeSetItemColl csic = client.ClientInstance.InternalChangeSet_GetItems(true);
			return ValidateChangeSetItemID(nID, csic);
		}

		private static bool ValidateChangeSetItemID(int nID, ChangeSetItemColl csic)
		{
			// get the change set.
			bool bValid = false;
			if (csic != null)
			{
				bValid = ((nID >= 0) && (nID < csic.Count));
			}
			return bValid;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		[Hidden]
		public static VaultUser[] GetUsers()
		{
			Login(VaultConnection.AccessLevelType.Admin, true, false);
			VaultUser[] users = null;
			client.ClientInstance.Connection.ListUsers(ref users);
			return users;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="userid"></param>
		/// <returns></returns>
		[Hidden]
		public static VaultFolderRightsItem[] GetUsersRights(int userid)
		{
			Login(VaultConnection.AccessLevelType.Admin, true, false);
			VaultFolderRightsItem[] rights = null;
			client.ClientInstance.Connection.ListRightsByUser(userid, ref rights);
			return rights;
		}

		/// <summary>
		/// Set a working folder association between a repository folder and a location on disk.
		/// </summary>
		/// <param name="repositoryFolderPath">The path to the repository folder.</param>
		/// <param name="diskPath">The path to a directory on disk.</param>
		/// <param name="createDiskPath">If createDiskPath is true, diskPath will be created if it doesn't exist.</param>
		/// <param name="forceSubfoldersToInherit">True to force subfolders to inherit this working folder setting.</param>
		[RecommendedOptionDefault("createDiskPath", "true"), RecommendedOptionDefault("forceSubfoldersToInherit", "false")]
		public static void SetWorkingFolder(string repositoryFolderPath, string diskPath, bool createDiskPath, bool forceSubfoldersToInherit)
		{
			try
			{
				string strReposFolder = RepositoryPath.NormalizeFolder(repositoryFolderPath);
				RepositoryUtil.ValidateReposPath(strReposFolder);

				if (createDiskPath)
				{
					if (!Directory.Exists(diskPath))
						Directory.CreateDirectory(diskPath);
					if (!Directory.Exists(diskPath))
						throw new Exception(string.Format("{0} does not exist and could not be created", diskPath));
				}

				VaultClientTreeObject obj = client.ClientInstance.TreeCache.Repository.Root.FindTreeObjectRecursive(strReposFolder);
				if (obj == null)
					throw new Exception(string.Format("{0} does not exist in the repository", strReposFolder));

				if (obj is VaultClientFile)
					obj = obj.Parent;

				client.ClientInstance.TreeCache.SetWorkingFolder(obj.FullPath, diskPath, forceSubfoldersToInherit, false);
			}
			catch (Exception e)
			{
				int errorcode = VaultConnection.GetSoapExceptionStatusCodeInt(e);
				if (errorcode == VaultLib.VaultStatusCode.FailInvalidSessionToken)
				{
					ServerOperations.Logout();
				}
				throw e;
			}
		}

		/// <summary>
		/// Set a working folder association between a repository folder and a location on disk.
		/// </summary>
		/// <param name="repositoryFolderPath">The path to the repository folder.</param>
		/// <param name="diskPath">The path to a directory on disk.</param>
		/// <param name="createDiskPath">If createDiskPath is true, diskPath will be created if it doesn't exist.</param>
		[Hidden]
		public static void SetWorkingFolder(string repositoryFolderPath, string diskPath, bool createDiskPath)
		{
			try
			{
				string strReposFolder = RepositoryPath.NormalizeFolder(repositoryFolderPath);
				RepositoryUtil.ValidateReposPath(strReposFolder);

				if (createDiskPath)
				{
					if (!Directory.Exists(diskPath))
						Directory.CreateDirectory(diskPath);
					if (!Directory.Exists(diskPath))
						throw new Exception(string.Format("{0} does not exist and could not be created", diskPath));
				}

				VaultClientTreeObject obj = client.ClientInstance.TreeCache.Repository.Root.FindTreeObjectRecursive(strReposFolder);
				if (obj == null)
					throw new Exception(string.Format("{0} does not exist in the repository", strReposFolder));

				if (obj is VaultClientFile)
					obj = obj.Parent;

				client.ClientInstance.TreeCache.SetWorkingFolder(obj.FullPath, diskPath);
			}
			catch (Exception e)
			{
				int errorcode = VaultConnection.GetSoapExceptionStatusCodeInt(e);
				if (errorcode == VaultLib.VaultStatusCode.FailInvalidSessionToken)
				{
					ServerOperations.Logout();
				}
				throw e;
			}
		}

		/// <summary>
		/// Return the repository id that corresponds to the given repository name
		/// </summary>
		/// <param name="repositoryName">The name of a Vault repository.</param>
		/// <returns>The repository id that matches the name provided.</returns>
		[Hidden]
		public static int GetRepositoryId(string repositoryName)
		{
			try
			{
				VaultRepositoryInfo[] reps = null;
				//List all the repositories on the server.
				client.ClientInstance.ListRepositories(ref reps);

				int repositoryId = -1;

				//Search for the one that we want.
				foreach (VaultRepositoryInfo r in reps)
				{
					if (String.Compare(r.RepName, repositoryName, true) == 0)
					{
						//This will load up the client side cache files and refresh the repository structure.
						//See http://support.sourcegear.com/viewtopic.php?t=6 for more on client side cache files.
						repositoryId = r.RepID;
						break;
					}
				}
				if (repositoryId == -1)
					throw new ArgumentException(string.Format("Repository {0} not found", repositoryName));

				return repositoryId;
			}
			catch (Exception e)
			{
				int errorcode = VaultConnection.GetSoapExceptionStatusCodeInt(e);
				if (errorcode == VaultLib.VaultStatusCode.FailInvalidSessionToken)
				{
					ServerOperations.Logout();
				}
				throw e;
			}
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="rights"></param>
		/// <returns></returns>
		[Hidden]
		public static string DecodeUserRights(uint rights)
		{
			StringBuilder rightsString = new StringBuilder("---");
			if ((rights & 1) != 0)
				rightsString[0] = 'R';
			if ((rights & 2) != 0)
				rightsString[1] = 'C';
			if ((rights & 4) != 0)
				rightsString[2] = 'A';
			return rightsString.ToString();
		}

		private static string GetSessionFileName()
		{
			return Path.Combine(client.ClientInstance.LocalStoreBasePath, SESSION_FILENAME);
		}
		private static void StoreSession(string strURLBase, string strUsername, string password, string strRepos)
		{
			TextWriter tw = null;
			try
			{
				VaultLib.VaultCrypto crypt = new VaultLib.VaultCrypto(_cryptVector, _cryptKey);

				tw = new StreamWriter(new FileStream(GetSessionFileName(), FileMode.Create, FileAccess.Write, FileShare.None));
				tw.WriteLine(strURLBase);
				tw.WriteLine(crypt.Encrypt(strUsername));
				tw.WriteLine(crypt.Encrypt(password));
				tw.WriteLine(strRepos);
				tw.Close();
			}
			catch (Exception)
			{
				// if anything goes wrong with the encryption, just do it without encryption
				try
				{
					if (tw != null)
						tw.Close();

					tw = new StreamWriter(new FileStream(GetSessionFileName(), FileMode.Create, FileAccess.Write, FileShare.None));
					tw.WriteLine(strURLBase);
					tw.WriteLine(strUsername);
					tw.WriteLine(password);
					tw.WriteLine(strRepos);
					tw.Close();
				}
				catch (Exception)
				{
					throw;
				}
			}
		}
		private static void RetrieveSession(ref string strURLBase, ref string strUsername, ref string password, ref string strRepos)
		{
			TextReader tw = null;
			try
			{
				VaultLib.VaultCrypto crypt = new VaultLib.VaultCrypto(_cryptVector, _cryptKey);

				tw = new StreamReader(new FileStream(GetSessionFileName(), FileMode.Open, FileAccess.Read, FileShare.Read));

				strURLBase = tw.ReadLine();
				strUsername = crypt.Decrypt(tw.ReadLine());
				password = crypt.Decrypt(tw.ReadLine());
				strRepos = tw.ReadLine();

				tw.Close();
			}

			catch (Exception)
			{
				// if anything goes wrong with the encryption, just do it without encryption
				try
				{
					if (tw != null)
						tw.Close();

					tw = new StreamReader(new FileStream(GetSessionFileName(), FileMode.Open, FileAccess.Read, FileShare.Read));
					strURLBase = tw.ReadLine();
					strUsername = tw.ReadLine();
					password = tw.ReadLine();
					strRepos = tw.ReadLine();
					tw.Close();
				}
				catch (Exception)
				{
				}
			}
		}

		/// <summary>
		/// Remove login information that was previously cached with SetLoginOptions
		/// </summary>
		[DoesNotRequireRepository, DoesNotRequireLogin]
		public static void PurgeSession()
		{
			try
			{
				Logout();

				ClientInstance ci = client.CreateClientInstance(false);
				ci.Init(VaultClientNetLib.VaultConnection.AccessLevelType.Client);

				string strFN = GetSessionFileName();
				if (File.Exists(strFN) == true)
				{
					File.Delete(strFN);
				}
			}
			catch (Exception)
			{
			}
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="message"></param>
		[Hidden]
		public static void NewMessageHandler(object sender, ProgressMessage message)
		{
			WriteUserMessage(message.Message);
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="aProgressMessages"></param>
		[Hidden]
		public static void NewBulkMessagesHandler(object sender, ProgressMessage[] aProgressMessages)
		{
			foreach (ProgressMessage message in aProgressMessages)
			{
				if (client.Verbose || message.Level == ProgressMessage.MessageLevel.Error)
				{
					WriteUserMessage(message.Message);
				}
			}
		}


	}
	/// <summary>
	/// Describes what can be done with locally modified files when a get is performed.
	/// </summary>
	public enum BackupOption
	{
		/// <summary>
		/// Backup files.
		/// </summary>
		yes,
		/// <summary>
		/// Do not backup files.
		/// </summary>
		no,
		/// <summary>
		/// Use the default behavior in the user's options.
		/// </summary>
		usedefault
	}
	/// <summary>
	/// Controls how the output of the history command is sorted.
	/// </summary>
	public enum DateSortOption
	{
		/// <summary>
		/// Sort history items from oldest to newest.
		/// </summary>
		asc,
		/// <summary>
		/// Sort history items from newest to oldest.
		/// </summary>
		desc
	}

	class DiffDefines
	{
		public const string DiffEnv = "VAULTDIFF";
		public const string DiffBin = "diff";

		public const string DiffLeftItem = "%LEFT_PATH%";
		public const string DiffRightItem = "%RIGHT_PATH%";
	}

	/// <summary>
	/// Describes the distinct kinds of comparisons that can be done.
	/// </summary>
	public enum CompareToOption
	{
		/// <summary>
		/// Compares to the latest version in the repository.
		/// </summary>
		current,
		/// <summary>
		/// Compares to a specific label.
		/// </summary>
		label,
		/// <summary>
		/// Compares with the last version that was downloaded from the server (the "baseline" version).
		/// </summary>
		lastget,
		/// <summary>
		/// Compares with a file or folder at a local path.
		/// </summary>
		local,
		/// <summary>
		/// Compares with a file or folder at the given repository path.
		/// </summary>
		repository
	}

	class UsageException : System.Exception
	{
		public UsageException(string s)
			: base(s)
		{
		}
	}

	/// <summary>
	/// An object for holding the paths to 2 items to diff and an int to describe any
	/// errors that occured during the get of those items.
	/// 
	/// This class was made to facilitate the diff of shelved items from Eclipse.  The 
	/// method that preps the diff has 2 ref params plus a return value, Eclipse can't 
	/// handle the refs.
	/// </summary>
	public class ShelveDiffInfo
	{
		public String leftItemStr;

		public String rightItemStr;

		public int diffError;

		public ShelveDiffInfo(String left, String right, int diffE)
		{
			this.leftItemStr = left;
			this.rightItemStr = right;
			this.diffError = diffE;
		}
	}

	/// <summary>
	/// An object containing the following information for a specific vault transaction:  
	/// the user's id, the user's login, the comment, and an array of VaultTxDetailHistoryItem 
	/// objects.
	/// </summary>
	public class TxInfo
	{
		/// <summary>
		/// The id of the user who completed the transaction.
		/// </summary>
		public int userid;

		/// <summary>
		/// The login of the user who completed the transaction.
		/// </summary>
		public String userlogin;

		/// <summary>
		/// The changeset comment for the transaction.
		/// </summary>
		public String changesetComment;

		/// <summary>
		/// An array of the items included in the transaction.
		/// </summary>
		public VaultTxDetailHistoryItem[] items;

		/// <summary>
		/// TxInfo Constructor
		/// </summary>
		/// <param name="id">The id of the user who completed the transaction.</param>
		/// <param name="login">The login of the user who completed the transaction.</param>
		/// <param name="comment">The changeset comment for the transaction.</param>
		/// <param name="items">The items included in the transation.</param>
		public TxInfo(int id, String login, String comment, VaultTxDetailHistoryItem[] items)
		{
			this.userid = id;
			this.userlogin = login;
			this.changesetComment = comment;
			this.items = items;
		}
	}

	// Define a custom attribute with one named parameter.
	/// <summary>
	/// Allows the specification of a recommended default value for a parameter.
	/// </summary>
	[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
	public class RecommendedOptionDefault : Attribute
	{
		private string _option;
		private string _defaultValue;
		/// <summary>
		/// 
		/// </summary>
		/// <param name="option"></param>
		/// <param name="defaultValue"></param>
		public RecommendedOptionDefault(string option, string defaultValue)
		{
			_option = option;
			_defaultValue = defaultValue;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="defaultValue"></param>
		public RecommendedOptionDefault(string defaultValue)
		{
			_option = null;
			_defaultValue = defaultValue;
		}
		/// <summary>
		/// 
		/// </summary>
		public string Option
		{
			get
			{
				return _option;
			}
		}
		/// <summary>
		/// 
		/// </summary>
		public string DefaultValue
		{
			get
			{
				return _defaultValue;
			}
		}
	}
	// Define a custom attribute with one named parameter.
	/// <summary>
	/// Specifies a parameter of type String[] which is an array of wildcard strings.
	/// </summary>
	[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
	public class WildcardArray : Attribute
	{
		private string _option;
		/// <summary>
		/// 
		/// </summary>
		/// <param name="option"></param>
		public WildcardArray(string option)
		{
			_option = option;
		}
		/// <summary>
		/// 
		/// </summary>
		public string Option
		{
			get
			{
				return _option;
			}
		}
	}
	// Define a custom attribute with one named parameter.
	/// <summary>
	/// Specifies a parameter that only accepts a local path.
	/// </summary>
	[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
	public class LocalPathOnly : Attribute
	{
		private string _option;
		/// <summary>
		/// 
		/// </summary>
		/// <param name="option"></param>
		public LocalPathOnly(string option)
		{
			_option = option;
		}
		/// <summary>
		/// 
		/// </summary>
		public string Option
		{
			get
			{
				return _option;
			}
		}
	}
	// Define a custom attribute with one named parameter.
	/// <summary>
	/// Specifies a parameter that will accept either a remote or local path.
	/// </summary>
	[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
	public class LocalOrRemotePath : Attribute
	{
		private string _option;
		/// <summary>
		/// 
		/// </summary>
		/// <param name="option"></param>
		public LocalOrRemotePath(string option)
		{
			_option = option;
		}
		/// <summary>
		/// 
		/// </summary>
		public string Option
		{
			get
			{
				return _option;
			}
		}
	}
	// Define a custom attribute with one named parameter.
	/// <summary>
	/// Specifies a parameter that will only accept a remote path.
	/// </summary>
	[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
	public class RemotePathOnly : Attribute
	{
		private string _option;
		/// <summary>
		/// 
		/// </summary>
		/// <param name="option"></param>
		public RemotePathOnly(string option)
		{
			_option = option;
		}
		/// <summary>
		/// 
		/// </summary>
		public string Option
		{
			get
			{
				return _option;
			}
		}
	}
	/// <summary>
	/// Specifies a method does not require login.
	/// </summary>
	[AttributeUsage(AttributeTargets.All)]
	public class DoesNotRequireLogin : Attribute
	{
	}
	/// <summary>
	/// Specifies a method does not require a repository be set.
	/// </summary>
	[AttributeUsage(AttributeTargets.All)]
	public class DoesNotRequireRepository : Attribute
	{
	}
	/// <summary>
	/// Specifies that a method should be hidden from nant.
	/// </summary>
	[AttributeUsage(AttributeTargets.All)]
	public class Hidden : Attribute
	{
	}
}
