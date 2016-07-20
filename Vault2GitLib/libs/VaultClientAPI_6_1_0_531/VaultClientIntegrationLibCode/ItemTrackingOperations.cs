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

using MantisLib;

namespace VaultClientIntegrationLib
{

	/// <summary>
	/// This class encapsulates the information required to create or modify a bug tracking item.
	/// </summary>
	public class FortressItemExpanded
	{
		#region Private Fields
		private MantisItemExpanded item = null;
		private MantisItemType[] types = null;
		private MantisStatus[] statuses = null;
		private MantisUser[] users = null;
		private MantisPlatform[] platforms = null;
		private MantisPriority[] priorities = null;
		private MantisProject[] projects = null;
		private MantisMilestone[] milestones = null;
		private MantisCategory[] categories = null;
		private MantisTimeUnit[] timeunits = null;
		private int projectID = -1;
		#endregion

		#region Public Fields
		/// <summary>
		/// A bool determining whether or not exceptions will be thrown if unrecognized string values are encountered by Fortress.
		/// </summary>
		[RecommendedOptionDefault("true")]
		public bool throwExceptions = true;

		/// <summary>
		/// The name of the project.
		/// </summary>
		public string ProjectName = "";

		/// <summary>
		/// The bug tracking item type (i.e. Bug, Feature, etc).
		/// </summary>
		public string ItemType = "";

		/// <summary>
		/// The status of the item (i.e. Open, Completed, In Progress, etc).
		/// </summary>
		public string Status = "";

		/// <summary>
		/// The platform for the item (i.e. Windows, Unix, Unknown, etc).
		/// </summary>
		public string Platform = "";

		/// <summary>
		/// [Note: Deprecated.  Use TimeEstimateValues / TimeEstimateUnitLabel.]  The estimated time to complete the item (i.e. Unknown, One Hour, One Month, etc).
		/// </summary>
		[RecommendedOptionDefault("\"\"")]
		public string TimeEstimate = "";

		/// <summary>
		/// The estimated time to complete the item TimeEstimateValue = quantity (30).  Used in conjunction with TimeEstimateUnitLabel"
		/// </summary>
		[RecommendedOptionDefault("0")]
		public int TimeEstimateValue = 0;
		/// <summary>
		/// TimeEstimateUnitLabel = "week(s) | day(s) | hour(s) | minute(s).  Used in conjuction with TimeEstimateValue.
		/// </summary>
		[RecommendedOptionDefault("\"\"")]
		public string TimeEstimateUnitLabel = "";

		/// <summary>
		/// The date the item should be completed by.  Format is "yyyy-mm-dd".
		/// </summary>
		[RecommendedOptionDefault("\"\"")]
		public string DueDate = "";

		/// <summary>
		/// The user the item is to be assigned to.
		/// </summary>
		[RecommendedOptionDefault("\"\"")]
		public string Assignee = "";

		/// <summary>
		/// The user set to resolve the item after completion.
		/// </summary>
		[RecommendedOptionDefault("\"\"")]
		public string Resolver = "";

		/// <summary>
		/// A description of the item.
		/// </summary>
		public string Description = "";

		/// <summary>
		/// The priority of the item (i.e. Unknown, Low, Urgent, etc).
		/// </summary>
		public string Priority = "";

		/// <summary>
		/// The details of the item.
		/// </summary>
		public string Details = "";

		/// <summary>
		/// The version of the item.
		/// </summary>
		[RecommendedOptionDefault("\"\"")]
		public string VersionStr = "";

		/// <summary>
		/// A custom field.
		/// </summary>
		[RecommendedOptionDefault("\"\"")]
		public string Custom1 = "";

		/// <summary>
		/// A custom field.
		/// </summary>
		[RecommendedOptionDefault("\"\"")]
		public string Custom2 = "";

		/// <summary>
		/// The category to which the item will belong.
		/// </summary>
		[RecommendedOptionDefault("\"\"")]
		public string Category = "";

		/// <summary>
		/// The milestone to which the item will belong.
		/// </summary>
		[RecommendedOptionDefault("\"\"")]
		public string Milestone = "";

		/// <summary>
		/// True to use html in the details section, false otherwise.
		/// </summary>
		[RecommendedOptionDefault("false")]
		public bool UseHtmlInDetails = false;

		#endregion

		/// <summary>
		/// Creates a new FortressItemExpanded object.
		/// </summary>
		public FortressItemExpanded()
		{
			item = new MantisItemExpanded();
		}

