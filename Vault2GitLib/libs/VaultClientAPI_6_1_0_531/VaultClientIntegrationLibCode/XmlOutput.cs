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
using MantisLib;
namespace VaultClientIntegrationLib
{
	/// <summary>
	/// Summary description for XmlOutput.
	/// </summary>
	public class XmlHelper
	{
		private XmlHelper()
		{
		}

		private static string GetStatusString(WorkingFolderFileStatus st)
		{
			return (st != WorkingFolderFileStatus.None) ? st.ToString() : string.Empty;
		}

		public static void XmlOutput(System.Xml.XmlWriter xml, VaultBlameRegionResponse vbrr)
		{
			xml.WriteComment(string.Format("User {0} last changed region with the comment:\r\n {1}", vbrr.UserName, vbrr.Comment));
			xml.WriteStartElement("blame");
			xml.WriteElementString("user", vbrr.UserName);
			xml.WriteElementString("version", vbrr.OriginatingVersion.ToString());
			xml.WriteElementString("comment", vbrr.Comment);
			xml.WriteElementString("txDate", vbrr.TxDate.ToString());
			xml.WriteEndElement();
		}
		public static void XmlOutput(System.Xml.XmlWriter xml, VaultClientTreeObject treeObject)
		{
			xml.WriteStartElement("vaulttreeobject");
			xml.WriteElementString("name", treeObject.Name);
			xml.WriteElementString("fullpath", treeObject.FullPath);
			xml.WriteElementString("objectid", treeObject.ID.ToString());
			xml.WriteElementString("version", treeObject.Version.ToString());
			xml.WriteElementString("objverid", treeObject.ObjVerID.ToString());
			xml.WriteElementString("transactiondate", treeObject.TxDate.ToString());
			xml.WriteElementString("modifieddate", treeObject.ModifiedDate.ToString());
			xml.WriteEndElement();
		}

