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
using System.Text;
using System.IO;

using VaultLib;
using VaultClientOperationsLib;

namespace VaultClientIntegrationLib
{
	/// <summary>
	/// Summary description for RepositoryUtil.
	/// </summary>
	public class RepositoryUtil
	{
        /// <summary>
        /// 
        /// </summary>
		public RepositoryUtil()
		{
			//
			// TODO: Add constructor logic here
			//
		}
        /// <summary>
        /// Validates a repository path in a very basic way.  All repository paths must start with 
        /// $, and if there is more than one character, the second character must be /
        /// Throws a UsageException if given a path that doesn't meet the requirements.
        /// </summary>
        /// <param name="s"></param>
		public static void ValidateReposPath(string s)
		{
			if (s[0] != '$')
			{
				throw new UsageException(string.Format("Invalid repository path: {0}", s));
			}

			if (s.Length > 1)
			{
				if (s[1] != '/')
				{
					throw new UsageException(string.Format("Invalid repository path: {0}", s));
				}
			}
		}
        /// <summary>
        /// Returns the repository path associated with the input path.  Returns null if the path does not map to a repository path.
        /// </summary>
        /// <param name="testPath">May be either a local disk path, or a repository path.</param>
        /// <returns></returns>
		public static string CleanUpPathAndReturnRepositoryPath(string testPath)
		{
			if (testPath == null || testPath == "")
				throw new Exception("Invalid Path: empty string is not valid");

			if (testPath.StartsWith("$"))
			{
				string strFolderNormalized = RepositoryPath.NormalizeFolder(testPath);
				ValidateReposPath(strFolderNormalized);
				return strFolderNormalized;
			}
			else
			{
				if (!Path.IsPathRooted(testPath))
				{
					testPath = Path.Combine(Directory.GetCurrentDirectory(), testPath);
				}
				testPath = ServerOperations.client.ClientInstance.TreeCache.GetCorrespondingRepositoryPath(Misc.NormalizeDiskPath(testPath));
				return testPath;
			}
		}

        /// <summary>
        /// Returns true if an object exists at the given path.
        /// </summary>
        /// <param name="testPath">This path can either be a repository path or a disk path.</param>
        /// <returns></returns>
		public static bool PathExists(string testPath)
		{
			string tmpstr = CleanUpPathAndReturnRepositoryPath(testPath);
			if (tmpstr == null)
			{
				return false;
			}
			VaultClientTreeObject to = ServerOperations.client.ClientInstance.TreeCache.Repository.Root.FindTreeObjectRecursive(tmpstr);
			if (to != null)
				return true;
			else
				return false;
		
		}
		
        /// <summary>
        /// Searches for all objects with the given object id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
		public static VaultClientTreeObjectColl FindVaultTreeObjectsByObjID(long id) 
		{
			VaultClientTreeObjectColl coll = new VaultClientTreeObjectColl();
			ServerOperations.client.ClientInstance.TreeCache.Repository.Root.FindTreeObjectsRecursive(id, ref coll);
			return coll;
		}