		/// <summary>
		/// Creates a MantisItem from the FortressItemExpanded object.
		/// </summary>
		/// <returns>a MantisItem object</returns>
		[Hidden]
		public MantisItem GetMantisItem()
		{
			return new MantisItem(item);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="item"></param>
		[Hidden]
		public void UpdateWithNewMantisItem(MantisItemFullDetail item)
		{
			this.item = (MantisItemExpanded)item;
		}

		/// <summary>
		/// Validates/constructs an item based on the information contained in the public fields.
		/// </summary>
		public void Validate()
		{
			if (!this.ProjectName.Equals(""))
				SetProject(this.ProjectName);
			if (!this.ItemType.Equals(""))
				SetItemType(this.ItemType);
			if (!this.Status.Equals(""))
				SetStatus(this.Status);
			if (!this.Assignee.Equals(""))
				SetAssignee(this.Assignee);
			if (!this.Resolver.Equals(""))
				SetResolver(this.Resolver);
			if (!this.Category.Equals(""))
				SetCategory(this.Category);
			if (!this.Milestone.Equals(""))
				SetMilestone(this.Milestone);
			if (!this.Platform.Equals(""))
				SetPlatform(this.Platform);
			if (!this.Priority.Equals(""))
				SetPriority(this.Priority);
			if (!this.Description.Equals(""))
				SetDescription(this.Description);
			if (!this.Details.Equals(""))
				SetDetails(this.Details);
			if (!this.VersionStr.Equals(""))
				SetVersion(this.VersionStr);
			if (!this.Custom1.Equals(""))
				SetCustom1(this.Custom1);
			if (!this.Custom2.Equals(""))
				SetCustom2(this.Custom2);
			if (!this.TimeEstimateUnitLabel.Equals(""))
				SetEstimateTimeUnit(this.TimeEstimateUnitLabel);

			if (this.TimeEstimateValue < 0) { this.TimeEstimateValue = 0; }

			if (!this.DueDate.Equals(""))
				SetDueDate(this.DueDate);

			SetUseHtmlInDetails(this.UseHtmlInDetails);
		}

		#region Use strings to set item fields

		private void SetItemType(string type)
		{
			if (types == null)
			{
				types = ItemTrackingOperations.ProcessCommandListFortressItemTypes();
			}

			foreach (MantisItemType t in types)
			{
				if (t.Label.ToLower().Equals(type.ToLower()))
				{
					item.TypeID = t.ID;
					return;
				}
			}

			// if we've reached this point, the string didn't match anything, so throw if throwExceptions is true
			if (throwExceptions)
				throw new Exception(string.Format("{0} is not a type recognized by {1}.", type, VaultLib.Brander.ProductName));

		}

		private void SetStatus(string status)
		{
			if (statuses == null)
			{
				statuses = ItemTrackingOperations.ProcessCommandListFortressStatuses();
			}

			foreach (MantisStatus s in statuses)
			{
				if (s.Label.ToLower().Equals(status.ToLower()))
				{
					item.StatusID = s.ID;
					return;
				}
			}

			// if we've reached this point, the string didn't match anything, so throw if throwExceptions is true
			if (throwExceptions)
				throw new Exception(string.Format("{0} is not a status recognized by {1}.", status, VaultLib.Brander.ProductName));
		}

		private void SetAssignee(string assignee)
		{
			if (users == null)
			{
				if (projectID != -1)
				{
					users = ItemTrackingOperations.ProcessCommandListFortressUsers(projectID);
				}
				else
				{
					throw new Exception("This operation requires that a project be set.");
				}
			}

			foreach (MantisUser u in users)
			{
				if (u.Login.ToLower().Equals(assignee.ToLower()))
				{
					item.AssigneeID = u.ID;
					return;
				}
			}

			// if we've reached this point, the string didn't match anything, so throw if throwExceptions is true
			if (throwExceptions)
				throw new Exception(string.Format("{0} is not a user login recognized by {1}.", assignee, VaultLib.Brander.ProductName));
		}

		private void SetResolver(string resolver)
		{
			if (users == null)
			{
				if (projectID != -1)
				{
					users = ItemTrackingOperations.ProcessCommandListFortressUsers(projectID);
				}
				else
				{
					throw new Exception("This operation requires that a project be set.");
				}
			}

			foreach (MantisUser u in users)
			{
				if (u.Login.ToLower().Equals(resolver.ToLower()))
				{
					item.ResolverID = u.ID;
					return;
				}
			}

			// if we've reached this point, the string didn't match anything, so throw if throwExceptions is true
			if (throwExceptions)
				throw new Exception(string.Format("{0} is not a user login recognized by {1}.", resolver, VaultLib.Brander.ProductName));
		}

		private void SetProject(string project)
		{
			if (projects == null)
			{
				projects = ItemTrackingOperations.ProcessCommandListFortressProjects();
			}

			foreach (MantisProject p in projects)
			{
				if (p.Name.ToLower().Equals(project.ToLower()))
				{
					item.ProjectID = p.ID;
					projectID = p.ID;
					return;
				}
			}

			// if we've reached this point, the string didn't match anything, so throw if throwExceptions is true
			if (throwExceptions)
				throw new Exception(string.Format("{0} is not a project name recognized by {1}.", project, VaultLib.Brander.ProductName));
		}

		private void SetCategory(string category)
		{
			if (categories == null)
			{
				if (projectID != -1)
				{
					categories = ItemTrackingOperations.ProcessCommandListFortressCategories(projectID);
				}
				else
				{
					throw new Exception("This operation requires that a project be set.");
				}
			}

			foreach (MantisCategory c in categories)
			{
				if (c.Label.ToLower().Equals(category.ToLower()))
				{
					item.CategoryID = c.ID;
					return;
				}
			}

			// if we've reached this point, the string didn't match anything, so throw if throwExceptions is true
			if (throwExceptions)
				throw new Exception(string.Format("{0} is not a category recognized by {1}.", category, VaultLib.Brander.ProductName));
		}

		private void SetMilestone(string milestone)
		{
			if (milestones == null)
			{
				if (projectID != -1)
				{
					milestones = ItemTrackingOperations.ProcessCommandListFortressMilestones(projectID);
				}
				else
				{
					throw new Exception("This operation requires that a project be set.");
				}
			}

			foreach (MantisMilestone m in milestones)
			{
				if (m.Name.ToLower().Equals(milestone.ToLower()))
				{
					item.MilestoneID = m.ID;
					return;
				}
			}

			// if we've reached this point, the string didn't match anything, so throw if throwExceptions is true
			if (throwExceptions)
				throw new Exception(string.Format("{0} is not a milestone recognized by {1}.", milestone, VaultLib.Brander.ProductName));
		}

		private void SetPriority(string priority)
		{
			if (priorities == null)
			{
				priorities = ItemTrackingOperations.ProcessCommandListFortressPriorities();
			}

			foreach (MantisPriority p in priorities)
			{
				if (p.Label.ToLower().Equals(priority.ToLower()))
				{
					item.PriorityID = p.ID;
					return;
				}
			}

			// if we've reached this point, the string didn't match anything, so throw if throwExceptions is true
			if (throwExceptions)
				throw new Exception(string.Format("{0} is not a priority recognized by {1}.", priority, VaultLib.Brander.ProductName));
		}

		private void SetPlatform(string platform)
		{
			if (platforms == null)
			{
				platforms = ItemTrackingOperations.ProcessCommandListFortressPlatforms();
			}

			foreach (MantisPlatform p in platforms)
			{
				if (p.Label.ToLower().Equals(platform.ToLower()))
				{
					item.PlatformID = p.ID;
					return;
				}
			}

			// if we've reached this point, the string didn't match anything, so throw if throwExceptions is true
			if (throwExceptions)
				throw new Exception(string.Format("{0} is not a platform recognized by {1}.", platform, VaultLib.Brander.ProductName));
		}

		private void SetDescription(string description)
		{
			item.Description = description;
		}

		private void SetDetails(string details)
		{
			item.Details = details;
		}

		private void SetVersion(string version)
		{
			item.Version = version;
		}

		private void SetCustom1(string custom1)
		{
			item.Custom1 = custom1;
		}

		private void SetCustom2(string custom2)
		{
			item.Custom2 = custom2;
		}

		private void SetEstimateTimeUnit(string strTimeUnit)
		{
			if (timeunits == null)
			{
				timeunits = ItemTrackingOperations.ProcessCommandListFortressTimeUnits();
			}

			bool bFound = false;
			foreach (MantisTimeUnit tu in timeunits)
			{
				if (string.Compare(tu.Label, strTimeUnit, true) == 0)
				{
					bFound = true;
					item.TimeEstimateUnitID = tu.ID;
					break;
				}
			}

			// if we've reached this point, the string didn't match anything, so throw if throwExceptions is true
			if ((bFound == true) && (throwExceptions == true))
				throw new Exception(string.Format("{0} is not a time unit recognized by {1}.", strTimeUnit, VaultLib.Brander.ProductName));
		}

		private void SetUseHtmlInDetails(bool useHtmlInDetails)
		{
			item.Html = useHtmlInDetails;
		}

		private void SetDueDate(string dueDate)
		{
			if ((dueDate != null) && (dueDate.Length == 10))
			{
				item.DueDate = new DateTime(Convert.ToInt32(dueDate.Substring(0, 4)), Convert.ToInt32(dueDate.Substring(5, 2)), Convert.ToInt32(dueDate.Substring(8, 2)));
			}
		}
		#endregion
	}

