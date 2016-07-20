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

	Special thanks to Darren Sargent for implementing the -verbose 
	option on the LISTUSERS command
	
*/

using System;
using System.IO;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using MantisLib;

using VaultClientIntegrationLib;
using VaultClientOperationsLib;
using VaultClientNetLib;
using VaultLib;

namespace VaultCmdLineClient
{

	class VaultCmdLineClient
	{
		private System.Xml.XmlWriter _xml = null;
		private Args _args = null;
		public static bool CommandNeedsLogin(Command c)
		{
			bool bRet = true;
			switch(c)
			{
				case Command.CREATEHTMLHELP:
				case Command.HELP:
				case Command.HELPHTML:
				case Command.NONE:
				case Command.INVALID:
				case Command.FORGETLOGIN:
					bRet = false;
					break;
			}
			return bRet;
		}
		public static bool CommandNeedsRepositorySpecified(Command c)
		{
			bool bRet = true;
			switch (c)
			{
				case Command.ADDREPOSITORY:
				case Command.ADDUSER:
				case Command.CREATEHTMLHELP:
				case Command.LISTUSERS:
				case Command.FORGETLOGIN:
				case Command.HELP:
				case Command.HELPHTML:
				case Command.NONE:
				case Command.INVALID:
				case Command.LISTREPOSITORIES:
				case Command.LISTPROJECTS:
				case Command.REMEMBERLOGIN:
					bRet = false;
					break;
			}
			return bRet;
		}
		public static bool CommandNeedsAdmin(Command c)
		{
			bool bRet = false;
			switch (c)
			{
				case Command.ADDREPOSITORY:
				case Command.ADDUSER:
				case Command.LISTUSERS:
				case Command.OBLITERATE:
					bRet = true;
					break;
			}
			return bRet;
		}


		bool ParseBool(string s)
		{
			bool bRet = false;

			switch (s.ToLower().Trim())
			{
				case "yes":
				case "true":
					bRet = true;
					break;
			}
			return bRet;
		}

		static public Option LookupOptionByString(string strOption)
		{
			Option oRet = Option.INVALID;

			foreach (Option o in Enum.GetValues(typeof(Option)))
			{
				// Note that our cmdline options are case-INsensitive.
				// Most UNIX apps have cmdline options which are case-sensitive
				bool bCaseInsensitive = true;
				if (string.Compare(strOption, o.ToString(), bCaseInsensitive) == 0)
				{
					oRet = o;
					break;
				}
			}
			return oRet;
		}

		static public Command LookupCommandByString(string strCmd)
		{
			Command cmdRet = Command.NONE;

			foreach (Command c in Enum.GetValues(typeof(Command)))
			{
				// Note that our cmdline options are case-INsensitive.
				// Most UNIX apps have cmdline options which are case-sensitive
				bool bCaseInsensitive = true;
				if (string.Compare(strCmd, c.ToString(), bCaseInsensitive) == 0)
				{
					cmdRet = c;
					break;
				}
			}
			return cmdRet;
		}
		static public PerformDeletionsType LookupPerformDeletionsOptionByString(string strPerformDeletionsOption)
		{
			PerformDeletionsType pdoRet = PerformDeletionsType.DoNotRemoveWorkingCopy;

			foreach (PerformDeletionsType pdo in Enum.GetValues(typeof(PerformDeletionsType)))
			{
				// Note that our cmdline options are case-INsensitive.
				// Most UNIX apps have cmdline options which are case-sensitive
				bool bCaseInsensitive = true;
				if (string.Compare(strPerformDeletionsOption, pdo.ToString(), bCaseInsensitive) == 0)
				{
					pdoRet = pdo;
					break;
				}
			}
			return pdoRet;
		}
		static public MergeOption LookupMergeOptionByString(string strMergeOption)
		{
			MergeOption moRet = MergeOption.none;

			foreach (MergeOption mo in Enum.GetValues(typeof(MergeOption)))
			{
				// Note that our cmdline options are case-INsensitive.
				// Most UNIX apps have cmdline options which are case-sensitive
				bool bCaseInsensitive = true;
				if (string.Compare(strMergeOption, mo.ToString(), bCaseInsensitive) == 0)
				{
					moRet = mo;
					break;
				}
			}
			return moRet;
		}
		static public FileTimeOption LookupFileTimeOptionByString(string strFileTimeOption)
		{
			FileTimeOption ftoRet = FileTimeOption.none;

			foreach (FileTimeOption fto in Enum.GetValues(typeof(FileTimeOption)))
			{
				// Note that our cmdline options are case-INsensitive.
				// Most UNIX apps have cmdline options which are case-sensitive
				bool bCaseInsensitive = true;
				if (string.Compare(strFileTimeOption, fto.ToString(), bCaseInsensitive) == 0)
				{
					ftoRet = fto;
					break;
				}
			}
			return ftoRet;
		}
		static public CompareToOption LookupCompareToOptionByString(string strCompareToOption)
		{
			CompareToOption ctoRet = CompareToOption.current;

			foreach (CompareToOption cto in Enum.GetValues(typeof(CompareToOption)))
			{
				// Note that our cmdline options are case-INsensitive.
				// Most UNIX apps have cmdline options which are case-sensitive
				bool bCaseInsensitive = true;
				if (string.Compare(strCompareToOption, cto.ToString(), bCaseInsensitive) == 0)
				{
					ctoRet = cto;
					break;
				}
			}
			return ctoRet;
		}
		static public DateSortOption LookupDateSortOptionByString(string strDateSort)
		{
			DateSortOption dsoRet = DateSortOption.desc;

			foreach (DateSortOption dso in Enum.GetValues(typeof(DateSortOption)))
			{
				// Note that our cmdline options are case-INsensitive.
				// Most UNIX apps have cmdline options which are case-sensitive
				bool bCaseInsensitive = true;
				if (string.Compare(strDateSort, dso.ToString(), bCaseInsensitive) == 0)
				{
					dsoRet = dso;
					break;
				}
			}
			return dsoRet;
		}

		void WriteUserMessage(object sender, string message)
		{
			string msg = message.Replace("--", "[dash][dash]");

			if (msg.EndsWith("-")) { msg = msg.Substring(0, msg.Length - 1) + "[dash]"; }

			_xml.WriteComment(msg);
		}
		
		/// <summary>
		/// write out the internal change set
		/// </summary>
		void WriteChangeSet(ChangeSetItemColl csic)
		{
			XmlHelper.XmlOutput(_xml, csic);
		}

