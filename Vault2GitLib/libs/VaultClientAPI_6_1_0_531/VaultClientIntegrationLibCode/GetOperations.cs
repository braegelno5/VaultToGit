/*
	SourceGear Vault
	Copyright 2002-2008 SourceGear LLC
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
using System.Text.RegularExpressions;

using VaultLib;
using VaultClientOperationsLib;
using VaultClientNetLib;


namespace VaultClientIntegrationLib
{
	/// <summary>
	/// This class encapsulates all of the options that can be passed into a simple get
	/// request.  When a new GetOptions is constructed, it is prefilled with the user's current options.
	/// </summary>
	public class GetOptions
	{
		/// <summary>
		/// Create a new GetOptions object.  When a new GetOptions is constructed, it is prefilled with the user's current options.
		/// </summary>
		public GetOptions()
		{
			Merge = (MergeType) ServerOperations.client.ClientInstance.UserOptions.GetInt(VaultOptions.Merge);
			Recursive = ServerOperations.client.ClientInstance.UserOptions.GetBool(VaultOptions.Recursive);
			MakeWritable = (MakeWritableType) ServerOperations.client.ClientInstance.UserOptions.GetInt(VaultOptions.MakeWritable);
			SetFileTime = (SetFileTimeType)ServerOperations.client.ClientInstance.UserOptions.GetInt(VaultOptions.FileTime);
			PerformDeletions = (PerformDeletionsType)ServerOperations.client.ClientInstance.UserOptions.GetInt(VaultOptions.DeleteLocally);
		}

		/// <summary>
		/// Returns the user options collection containing the get options.
		/// </summary>
		/// <returns>A UserOptionsCollection object.</returns>
		public UserOptionsCollection GetUserOptionsCollection()
		{
			UserOptionsCollection coll = new UserOptionsCollection();
			coll.SetOption(VaultOptions.Merge, "" + (int)Merge);
			coll.SetOption(VaultOptions.Recursive, "" + (bool)Recursive);
			coll.SetOption(VaultOptions.MakeWritable, "" + (int)MakeWritable);
			coll.SetOption(VaultOptions.FileTime, "" + (int)SetFileTime);
			coll.SetOption(VaultOptions.DeleteLocally, "" + (int)PerformDeletions);
			return coll;
		}
		/// <summary>
		/// Controls how and if files that have been modified locally are replaced with versions from the repository.
		/// Possible values are:  "OverwriteWorkingCopy", "MergeLater", "AttemptAutomaticMerge", and "Unspecified".
		/// </summary>
		[RecommendedOptionDefault("\"AttemptAutomaticMerge\"")]
		public MergeType Merge = MergeType.Unspecified;
		/// <summary>
		/// Controls if folders are recursively fetched.
		/// </summary>
		[RecommendedOptionDefault("true")]
		public bool Recursive = true;
		/// <summary>
		/// Controls if files are marked as writable when fetched.
		/// Possible values are:  "MakeNonMergableFilesReadOnly", "MakeAllFilesReadOnly", and "MakeAllFilesWritable".
		/// </summary>
		[RecommendedOptionDefault("\"MakeNonMergableFilesReadOnly\"")]
		public MakeWritableType MakeWritable = MakeWritableType.MakeNonMergableFilesReadOnly;
		/// <summary>
		/// Controls the date placed in the file's Last Modified attribute.
		/// Possible values are:  "Current", "Modification", and "CheckIn".
		/// </summary>
		[RecommendedOptionDefault("\"Current\"")]
		public SetFileTimeType SetFileTime = SetFileTimeType.Current;
		/// <summary>
		/// Controls if files which are deleted, moved or renamed in the repository are deleted, moved
		/// or renamed on disk when the get is completed.  This option is ignored for get operations 
		/// to locations outside of working folders.
		/// Possible values are:  "RemoveWorkingCopy", "DoNotRemoveWorkingCopy", and "RemoveWorkingCopyIfUnmodified".
		/// </summary>
		[RecommendedOptionDefault("\"RemoveWorkingCopyIfUnmodified\"")]
		public PerformDeletionsType PerformDeletions = PerformDeletionsType.RemoveWorkingCopyIfUnmodified;
		/// <summary>
		/// Mergable files will be converted to the given line-ending type when fetched.
		/// Available types are: "None", "Native", "CR", "LF", or "CRLF".
		/// "none" means "don't override".
		/// </summary>
		[RecommendedOptionDefault("\"None\"")]
		public VaultEOL OverrideEOL = VaultEOL.None;
	}
	/// <summary>
	/// Summary description for GetOperations.
	/// </summary>
	public class GetOperations
	{
		private GetOperations()
		{
			//
			// TODO: Add constructor logic here
			//
		}

		/// <summary>
		/// Gets the latest version of Vault objects to a working folder.  This method requires that a working folder
		/// is already set for the objects to be downloaded.
		/// </summary>
		/// <param name="objectPaths">An array of paths to get.  These can be either local paths or repository paths.  If they are repository paths, wildcards can be included.</param>
		/// <param name="getOptions">The options that will control how the get is performed.</param>
		[LocalOrRemotePath("objectPaths")]
		public static void ProcessCommandGet(string[] objectPaths, GetOptions getOptions)
		{
			try
			{
				// refresh the tree.
				ServerOperations.client.ClientInstance.Refresh();

				if(getOptions.Merge == MergeType.Unspecified)
				{
					// use a default merge type if a valid merge type was not set
					getOptions.Merge = MergeType.AttemptAutomaticMerge;
				}

				ArrayList gresponses = new ArrayList();

				int orig_OverrideNativeEOL = ServerOperations.client.ClientInstance.WorkingFolderOptions.OverrideNativeEOL;
				ServerOperations.client.ClientInstance.WorkingFolderOptions.OverrideNativeEOL = (int) getOptions.OverrideEOL;
				try
				{
					// only force the GET to really GET if this is an overwrite.
					// no need to use VaultDefine.Latest on the GET request
					// unless OverwriteWorkingCopy was specified.
					// this flag will let the lower client libraries decide how to handle the request.
					bool bForceLatest = (getOptions.Merge != MergeType.OverwriteWorkingCopy) ? false : true;
					foreach (string objectPath in objectPaths)
					{
						VaultClientTreeObject[] treeobjects = RepositoryUtil.MatchWildcardToTreeObjects(objectPath);
						foreach (VaultClientTreeObject treeobj in treeobjects)
						{
							RepositoryUtil.CheckForWorkingFolder(treeobj, false);
							ServerOperations.client.ClientInstance.PerformPendingServerNamespaceChanges(objectPath);
							if (treeobj is VaultClientFolder)
							{
								VaultGetResponse[] tmpresponse = ServerOperations.client.ClientInstance.Get((VaultClientFolder)treeobj, getOptions.Recursive, bForceLatest, getOptions.MakeWritable, getOptions.SetFileTime, getOptions.Merge, null);
								if (tmpresponse != null)
								{
									gresponses.AddRange(tmpresponse);
								}
								ServerOperations.client.ClientInstance.PerformPendingLocalDeletions(objectPath, getOptions.PerformDeletions);
							}
							else
							{
								VaultGetResponse[] tmpresponse = ServerOperations.client.ClientInstance.Get((VaultClientFile)treeobj, bForceLatest, getOptions.MakeWritable, getOptions.SetFileTime, getOptions.Merge, null);
								if (tmpresponse != null)
								{
									gresponses.AddRange(tmpresponse);
								}
							}
						}
					}
				}
				finally
				{
					ServerOperations.client.ClientInstance.WorkingFolderOptions.OverrideNativeEOL = orig_OverrideNativeEOL;
				}

				if (gresponses != null && gresponses.Count > 0)
				{
					foreach (VaultGetResponse vgr in gresponses)
					{
						if (vgr.Response.Status != VaultStatusCode.Success && vgr.Response.Status != VaultStatusCode.SuccessRequireFileDownload)
						{
							throw new Exception(string.Format("Error getting {0}: {1}", vgr.File.FullPath, VaultConnection.GetSoapExceptionMessage(vgr.Response.Status)));
						}
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
		/// Gets the latest version of Vault objects to a location outside of a working folder.  If you use this method to download
		/// an object, you will not be able to check in from the that folder on disk.  If you will need to check in from this location,
		/// use ProcessCommandGet instead.
		/// </summary>
		/// <param name="objectPaths">An array of paths to get.  These can be either local paths or repository paths.  If they are repository paths, wildcards can be included.</param>
		/// <param name="getOptions">The options that will control how the get is performed.</param>
		/// <param name="destPath">The location on disk where the downloaded files will be placed.  You will not be able to check in from this location.</param>
		[LocalOrRemotePath("objectPaths")]
		public static void ProcessCommandGetToLocationOutsideWorkingFolder(string[] objectPaths, GetOptions getOptions, string destPath)
		{
			try
			{
				ServerOperations.client.ClientInstance.Refresh();
				if (destPath == null)
					throw new Exception("For this type of get, destPath cannot be null");
				// get the merge option.
				MergeType mt = getOptions.Merge;
				// on a non working folder only valid options :
				//	a) overwrite OR b) do not overwrite (later)
				switch (mt)
				{
					case MergeType.AttemptAutomaticMerge:
						// in this case, automatic merge is not possible, switch to do not overwrite
						mt = MergeType.MergeLater;
						break;
					case MergeType.OverwriteWorkingCopy:
					case MergeType.MergeLater:
						// do nothing - set correctly
						break;
					default:
						// the default value
						mt = MergeType.OverwriteWorkingCopy;
						break;
				}

				VaultGetResponse[] gresponses = null;

				int orig_OverrideNativeEOL = ServerOperations.client.ClientInstance.WorkingFolderOptions.OverrideNativeEOL;
				ServerOperations.client.ClientInstance.WorkingFolderOptions.OverrideNativeEOL = (int) getOptions.OverrideEOL;
				try
				{
					ArrayList array = new ArrayList();
					foreach (string objectPath in objectPaths)
					{
						VaultClientTreeObject[] treeobjects = RepositoryUtil.MatchWildcardToTreeObjects(objectPath);
						foreach (VaultClientTreeObject treeobj in treeobjects)
						{
							VaultClientFolder folder = (treeobj is VaultClientFolder) ? (VaultClientFolder)treeobj : treeobj.Parent;
							string wf = ServerOperations.client.ClientInstance.TreeCache.GetBestWorkingFolder(folder);
							if (wf != null)
								if (String.Compare(Misc.NormalizeDiskPath(destPath), wf, true) == 0)
									throw new Exception("You have supplied a destPath which matches the working folder already associated with this object.  Remove the destpath argument when updating a local working folder, or remove the working folder association for " + folder.FullPath);
							if (treeobj is VaultClientFolder)
								gresponses = ServerOperations.client.ClientInstance.GetToNonWorkingFolder((VaultClientFolder)treeobj, getOptions.Recursive, true, (mt == MergeType.OverwriteWorkingCopy) ? true : false, getOptions.MakeWritable, getOptions.SetFileTime, destPath, null);
							else
							{
								array.Add((VaultClientFile)treeobj);
							}
						}
					}
					if (array.Count > 0)
					{
						string commonRepositoryParentPath = RepositoryUtil.GetCommonParent(array);
						gresponses = ServerOperations.client.ClientInstance.GetToNonWorkingFolder((VaultClientFile[])array.ToArray(typeof(VaultClientFile)), true, (mt == MergeType.OverwriteWorkingCopy) ? true : false, getOptions.MakeWritable, getOptions.SetFileTime, commonRepositoryParentPath, destPath, null);
					}
				}
				finally
				{
					ServerOperations.client.ClientInstance.WorkingFolderOptions.OverrideNativeEOL = orig_OverrideNativeEOL;
				}

				if (gresponses != null)
				{
					foreach (VaultGetResponse vgr in gresponses)
					{
						if (vgr.Response.Status != VaultStatusCode.Success && vgr.Response.Status != VaultStatusCode.SuccessRequireFileDownload)
						{
							throw new Exception(string.Format("Error getting {0}: {1}", vgr.File.FullPath, VaultConnection.GetSoapExceptionMessage(vgr.Response.Status)));
						}
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
		/// Get the version of a Vault object which has the provided label.  This method will download the Vault objects into a non 
		/// working folder.  If you use this method to download objects, you will not be able to check in from the that folder on disk.  
		/// If you will need to check in from this location, use ProcessCommandGetLabelToTempWorkingFolder instead.
		/// </summary>
		/// <param name="objectPath">The path to an object.  This can be either a local path or a repository path.</param>
		/// <param name="label">The label that was applied to the object.</param>
		/// <param name="labelSubItem">If the specified item is shared to multiple places in the label, use this parameter to specify which subitem to download.  Can (and usually should) be null.</param>
		/// <param name="getOptions">The options that will control how the get is performed.</param>
		/// <param name="destPath">The location on disk where the downloaded files will be placed.  You will not be able to check in from this location.</param>
		[LocalOrRemotePath("objectPath"), RecommendedOptionDefault("labelSubItem", "null")]
		public static void ProcessCommandGetLabelToLocationOutsideWorkingFolder(string objectPath, string label, string labelSubItem, GetOptions getOptions, string destPath)
		{
			performLabelGet(objectPath, label, labelSubItem, null, destPath, getOptions);
		}
		/// <summary>
		/// Get the version of a Vault object which has the provided label.  This method will download the Vault object into a temporary 
		/// working folder.  If you use this method to download objects, you will not be able to check in from the that folder on disk.
		/// </summary>
		/// <param name="objectPath">The path to an object.  This can be either a local path or a repository path.</param>
		/// <param name="label">The label that was applied to the object.</param>
		/// <param name="labelSubItem">If the specified item is shared to multiple places in the label, use this parameter to specify which subitem to download.  Can (and usually should) be null.</param>
		/// <param name="getOptions">The options that will control how the get is performed.</param>
		/// <param name="tmpWorkingFolder">The location on disk where the downloaded files will be placed.  You will not be able to check in from this location.</param>
		[LocalOrRemotePath("objectPath"), RecommendedOptionDefault("labelSubItem", "null")]
		public static void ProcessCommandGetLabelToTempWorkingFolder(string objectPath, string label, string labelSubItem, GetOptions getOptions, string tmpWorkingFolder)
		{
			performLabelGet(objectPath, label, labelSubItem, tmpWorkingFolder, null, getOptions);
		}
		private static void performLabelGet(string objectPath, string label, string labelSubItem, string labelWorkingFolder, string destPath, GetOptions go)
		{
			try
			{
				bool bSuccess = false;
				string[] discoveredPaths;
				VaultClientTreeObject labelStructure = null;
				long labelSubItemId = 0;
				long labelID = 0;

				// retrieve the merge option.
				MergeType mt = go.Merge;
				// Labels can't automerge.
				switch (mt)
				{
					case MergeType.AttemptAutomaticMerge:
						// in this case, automatic merge is not possible, switch to do not overwrite
						mt = MergeType.MergeLater;
						break;
					case MergeType.OverwriteWorkingCopy:
					case MergeType.MergeLater:
						// do nothing - set correctly
						break;
					default:
						// the default value
						mt = MergeType.OverwriteWorkingCopy;
						break;
				}

				ServerOperations.client.ClientInstance.Refresh();

				VaultClientTreeObject reposTreeObj = RepositoryUtil.FindVaultTreeObjectAtReposOrLocalPath(objectPath);

				labelSubItemId = reposTreeObj.ID;

				int orig_OverrideNativeEOL = ServerOperations.client.ClientInstance.WorkingFolderOptions.OverrideNativeEOL;
				ServerOperations.client.ClientInstance.WorkingFolderOptions.OverrideNativeEOL = (int) go.OverrideEOL;
				try
				{
					// This is the ID of the file/folder where the label structure was
					// created.
					long rootID = 0;

					try 
					{
						bSuccess = ServerOperations.client.ClientInstance.GetByLabel_GetStructure(reposTreeObj.FullPath, label, ref labelID, labelSubItem, out discoveredPaths, out labelStructure, out rootID);
					}
					catch (Exception e)
					{
						if (labelStructure == null)
						{
							throw new Exception(string.Format("Could not find label \"{0}\" created at item \"{1}\".  {2}", label, reposTreeObj.FullPath, e.Message));
						} 
						else 
						{
							throw;
						}
					}


					if(bSuccess == true)
					{
						VaultClientTreeObjectColl treeObjects = new VaultClientTreeObjectColl();

						if (labelStructure is VaultClientFile)
						{
							if (((VaultClientFile) labelStructure).ID == labelSubItemId)
								treeObjects.Add(labelStructure);
						}
						else
						{
							((VaultClientFolder) labelStructure).FindTreeObjectsRecursive(labelSubItemId, ref treeObjects);
						}

						if(treeObjects.Count < 1)
						{
							throw new Exception("The specified item could not be found in the label.");
						}
						else if(treeObjects.Count > 1)
						{
							throw new Exception("The specified item was not specific within the label.");
						}

						VaultClientTreeObject treeObject = (VaultClientTreeObject)treeObjects[0];
						labelSubItem = discoveredPaths[0];

						// get to non-working folder
						if(destPath != null)
						{
							if (labelStructure is VaultClientFolder)
							{
								ServerOperations.client.ClientInstance.GetByLabelToNonWorkingFolder_GetData(
									(VaultClientFolder) labelStructure, 
									go.Recursive, 
									(mt == MergeType.OverwriteWorkingCopy) ? true : false, 
									go.MakeWritable, 
									go.SetFileTime, 
									destPath,
									null, 
									labelID,
									reposTreeObj.FullPath,
									labelSubItem);
							}
							else
							{
								// We have to invent a parent for the file in the label structure so the
								// get works correctly.

								VaultClientFolder parent = new VaultClientFolder();
								parent.Name = Guid.NewGuid().ToString();
								parent.Files.Add((VaultClientFile) labelStructure);
								labelStructure.Parent = parent;

								ServerOperations.client.ClientInstance.GetByLabelToNonWorkingFolder_GetData(
									(VaultClientFile) labelStructure, 
									(mt == MergeType.OverwriteWorkingCopy) ? true : false, 
									go.MakeWritable, 
									go.SetFileTime, 
									destPath, 
									null,
									labelID,
									reposTreeObj.FullPath,
									labelSubItem);
							}
						}
							// get to working folder
						else
						{
							VaultClientFolder labelRootFolder;

							if(treeObject == null)
							{
								throw new Exception(string.Format("{0} does not exist in the label structure for {1}", reposTreeObj.FullPath, label));
							}

							if(labelStructure is VaultClientFolder)
							{				
								labelRootFolder = (VaultClientFolder)treeObjects[0];
							}
							else
							{
								labelRootFolder = RepositoryUtil.GetFakeLabelParent((VaultClientFile) labelStructure, reposTreeObj.FullPath, label);
							}

							//There is no matching unset to this command, because the label working folder association is never saved to disk.
							//It will be lost as soon as this process goes out of memory.
							ServerOperations.client.ClientInstance.TreeCache.SetLabelWorkingFolder(labelRootFolder.FullPath, labelWorkingFolder);
						
							if(labelStructure is VaultClientFolder)
							{
								ServerOperations.client.ClientInstance.GetByLabel_GetData((VaultClientFolder) labelStructure,
								                                                          go.Recursive,
								                                                          go.MakeWritable,
								                                                          go.SetFileTime,
								                                                          mt,
								                                                          null,
								                                                          labelID,
								                                                          reposTreeObj.FullPath,
								                                                          labelSubItem);
							}
							else
							{
								ServerOperations.client.ClientInstance.GetByLabel_GetData((VaultClientFile) labelStructure,
								                                                          go.MakeWritable,
								                                                          go.SetFileTime,
								                                                          mt,
								                                                          null,
								                                                          labelID,
								                                                          reposTreeObj.FullPath,
								                                                          labelSubItem);
							}
						}
					}
					else
					{
						string subItemOptions = String.Empty;
					
						foreach(string item in discoveredPaths)
						{
							subItemOptions += string.Format("   {0}{1}", item, Environment.NewLine);
						}

						throw new Exception(
							string.Format("The specified item is shared to multiple places in the label structure.{0}" +
							              "Please use \"vault.exe GETLABEL '{1}' '{2}' labelpath\", where labelpath is one of:{0}{0}{3}", 
							              Environment.NewLine, reposTreeObj.FullPath, label, subItemOptions)
							);
					}
				}
				finally
				{
					ServerOperations.client.ClientInstance.WorkingFolderOptions.OverrideNativeEOL = orig_OverrideNativeEOL;
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
		/// Get a specific version of a Vault object.  
		/// This method will download the object into the working folder that has been set.
		/// This method requires that a working folder
		/// is already set for the objects to be downloaded.
		/// </summary>
		/// <param name="objectPath">The path to an object.  This can be either a local path or a repository path.</param>
		/// <param name="version">The version of the object that will be downloaded.</param>
		/// <param name="getOptions">The options that will control how the get is performed.</param>
		[LocalOrRemotePath("objectPath")]
		public static void ProcessCommandGetVersion(string objectPath, int version, GetOptions getOptions)
		{
			performGetVersion(objectPath, version, null, getOptions);
		}
		/// <summary>
		/// Get a specific version of a Vault object to a location outside of a working folder.  If you use this method to download
		/// an object, you will not be able to check in from the that folder on disk.  If you will need to check in from this location,
		/// use ProcessCommandGetVersion instead.
		/// </summary>
		/// <param name="objectPath">The path to an object.  This can be either a local path or a repository path.</param>
		/// <param name="version">The version of the object that will be downloaded.</param>
		/// <param name="getOptions">The options that will control how the get is performed.</param>
		/// <param name="destPath">The location on disk where the downloaded files will be placed.  You will not be able to check in from this location.</param>
		[LocalOrRemotePath("objectPath")]
		public static void ProcessCommandGetVersionToLocationOutsideWorkingFolder(string objectPath, int version, GetOptions getOptions, string destPath)
		{
			performGetVersion(objectPath, version, destPath, getOptions);
		}
		
		private static void performGetVersion(string objectPath, int version, string strDestFolder, GetOptions getOptions)
		{
			try
			{
				ServerOperations.client.ClientInstance.Refresh();

				VaultClientTreeObject treeObjectToRetrieve = RepositoryUtil.FindVaultTreeObjectAtReposOrLocalPath(objectPath);

				// retrieve the merge option.
				MergeType mt = getOptions.Merge;

				// this always overwrites, unless specified to auto merge
				switch (mt)
				{
					case MergeType.AttemptAutomaticMerge:
						// in this case, automatic merge is not possible, switch to do not overwrite
						mt = MergeType.MergeLater;
						break;
					case MergeType.OverwriteWorkingCopy:
					case MergeType.MergeLater:
						// do nothing - set correctly
						break;
					default:
						// the default value
						mt = MergeType.OverwriteWorkingCopy;
						break;
				}

				VaultGetResponse[] getResponses = null;

				int orig_OverrideNativeEOL = ServerOperations.client.ClientInstance.WorkingFolderOptions.OverrideNativeEOL;
				ServerOperations.client.ClientInstance.WorkingFolderOptions.OverrideNativeEOL = (int) getOptions.OverrideEOL;
				try
				{
					if (treeObjectToRetrieve is VaultClientFolder)
					{
						VaultClientFolder vcFolder = (VaultClientFolder)treeObjectToRetrieve;
						vcFolder.Version = version;

						VaultFolderDelta vfDelta = new VaultFolderDelta();
						try
						{
							ServerOperations.client.ClientInstance.Connection.GetBranchStructure(ServerOperations.client.ClientInstance.ActiveRepositoryID, treeObjectToRetrieve.FullPath, vcFolder.ID, version, ref vfDelta, false);
						}
						catch
						{
							if ( vfDelta.ObjVerID < 1 )
								throw new Exception("There is no version " + version + " of " + treeObjectToRetrieve.FullPath + " in " + ServerOperations.client.ClientInstance.Repository.RepName + ".");
							else
								throw;
						}

						vcFolder = new VaultClientFolder(vfDelta, vcFolder.Parent);

						if ( strDestFolder == null )
						{
							if ( ServerOperations.client.ClientInstance.TreeCache.GetBestWorkingFolder(vcFolder) == null )
								throw new Exception(vcFolder.FullPath + " has no working folder set.");
							getResponses = ServerOperations.client.ClientInstance.Get(vcFolder, getOptions.Recursive, false, getOptions.MakeWritable, getOptions.SetFileTime, mt, null);
						}
						else
							getResponses = ServerOperations.client.ClientInstance.GetByDisplayVersionToNonWorkingFolder(vcFolder, getOptions.Recursive, getOptions.MakeWritable, getOptions.SetFileTime, strDestFolder, null);
					}
					else
					{
						VaultClientFile vcFile = (VaultClientFile)treeObjectToRetrieve;
						vcFile = new VaultClientFile(ServerOperations.client.ClientInstance.TreeCache.Repository.Root.FindFileRecursive(treeObjectToRetrieve.FullPath));

						// Set the version.
						vcFile.Version = version;

						if ( strDestFolder == null )
							getResponses = ServerOperations.client.ClientInstance.GetByDisplayVersion(vcFile, getOptions.MakeWritable, getOptions.SetFileTime, mt, null);
						else
							getResponses = ServerOperations.client.ClientInstance.GetByDisplayVersionToNonWorkingFolder(vcFile, getOptions.MakeWritable, getOptions.SetFileTime, vcFile.Parent.FullPath, strDestFolder, null);
					}
				}
				finally
				{
					ServerOperations.client.ClientInstance.WorkingFolderOptions.OverrideNativeEOL = orig_OverrideNativeEOL;
				}

				if (getResponses == null)
					throw new Exception(string.Format("Error getting {0}: Version does not exist.", treeObjectToRetrieve.FullPath));

				foreach ( VaultGetResponse vgr in getResponses )
				{
					if (vgr.Response.Status != VaultStatusCode.Success && vgr.Response.Status != VaultStatusCode.SuccessRequireFileDownload)
						throw new Exception(string.Format("Error getting {0}: {1}", vgr.File.FullPath, VaultConnection.GetSoapExceptionMessage(vgr.Response.Status)));
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
		/// GETWILDCARD will retrieve all files within the folder specified by
		/// repositoryfolder whose name matches one of the wildcards specified.
		/// You may use '?' to match a single character or '*' to match a range of
		/// characters.  This method requires that a working folder
		/// is already set for the objects to be downloaded.
		/// </summary>
		/// <param name="folderPath">The path to an folder.  This can be either a local path or a repository path.</param>
		/// <param name="wildcardArray">An array of strings that will be used as wildcards to match.  You may use '?' to match a single character or '*' to match a range of characters.</param>
		/// <param name="getOptions">The options that will control how the get is performed.</param>
		[LocalOrRemotePath("folderPath"), WildcardArray("wildcardArray")]
		public static void ProcessCommandGetWildcard(string folderPath, string[] wildcardArray, GetOptions getOptions)
		{
			try
			{
				ArrayList RegexArray = new ArrayList();

				// get the merge option.
				MergeType mt = getOptions.Merge;
				if(mt == MergeType.Unspecified)
				{
					// use a default merge type if a valid merge type was not set
					mt = MergeType.AttemptAutomaticMerge;
				}
			
				ServerOperations.client.ClientInstance.Refresh();

				// for consistency's sake, currently doing case insensitve regex for wildcard
				bool bCaseInsensitive = true; 

				foreach (string wc in wildcardArray)
				{
					Wildcard wildcard = new Wildcard(wc);
					Regex regex = new Regex(wildcard.ToRegex(bCaseInsensitive));
					RegexArray.Add(regex);
				}
			
				VaultClientFolder vcfolder = RepositoryUtil.FindVaultFolderAtReposOrLocalPath(folderPath);

				int orig_OverrideNativeEOL = ServerOperations.client.ClientInstance.WorkingFolderOptions.OverrideNativeEOL;
				ServerOperations.client.ClientInstance.WorkingFolderOptions.OverrideNativeEOL = (int) getOptions.OverrideEOL;
				try
				{
					RepositoryUtil.CheckForWorkingFolder(vcfolder, false);
					ServerOperations.client.ClientInstance.PerformPendingServerNamespaceChanges(folderPath);
					ServerOperations.client.ClientInstance.GetByRegex(vcfolder, RegexArray, getOptions.Recursive, true, getOptions.MakeWritable, getOptions.SetFileTime, mt, null);
					ServerOperations.client.ClientInstance.PerformPendingLocalDeletions(folderPath, getOptions.PerformDeletions);
				}
				finally
				{
					ServerOperations.client.ClientInstance.WorkingFolderOptions.OverrideNativeEOL = orig_OverrideNativeEOL;
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
		/// GETWILDCARD will retrieve all files within the folder specified by
		/// repositoryfolder whose name matches one of the wildcards specified.
		/// You may use '?' to match a single character or '*' to match a range of
		/// characters.  If you use this method to download an object, you will not be 
		/// able to check in from the that folder on disk.  If you will need to 
		/// check in from this location, use ProcessCommandGetVersion instead.
		/// </summary>
		/// <param name="folderPath">The path to an folder.  This can be either a local path or a repository path.</param>
		/// <param name="wildcardArray">An array of strings that will be used as wildcards to match.  You may use '?' to match a single character or '*' to match a range of characters.</param>
		/// <param name="getOptions">The options that will control how the get is performed.</param>
		/// <param name="destPath">The location on disk where the downloaded files will be placed.  You will not be able to check in from this location.</param>
		[LocalOrRemotePath("folderPath"), WildcardArray("wildcardArray")]
		public static void ProcessCommandGetWildcardToNonWorkingFolder(string folderPath, string[] wildcardArray, GetOptions getOptions, string destPath)
		{
			try
			{
				ArrayList RegexArray = new ArrayList();

				// get the merge option.
				MergeType mt = getOptions.Merge;
			
				// on a non working folder only valid options :
				//	a) overwrite OR b) do not overwrite (later)
				switch (mt)
				{
					case MergeType.AttemptAutomaticMerge:
						// in this case, automatic merge is not possible, switch to do not overwrite
						mt = MergeType.MergeLater;
						break;
					case MergeType.OverwriteWorkingCopy:
					case MergeType.MergeLater:
						// do nothing - set correctly
						break;
					default:
						// the default value
						mt = MergeType.OverwriteWorkingCopy;
						break;
				}
			
				ServerOperations.client.ClientInstance.Refresh();

				// for consistency's sake, currently doing case insensitve regex for wildcard
				bool bCaseInsensitive = true; 

				foreach (string wc in wildcardArray)
				{
					Wildcard wildcard = new Wildcard(wc);
					Regex regex = new Regex(wildcard.ToRegex(bCaseInsensitive));
					RegexArray.Add(regex);
				}
			
				VaultClientFolder vcfolder = RepositoryUtil.FindVaultFolderAtReposOrLocalPath(folderPath);

				int orig_OverrideNativeEOL = ServerOperations.client.ClientInstance.WorkingFolderOptions.OverrideNativeEOL;
				ServerOperations.client.ClientInstance.WorkingFolderOptions.OverrideNativeEOL = (int) getOptions.OverrideEOL;
				try
				{
					ServerOperations.client.ClientInstance.GetByRegexToNonWorkingFolder(vcfolder, RegexArray, getOptions.Recursive, true, (mt == MergeType.OverwriteWorkingCopy) ? true : false, getOptions.MakeWritable, getOptions.SetFileTime, destPath, null);
				}
				finally
				{
					ServerOperations.client.ClientInstance.WorkingFolderOptions.OverrideNativeEOL = orig_OverrideNativeEOL;
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
	}
}