	/// <summary>
	/// Summary description for ItemTrackingOperations.
	/// </summary>
	public class ItemTrackingOperations
	{
		/// <summary>
		/// Default constructor.
		/// </summary>
		public ItemTrackingOperations()
		{
			//
			// TODO: Add constructor logic here
			//
		}

		/// <summary>
		/// Returns an array containing Fortress projects.
		/// </summary>
		/// <returns>An array of MantisProject objects describing all the Work Item projects.</returns>
		[DoesNotRequireRepository]
		public static MantisProject[] ProcessCommandListFortressProjects()
		{
			MantisProject[] projects = new MantisProject[0];
			if (ServerOperations.client.ClientInstance.Connection.LoggedIntoDragnet == false)
				ServerOperations.client.ClientInstance.Connection.InitDragnetService();

			ServerOperations.client.ClientInstance.Connection.ListDragnetProjects(out projects);

			return projects;
		}

		/// <summary>
		/// Returns an array of items matching the specifications of the query.
		/// </summary>
		/// <param name="qf">The MantisItemQueryFilter to process</param>
		/// <param name="sendDetails">True to include the details field in the returned items, false otherwise</param>
		/// <returns>An array of MantisItemExpanded objects matching the criteria of the query.</returns>
		[DoesNotRequireRepository, RecommendedOptionDefault("sendDetails", "true")]
		public static MantisItemExpanded[] ProcessCommandQueryFortressItems(MantisItemQueryFilter qf, bool sendDetails)
		{
			MantisItemExpanded[] items = new MantisItemExpanded[0];


			if (ServerOperations.client.ClientInstance.Connection.LoggedIntoDragnet == false)
				ServerOperations.client.ClientInstance.Connection.InitDragnetService();

			CloudColl clouds;
			ServerOperations.client.ClientInstance.Connection.QueryDragnetItems(qf, sendDetails, out items, out clouds);

			return items;

		}

		/// <summary>
		/// Lists the Work Item categories for a given project.
		/// </summary>
		/// <param name="projectName">The name of the project.</param>
		/// <returns>An array of MantisCategory objects describing all the categories for the given project.</returns>
		[DoesNotRequireRepository]
		public static MantisCategory[] ProcessCommandListFortressCategories(string projectName)
		{
			MantisCategory[] categories = new MantisCategory[0];

			if (ServerOperations.client.ClientInstance.Connection.LoggedIntoDragnet == false)
				ServerOperations.client.ClientInstance.Connection.InitDragnetService();

			MantisProject[] projects = new MantisProject[0];
			ServerOperations.client.ClientInstance.Connection.ListDragnetProjects(out projects);
			int pid = -1;
			foreach (MantisProject p in projects)
			{
				if (p.Name.Equals(projectName))
				{
					pid = p.ID;
					break;
				}
			}

			if (pid != -1)
			{
				categories = ProcessCommandListFortressCategories(pid);
			}
			else
			{
				throw new Exception(string.Format("The work item project {0} could not be found.", projectName));
			}

			return categories;
		}

		/// <summary>
		/// Lists the work item categories for a given project.
		/// </summary>
		/// <param name="projectID">The project id for which categories will be listed.</param>
		/// <returns>An array of MantisCategory objects.</returns>
		[Hidden]
		public static MantisCategory[] ProcessCommandListFortressCategories(int projectID)
		{
			MantisCategory[] categories = new MantisCategory[0];

			if (ServerOperations.client.ClientInstance.Connection.LoggedIntoDragnet == false)
				ServerOperations.client.ClientInstance.Connection.InitDragnetService();

			ServerOperations.client.ClientInstance.Connection.ListDragnetCategories(projectID, out categories);

			return categories;
		}