		private void DoListUsers(string repository)
			{
			VaultUser[] users = ServerOperations.GetUsers();
			// look up ID for repository specified
			int reposID=-1;
			try 
			{
				reposID = ServerOperations.GetRepositoryId( repository );
			}
			catch
			{
			}
			// sort users before outputting to XML stream
			Array.Sort(users, new UserItemComparer());

			_xml.WriteStartElement("listusers");
			foreach (VaultUser u in users)
			{
				_xml.WriteStartElement("user");
				_xml.WriteElementString("login", u.Login);
				if ( _args.Verbose )
				{
					_xml.WriteElementString("email", u.Email);
					_xml.WriteElementString("active", u.isActive.ToString());

					// groups
					_xml.WriteStartElement("groups");
					VaultGroup[] groups = u.BelongToGroups;

					// sort groups before outputting to XML stream
					Array.Sort(groups, new GroupComparer() );

					foreach ( VaultGroup aGroup in groups )
					{
						_xml.WriteElementString("group", aGroup.Name);
					}
					_xml.WriteEndElement();

					// rights
					_xml.WriteElementString("defaultRights", ServerOperations.DecodeUserRights(u.DefaultRights) );
					_xml.WriteStartElement("folderRights");
					VaultFolderRightsItem[] rights = ServerOperations.GetUsersRights(u.UserID);

					// sort rights before outputting XML stream
					Array.Sort(rights, new RightComparer() );

					if (rights.Length > 0)
					{
						foreach ( VaultFolderRightsItem anItem in rights )
						{
							if ( reposID == -1 || anItem.RepID == reposID )
							{
								_xml.WriteStartElement("singleRight");
								if (reposID == -1)
								{
									_xml.WriteElementString("repositoryID", anItem.RepID.ToString());
								}
								_xml.WriteElementString("folder", anItem.Path);
								_xml.WriteElementString("right", ServerOperations.DecodeUserRights(anItem.FolderRights));
								_xml.WriteEndElement();
							}
						}
					}
					_xml.WriteEndElement();
				}
				_xml.WriteEndElement();
			}
			_xml.WriteEndElement();
		}

		private void x_emitOpItem(string tag, VaultClientFile f)
		{
			_xml.WriteStartElement(tag);
			_xml.WriteElementString("fullpath", f.FullPath);
			_xml.WriteElementString("version", f.Version.ToString());
			_xml.WriteEndElement();
		}

		private void x_emitOpItem(string tag, VaultClientFolder f)
		{
			_xml.WriteStartElement(tag);
			_xml.WriteElementString("fullpath", f.FullPath);
			_xml.WriteElementString("version", f.Version.ToString());
			_xml.WriteEndElement();
		}