		public static void XmlOutput(System.Xml.XmlWriter xml, VaultClientCheckOutList checkOuts)
		{
			xml.WriteStartElement("checkoutlist");

			if ((checkOuts != null) && (checkOuts.Count > 0))
			{
				foreach (VaultClientCheckOutItem item in checkOuts)
				{
					xml.WriteStartElement("checkoutitem");
					xml.WriteElementString("id", item.FileID.ToString());

					foreach (VaultClientCheckOutUser user in item.CheckOutUsers)
					{
						xml.WriteStartElement("checkoutuser");
						xml.WriteElementString("username", user.Name);
						xml.WriteElementString("version", user.Version.ToString());
						xml.WriteElementString("repositorypath", user.RepPath);

						switch (user.LockType)
						{
							case VaultCheckOutType.None:
								xml.WriteElementString("locktype", "none");
								break;
							case VaultCheckOutType.CheckOut:
								xml.WriteElementString("locktype", "checkout");
								break;
							case VaultCheckOutType.Exclusive:
								xml.WriteElementString("locktype", "exclusive");
								break;
							default:
								xml.WriteElementString("locktype", "unknown");
								break;
						}

						xml.WriteElementString("comment", user.Comment);
						xml.WriteElementString("hostname", user.Hostname);
						xml.WriteElementString("localpath", user.LocalPath);
						xml.WriteElementString("folderid", user.FolderID.ToString());
						xml.WriteElementString("lockedwhen", user.LockedWhen.ToString());
						xml.WriteElementString("miscinfo", user.MiscInfo);
						xml.WriteEndElement();
					}

					xml.WriteEndElement();
				}
			}

			xml.WriteEndElement();
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="xml"></param>
		/// <param name="csic"></param>
		public static void XmlOutput(System.Xml.XmlWriter xml, ChangeSetItemColl csic)
		{
			XmlOutput(xml, csic, true, false);
		}

		public static void XmlOutput(System.Xml.XmlWriter xml, ChangeSetItemColl csic, bool bXmlBeginEnd, bool bShowModifiedItemStatus)
		{
			if ((bXmlBeginEnd == true) && (xml != null))
			{
				xml.WriteStartElement("changeset");
			}

			try
			{
				if ((csic != null) && (csic.Count > 0))
				{
					int i, nCnt;
					ChangeSetItem csi = null;
					for (i = 0, nCnt = csic.Count; i < nCnt; i++)
					{
						csi = csic[i];
						switch (csi.Type)
						{
							case ChangeSetItemType.AddFile:
								{
									ChangeSetItem_AddFile it = (ChangeSetItem_AddFile)csi;

									xml.WriteStartElement("AddFile");
									xml.WriteElementString("id", i.ToString());
									xml.WriteElementString("repospath", it.DisplayRepositoryPath);
									xml.WriteElementString("localpath", it.DiskFile);
									xml.WriteEndElement();

									break;
								}
							case ChangeSetItemType.AddFolder:
								{
									ChangeSetItem_AddFolder it = (ChangeSetItem_AddFolder)csi;

									xml.WriteStartElement("AddFolder");
									xml.WriteElementString("id", i.ToString());
									xml.WriteElementString("reposfolder", it.DisplayRepositoryPath);
									xml.WriteElementString("localfolder", it.DiskFolder);
									xml.WriteEndElement();

									break;
								}
							case ChangeSetItemType.AddPartialFolder:
								{
									ChangeSetItem_AddPartialFolder it = (ChangeSetItem_AddPartialFolder)csi;

									xml.WriteStartElement("AddAddPartialFolder");
									xml.WriteElementString("id", i.ToString());
									xml.WriteElementString("reposfolder", it.DisplayRepositoryPath);
									xml.WriteElementString("localfolder", it.DiskFolder);
									xml.WriteEndElement();

									break;
								}
							case ChangeSetItemType.DeleteFile:
								{
									ChangeSetItem_DeleteFile it = (ChangeSetItem_DeleteFile)csi;

									xml.WriteStartElement("DeleteFile");
									xml.WriteElementString("id", i.ToString());
									xml.WriteElementString("repospath", it.DisplayRepositoryPath);
									xml.WriteEndElement();

									break;
								}
							case ChangeSetItemType.DeleteFolder:
								{
									ChangeSetItem_DeleteFolder it = (ChangeSetItem_DeleteFolder)csi;

									xml.WriteStartElement("DeleteFolder");
									xml.WriteElementString("id", i.ToString());
									xml.WriteElementString("repospath", it.DisplayRepositoryPath);
									xml.WriteEndElement();

									break;
								}
							case ChangeSetItemType.CreateFolder:
								{
									ChangeSetItem_CreateFolder it = (ChangeSetItem_CreateFolder)csi;

									xml.WriteStartElement("CreateFolder");
									xml.WriteElementString("id", i.ToString());
									xml.WriteElementString("repospath", it.DisplayRepositoryPath);
									xml.WriteEndElement();

									break;
								}
							case ChangeSetItemType.BranchCopy:
								{
									ChangeSetItem_CopyBranch it = (ChangeSetItem_CopyBranch)csi;

									xml.WriteStartElement("CopyBranch");
									xml.WriteElementString("id", i.ToString());
									xml.WriteElementString("repospath", it.DisplayRepositoryPath);
									xml.WriteElementString("branchpath", it.BranchPath);
									xml.WriteEndElement();

									break;
								}
							case ChangeSetItemType.BranchShare:
								{
									ChangeSetItem_ShareBranch it = (ChangeSetItem_ShareBranch)csi;

									xml.WriteStartElement("ShareBranch");
									xml.WriteElementString("id", i.ToString());
									xml.WriteElementString("repospath", it.DisplayRepositoryPath);
									xml.WriteElementString("branchpath", it.BranchPath);
									xml.WriteEndElement();

									break;
								}
							case ChangeSetItemType.Share:
								{
									ChangeSetItem_Share it = (ChangeSetItem_Share)csi;

									xml.WriteStartElement("Share");
									xml.WriteElementString("id", i.ToString());
									xml.WriteElementString("repospath", it.RepositoryPath);
									xml.WriteElementString("sharepath", it.NewSharePath);
									xml.WriteEndElement();

									break;
								}
							case ChangeSetItemType.Pin:
								{
									ChangeSetItem_Pin it = (ChangeSetItem_Pin)csi;

									xml.WriteStartElement("Pin");
									xml.WriteElementString("id", i.ToString());
									xml.WriteElementString("repospath", it.DisplayRepositoryPath);
									xml.WriteEndElement();

									break;
								}
							case ChangeSetItemType.Rename:
								{
									ChangeSetItem_Rename it = (ChangeSetItem_Rename)csi;

									xml.WriteStartElement("Rename");
									xml.WriteElementString("id", i.ToString());
									xml.WriteElementString("repospath", it.OldRepositoryPath);
									xml.WriteElementString("newname", it.NewName);
									xml.WriteEndElement();
									break;
								}
							case ChangeSetItemType.Unpin:
								{
									ChangeSetItem_Unpin it = (ChangeSetItem_Unpin)csi;

									xml.WriteStartElement("Unpin");
									xml.WriteElementString("id", i.ToString());
									xml.WriteElementString("repospath", it.DisplayRepositoryPath);
									xml.WriteEndElement();

									break;
								}
							case ChangeSetItemType.Undelete:
								{
									ChangeSetItem_Undelete it = (ChangeSetItem_Undelete)csi;

									xml.WriteStartElement("Undelete");
									xml.WriteElementString("id", i.ToString());
									xml.WriteElementString("repospath", it.RepositoryPath);
									xml.WriteEndElement();

									break;
								}
							case ChangeSetItemType.Move:
								{
									ChangeSetItem_Move it = (ChangeSetItem_Move)csi;

									xml.WriteStartElement("Move");
									xml.WriteElementString("id", i.ToString());
									xml.WriteElementString("repospath", it.OldRepositoryPath);
									xml.WriteElementString("newpath", it.NewOwnerPath);
									xml.WriteEndElement();

									break;
								}
							case ChangeSetItemType.Modified:
								{
									ChangeSetItem_Modified it = (ChangeSetItem_Modified)csi;

									xml.WriteStartElement("ModifyFile");
									xml.WriteElementString("id", i.ToString());
									xml.WriteElementString("repospath", it.DisplayRepositoryPath);
									xml.WriteElementString("localpath", it.DiskFile);

									if (bShowModifiedItemStatus == true)
									{
										try
										{
											string strStatus = string.Empty;

											// get the status of Needs Merge or the working folder status
											if (it.NeedsMerge == false)
											{
												// get the working folder for the file
												VaultClientFile f = ServerOperations.client.ClientInstance.TreeCache.Repository.Root.FindFileRecursive(it.DisplayRepositoryPath);
												if (f != null)
												{
													// get a working folder
													WorkingFolder wf = ServerOperations.client.ClientInstance.GetWorkingFolder(f);
													strStatus = GetStatusString(wf.GetStatus(f));
												}
											}
											else
											{
												strStatus = "needs merge";
											}
											xml.WriteElementString("modifiedstatus", strStatus);
										}
										catch
										{
										}
									}
									xml.WriteEndElement();

									break;
								}
							case ChangeSetItemType.Unmodified:
								{
									ChangeSetItem_Unmodified it = (ChangeSetItem_Unmodified)csi;

									xml.WriteStartElement("UnmodifiedFile");
									xml.WriteElementString("id", i.ToString());
									xml.WriteElementString("respospath", it.DisplayRepositoryPath);
									xml.WriteElementString("localpath", it.DiskFile);
									xml.WriteEndElement();

									break;
								}
							case ChangeSetItemType.ChangeExtProperties:
								{
									ChangeSetItem_ChangeExtProperties it = (ChangeSetItem_ChangeExtProperties)csi;

									xml.WriteStartElement("ChangeExtProperties");
									xml.WriteElementString("id", i.ToString());
									xml.WriteElementString("repospath", it.DisplayRepositoryPath);
									xml.WriteEndElement();

									break;
								}
							case ChangeSetItemType.ChangeFileProperties:
								{
									ChangeSetItem_ChangeFileProperties it = (ChangeSetItem_ChangeFileProperties)csi;

									xml.WriteStartElement("ChangeFileProperties");
									xml.WriteElementString("id", i.ToString());
									xml.WriteElementString("repospath", it.DisplayRepositoryPath);
									xml.WriteEndElement();

									break;
								}
							case ChangeSetItemType.Rollback:
								{
									ChangeSetItem_Rollback it = (ChangeSetItem_Rollback)csi;

									xml.WriteStartElement("Rollback");
									xml.WriteElementString("id", i.ToString());
									xml.WriteElementString("repospath", it.DisplayRepositoryPath);
									xml.WriteEndElement();

									break;
								}
							case ChangeSetItemType.Obliterate:
								{
									ChangeSetItem_Obliterate it = (ChangeSetItem_Obliterate)csi;

									xml.WriteStartElement("Obliterate");
									xml.WriteElementString("id", i.ToString());
									xml.WriteElementString("repospath", it.DisplayRepositoryPath);
									xml.WriteEndElement();

									break;
								}
							case ChangeSetItemType.CheckedOutMissing:
								{
									ChangeSetItem_CheckedOutMissing it = (ChangeSetItem_CheckedOutMissing)csi;

									xml.WriteStartElement("CheckedOutMissing");
									xml.WriteElementString("id", i.ToString());
									xml.WriteElementString("respospath", it.DisplayRepositoryPath);
									xml.WriteEndElement();

									break;
								}
							case ChangeSetItemType.Snapshot:
								{
									ChangeSetItem_Snapshot it = (ChangeSetItem_Snapshot)csi;
									xml.WriteStartElement("Snapshot");
									xml.WriteElementString("id", i.ToString());
									xml.WriteElementString("respospath", it.RepositoryPath);
									xml.WriteElementString("parentpath", it.SnapshotPath);
									xml.WriteEndElement();

									break;
								}
							default:
								{
									// this should never happen.
									throw new Exception("There is a ChangeSet item we don't recognize.  Please contact SourceGear support.  Type:  " + csi.TypeString);
								}
						}
					}
				}
			}
			finally
			{
				// end the xml pair.
				if ((bXmlBeginEnd == true) && (xml != null))
				{
					xml.WriteEndElement();
				}
			}
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="xml"></param>
		/// <param name="list"></param>
		public static void XmlOutput(System.Xml.XmlWriter xml, SortedList list)
		{
			xml.WriteStartElement("listworkingfolders");
			foreach (string reposPath in list.Keys)
			{
				xml.WriteStartElement("workingfolder");
				xml.WriteAttributeString("reposfolder", reposPath);
				xml.WriteAttributeString("localfolder", (string)list[reposPath]);
				xml.WriteEndElement();
			}
			xml.WriteEndElement();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="xml"></param>
		/// <param name="vcfolder"></param>
		public static void XmlOutput(System.Xml.XmlWriter xml, VaultClientFolder vcfolder)
		{
			WorkingFolder wf = ServerOperations.client.ClientInstance.GetWorkingFolder(vcfolder);

			xml.WriteStartElement("folder");
			xml.WriteAttributeString("name", vcfolder.FullPath);
			if (wf != null)
			{
				xml.WriteAttributeString("workingfolder", wf.GetLocalFolderPath());
			}
			foreach (VaultClientFile file in vcfolder.Files)
			{
				xml.WriteStartElement("file");
				xml.WriteAttributeString("name", file.Name);
				xml.WriteAttributeString("version", file.Version.ToString());
				xml.WriteAttributeString("length", file.FileLength.ToString());
				xml.WriteAttributeString("objectid", file.ID.ToString());
				xml.WriteAttributeString("objectversionid", file.ObjVerID.ToString());

				string strCheckOuts = ServerOperations.client.ClientInstance.GetCheckOuts(file);
				if (
					(strCheckOuts != null)
					&& (strCheckOuts.Length > 0)
					)
				{
					xml.WriteAttributeString("checkouts", strCheckOuts);
				}

				if (wf != null)
				{
					WorkingFolderFileStatus st = wf.GetStatus(file);
					if (st != WorkingFolderFileStatus.None)
					{
						xml.WriteAttributeString("status", (st != WorkingFolderFileStatus.None) ? st.ToString() : string.Empty);
					}
				}

				xml.WriteEndElement();
			}

			foreach (VaultClientFolder subfolder in vcfolder.Folders)
			{
				XmlOutput(xml, subfolder);
			}

			xml.WriteEndElement();

		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="xml"></param>
		/// <param name="histitems"></param>
		public static void XmlOutput(System.Xml.XmlWriter xml, VaultHistoryItem[] histitems)
		{
			// produce results into the xml item.

			System.Text.StringBuilder sbAllCommentsForBugIDSearch = new System.Text.StringBuilder();

			xml.WriteStartElement("history");
			foreach (VaultHistoryItem hi in histitems)
			{
				xml.WriteStartElement("item");
				xml.WriteAttributeString("txid", hi.TxID.ToString());
				xml.WriteAttributeString("date", hi.TxDate.ToString());
				xml.WriteAttributeString("name", hi.Name);
				xml.WriteAttributeString("type", GetHistItemTypeString(hi.HistItemType));
				xml.WriteAttributeString("typeName", VaultHistoryType.GetHistoryTypeName(hi.HistItemType));
				xml.WriteAttributeString("version", hi.Version.ToString());
				xml.WriteAttributeString("objverid", hi.ObjVerID.ToString());
				xml.WriteAttributeString("user", hi.UserLogin);
				if (
					(hi.Comment != null)
					&& (hi.Comment.Length > 0)
					)
				{
					xml.WriteAttributeString("comment", hi.Comment);
					sbAllCommentsForBugIDSearch.AppendFormat("{0} ", hi.Comment);
				}
				xml.WriteAttributeString("actionString", hi.GetActionString());
				xml.WriteEndElement();
			}

			xml.WriteEndElement();

			string bugIds = "";

			//format item:12 or bug:12
			Regex itemregex = new Regex("\\b((item|bug):(?<itemid>[0-9]+))\\b", RegexOptions.IgnoreCase);

			string strBigComment = sbAllCommentsForBugIDSearch.ToString();
			MatchCollection mc = itemregex.Matches(strBigComment);

			foreach (Match m in mc)
			{
				if (m.Success)
				{
					bugIds += m.Groups["itemid"].Value + ",";
				}
			}

			//format item 12 or bug 12
			itemregex = new Regex("\\b((item|bug)\\s(?<itemid>[0-9]+))\\b", RegexOptions.IgnoreCase);
			mc = itemregex.Matches(strBigComment);

			foreach (Match m in mc)
			{
				if (m.Success)
				{
					bugIds += m.Groups["itemid"].Value + ",";
				}
			}

			if (bugIds.Length > 0 && bugIds.EndsWith(","))
			{
				bugIds = bugIds.Substring(0, bugIds.Length - 1);
			}

			xml.WriteElementString("bugsreferenced", bugIds);
		}

		private static string GetHistItemTypeString(int x)
		{
			return x.ToString();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="xml"></param>
		/// <param name="repositories"></param>
		public static void XmlOutput(System.Xml.XmlWriter xml, VaultRepositoryInfo[] repositories)
		{
			xml.WriteStartElement("listrepositories");
			foreach (VaultRepositoryInfo r in repositories)
			{
				xml.WriteStartElement("repository");
				xml.WriteElementString("name", r.RepName);
				xml.WriteElementString("files", r.FileCount.ToString());
				xml.WriteElementString("folders", r.FolderCount.ToString());
				xml.WriteElementString("dbsize", r.DbSize.ToString());
				xml.WriteEndElement();
			}
			xml.WriteEndElement();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="xml"></param>
		/// <param name="histitems"></param>
		public static void XmlOutput(System.Xml.XmlWriter xml, VaultTxHistoryItem[] histitems)
		{
			// produce results into the xml item.
			xml.WriteStartElement("history");
			foreach (VaultTxHistoryItem hi in histitems)
			{
				xml.WriteStartElement("item");
				xml.WriteAttributeString("version", hi.Version.ToString());
				xml.WriteAttributeString("date", hi.TxDate.ToString());
				xml.WriteAttributeString("user", hi.UserLogin);
				if (hi.Comment != null && hi.Comment.Length > 0)
				{
					xml.WriteAttributeString("comment", hi.Comment);
				}
				xml.WriteAttributeString("objverid", hi.ObjVerID.ToString());
				xml.WriteAttributeString("txid", hi.TxID.ToString());
				xml.WriteEndElement();
			}
			xml.WriteEndElement();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="xml"></param>
		/// <param name="info"></param>
		public static void XmlOutput(System.Xml.XmlWriter xml, TxInfo info)
		{
			xml.WriteStartElement("txinfo");
			xml.WriteElementString("login", info.userlogin);
			xml.WriteElementString("changeset comment", info.changesetComment);
			foreach (VaultTxDetailHistoryItem i in info.items)
			{
				xml.WriteStartElement("txdetail history item");
				xml.WriteElementString("name", i.Name);
				xml.WriteElementString("id", i.ID.ToString());
				xml.WriteEndElement();
			}
			xml.WriteEndElement();
		}

		/// <summary>
		/// Method will write Find in File Results the the xml writer
		/// </summary>
		/// <param name="xml"></param>
		/// <param name="arFiFData"></param>
		public static void XmlOutput(System.Xml.XmlWriter xml, FindInFilesData[] arFiFData)
		{
			// produce results into the xml item.
			xml.WriteStartElement("findinfiles");

			if ((arFiFData != null) && (arFiFData.Length > 0))
			{
				xml.WriteAttributeString("file-matches", arFiFData.Length.ToString());

				foreach (FindInFilesData fif in arFiFData)
				{
					foreach (string s in fif.FolderPaths)
					{
						if ((string.IsNullOrEmpty(s) == false) && ((fif.FindInFileLines != null) && (fif.FindInFileLines.Length > 0)))
						{
							xml.WriteStartElement("file");	// xml-file

							xml.WriteAttributeString("name", fif.FileName);
							xml.WriteAttributeString("path", s);
							xml.WriteAttributeString("version", fif.Version.ToString());
							xml.WriteAttributeString("line-matches", fif.FindInFileLines.Length.ToString());

							foreach (FindInFilesLineData fifld in fif.FindInFileLines)
							{
								xml.WriteStartElement("line");	// xml-line

								xml.WriteAttributeString("line-number", fifld.LineNumber.ToString());
								xml.WriteAttributeString("line", fifld.LineData);

								xml.WriteEndElement();			// xml-line
							}

							xml.WriteEndElement();			// xml-file
						}

					}
				}
			}
			else
			{
				xml.WriteAttributeString("file-matches", "0");
			}

			xml.WriteEndElement();

		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="xml"></param>
		/// <param name="projects"></param>
		public static void XmlOutput(System.Xml.XmlWriter xml, MantisProject[] projects)
		{
			// produce results into the xml item.
			foreach (MantisProject p in projects)
			{
				xml.WriteStartElement("project");
				xml.WriteElementString("name", p.Name);
				xml.WriteElementString("description", p.Description);
				xml.WriteEndElement();
			}

		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="xml"></param>
		/// <param name="categories"></param>
		public static void XmlOutput(System.Xml.XmlWriter xml, MantisCategory[] categories)
		{
			xml.WriteStartElement("work item categories");
			foreach (MantisCategory c in categories)
			{
				xml.WriteStartElement("category");
				xml.WriteElementString("label", c.Label);
				xml.WriteElementString("developer", c.DeveloperName);
				xml.WriteEndElement();
			}
			xml.WriteEndElement();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="xml"></param>
		/// <param name="milestones"></param>
		public static void XmlOutput(System.Xml.XmlWriter xml, MantisMilestone[] milestones)
		{
			xml.WriteStartElement("work item milestones");
			foreach (MantisMilestone m in milestones)
			{
				xml.WriteStartElement("milestone");
				xml.WriteElementString("name", m.Name);
				xml.WriteElementString("description", m.Description);
				xml.WriteElementString("goal date", m.GoalDate.ToString());
				xml.WriteEndElement();
			}
			xml.WriteEndElement();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="xml"></param>
		/// <param name="platforms"></param>
		public static void XmlOutput(System.Xml.XmlWriter xml, MantisPlatform[] platforms)
		{
			xml.WriteStartElement("work item platforms");
			foreach (MantisPlatform p in platforms)
			{
				xml.WriteStartElement("platform");
				xml.WriteElementString("label", p.Label);
				xml.WriteEndElement();
			}
			xml.WriteEndElement();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="xml"></param>
		/// <param name="priorities"></param>
		public static void XmlOutput(System.Xml.XmlWriter xml, MantisPriority[] priorities)
		{
			xml.WriteStartElement("work item priorities");
			foreach (MantisPriority p in priorities)
			{
				xml.WriteStartElement("priority");
				xml.WriteElementString("label", p.Label);
				xml.WriteEndElement();
			}
			xml.WriteEndElement();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="xml"></param>
		/// <param name="statuses"></param>
		public static void XmlOutput(System.Xml.XmlWriter xml, MantisStatus[] statuses)
		{
			xml.WriteStartElement("work item statuses");
			foreach (MantisStatus s in statuses)
			{
				xml.WriteStartElement("status");
				xml.WriteElementString("label", s.Label);
				xml.WriteEndElement();
			}
			xml.WriteEndElement();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="xml"></param>
		/// <param name="timeUnits"></param>
		public static void XmlOutput(System.Xml.XmlWriter xml, MantisTimeUnit[] timeUnits)
		{
			xml.WriteStartElement("work item time units");
			foreach (MantisTimeUnit t in timeUnits)
			{
				xml.WriteStartElement("time unit");
				xml.WriteElementString("label", t.Label);
				xml.WriteEndElement();
			}
			xml.WriteEndElement();
		}

		/// <summary>
		/// Method to write the deprecated MantisTimeEstimate class to an Xml Writer.
		/// </summary>
		/// <param name="xml"></param>
		/// <param name="timeEstimates"></param>
		public static void XmlOutput(System.Xml.XmlWriter xml, MantisTimeEstimate[] timeEstimates)
		{
			xml.WriteStartElement("work item time estimates");
			foreach (MantisTimeEstimate t in timeEstimates)
			{
				xml.WriteStartElement("time estimate");
				xml.WriteElementString("label", t.Label);
				xml.WriteEndElement();
			}
			xml.WriteEndElement();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="xml"></param>
		/// <param name="users"></param>
		public static void XmlOutput(System.Xml.XmlWriter xml, MantisUser[] users)
		{
			xml.WriteStartElement("vault pro users");
			foreach (MantisUser u in users)
			{
				xml.WriteStartElement("user");
				xml.WriteElementString("name", u.Name);
				xml.WriteElementString("login", u.Login);
				xml.WriteElementString("id", u.ID.ToString());
				xml.WriteEndElement();
			}
			xml.WriteEndElement();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="xml"></param>
		/// <param name="labels"></param>
		public static void XmlOutput(System.Xml.XmlWriter xml, MantisCustomLabel[] labels)
		{
			xml.WriteStartElement("work item custom labels");
			foreach (MantisCustomLabel l in labels)
			{
				xml.WriteStartElement("custom label");
				xml.WriteElementString("label", l.Label);
				xml.WriteEndElement();
			}
			xml.WriteEndElement();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="xml"></param>
		/// <param name="types"></param>
		public static void XmlOutput(System.Xml.XmlWriter xml, MantisItemType[] types)
		{
			xml.WriteStartElement("work item types");
			foreach (MantisItemType t in types)
			{
				xml.WriteStartElement("type");
				xml.WriteElementString("label", t.Label);
				xml.WriteEndElement();
			}
			xml.WriteEndElement();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="xml"></param>
		/// <param name="item"></param>
		public static void XmlOutput(System.Xml.XmlWriter xml, FortressItemExpanded item)
		{
			xml.WriteStartElement("work item");
			xml.WriteElementString("description", item.Description);
			xml.WriteElementString("details", item.Details);
			xml.WriteElementString("status", item.Status);
			xml.WriteElementString("priority", item.Priority);
			xml.WriteElementString("platform", item.Platform);
			xml.WriteElementString("assignee", item.Assignee);
			xml.WriteElementString("time estimate", string.Format("{0} {1}", item.TimeEstimateValue, item.TimeEstimateUnitLabel));
			xml.WriteElementString("version", item.VersionStr);
			xml.WriteElementString("item type", item.ItemType);
			xml.WriteElementString("project name", item.ProjectName);
			xml.WriteEndElement();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="xml"></param>
		/// <param name="item"></param>
		public static void XmlOutput(System.Xml.XmlWriter xml, MantisItemExpanded item)
		{
			xml.WriteStartElement("work item");
			xml.WriteElementString("id", item.ID.ToString());
			xml.WriteElementString("description", item.Description);
			xml.WriteElementString("details", item.Details);
			xml.WriteElementString("status", item.Status);
			xml.WriteElementString("created", item.Created.ToString());
			xml.WriteElementString("modified", item.LastModifiedString);
			xml.WriteElementString("project", item.Project);
			xml.WriteElementString("type", item.Type);
			xml.WriteElementString("milestone", item.Milestone);
			xml.WriteElementString("category", item.Category);
			xml.WriteElementString("priority", item.Priority);
			xml.WriteElementString("time estimate", string.Format("{0} {1}", item.TimeEstimateValue, item.TimeEstimateUnitLabel));
			xml.WriteElementString("platform", item.Platform);
			xml.WriteElementString("assignee", item.Assignee);
			xml.WriteElementString("resolver", item.Resolver);
			xml.WriteElementString("reporter", item.Reporter);
			xml.WriteElementString("version", item.Version);
			xml.WriteElementString("custom1", item.Custom1);
			xml.WriteElementString("custom2", item.Custom2);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="xml"></param>
		/// <param name="items"></param>
		public static void XmlOutput(System.Xml.XmlWriter xml, MantisItemExpanded[] items)
		{
			xml.WriteStartElement("work items");
			foreach (MantisItemExpanded item in items)
			{
				XmlOutput(xml, item);
			}
			xml.WriteEndElement();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="xml"></param>
		/// <param name="item"></param>
		public static void XmlOutput(System.Xml.XmlWriter xml, MantisItem item)
		{
			xml.WriteStartElement("work item");
			xml.WriteElementString("id", item.ID.ToString());
			xml.WriteElementString("description", item.Description);
			xml.WriteElementString("details", item.Details);
			xml.WriteEndElement();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="xml"></param>
		/// <param name="item"></param>
		public static void XmlOutput(System.Xml.XmlWriter xml, MantisItemFullDetail item)
		{
			XmlOutput(xml, (MantisItemExpanded)item);
			// write out attachment info?
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="xml"></param>
		/// <param name="aag"></param>
		public static void XmlOutput(System.Xml.XmlWriter xml, MilestoneAaG[] aag)
		{
			xml.WriteStartElement("at-a-glance (by milestone)");
			foreach (MilestoneAaG m in aag)
			{
				xml.WriteStartElement("milestone at-a-glance");
				xml.WriteElementString("milestone", m.Milestone);
				xml.WriteElementString("hours remaining", m.HoursRemaining.ToString("0.00"));
				xml.WriteElementString("my open items", m.MyOpenItems.ToString());
				xml.WriteElementString("my unresolved items", m.MyUnresolvedItems.ToString());
				xml.WriteElementString("not estimated", m.NotEstimated.ToString());
				xml.WriteElementString("open items", m.OpenItems.ToString());
				xml.WriteElementString("unassigned assignee", m.UnassignedAssignee.ToString());
				xml.WriteElementString("unassigned resolver", m.UnassignedResolver.ToString());
				xml.WriteElementString("unresolved items", m.UnresolvedItems.ToString());
				xml.WriteElementString("urgent", m.Urgent.ToString());
				xml.WriteEndElement();
			}
			xml.WriteEndElement();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="xml"></param>
		/// <param name="sq"></param>
		public static void XmlOutput(System.Xml.XmlWriter xml, SavedQuery sq)
		{
			xml.WriteStartElement("saved query");
			xml.WriteElementString("name", sq.Name);
			xml.WriteElementString("id", sq.QueryID.ToString());
			// write out qf stuff?
			xml.WriteEndElement();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="xml"></param>
		/// <param name="queries"></param>
		public static void XmlOutput(System.Xml.XmlWriter xml, SavedQuery[] queries)
		{
			xml.WriteStartElement("saved queries");
			foreach (SavedQuery sq in queries)
			{
				XmlOutput(xml, sq);
			}
			xml.WriteEndElement();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="xml"></param>
		/// <param name="att"></param>
		public static void XmlOutput(System.Xml.XmlWriter xml, MantisItemAttachmentFullDetail att)
		{
			xml.WriteStartElement("attachment full detail");
			xml.WriteElementString("filename", att.FileName);
			xml.WriteElementString("local path", att.LocalPath);
			xml.WriteElementString("attID", att.AttID.ToString());
			xml.WriteElementString("msgID", att.MsgID.ToString());
			xml.WriteElementString("description", att.Description);
			xml.WriteEndElement();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="xml"></param>
		/// <param name="str"></param>
		public static void XmlOutput(System.Xml.XmlWriter xml, string str)
		{
			xml.WriteStartElement("string");
			xml.WriteElementString("value", str);
			xml.WriteEndElement();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="xml"></param>
		/// <param name="i"></param>
		public static void XmlOutput(System.Xml.XmlWriter xml, int i)
		{
			xml.WriteStartElement("int");
			xml.WriteElementString("value", i.ToString());
			xml.WriteEndElement();
		}
	}
}