		/// <summary>
		/// Lists the work item custom labels.
		/// </summary>
		/// <returns>An array of MantisCustomLabel objects describing the work item custom labels.</returns>
		[DoesNotRequireRepository]
		public static MantisCustomLabel[] ProcessCommandListFortressCustomLabels()
		{
			MantisCustomLabel[] labels = new MantisCustomLabel[0];

			if (ServerOperations.client.ClientInstance.Connection.LoggedIntoDragnet == false)
				ServerOperations.client.ClientInstance.Connection.InitDragnetService();

			ServerOperations.client.ClientInstance.Connection.ListDragnetCustomLabels(out labels);

			return labels;
		}

		/// <summary>
		/// Lists all work item milestones for a given project.
		/// </summary>
		/// <param name="projectName">The name of the project.</param>
		/// <returns>An array of MantisMilestone objects describing all the milestones for the given project.</returns>
		[DoesNotRequireRepository]
		public static MantisMilestone[] ProcessCommandListFortressMilestones(string projectName)
		{
			MantisMilestone[] milestones = new MantisMilestone[0];

			if (ServerOperations.client.ClientInstance.Connection.LoggedIntoDragnet == false)
				ServerOperations.client.ClientInstance.Connection.InitDragnetService();

			MantisProject[] projects = new MantisProject[0];
			ServerOperations.client.ClientInstance.Connection.ListDragnetProjects(out projects);
			int pid = -1;
			foreach (MantisProject p in projects)
			{
				if (p.Name.Equals(projectName))
				{
					pid = p.ID;
					break;
				}
			}

			if (pid != -1)
			{
				milestones = ProcessCommandListFortressMilestones(pid);
			}
			else
			{
				throw new Exception(string.Format("The work item project {0} could not be found.", projectName));
			}

			return milestones;
		}

		/// <summary>
		/// Lists all work item milestones for a given project.
		/// </summary>
		/// <param name="projectID">The project id for which milestones will be listed.</param>
		/// <returns>An array of MantisMilestone objects.</returns>
		[Hidden]
		public static MantisMilestone[] ProcessCommandListFortressMilestones(int projectID)
		{
			MantisMilestone[] milestones = new MantisMilestone[0];

			if (ServerOperations.client.ClientInstance.Connection.LoggedIntoDragnet == false)
				ServerOperations.client.ClientInstance.Connection.InitDragnetService();

			ServerOperations.client.ClientInstance.Connection.ListDragnetMilestones(projectID, out milestones);

			return milestones;
		}

		/// <summary>
		/// Lists all work item platforms.
		/// </summary>
		/// <returns>An array of MantisPlatform objects describing all the platforms.</returns>
		[DoesNotRequireRepository]
		public static MantisPlatform[] ProcessCommandListFortressPlatforms()
		{
			MantisPlatform[] platforms = new MantisPlatform[0];

			if (ServerOperations.client.ClientInstance.Connection.LoggedIntoDragnet == false)
				ServerOperations.client.ClientInstance.Connection.InitDragnetService();

			ServerOperations.client.ClientInstance.Connection.ListDragnetPlatforms(out platforms);

			return platforms;
		}

		/// <summary>
		/// Lists all work item priorities.
		/// </summary>
		/// <returns>An array of MantisPriority objects describing all the priorities.</returns>
		[DoesNotRequireRepository]
		public static MantisPriority[] ProcessCommandListFortressPriorities()
		{
			MantisPriority[] priorities = new MantisPriority[0];

			if (ServerOperations.client.ClientInstance.Connection.LoggedIntoDragnet == false)
				ServerOperations.client.ClientInstance.Connection.InitDragnetService();

			ServerOperations.client.ClientInstance.Connection.ListDragnetPriorities(out priorities);

			return priorities;
		}

		/// <summary>
		/// Lists all work item statuses.
		/// </summary>
		/// <returns>An array of MantisStatus objects describing all the statuses.</returns>
		[DoesNotRequireRepository]
		public static MantisStatus[] ProcessCommandListFortressStatuses()
		{
			MantisStatus[] statuses = new MantisStatus[0];

			if (ServerOperations.client.ClientInstance.Connection.LoggedIntoDragnet == false)
				ServerOperations.client.ClientInstance.Connection.InitDragnetService();

			ServerOperations.client.ClientInstance.Connection.ListDragnetStatuses(out statuses);

			return statuses;
		}

		/// <summary>
		/// Lists all work item time estimates.
		/// </summary>
		/// <returns>An empty array of MantisTimeEstimate objects describing all the time estimates.</returns>
		[DoesNotRequireRepository]
		public static MantisTimeEstimate[] ProcessCommandListFortressTimeEstimates()
		{
			MantisTimeEstimate[] arTimeEst = new MantisTimeEstimate[1];
			arTimeEst[0].Hours = 1;
			arTimeEst[0].ID = 2;
			arTimeEst[0].Label = "One Hour";
			arTimeEst[0].Status = DbStatus.System;

			return arTimeEst;
		}


		/// <summary>
		/// Lists all work item types.
		/// </summary>
		/// <returns>An array of MantisItemType objects describing all the item types.</returns>
		[DoesNotRequireRepository]
		public static MantisItemType[] ProcessCommandListFortressItemTypes()
		{
			MantisItemType[] types = new MantisItemType[0];

			if (ServerOperations.client.ClientInstance.Connection.LoggedIntoDragnet == false)
				ServerOperations.client.ClientInstance.Connection.InitDragnetService();

			ServerOperations.client.ClientInstance.Connection.ListDragnetTypes(out types);

			return types;
		}

		/// <summary>
		/// Lists all work item time units
		/// </summary>
		/// <returns>An array of MantisTimeUnit objects describing all the time units.</returns>
		[DoesNotRequireRepository]
		public static MantisTimeUnit[] ProcessCommandListFortressTimeUnits()
		{
			MantisTimeUnit[] tu = new MantisTimeUnit[0];

			if (ServerOperations.client.ClientInstance.Connection.LoggedIntoDragnet == false)
				ServerOperations.client.ClientInstance.Connection.InitDragnetService();

			ServerOperations.client.ClientInstance.Connection.ListDragnetTimeUnits(out tu);

			return tu;
		}