		public bool ProcessCommand(Args curArg)
		{
			// assign the new set of arguments
			_args = curArg;

			ClientConnection cc = ServerOperations.client;
			cc.LoginOptions.URL = _args.Url;
			cc.LoginOptions.User = _args.User;
			cc.LoginOptions.Password = _args.Password;
			cc.LoginOptions.Repository = _args.Repository;
			cc.LoginOptions.ProxyDomain = _args.ProxyDomain;
			cc.LoginOptions.ProxyServer = _args.ProxyServer;
			cc.LoginOptions.ProxyPort = _args.ProxyPort;
			cc.LoginOptions.ProxyUser = _args.ProxyUser;
			cc.LoginOptions.ProxyPassword = _args.ProxyPassword;
			cc.Comment = _args.Comment;
			cc.AutoCommit = _args.AutoCommit;
			cc.Verbose = _args.Verbose;

			// get access level req'd by the command
			VaultConnection.AccessLevelType altCommand = (CommandNeedsAdmin(_args.Cmd) == false) ? VaultConnection.AccessLevelType.Client : VaultConnection.AccessLevelType.Admin;
			cc.LoginOptions.AccessLevel = altCommand;
			if (CommandNeedsLogin(_args.Cmd))
				ServerOperations.Login();
			if (CommandNeedsRepositorySpecified(_args.Cmd) && cc.ClientInstance.ActiveRepositoryID == -1)
				throw new UsageException(string.Format("You must specify a repository for the {0} command", _args.Cmd));
			try
			{
				switch (curArg.Cmd)
				{
					case Command.ADD:
						#region
						{
							// The first item must be the repository folder
							// All other items are local paths to be added
							if (curArg.items.Count < 2)
							{
								throw new UsageException("usage: ADD repository_folder path_to_add [...]");
							}

							string strReposFolder = (string)curArg.items[0];

							ArrayList strItemArray = new ArrayList();
							for (int i = 1; i < curArg.items.Count; i++)
							{
								strItemArray.Add(curArg.items[i]);
							}

							ServerOperations.ProcessCommandAdd(strReposFolder, (string[])strItemArray.ToArray(typeof(string)));
							break;
						}
						#endregion
					case Command.ADDREPOSITORY:
						#region
						{
							if (curArg.items.Count != 1)
							{
								throw new UsageException("usage: ADDREPOSITORY repository_name");
							}

							string strNewReposName = (string)curArg.items[0];

							ServerOperations.ProcessCommandAddRepository(strNewReposName, true);
							break;
						}
						#endregion
					case Command.ADDUSER:
						#region
						{
							if ((curArg.items.Count == 0) || (curArg.items.Count > 3))
							{
								throw new UsageException("usage: ADDUSER login [password] [email]");
							}

							string strLogin = (string)curArg.items[0];

							string strPassword = string.Empty;
							if (curArg.items.Count > 1)
							{
								strPassword = (string)curArg.items[1];
							}

							string strEmail = null;
							if (curArg.items.Count == 3)
							{
								strEmail = (string)curArg.items[2];
							}

							ServerOperations.ProcessCommandAddUser(strLogin, strPassword, strEmail);
							DoListUsers(_args.Repository);

							break;
						}
						#endregion
					case Command.BATCH:
						#region
						{
							throw new Exception(string.Format("{0} cannot be called as a batch command", curArg.Cmd));
						}
						#endregion
					case Command.BLAME:
						#region
						{
							if (curArg.items.Count != 2 && curArg.items.Count != 3)
							{
								throw new UsageException("usage: BLAME path linenumber [endversion]");
							}
							string strReposPath = (string)curArg.items[0];

							int linenumber = int.Parse((string)curArg.items[1]);
							int endversion = -1;
							if (curArg.items.Count == 3)
								endversion = int.Parse((string)curArg.items[2]);

							VaultBlameRegionResponse bn = ServerOperations.ProcessCommandBlame(strReposPath, endversion, linenumber, linenumber);
							XmlHelper.XmlOutput(_xml, bn);
							break;
						}
						#endregion
					case Command.BRANCH:
						#region
						{
							if (curArg.items.Count != 2)
							{
								throw new UsageException("usage: BRANCH from_path to_path");
							}

							string strReposPath_From = (string)curArg.items[0];

							string strReposPath_To = (string)curArg.items[1];

							ServerOperations.ProcessCommandBranch(strReposPath_From, strReposPath_To);
							break;
						}
						#endregion
					case Command.CHECKOUT:
						#region
						{
							if (curArg.items.Count < 1)
							{
								throw new UsageException("usage: CHECKOUT item [...]");
							}

							ArrayList strItemArray = new ArrayList();
							string curItem = null;
							foreach (string strReposItem in curArg.items)
							{
								curItem = strReposItem;
								if (curArg.Wildcard != null && curArg.Wildcard.Length > 0)
								{
									if (!curItem.EndsWith("/"))
										curItem = curItem + "/";
									if (curArg.Recursive == true)
										curItem = curItem + "**/";
									curItem = curItem + curArg.Wildcard;
								}
								strItemArray.Add(curItem);
							}

							bool originalBackupOption = false;
							bool resetMakeBackupOption = setMakeBackupOption(_args.MakeBackup, out originalBackupOption);

							try
							{
								GetOptions go = new GetOptions();
								go.Merge = _args.Merge;
								go.Recursive = _args.Recursive;
								go.MakeWritable = _args.MakeWritable;
								go.SetFileTime = _args.SetFileTime;
								go.PerformDeletions = _args.PerformDeletions;
								go.OverrideEOL = _args.OverrideEOL;
								ServerOperations.ProcessCommandCheckout((string[])strItemArray.ToArray(typeof(string)), _args.CheckOutExclusive, true, go);
							}
							finally
							{
								restoreMakeBackupOption(_args.MakeBackup, originalBackupOption);
							}
							break;
						}
						#endregion
					case Command.CLOAK:
						#region
						{
							if (curArg.items.Count < 1)
							{
								throw new UsageException("usage: CLOAK item [...]");
							}

							ArrayList strItemArray = new ArrayList();
							foreach (string strReposItem in curArg.items)
							{
								strItemArray.Add(strReposItem);
							}

							ServerOperations.ProcessCommandCloak((string[])strItemArray.ToArray(typeof(string)));
							break;
						}
						#endregion
					case Command.COMMIT:
					case Command.CHECKIN:
						#region
						{
							ArrayList strItemArray = new ArrayList();
							foreach (string strReposItem in curArg.items)
							{
								strItemArray.Add(strReposItem);
							}

							ChangeSetItemColl csicRemove = null;
							ServerOperations.ProcessCommandCommit((string[])strItemArray.ToArray(typeof(string)), _args.Unchanged, _args.KeepCheckedOut, _args.LocalCopy, _args.ResolveMerge, out csicRemove);

							if ((_args.Verbose == true) && (csicRemove != null) && (csicRemove.Count > 0))
							{
								// if in verbose mode, write out any skipped change set items and the reason
								System.Xml.XmlTextWriter xmlOmit = new System.Xml.XmlTextWriter(_args.Out);

								xmlOmit.WriteStartElement("omitted changeset items");
								XmlHelper.XmlOutput(xmlOmit, csicRemove, false, true);
								xmlOmit.WriteEndElement();
							}
							break;
						}
						#endregion
					case Command.CREATEHTMLHELP:
						#region
						{
							Help help = new Help(_xml);
							help.WriteHTML(false);
							break;
						}
						#endregion
					case Command.CREATEFOLDER:
						#region
						{
							if (curArg.items.Count != 1)
							{
								throw new UsageException("usage: CREATEFOLDER foldername");
							}

							string strReposFolder = (string)curArg.items[0];

							ServerOperations.ProcessCommandCreateFolder(strReposFolder);
							break;
						}
						#endregion
					case Command.DELETE:
						#region
						{
							if (curArg.items.Count < 1)
							{
								throw new UsageException("usage: DELETE item [...]");
							}

							ArrayList strItemArray = new ArrayList();
							foreach (string strReposItem in curArg.items)
							{
								strItemArray.Add(strReposItem);
							}

							ServerOperations.ProcessCommandDelete((string[])strItemArray.ToArray(typeof(string)));
							break;
						}
						#endregion
					case Command.DELETELABEL:
						#region
						{
							if (curArg.items.Count != 2)
							{
								throw new UsageException("usage: DELETELABEL item label_name");
							}

							string strReposPath = RepositoryPath.NormalizeFolder((string)curArg.items[0]);

							string strLabelName = (string)curArg.items[1];

							ServerOperations.ProcessCommandDeleteLabel(strReposPath, strLabelName);
							break;
						}
						#endregion
					case Command.DIFF:
						#region
						{
							CompareToOption cto = VaultCmdLineClient.LookupCompareToOptionByString(curArg.DiffCompareTo);

							ServerOperations.ProcessCommandDiff(curArg.DiffBin, curArg.DiffArgs, cto,
								curArg.Recursive, (string)curArg.items[0], curArg.items.Count > 1 ? (string)curArg.items[1] : null);
							break;
						}
						#endregion

					case Command.FINDINFILES:
						#region
						{
							if (curArg.items.Count < 2)
							{
								throw new UsageException("usage: FINDINFILES searchstring repositoryfilepath repositoryfilepath ... repositoryfilepath");
							}

							string strSearchString = (string)curArg.items[0];
							
							string[] arFiles = new string[curArg.items.Count - 1];
							for (int i = 0; i < arFiles.Length; i++)
							{
								arFiles[i] = RepositoryPath.CleanPath((string)curArg.items[i + 1]);
							}

							FindInFilesData[] fifData = ServerOperations.ProcessCommandFindInFilesByFileList(strSearchString, arFiles, _args.MatchCase, _args.MatchWord, _args.patternMatch);
							XmlHelper.XmlOutput(_xml, fifData);

							break;
						}
						#endregion


					case Command.FINDINFOLDER:
						#region
						{
							if (curArg.items.Count != 2)
							{
								throw new UsageException("usage: FINDINFOLDER searchstring repositorypath");
							}

							string strSearchString = (string)curArg.items[0];
							string strReposPath = RepositoryPath.NormalizeFolder((string)curArg.items[1]);

							string[] arIncludeFiles = null, arExcludeFiles = null;
							if (string.IsNullOrEmpty(_args.IncludeFiles) == false)
							{
								// if a file name contains a "," or ";", it must be escaped in the string.
								bool bContainsEscapeChars = ((_args.IncludeFiles.IndexOf("\\,") >= 0) || (_args.IncludeFiles.IndexOf("\\;") >= 0));
								if (bContainsEscapeChars == false)
								{
									arIncludeFiles = _args.IncludeFiles.Split(new char[] { ',', ';' });
								}
								else
								{
									string strNew = _args.IncludeFiles.Replace("\\,", "\r");
									strNew = strNew.Replace("\\;", "\n");

									arIncludeFiles = strNew.Split(new char[] { ',', ';' });

									// replace any ; or , in the file name
									for (int i = 0; i < arIncludeFiles.Length; i++)
									{
										if (arIncludeFiles[i].IndexOf('\n') >= 0)
										{
											arIncludeFiles[i] = arIncludeFiles[i].Replace("\n", ";");
										}
										if (arIncludeFiles[i].IndexOf('\r') >= 0)
										{
											arIncludeFiles[i] = arIncludeFiles[i].Replace("\r", ",");
										}
									}
								}
							}

							if (string.IsNullOrEmpty(_args.ExcludeFiles) == false)
							{
								// if a file name contains a "," or ";", it must be escaped in the string.
								bool bContainsEscapeChars = ((_args.ExcludeFiles.IndexOf("\\,") >= 0) || (_args.ExcludeFiles.IndexOf("\\;") >= 0));
								if (bContainsEscapeChars == false)
								{
									arExcludeFiles = _args.ExcludeFiles.Split(new char[] { ',', ';' });
								}
								else
								{
									string strNew = _args.ExcludeFiles.Replace("\\,", "\r");
									strNew = strNew.Replace("\\;", "\n");

									arExcludeFiles = strNew.Split(new char[] { ',', ';' });

									// replace any ; or , in the file name
									for (int i = 0; i < arExcludeFiles.Length; i++)
									{
										if (arExcludeFiles[i].IndexOf('\n') >= 0)
										{
											arExcludeFiles[i] = arExcludeFiles[i].Replace("\n", ";");
										}
										if (arExcludeFiles[i].IndexOf('\r') >= 0)
										{
											arExcludeFiles[i] = arExcludeFiles[i].Replace("\r", ",");
										}
									}
								}
							}
							FindInFilesData[] fifData = ServerOperations.ProcessCommandFindInFilesByFolder(strSearchString, strReposPath, _args.Recursive, arIncludeFiles, arExcludeFiles, _args.MatchCase, _args.MatchWord, _args.patternMatch);
							XmlHelper.XmlOutput(_xml, fifData);

							break;
						}
						#endregion

					case Command.FORGETLOGIN:
						#region
						{
							if (curArg.items.Count != 0)
							{
								throw new UsageException("usage: FORGETLOGIN");
							}

							ServerOperations.PurgeSession();
							break;
						}
						#endregion
					case Command.GET:
						#region
						{
							if (curArg.items.Count < 1)
							{
								throw new UsageException("usage: GET item [...]");
							}

							ArrayList strItemArray = new ArrayList();
							foreach (string strReposItem in curArg.items)
							{
								strItemArray.Add(strReposItem);
							}

							if (_args.DestPath != null && _args.WorkingFolder != null)
								throw new UsageException("The -nonworkingfolder and -workingfolder options are mutually exclusive: only one may be used.");
							if (_args.WorkingFolder != null && strItemArray.Count > 1)
								throw new UsageException("When specifying the -workingfolder option, only one repository item can be retrieved.");

							VaultNameValueCollection cloaks = null;
							bool bResetCloaks = clearCloaksIfNecessary(_args.RespectCloaks, out cloaks);

							bool originalBackupOption = false;
							bool resetMakeBackupOption = setMakeBackupOption(_args.MakeBackup, out originalBackupOption);

							string originalWorkingFolder = null;
							string repositoryPath = null;
							bool resetWorkingFolder = setTempWorkingFolderIfNecessary(_args.WorkingFolder != null, (string)strItemArray[0], _args.WorkingFolder, out repositoryPath, out originalWorkingFolder);

							// Do as much as possible within a try..finally to ensure the 
							// temporary cloak/backup/working folder changes we make get 
							// reset even if there's an error.
							try
							{
								GetOptions go = new GetOptions();
								go.Merge = _args.Merge;
								go.Recursive = _args.Recursive;
								go.MakeWritable = _args.MakeWritable;
								go.SetFileTime = _args.SetFileTime;
								go.PerformDeletions = _args.PerformDeletions;
								go.OverrideEOL = _args.OverrideEOL;
								if (_args.DestPath != null)
									GetOperations.ProcessCommandGetToLocationOutsideWorkingFolder((string[])strItemArray.ToArray(typeof(string)), go, _args.DestPath);
								else
									GetOperations.ProcessCommandGet((string[])strItemArray.ToArray(typeof(string)), go);
							}
							finally
							{
								restoreCloaksIfNecessary(bResetCloaks, cloaks);
								restoreMakeBackupOption(_args.MakeBackup, originalBackupOption);
								restoreWorkingFolderIfNecessary(resetWorkingFolder, repositoryPath, originalWorkingFolder);
							}
							break;
						}
						#endregion
					case Command.GETLABEL:
						#region
						{
							if (curArg.items.Count < 2 || curArg.items.Count > 3)
							{
								throw new UsageException("usage: GETLABEL repository_item label [labelpath]");
							}

							string strReposItem = (string)curArg.items[0];

							string strLabel = (string)curArg.items[1];

							string strLabelPath = (curArg.items.Count == 3) ? (string)curArg.items[2] : null;

							if (_args.DestPath == null && _args.LabelWorkingFolder == null)
							{
								throw new UsageException("usage: GETLABEL requires either -nonworkingfolder or -labelworkingfolder to be set");
							}

							if (_args.DestPath != null && _args.LabelWorkingFolder != null)
							{
								throw new UsageException("usage: -nonworkingfolder and -labelworkingfolder are mutually exclusive for the GETLABEL command.");
							}

							bool originalBackupOption = false;
							bool resetMakeBackupOption = setMakeBackupOption(_args.MakeBackup, out originalBackupOption);
							try
							{
								GetOptions go = new GetOptions();
								go.Merge = _args.Merge;
								go.Recursive = _args.Recursive;
								go.MakeWritable = _args.MakeWritable;
								go.SetFileTime = _args.SetFileTime;
								go.PerformDeletions = _args.PerformDeletions;
								go.OverrideEOL = _args.OverrideEOL;
								if (_args.DestPath != null)
									GetOperations.ProcessCommandGetLabelToLocationOutsideWorkingFolder(strReposItem, strLabel, strLabelPath, go, _args.DestPath);
								else
									GetOperations.ProcessCommandGetLabelToTempWorkingFolder(strReposItem, strLabel, strLabelPath, go, _args.LabelWorkingFolder);

							}
							finally
							{
								restoreMakeBackupOption(_args.MakeBackup, originalBackupOption);
							}
							break;
						}
						#endregion
					case Command.GETLABELDIFFS:
						#region
						{
							throw new UsageException("This command has been deprecated.  Use HISTORY instead.");
						}
						#endregion
					case Command.GETVERSION:
						#region
						{
							const string usageMsg = "usage: GETVERSION version item [destination_folder]";
							string strDestFolder = null;
							if (curArg.items.Count == 2)
							{
								// this is okay, we use the user's working folder
							}
							else if (curArg.items.Count == 3)
							{
								strDestFolder = (string)curArg.items[2];
							}
							else
								throw new UsageException(usageMsg);

							int version = 0;
							try
							{
								version = Int32.Parse((string)curArg.items[0]);
							}
							catch (FormatException)
							{
								throw new UsageException(usageMsg);
							}
							string strReposItem = (string)curArg.items[1];

							VaultNameValueCollection cloaks = null;
							bool bResetCloaks = clearCloaksIfNecessary(_args.RespectCloaks, out cloaks);

							bool originalBackupOption = false;
							bool resetMakeBackupOption = setMakeBackupOption(_args.MakeBackup, out originalBackupOption);

							string originalWorkingFolder = null;
							string repositoryPath = null;
							bool resetWorkingFolder = setTempWorkingFolderIfNecessary(_args.useWorkingFolder, strReposItem, strDestFolder, out repositoryPath, out originalWorkingFolder);

							try
							{
								GetOptions go = new GetOptions();
								go.Merge = _args.Merge;
								go.Recursive = _args.Recursive;
								go.MakeWritable = _args.MakeWritable;
								go.SetFileTime = _args.SetFileTime;
								go.PerformDeletions = _args.PerformDeletions;
								go.OverrideEOL = _args.OverrideEOL;
								if (_args.useWorkingFolder)
									GetOperations.ProcessCommandGetVersion(strReposItem, version, go);
								else
									GetOperations.ProcessCommandGetVersionToLocationOutsideWorkingFolder(strReposItem, version, go, strDestFolder);
							}
							finally
							{
								restoreCloaksIfNecessary(bResetCloaks, cloaks);
								restoreMakeBackupOption(_args.MakeBackup, originalBackupOption);
								restoreWorkingFolderIfNecessary(resetWorkingFolder, repositoryPath, originalWorkingFolder);
							}
							break;
						}
						#endregion
					case Command.GETWILDCARD:
						#region
						{
							if (curArg.items.Count < 2)
							{
								throw new UsageException("usage: GETWILDCARD repospath wildcard [...]");
							}

							string strReposItem = (string)curArg.items[0];

							ArrayList strWildcardArray = new ArrayList();
							for (int i = 1; i < curArg.items.Count; i++)
							{
								strWildcardArray.Add((string)curArg.items[i]);
							}

							VaultNameValueCollection cloaks = null;
							bool bResetCloaks = clearCloaksIfNecessary(_args.RespectCloaks, out cloaks);

							try
							{
								GetOptions go = new GetOptions();
								go.Merge = _args.Merge;
								go.Recursive = _args.Recursive;
								go.MakeWritable = _args.MakeWritable;
								go.SetFileTime = _args.SetFileTime;
								go.PerformDeletions = _args.PerformDeletions;
								go.OverrideEOL = _args.OverrideEOL;
								if (_args.DestPath != null)
									GetOperations.ProcessCommandGetWildcardToNonWorkingFolder(strReposItem, (string[])strWildcardArray.ToArray(typeof(string)), go, _args.DestPath);
								else
									GetOperations.ProcessCommandGetWildcard(strReposItem, (string[])strWildcardArray.ToArray(typeof(string)), go);
							}
							finally
							{
								restoreCloaksIfNecessary(bResetCloaks, cloaks);
							}

							break;
						}
						#endregion
					case Command.HELPHTML:
						#region
						{
							Help help = new Help(_xml);
							help.WriteHTML(true);
							break;
						}
						#endregion
					case Command.HELP:
						#region
						{
							Help help = new Help(_xml);

							if (curArg.items.Count < 1)
								help.Write();
							else
								help.Write((string)curArg.items[0]);

							break;
						}
						#endregion
					case Command.HISTORY:
						#region
						{
							if (curArg.items.Count != 1)
							{
								throw new UsageException("usage: HISTORY repository_path");
							}

							// cannot contain BOTH include / exclude users
							if ((string.IsNullOrEmpty(_args.HistoryExcludedUsers) == false) && (string.IsNullOrEmpty(_args.HistoryIncludedUsers) == false))
							{
								throw new UsageException("The -excludeusers and -includeusers options are mutually exclusive: only one may be used.");
							}

							// determine how to filter users.
							char chFilterUsers = 'X';
							string strFilterUsers = null;
							if (string.IsNullOrEmpty(_args.HistoryIncludedUsers) == false)
							{
								chFilterUsers = 'I';
								strFilterUsers = _args.HistoryIncludedUsers;
							}
							else if (string.IsNullOrEmpty(_args.HistoryExcludedUsers) == false)
							{
								chFilterUsers = 'E';
								strFilterUsers = _args.HistoryExcludedUsers;
							}

							// cannot contain BOTH include / exclude actions
							if ((string.IsNullOrEmpty(_args.HistoryExcludedActions) == false) && (string.IsNullOrEmpty(_args.HistoryIncludedActions) == false))
							{
								throw new UsageException("The -excludeactions and -includeactions options are mutually exclusive: only one may be used.");
							}
							// determine how to filter actions
							char chFilterActions = 'X';
							string strFilterActions = null;
							if (string.IsNullOrEmpty(_args.HistoryIncludedActions) == false)
							{
								chFilterActions = 'I';
								strFilterActions = _args.HistoryIncludedActions;
							}
							else if (string.IsNullOrEmpty(_args.HistoryExcludedActions) == false)
							{
								chFilterActions = 'E';
								strFilterActions = _args.HistoryExcludedActions;
							}

							// the repo path
							string strReposPath = (string)curArg.items[0];

							// run the query.
							VaultHistoryItem[] histitems = ServerOperations.ProcessCommandHistoryEx(strReposPath, _args.Recursive, _args.DateSort, chFilterUsers, strFilterUsers, chFilterActions, strFilterActions, _args.HistoryBeginDate.ToString(), _args.HistoryEndDate.ToString(), _args.HistoryBeginLabel, _args.HistoryEndLabel, _args.VersionHistoryBeginVersion, _args.HistoryEndVersion, _args.HistoryRowLimit, null);
							XmlHelper.XmlOutput(_xml, histitems);
							break;
						}
						#endregion
					case Command.LABEL:
						#region
						{
							const string usageErrMsg = "usage: LABEL repositorypath labelname [version]";
							if (curArg.items.Count != 2 && curArg.items.Count != 3)
							{
								throw new UsageException(usageErrMsg);
							}

							string strReposPath = (string)curArg.items[0];

							string labelName = (string)curArg.items[1];

							long version = VaultDefine.Latest;
							if (curArg.items.Count == 3)
							{
								try
								{
									version = long.Parse((string)curArg.items[2]);
								}
								catch (FormatException)
								{
									throw new UsageException(usageErrMsg);
								}
							}

							ServerOperations.ProcessCommandLabel(strReposPath, labelName, version);
							break;
						}
						#endregion
					case Command.LISTCHANGESET:
						#region
						{
							ArrayList strItemArray = new ArrayList();
							foreach (string strReposItem in curArg.items)
							{
								strItemArray.Add(strReposItem);
							}

							ChangeSetItemColl csic = ServerOperations.ProcessCommandListChangeSet((string[])strItemArray.ToArray(typeof(string)));
							XmlHelper.XmlOutput(_xml, csic, true, _args.Verbose);
							break;
						}
						#endregion
					case Command.LISTCHECKOUTS:
						#region
						{
							if (curArg.items.Count != 0)
							{
								throw new UsageException("usage: LISTCHECKOUTS");
							}

							VaultClientCheckOutList checkouts = ServerOperations.ProcessCommandListCheckOuts();
							XmlHelper.XmlOutput(_xml, checkouts);
							break;
						}
						#endregion
					case Command.LISTFOLDER:
						#region
						{
							if (curArg.items.Count != 1)
							{
								throw new UsageException("usage: LISTFOLDER repository_folder");
							}

							string strReposFolder = (string)curArg.items[0];

							RepositoryUtil.Refresh();

							VaultClientFolder vcfolder = ServerOperations.ProcessCommandListFolder(strReposFolder, _args.Recursive);
							XmlHelper.XmlOutput(_xml, vcfolder);
							break;
						}
						#endregion
					case Command.LISTOBJECTPROPERTIES:
						#region
						{
							if (curArg.items.Count != 1)
							{
								throw new UsageException("usage: LISTOBJECTPROPERTIES repositorypath");
							}

							string strReposFolder = (string)curArg.items[0];

							RepositoryUtil.Refresh();

							VaultClientTreeObject vcfolder = ServerOperations.ProcessCommandListObjectProperties(strReposFolder);
							XmlHelper.XmlOutput(_xml, vcfolder);
							break;
						}
						#endregion
					case Command.LISTPROJECTS:
						#region
						{
							if (curArg.items.Count != 0)
							{
								throw new UsageException("usage: LISTPROJECTS");
							}

							MantisProject[] projs = ItemTrackingOperations.ProcessCommandListFortressProjects();
							XmlHelper.XmlOutput(_xml, projs);
							break;
						}
						#endregion
					case Command.LISTREPOSITORIES:
						#region
						{
							if (curArg.items.Count != 0)
							{
								throw new UsageException("usage: LISTREPOSITORIES");
							}

							VaultRepositoryInfo[] reps = ServerOperations.ProcessCommandListRepositories();
							XmlHelper.XmlOutput(_xml, reps);
							break;
						}
						#endregion
					case Command.LISTUSERS:
						#region
						{
							if (curArg.items.Count != 0)
							{
								throw new UsageException("usage: LISTUSERS");
							}
							DoListUsers(_args.Repository);

							break;
						}
						#endregion
					case Command.LISTWORKINGFOLDERS:
						#region
						{
							if (curArg.items.Count != 0)
							{
								throw new UsageException("usage: LISTWORKINGFOLDERS");
							}

							SortedList list = ServerOperations.GetWorkingFolderAssignments();

							XmlHelper.XmlOutput(_xml, list);

							break;
						}
						#endregion
					case Command.MOVE:
						#region
						{
							if (curArg.items.Count != 2)
							{
								throw new UsageException("usage: MOVE path_from path_to");
							}

							string strReposPath_From = (string)curArg.items[0];

							string strReposPath_To = (string)curArg.items[1];

							ServerOperations.ProcessCommandMove(strReposPath_From, strReposPath_To);
							break;
						}
						#endregion
					case Command.OBLITERATE:
						#region
						{
							if (curArg.items.Count != 1)
							{
								throw new UsageException("usage: OBLITERATE path_to_deleted_item");
							}

							string strReposPath = (string)curArg.items[0];

							if (_args.YesIAmSure == false)
							{
								_xml.WriteComment("You have not provided the -yesiamsure option to the OBLITERATE command.  \nOBLITERATE is a destructive and non-reversible command, which should not be \nused lightly.  If you are still sure that you would like to permanently \ndestroy " + strReposPath + " and all of its children, \nthen add the -yesiamsure flag to your command.");
								break;
							}

							ServerOperations.ProcessCommandObliterate(strReposPath);
							break;
						}
						#endregion
					case Command.REMEMBERLOGIN:
						#region
						{
							if (curArg.items.Count != 0)
							{
								throw new UsageException("This command accepts no arguments.");
							}

							ServerOperations.Login(VaultConnection.AccessLevelType.Client, false, true);
							break;
						}
						#endregion
					case Command.PIN:
						#region
						{
							if (curArg.items.Count < 1 || curArg.items.Count > 2)
							{
								throw new UsageException("usage: PIN repository_path [version]");
							}

							string strReposPath = RepositoryPath.NormalizeFolder((string)curArg.items[0]);

							int version = 0;
							if (curArg.items.Count == 2)
							{
								string strVersion = (string)curArg.items[1];
								version = int.Parse(strVersion);
							}
							else
							{
								version = VaultDefine.Latest;
							}

							ServerOperations.ProcessCommandPin(strReposPath, version);
							break;
						}
						#endregion
					case Command.RENAME:
						#region
						{
							if (curArg.items.Count != 2)
							{
								throw new UsageException("usage: RENAME from_name to_name");
							}

							string strReposPath = RepositoryPath.NormalizeFolder((string)curArg.items[0]);

							string strNewName = (string)curArg.items[1];

							ServerOperations.ProcessCommandRename(strReposPath, strNewName);
							break;
						}
						#endregion
					case Command.RENAMELABEL:
						#region
						{
							if (curArg.items.Count != 3)
							{
								throw new UsageException("usage: RENAMELABEL item from_label_name to_label_name");
							}

							string strReposPath = RepositoryPath.NormalizeFolder((string)curArg.items[0]);

							string strOldLabelName = (string)curArg.items[1];
							string strNewLabelName = (string)curArg.items[2];

							ServerOperations.ProcessCommandRenameLabel(strReposPath, strOldLabelName, strNewLabelName);
							break;
						}
						#endregion
					case Command.SETWORKINGFOLDER:
						#region
						{
							if (curArg.items.Count != 2)
							{
								throw new UsageException("usage: SETWORKINGFOLDER repository_folder local_folder");
							}

							ServerOperations.SetWorkingFolder((string)curArg.items[0], (string)curArg.items[1], true, curArg.ForceSubfoldersToInherit);

							SortedList list = ServerOperations.GetWorkingFolderAssignments();

							XmlHelper.XmlOutput(_xml, list);

							break;
						}
						#endregion
					case Command.SHARE:
						#region
						{
							if (curArg.items.Count != 2)
							{
								throw new UsageException("usage: SHARE repository_path_from repository_path_to");
							}

							string strReposPath_From = (string)curArg.items[0];

							string strReposPath_To = (string)curArg.items[1];

							ServerOperations.ProcessCommandShare(strReposPath_From, strReposPath_To);
							break;
						}
						#endregion
					case Command.UNCLOAK:
						#region
						{
							if (curArg.items.Count < 1)
							{
								throw new UsageException("usage: CLOAK item [...]");
							}

							ArrayList strItemArray = new ArrayList();
							foreach (string strReposItem in curArg.items)
							{
								strItemArray.Add(strReposItem);
							}

							ServerOperations.ProcessCommandUncloak((string[])strItemArray.ToArray(typeof(string)));
							break;
						}
						#endregion
					case Command.UNDOCHANGESETITEM:
						#region
						{
							if (curArg.items.Count != 1)
							{
								throw new UsageException("usage: UNDOCHANGESETITEM changesetitem_id");
							}

							int nChgSetItemID = -1;
							try
							{
								nChgSetItemID = Convert.ToInt32(curArg.items[0]);
							}
							catch
							{
								nChgSetItemID = -1;
							}

							ServerOperations.ProcessCommandUndoChangeSetItem(nChgSetItemID);
							ChangeSetItemColl csic = ServerOperations.ProcessCommandListChangeSet(null);
							XmlHelper.XmlOutput(_xml, csic);
							break;
						}
						#endregion
					case Command.UNDOCHECKOUT:
						#region
						{
							if (curArg.items.Count < 1)
							{
								throw new UsageException("usage: UNDOCHECKOUT repository_path");
							}

							ArrayList strItemArray = new ArrayList();
							foreach (string strReposItem in curArg.items)
							{
								strItemArray.Add(strReposItem);
							}


							bool originalBackupOption = false;
							bool resetMakeBackupOption = setMakeBackupOption(_args.MakeBackup, out originalBackupOption);

							try
							{
								ServerOperations.ProcessCommandUndoCheckout((string[])strItemArray.ToArray(typeof(string)), _args.Recursive, _args.LocalCopy);
							}
							finally
							{
								restoreMakeBackupOption(_args.MakeBackup, originalBackupOption);
							}
							break;
						}
						#endregion
					case Command.UNPIN:
						#region
						{
							if (curArg.items.Count != 1)
							{
								throw new UsageException("usage: UNPIN repository_path");
							}

							string strReposPath = RepositoryPath.NormalizeFolder((string)curArg.items[0]);

							ServerOperations.ProcessCommandUnPin(strReposPath);
							break;
						}
						#endregion
					case Command.UNSETWORKINGFOLDER:
						#region
						{
							if (curArg.items.Count != 1)
							{
								throw new UsageException("The UNSETWORKINGFOLDER command requires 1 argument.");
							}

							string strReposFolder = (string)curArg.items[0];
							ServerOperations.RemoveWorkingFolder(strReposFolder, curArg.RecursiveUnset);

							SortedList list = ServerOperations.GetWorkingFolderAssignments();

							XmlHelper.XmlOutput(_xml, list);

							break;
						}
						#endregion
					case Command.VERSIONHISTORY:
						#region
						{
							if (curArg.items.Count != 1)
							{
								throw new UsageException("usage: VERSIONHISTORY repository_folder");
							}

							string strReposPath = (string)curArg.items[0];

							VaultTxHistoryItem[] histitems = ServerOperations.ProcessCommandVersionHistory(strReposPath, _args.VersionHistoryBeginVersion, _args.HistoryBeginDate, _args.HistoryEndDate, _args.HistoryRowLimit);
							XmlHelper.XmlOutput(_xml, histitems);
							break;
						}
						#endregion
					default:
						{
							throw new UsageException("no command specified - run 'vault.exe HELP' for help");
							//break;
						}
				}
			}
			catch (Exception)
		{
				// force a logout if an error occurred.
				ServerOperations.Logout();
				throw;
		}

			return true;
		}