        /// <summary>
        /// Searches for the Vault object at the given path.  Throws an exception if no object can be found.
        /// </summary>
        /// <param name="testPath">This path can either be a repository path or a disk path.</param>
        /// <returns></returns>
		public static VaultClientTreeObject FindVaultTreeObjectAtReposOrLocalPath(string testPath)
		{
			string tmpstr = CleanUpPathAndReturnRepositoryPath(testPath);
			if (tmpstr == null)
			{
				throw new Exception("Could not map path to anything useful: " + testPath);
			}
			VaultClientTreeObject to = ServerOperations.client.ClientInstance.TreeCache.Repository.Root.FindTreeObjectRecursive(tmpstr);
			if (to == null)
				throw new Exception("No object was found at the repository path: " + tmpstr);
			testPath = tmpstr;
			return to;
		}
        /// <summary>
        /// Searches for the Vault folder at the given path.  Throws an exception if no object can be found, or if a file is there.
        /// </summary>
        /// <param name="testPath">This path can either be a repository path or a disk path.</param>
        /// <returns></returns>
		public static VaultClientFolder FindVaultFolderAtReposOrLocalPath(string testPath)
		{
			VaultClientTreeObject to = FindVaultTreeObjectAtReposOrLocalPath(testPath);
			if (! (to is VaultClientFolder))
				throw new Exception("No folder was found at the path: " + testPath);
			return (VaultClientFolder)to;
		}
        /// <summary>
        /// Searches for the Vault file at the given path.  Throws an exception if no object can be found, or if a folder is there.
        /// </summary>
        /// <param name="testPath">This path can either be a repository path or a disk path.</param>
        /// <returns></returns>
		public static VaultClientFile FindVaultFileAtReposOrLocalPath(string testPath)
		{
			VaultClientTreeObject to = FindVaultTreeObjectAtReposOrLocalPath(testPath);
			if (! (to is VaultClientFile))
				throw new Exception("No file was found at the path: " + testPath);
			return (VaultClientFile)to;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="file"></param>
		/// <param name="currentPath"></param>
		/// <param name="labelName"></param>
		/// <returns></returns>
		public static VaultClientFolder GetFakeLabelParent(VaultClientFile file, string currentPath, string labelName)
		{
			VaultClientFolder fakeParent = new VaultClientFolder();

			VaultLib.VaultMD5CryptoServiceProvider md5 = new VaultLib.VaultMD5CryptoServiceProvider();
			ASCIIEncoding enc = new ASCIIEncoding();
			
			byte[] currentPathBytes = enc.GetBytes(currentPath.ToLower());
			byte[] labelBytes = enc.GetBytes(labelName.ToLower());

			byte[] combinedBytes = new byte[currentPathBytes.Length + labelBytes.Length + 1];

			byte[] currentPathHash = md5.ComputeHash(currentPathBytes);
			byte[] labelHash = md5.ComputeHash(labelBytes);

			string currentPathMD5 = Convert.ToBase64String(currentPathHash);
			string labelMD5 = Convert.ToBase64String(labelHash);

			fakeParent.Name = string.Format("label:{0}:{1}", currentPathMD5, labelMD5);

			fakeParent.Files.Add(file);
			file.Parent = fakeParent;

			return fakeParent;
		}

        /// <summary>
        /// Checks that a working folder is set for the object.  Throws an exception if no working folder is set.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="isCheckout">Changes the wording of the exception message.</param>
		public static void CheckForWorkingFolder(VaultClientTreeObject obj, bool isCheckout)
		{
			WorkingFolder wf = null;
			if (obj is VaultClientFolder )
				wf = ServerOperations.client.ClientInstance.GetWorkingFolder((VaultClientFolder)obj);
			else
				wf = ServerOperations.client.ClientInstance.GetWorkingFolder((VaultClientFile)obj);
			if (wf == null)
			{
				string strMsg = "";
				if (isCheckout)
					strMsg = "There is no working folder specified for " + obj.FullPath + "\nYou can set one using " 
						+ " SETWORKINGFOLDER or use a temporary working folder by using the -workingfolder option with GET.";
				else
					strMsg = "There is no working folder specified for " + obj.FullPath + "\nYou can set one using " 
						+ "SETWORKINGFOLDER, use a temporary working folder by using the -workingfolder option with GET, or use the " 
						+ "DESTPATH option to get \nto a non-working folder";
				throw new Exception(strMsg);
			}
		}
        /// <summary>
        /// Removes trailing path delimiters from a folder path.
        /// </summary>
        /// <param name="folderPath"></param>
        /// <returns></returns>
		public static string NormalizeFolderPath(string folderPath)
		{
			char[] chars = folderPath.ToCharArray();
			if ( chars[chars.Length-1] == '\\' || chars[chars.Length-1] == '/' )
				return folderPath.Remove(folderPath.Length-1, 1);
			else
				return folderPath;
		}
        /// <summary>
        /// Calls clientInstance.Refresh()
        /// </summary>
		public static void Refresh()
		{
			ServerOperations.client.ClientInstance.Refresh();
		}

        /// <summary>
        /// Returns an array filled with all objects that match the name passed in
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="name"></param>
        /// <param name="recursive"></param>
        /// <returns></returns>
		public static VaultClientTreeObject[] GetObjectsByName(VaultClientFolder folder, string name, bool recursive)
		{
			ArrayList array = new ArrayList();
			VaultClientTreeObject myobj = folder.GetObjectByName(name);
			if (myobj != null)
				array.Add(myobj);
			if (recursive == true && folder.Folders != null)
			{
				foreach (VaultClientFolder subfolder in folder.Folders)
				{
					array.AddRange(GetObjectsByName(subfolder, name, recursive));
				}
			}
			return (VaultClientTreeObject[])array.ToArray(typeof(VaultClientTreeObject));
		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="repositoryPath"></param>
        /// <returns></returns>
		public static VaultClientTreeObject[] MatchWildcardToTreeObjects(string repositoryPath)
		{
			
			char[] wildcardchars = "*?".ToCharArray();
			if (repositoryPath.IndexOfAny(wildcardchars) < 0)
				return new VaultClientTreeObject[] { FindVaultTreeObjectAtReposOrLocalPath(repositoryPath) };
			ValidateReposPath(repositoryPath);
			repositoryPath = repositoryPath.TrimEnd(new char[] {VaultLib.VaultDefine.PathSeparator});
			
			repositoryPath = repositoryPath.Substring(2); //Skip over "$/"
			string[] wildcardsubPaths = repositoryPath.Split(VaultLib.VaultDefine.PathSeparator);
			bool recursive = false;
			
			VaultClientTreeObject baseToSearchFrom = FindVaultTreeObjectAtReposOrLocalPath("$/");
			
			ArrayList possibleParentFoldersFromLastRound = new ArrayList();
			ArrayList possibleParentFoldersFromThisRound = new ArrayList();
			possibleParentFoldersFromLastRound.Add(baseToSearchFrom);
			foreach (string wildcard in wildcardsubPaths)
			{
				if (wildcard == "**")
				{
					recursive = true;
					continue;
				}
				foreach (VaultClientTreeObject possibleParent in possibleParentFoldersFromLastRound)
				{
					if (wildcard.IndexOfAny(wildcardchars) < 0)
					{
						//This is a regular path.
						if (possibleParent is VaultClientFolder)
						{
							VaultClientFolder folder = (VaultClientFolder)possibleParent;
							VaultClientTreeObject[] foundobjs = GetObjectsByName(folder, wildcard, recursive);
							if (foundobjs != null && foundobjs.Length > 0)
								possibleParentFoldersFromThisRound.AddRange(foundobjs);
						}
					}
					else
					{
						ArrayList RegexArray = new ArrayList();
						Wildcard wildcardobj = new Wildcard(wildcard);
						Regex regex = new Regex(wildcardobj.ToRegex(true));
						RegexArray.Add(regex);

						if (possibleParent is VaultClientFolder)
						{
							VaultClientFolder folder = (VaultClientFolder)possibleParent;
							VaultClientFileColl foundFiles = null;
							VaultClientFolderColl foundFolders = null;
							ServerOperations.client.ClientInstance.GetFileAndFolderListsByRegex(folder, RegexArray, recursive, out foundFiles, out foundFolders);
							if (foundFiles != null && foundFiles.Count > 0)
								possibleParentFoldersFromThisRound.AddRange(foundFiles);
							if (foundFolders != null && foundFolders.Count > 0)
								possibleParentFoldersFromThisRound.AddRange(foundFolders);
						}
					}
				}
				
				possibleParentFoldersFromLastRound = (ArrayList)possibleParentFoldersFromThisRound.Clone();
				possibleParentFoldersFromThisRound.Clear();
				recursive = false;
			}
			return (VaultClientTreeObject[])possibleParentFoldersFromLastRound.ToArray(typeof(VaultClientTreeObject));
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="arrayOfVaultFiles"></param>
        /// <returns></returns>
		public static string GetCommonParent(ArrayList arrayOfVaultFiles)
		{
			VaultClientFolder bestParentSoFar = null;
			foreach (VaultClientFile file in arrayOfVaultFiles)
			{
				if (bestParentSoFar == null)
					bestParentSoFar = file.Parent;
				else
				{
					while (bestParentSoFar.FindFileRecursive(file.FullPath) == null)
					{
						bestParentSoFar = bestParentSoFar.Parent;
						if (bestParentSoFar == null)
							return null;
					}
				}
			}
			if (bestParentSoFar != null)
				return bestParentSoFar.FullPath;
			else
				return null;
		}
	}
}