		/// <summary>
		/// Lists all Vault Pro users for a given project.
		/// </summary>
		/// <param name="projectName">The name of the project for which users will be listed.</param>
		/// <returns>An array of MantisUser objects describing all the users for the given project.</returns>
		[DoesNotRequireRepository]
		public static MantisUser[] ProcessCommandListFortressUsers(string projectName)
		{
			MantisUser[] users = new MantisUser[0];

			if (ServerOperations.client.ClientInstance.Connection.LoggedIntoDragnet == false)
				ServerOperations.client.ClientInstance.Connection.InitDragnetService();

			MantisProject[] projects = new MantisProject[0];
			ServerOperations.client.ClientInstance.Connection.ListDragnetProjects(out projects);
			int pid = -1;
			foreach (MantisProject p in projects)
			{
				if (p.Name.Equals(projectName))
				{
					pid = p.ID;
					break;
				}
			}

			if (pid != -1)
			{
				users = ProcessCommandListFortressUsers(pid);
			}
			else
			{
				throw new Exception(string.Format("The work item project {0} could not be found.", projectName));
			}

			return users;
		}

		/// <summary>
		/// Lists all Vault Pro users for a given project.
		/// </summary>
		/// <param name="projectID">The project id for which users will be listed.</param>
		/// <returns>An array of MantisUser objects.</returns>
		[Hidden]
		public static MantisUser[] ProcessCommandListFortressUsers(int projectID)
		{
			MantisUser[] users = new MantisUser[0];

			if (ServerOperations.client.ClientInstance.Connection.LoggedIntoDragnet == false)
				ServerOperations.client.ClientInstance.Connection.InitDragnetService();

			ServerOperations.client.ClientInstance.Connection.ListDragnetUsers(projectID, out users);

			return users;
		}

		/// <summary>
		/// Returns the full details of a given item.
		/// </summary>
		/// <param name="itemID">The id of the item for which full details will be returned.</param>
		/// <returns>A MantisItemFullDetail object describing the given item.</returns>
		[DoesNotRequireRepository]
		public static MantisItemFullDetail ProcessCommandListFortressItemFullDetails(int itemID)
		{
			MantisItemFullDetail mifd = null;

			if (ServerOperations.client.ClientInstance.Connection.LoggedIntoDragnet == false)
				ServerOperations.client.ClientInstance.Connection.InitDragnetService();

			ServerOperations.client.ClientInstance.Connection.ListItemFullDetails(itemID, out mifd);

			return mifd;
		}

		/// <summary>
		/// Lists At A Glance information.
		/// </summary>
		/// <param name="projectName">The name of the project for which information will be listed.</param>
		/// <returns>An array of MilestoneAaG objects describing the at a glance information for each milestone in the given project.</returns>
		[DoesNotRequireRepository]
		public static MilestoneAaG[] ProcessCommandFortressListAtAGlance(string projectName)
		{
			MilestoneAaG[] ataglance = new MilestoneAaG[0];

			if (ServerOperations.client.ClientInstance.Connection.LoggedIntoDragnet == false)
				ServerOperations.client.ClientInstance.Connection.InitDragnetService();

			MantisProject[] projects = new MantisProject[0];
			ServerOperations.client.ClientInstance.Connection.ListDragnetProjects(out projects);
			int pid = -1;
			foreach (MantisProject p in projects)
			{
				if (p.Name.Equals(projectName))
				{
					pid = p.ID;
					break;
				}
			}

			if (pid != -1)
			{
				ataglance = ProcessCommandFortressListAtAGlance(pid);
			}
			else
			{
				throw new Exception(string.Format("The work item project {0} could not be found.", projectName));
			}

			return ataglance;
		}

		/// <summary>
		/// Lists At A Glance information.
		/// </summary>
		/// <param name="projectID">The project id for which information will be listed.</param>
		/// <returns>An array of MilestoneAaG objects.</returns>
		[Hidden]
		public static MilestoneAaG[] ProcessCommandFortressListAtAGlance(int projectID)
		{
			MilestoneAaG[] ataglance = new MilestoneAaG[0];

			if (ServerOperations.client.ClientInstance.Connection.LoggedIntoDragnet == false)
				ServerOperations.client.ClientInstance.Connection.InitDragnetService();

			ServerOperations.client.ClientInstance.Connection.ListAtAGlance(projectID, out ataglance);

			return ataglance;
		}

		/// <summary>
		/// Lists all open items for a given project.
		/// </summary>
		/// <param name="projectName">The name of the project for which items will be listed.</param>
		/// <returns>An array of MantisItemExpanded objects describing all the open items for the given project.</returns>
		[DoesNotRequireRepository]
		public static MantisItemExpanded[] ProcessCommandListOpenFortressItems(string projectName)
		{
			MantisItemExpanded[] items = new MantisItemExpanded[0];

			if (ServerOperations.client.ClientInstance.Connection.LoggedIntoDragnet == false)
				ServerOperations.client.ClientInstance.Connection.InitDragnetService();

			MantisProject[] projects = new MantisProject[0];
			ServerOperations.client.ClientInstance.Connection.ListDragnetProjects(out projects);
			int pid = -1;
			foreach (MantisProject p in projects)
			{
				if (p.Name.Equals(projectName))
				{
					pid = p.ID;
					break;
				}
			}

			if (pid != -1)
			{
				items = ProcessCommandListOpenFortressItems(pid);
			}
			else
			{
				throw new Exception(string.Format("The work item project {0} could not be found.", projectName));
			}

			return items;
		}