		public bool PreProcessCommand(Args arg)
		{
			bool bSuccess = true;
			if (arg.Cmd == Command.BATCH)
			{
				if (arg.items.Count == 1)
			{
					ProcessCommandBatch(arg);
				}
				else
				{
					arg.Error = true;
					arg.ErrorMessage = string.Format("usage: {0} file name | -", arg.Cmd.ToString());
			}

				bSuccess = (arg.Error == false);
			}
			return bSuccess;
		}

		void ProcessCommandBatch(Args arg)
		{
			arg.SetBatchModeOperation((string)arg.items[0]);
		}

		public VaultCmdLineClient(Args args, System.Xml.XmlWriter xml)
		{
			_args = args;
			_xml = xml;
			ServerOperations.GetInstance().ChangesetOutput += new ServerOperations.ChangesetOutputEventHandler(WriteChangeSet);
			ServerOperations.GetInstance().UserMessage += new ServerOperations.UserMessageEventHandler(WriteUserMessage);
		}


		[STAThread]
		static int Main(string[] args)
		{
			SimpleLogger.Log.ConfigureFromAppSettings("VaultCLC");

			int nRetCode = 0;
			bool bOK = false;

			// create the args.
			Args cmdlineargs = new Args(args);

			// create the output writer - based on the OUT option
			System.Xml.XmlTextWriter xml = new System.Xml.XmlTextWriter(cmdlineargs.Out);
			xml.Formatting = Formatting.Indented;

			// create the cmd line client object
			VaultCmdLineClient cmdlineclient = new VaultCmdLineClient(cmdlineargs, xml);

			// pre process check on the original command
			if ( cmdlineargs.Error == false )
			{
				cmdlineclient.PreProcessCommand(cmdlineargs);
			}

			xml.WriteStartElement("vault");
			if(cmdlineargs.Error)
			{
				xml.WriteElementString("error", cmdlineargs.ErrorMessage);
			}
			else
			{
				try
				{
					// if there is a batch, read each line until no more commands
					if ( cmdlineargs.InBatchMode == false )
					{
						// process the one command.
						bOK = cmdlineclient.ProcessCommand(cmdlineargs);
					}
					else
					{
						// while there are commands on the batch input stream
						// keep processing.
						string strCmd = null;
						while ( null != (strCmd = cmdlineargs.BatchTextReader.ReadLine()) )
						{
							if ( strCmd.Length > 0 )
							{
								// merge line opts with original cmd line opts
								Args batch_args = cmdlineargs.CreateBatchCmdArgs();
								batch_args.ParseBatchCommand(strCmd);
								if (batch_args.Error)
								{
									xml.WriteElementString("error", batch_args.ErrorMessage);
									bOK = false;
									break;
								}

								// parse and process the batch command
								bOK = cmdlineclient.ProcessCommand(batch_args);
								if ( bOK == false )
								{
									break;
								}
							}
						}
					}
				}
				catch (UsageException e)
				{
					xml.WriteStartElement("error");
                    xml.WriteString(e.Message);
                    xml.WriteEndElement();
					bOK = false;
				}
				catch (Exception e)
				{
					xml.WriteStartElement("error");
					xml.WriteStartElement("exception");
					xml.WriteString(e.ToString());
//					if (e.InnerException != null)
//					{
//						xml.WriteStartElement("innerexception");
//						xml.WriteContent(e.InnerException.ToString());
//						xml.WriteEndElement();
//					}
					xml.WriteEndElement();
					xml.WriteEndElement();
					bOK = false;
				}
				finally
				{
					// always force a logout here.
					ServerOperations.Logout();
				}
			}

			xml.WriteStartElement("result");
			xml.WriteElementString("success", bOK.ToString());
			xml.WriteEndElement();

			xml.WriteEndElement();

			// clean up any left over streams
			cmdlineargs.CloseOutputStream();
			cmdlineargs.CloseInputBatchStream();

			if ( bOK == false )
			{
				nRetCode = -1;
			}
			return nRetCode;
		}

		public bool clearCloaksIfNecessary(bool respectCloaks, out VaultNameValueCollection cloaks)
		{
			if(_args.RespectCloaks == false)
			{
				cloaks = ServerOperations.client.Cloaks;
				ServerOperations.client.Cloaks = null;
				return true;
			}
			cloaks = null;
			return false;
		}

		public void restoreCloaksIfNecessary(bool bResetCloaks, VaultNameValueCollection cloaks)
		{
			if (bResetCloaks)
				ServerOperations.client.Cloaks = cloaks;
	}

		public bool setTempWorkingFolderIfNecessary(bool setWorkingFolder, string folderPath, string tmpWorkingFolderPath, out string repositoryPath, out string originalWorkingFolder)
	{
			// Temporarily change the working folder if the user specified a destination path and -useworkingfolder.
			VaultClientTreeObject workingFolderTreeObject = null;
			originalWorkingFolder = null;
			repositoryPath = folderPath;
			if (setWorkingFolder == false)
				return false;
			workingFolderTreeObject = RepositoryUtil.FindVaultTreeObjectAtReposOrLocalPath(folderPath);
			repositoryPath = workingFolderTreeObject.FullPath;
			if ( !Misc.stringIsBlank(tmpWorkingFolderPath) )
			{
				if ( workingFolderTreeObject is VaultClientFile )
					workingFolderTreeObject = workingFolderTreeObject.Parent;

				// You have to use TreeCache.GetWorkingFolder here because it retrieves only directly
				// assigned working paths, not inherited working paths, and that's what we want.
				originalWorkingFolder = ServerOperations.client.ClientInstance.TreeCache.GetWorkingFolder(workingFolderTreeObject.FullPath);

				if ( Misc.stringIsBlank(originalWorkingFolder) || RepositoryUtil.NormalizeFolderPath(originalWorkingFolder) != RepositoryUtil.NormalizeFolderPath(tmpWorkingFolderPath) )
				{
					ServerOperations.SetWorkingFolder(workingFolderTreeObject.FullPath, tmpWorkingFolderPath, true);
				}
				return true;
			}
			return false;
		}
		public void restoreWorkingFolderIfNecessary(bool resetWorkingFolder, string repositoryPath, string originalWorkingFolderPath)
		{
			// Reset the original working folder, if we changed it.
			if ( resetWorkingFolder && repositoryPath != null)
			{
				if ( originalWorkingFolderPath == null ) 
				{
					// Bug fix for removal of working folder for files instead of folders
					VaultClientTreeObject workingFolderTreeObject = RepositoryUtil.FindVaultTreeObjectAtReposOrLocalPath(repositoryPath);
					if (workingFolderTreeObject is VaultClientFile)
					{
						workingFolderTreeObject = workingFolderTreeObject.Parent;
						repositoryPath = workingFolderTreeObject.FullPath;
					} 
					ServerOperations.client.ClientInstance.TreeCache.RemoveWorkingFolder(repositoryPath);
				}
				else 
				{
					// Bug fix for removal of working folder for files instead of folders
					VaultClientTreeObject workingFolderTreeObject = RepositoryUtil.FindVaultTreeObjectAtReposOrLocalPath(repositoryPath);
					if (workingFolderTreeObject is VaultClientFile)
					{
						workingFolderTreeObject = workingFolderTreeObject.Parent;
						repositoryPath = workingFolderTreeObject.FullPath;
					} 
					ServerOperations.client.ClientInstance.TreeCache.SetWorkingFolder(repositoryPath, originalWorkingFolderPath);
				}
			}
		}

		public bool setMakeBackupOption(BackupOption makeBackup, out bool originalBackupOption)
		{
			ClientConnection cc = ServerOperations.client;
			// if the backup option is specified, set it for these gets only
			originalBackupOption = cc.MakeBackups;
			if (makeBackup == BackupOption.yes)
			{
				cc.MakeBackups = true;
			} 
			else if (makeBackup == BackupOption.no)
			{
				cc.MakeBackups = false;
			}
			return cc.MakeBackups != originalBackupOption;
		}

		public void restoreMakeBackupOption(BackupOption makeBackup, bool originalBackupOption)
		{
			if (makeBackup != BackupOption.usedefault)
				ServerOperations.client.MakeBackups = originalBackupOption;
		}
	}
}