		/// <summary>
		/// Lists all open items for a given project.
		/// </summary>
		/// <param name="projectID">The project id for which items will be listed.</param>
		/// <returns>An array of MantisItemExpanded objects.</returns>
		[Hidden]
		public static MantisItemExpanded[] ProcessCommandListOpenFortressItems(int projectID)
		{
			MantisItemExpanded[] items = new MantisItemExpanded[0];

			if (ServerOperations.client.ClientInstance.Connection.LoggedIntoDragnet == false)
				ServerOperations.client.ClientInstance.Connection.InitDragnetService();

			CloudColl clouds;
			ServerOperations.client.ClientInstance.Connection.ListDragnetOpenItems(projectID, true, out items, out clouds);

			return items;
		}

		/// <summary>
		/// Lists the open bug tracking items for the user (currently logged in) for the given project.
		/// </summary>
		/// <param name="projectName">The name of the project for which items will be listed.</param>
		/// <returns>An array of MantisItemExpanded objects describing all the open items assigned to the user currently logged in for the given project.</returns>
		[DoesNotRequireRepository]
		public static MantisItemExpanded[] ProcessCommandListMyOpenFortressItems(string projectName)
		{
			MantisItemExpanded[] items = new MantisItemExpanded[0];

			if (ServerOperations.client.ClientInstance.Connection.LoggedIntoDragnet == false)
				ServerOperations.client.ClientInstance.Connection.InitDragnetService();

			MantisProject[] projects = new MantisProject[0];
			ServerOperations.client.ClientInstance.Connection.ListDragnetProjects(out projects);
			int pid = -1;
			foreach (MantisProject p in projects)
			{
				if (p.Name.Equals(projectName))
				{
					pid = p.ID;
					break;
				}
			}

			if (pid != -1)
			{
				items = ProcessCommandListMyOpenFortressItems(pid);
			}
			else
			{
				throw new Exception(string.Format("The work item project {0} could not be found.", projectName));
			}

			return items;
		}

		/// <summary>
		/// Lists the open bug tracking items for the user (currently logged in) for the given project.
		/// </summary>
		/// <param name="projectID">The project id for which items will be listed.</param>
		/// <returns>An array of MantisItemExpanded objects.</returns>
		[Hidden]
		public static MantisItemExpanded[] ProcessCommandListMyOpenFortressItems(int projectID)
		{
			MantisItemExpanded[] items = new MantisItemExpanded[0];

			if (ServerOperations.client.ClientInstance.Connection.LoggedIntoDragnet == false)
				ServerOperations.client.ClientInstance.Connection.InitDragnetService();

			CloudColl clouds;
			ServerOperations.client.ClientInstance.Connection.ListDragnetMyOpenItems(projectID, true, out items, out clouds);

			return items;
		}

		/// <summary>
		/// Returns an object containing the full info for a bug tracking item attachment.
		/// </summary>
		/// <param name="msgID">The message id.</param>
		/// <param name="attID">The attachment id.</param>
		/// <returns>A MantisItemAttachmentFullDetail object describing the work item attachment.</returns>
		[DoesNotRequireRepository]
		public static MantisItemAttachmentFullDetail ProcessCommandListFortressItemAttachmentInfo(int msgID, int attID)
		{
			MantisItemAttachmentFullDetail miafd = new MantisItemAttachmentFullDetail();

			if (ServerOperations.client.ClientInstance.Connection.LoggedIntoDragnet == false)
				ServerOperations.client.ClientInstance.Connection.InitDragnetService();

			ServerOperations.client.ClientInstance.Connection.DragnetServiceInstance.ListAttachmentInfo(msgID, attID, out miafd);

			return miafd;
		}

		/// <summary>
		/// Saves changes to an existing bug tracking item.
		/// </summary>
		/// <param name="item">The MantisItem which has been modified.</param>
		[DoesNotRequireRepository]
		public static MantisItem ProcessCommandModifyFortressItem(MantisItem item)
		{
			MantisItem mi = item;
			if (ServerOperations.client.ClientInstance.Connection.LoggedIntoDragnet == false)
				ServerOperations.client.ClientInstance.Connection.InitDragnetService();

			//ServerOperations.client.ClientInstance.ModifyItem(item);
			ServerOperations.client.ClientInstance.ModifyItem2(ref mi);

			return mi;
		}

		/// <summary>
		/// Adds a new bug tracking item.
		/// </summary>
		/// <param name="item">The FortressItemExpanded to be added.</param>
		/// <returns>The new FortressItemExpanded.</returns>
		[DoesNotRequireRepository]
		public static FortressItemExpanded ProcessCommandAddFortressItem(FortressItemExpanded item)
		{
			MantisItem addedItem = ProcessCommandAddFortressItem(item.GetMantisItem());
			if (addedItem.ID > 0)
			{
				MantisItemFullDetail mifd = ProcessCommandListFortressItemFullDetails(addedItem.ID);
				item.UpdateWithNewMantisItem(mifd);
				return item;
			}
			else
			{
				throw new Exception("An unknown error occurred while attempting to add a work item.");
			}
		}

		/// <summary>
		/// Adds a new bug tracking item.
		/// </summary>
		/// <param name="item">The MantisItem to be added.</param>
		/// <returns>The new MantisItem.</returns>
		[Hidden]
		public static MantisItem ProcessCommandAddFortressItem(MantisItem item)
		{
			MantisItem mi = item;
			if (ServerOperations.client.ClientInstance.Connection.LoggedIntoDragnet == false)
				ServerOperations.client.ClientInstance.Connection.InitDragnetService();

			ServerOperations.client.ClientInstance.AddItem(ref mi);

			return mi;
		}

		/// <summary>
		/// Saves a new query as a SavedQuery object.
		/// </summary>
		/// <param name="queryName">The name of the query as a string.</param>
		/// <param name="qf">The MantisItemQueryFilter to be saved.</param>
		/// <returns>A SavedQuery object.</returns>
		[DoesNotRequireRepository]
		public static SavedQuery ProcessCommandSaveNewFortressQuery(string queryName, MantisItemQueryFilter qf)
		{
			SavedQuery sq = new SavedQuery();

			if (ServerOperations.client.ClientInstance.Connection.LoggedIntoDragnet == false)
				ServerOperations.client.ClientInstance.Connection.InitDragnetService();

			ServerOperations.client.ClientInstance.Connection.SaveNewDragnetQuery(queryName, qf, ref sq);

			return sq;
		}

		/// <summary>
		/// Runs and returns the results of a given saved query.
		/// </summary>
		/// <param name="projectName">The name of the parent project.</param>
		/// <param name="queryName">The name of the saved query.</param>
		/// <param name="sendDetails">true to include the details field in the returned items, false otherwise.</param>
		/// <returns>An array of MantisItemExpanded objects matching the criteria of the given query.</returns>
		[DoesNotRequireRepository]
		public static MantisItemExpanded[] ProcessCommandRunSavedFortressQuery(string projectName, string queryName, bool sendDetails)
		{
			MantisItemExpanded[] items = new MantisItemExpanded[0];

			if (ServerOperations.client.ClientInstance.Connection.LoggedIntoDragnet == false)
				ServerOperations.client.ClientInstance.Connection.InitDragnetService();

			MantisProject[] projects = new MantisProject[0];
			ServerOperations.client.ClientInstance.Connection.ListDragnetProjects(out projects);
			int pid = -1;
			foreach (MantisProject p in projects)
			{
				if (p.Name.Equals(projectName))
				{
					pid = p.ID;
					break;
				}
			}

			if (pid != -1)
			{
				SavedQuery[] queries = new SavedQuery[0];
				ServerOperations.client.ClientInstance.Connection.ListSavedQueries(pid, ref queries);
				int qid = -1;
				foreach (SavedQuery q in queries)
				{
					if (q.Name.Equals(queryName))
					{
						qid = q.QueryID;
						break;
					}
				}

				if (qid != -1)
				{
					SavedQuery sq = null;
					ServerOperations.client.ClientInstance.Connection.GetSavedQuery(qid, ref sq);
					if (sq != null)
					{
						items = ProcessCommandRunSavedFortressQuery(qid, sendDetails, sq);
					}
					else
					{
						//error retrieving SavedQuery
						throw new Exception("An unknown error occurred while retrieving the saved query.");
					}
				}
				else
				{
					//query not found
					throw new Exception("The requested saved query could not be found.");
				}
			}
			else
			{
				//project not found
				throw new Exception(string.Format("The work item project {0} could not be found.", projectName));
			}

			return items;
		}

		/// <summary>
		/// Runs and returns the results of a given SavedQuery.
		/// </summary>
		/// <param name="qid">The id of the SavedQuery.</param>
		/// <param name="sendDetails">true to include the details field in the returned items, false otherwise.</param>
		/// <param name="sq">The SavedQuery object.</param>
		/// <returns>An array of MantisItemExpanded objects.</returns>
		[Hidden]
		public static MantisItemExpanded[] ProcessCommandRunSavedFortressQuery(int qid, bool sendDetails, SavedQuery sq)
		{
			MantisItemExpanded[] items = new MantisItemExpanded[0];

			if (ServerOperations.client.ClientInstance.Connection.LoggedIntoDragnet == false)
				ServerOperations.client.ClientInstance.Connection.InitDragnetService();

			CloudColl clouds;
			ServerOperations.client.ClientInstance.Connection.RunSavedDragnetQuery(qid, sendDetails, ref sq, out items, out clouds);

			return items;
		}

		/// <summary>
		/// Saves modifications to a SavedQuery object.
		/// </summary>
		/// <param name="sq">The SavedQuery object which has been modified.</param>
		[DoesNotRequireRepository]
		public static void ProcessCommandModifySavedFortressQuery(SavedQuery sq)
		{
			if (ServerOperations.client.ClientInstance.Connection.LoggedIntoDragnet == false)
				ServerOperations.client.ClientInstance.Connection.InitDragnetService();

			ServerOperations.client.ClientInstance.Connection.ModifyDragnetQuery(sq);
		}

		/// <summary>
		/// Returns an array of SavedQuery objects for the given project.
		/// 
		/// Note:  The SavedQuery objects will only have the name and id fields set.  
		/// To get the MantisItemQueryFilter, use ProcessCommandGetFortressSavedQuery
		/// to retrieve the full object.
		/// </summary>
		/// <param name="projectName">The name of the project for which saved queries will be retrieved.</param>
		/// <returns>An array of SavedQuery objects describing the saved queries for the given project.</returns>
		[DoesNotRequireRepository]
		public static SavedQuery[] ProcessCommandListFortressSavedQueries(string projectName)
		{
			SavedQuery[] queries = new SavedQuery[0];

			if (ServerOperations.client.ClientInstance.Connection.LoggedIntoDragnet == false)
				ServerOperations.client.ClientInstance.Connection.InitDragnetService();

			MantisProject[] projects = new MantisProject[0];
			ServerOperations.client.ClientInstance.Connection.ListDragnetProjects(out projects);
			int pid = -1;
			foreach (MantisProject p in projects)
			{
				if (p.Name.Equals(projectName))
				{
					pid = p.ID;
					break;
				}
			}

			if (pid != -1)
			{
				queries = ProcessCommandListFortressSavedQueries(pid);
			}
			else
			{
				throw new Exception(string.Format("The work item project {0} could not be found.", projectName));
			}

			return queries;
		}

		/// <summary>
		/// Returns an array of SavedQuery objects for the given project id.
		/// 
		/// Note:  The SavedQuery objects will only have the name and id fields set.  
		/// To get the MantisItemQueryFilter, use ProcessCommandGetFortressSavedQuery
		/// to retrieve the full object.
		/// </summary>
		/// <param name="projectID">The project id for which saved queries will be retrieved.</param>
		/// <returns>An array of SavedQuery objects</returns>
		[Hidden]
		public static SavedQuery[] ProcessCommandListFortressSavedQueries(int projectID)
		{
			SavedQuery[] queries = new SavedQuery[0];

			if (ServerOperations.client.ClientInstance.Connection.LoggedIntoDragnet == false)
				ServerOperations.client.ClientInstance.Connection.InitDragnetService();

			ServerOperations.client.ClientInstance.Connection.ListSavedQueries(projectID, ref queries);

			return queries;
		}

		[Hidden]
		public static CloudItem[][] ProcessCommandGetOpenItemTags(int projID, bool bOrderFlatAlpha, int pageby)
		{
			if (ServerOperations.client.ClientInstance.Connection.LoggedIntoDragnet == false)
				ServerOperations.client.ClientInstance.Connection.InitDragnetService();

			CloudItem[][] items = new CloudItem[0][];

			ServerOperations.client.ClientInstance.Connection.GetDragnetOpenItemTags(projID, bOrderFlatAlpha, pageby, out items);

			return items;
		}

		/// <summary>
		/// Deletes a query, given the query id.
		/// </summary>
		/// <param name="queryID">The id of the query to delete.</param>
		[DoesNotRequireRepository]
		public static void ProcessCommandDeleteFortressQuery(int queryID)
		{
			if (ServerOperations.client.ClientInstance.Connection.LoggedIntoDragnet == false)
				ServerOperations.client.ClientInstance.Connection.InitDragnetService();

			ServerOperations.client.ClientInstance.Connection.DeleteDragnetQuery(queryID);
		}

		/// <summary>
		/// Retrieves a SavedQuery object given the query id.
		/// </summary>
		/// <param name="queryID">The id of the query to retrieve.</param>
		/// <returns>a SavedQuery object matching the given id.</returns>
		[DoesNotRequireRepository]
		public static SavedQuery ProcessCommandGetFortressSavedQuery(int queryID)
		{
			SavedQuery query = new SavedQuery();

			if (ServerOperations.client.ClientInstance.Connection.LoggedIntoDragnet == false)
				ServerOperations.client.ClientInstance.Connection.InitDragnetService();

			ServerOperations.client.ClientInstance.Connection.GetSavedQuery(queryID, ref query);

			return query;
		}

		/// <summary>
		/// Downloads the requested attachment to the given local path.
		/// </summary>
		/// <param name="msgID">The message id.</param>
		/// <param name="attID">The attachment id.</param>
		/// <param name="filename">The name of the file.</param>
		/// <param name="strReceivedFilePath">The local path to download the attachment to.</param>
		/// <returns>The path of the downloaded attachment as a string.</returns>
		[DoesNotRequireRepository]
		public static string ProcessCommandDownloadFortressAttachment(string msgID, string attID, string filename, string strReceivedFilePath)
		{
			if (ServerOperations.client.ClientInstance.Connection.LoggedIntoDragnet == false)
				ServerOperations.client.ClientInstance.Connection.InitDragnetService();

			long bytesRead = 0;
			ServerOperations.client.ClientInstance.Connection.DownloadDragnetAttachment(msgID, attID, filename, ref strReceivedFilePath, ref bytesRead);

			return strReceivedFilePath;
		}



		/// <summary>
		/// Adds a comment to an existing bug tracking item.
		/// </summary>
		/// <param name="bugID">The id of the bug to which the comment will be added.</param>
		/// <param name="subject">The subject for the comment.</param>
		/// <param name="comment">The comment to be added to the bug.</param>
		/// <param name="useHtml">True to use html, false otherwise.</param>
		[DoesNotRequireRepository, RecommendedOptionDefault("useHtml", "false")]
		public static void ProcessCommandAddFortressItemComment(int bugID, string subject, string comment, bool useHtml) 
		{
			if (ServerOperations.client.ClientInstance.Connection.LoggedIntoDragnet == false)
				ServerOperations.client.ClientInstance.Connection.InitDragnetService();

			MantisItemFullDetail mifd = ProcessCommandListFortressItemFullDetails(bugID);

			MantisItem mi = new MantisItem(mifd);

			int uid = ProcessCommandGetFortressLoggedInUserID();

			string ulogin = "";

			MantisUser[] users = ProcessCommandListFortressUsers(-1);

			foreach (MantisUser user in users)
			{
				if (uid == user.ID)
				{
					ulogin = user.Login;
					break;
				}
			}

			MantisItemComment mic = new MantisItemComment(bugID, uid, ulogin, subject, comment);
			mic.Html = useHtml;

			ServerOperations.client.ClientInstance.Connection.AddItemComment(mi, mic);
		}
		/// <summary>
		/// Adds a comment to an existing bug tracking item.
		/// </summary>
		/// <param name="bugID">The id of the bug to which the comment will be added.</param>
		/// <param name="comment">The comment to be added to the bug.</param>
		/// <param name="useHtml">True to use html, false otherwise.</param>
		[Hidden]
		public static void ProcessCommandAddFortressItemComment(int bugID, string comment, bool useHtml) 
		{
			ProcessCommandAddFortressItemComment(bugID, "", comment, useHtml);
		}
		/// <summary>
		/// Returns the id of the user that is currently logged in.
		/// </summary>
		/// <returns>An int.</returns>
		[Hidden]
		public static int ProcessCommandGetFortressLoggedInUserID()
		{
			if (ServerOperations.client.ClientInstance.Connection.LoggedIntoDragnet == false)
				ServerOperations.client.ClientInstance.Connection.InitDragnetService();

			return ServerOperations.client.ClientInstance.Connection.GetDragnetLoggedInUserID();
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		[Hidden]
		public static bool ProcessCommandIsBugIDValid(int id)
		{
			if (ServerOperations.client.ClientInstance.Connection.LoggedIntoDragnet == false)
				ServerOperations.client.ClientInstance.Connection.InitDragnetService();

			return ServerOperations.client.ClientInstance.Connection.IsBugIDValid(id);
		}
	}

}
