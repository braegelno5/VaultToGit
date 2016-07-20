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

using VaultLib;

namespace VaultClientIntegrationLib
{
    /// <summary>
    /// 
    /// </summary>
	public class GroupComparer : IComparer
	{
        /// <summary>
        /// 
        /// </summary>
		public GroupComparer ()
		{
		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
		int IComparer.Compare( Object x, Object y )  
		{
			if ( ! (x is VaultGroup) || ! (y is VaultGroup) )
				throw new InvalidCastException("One of objects supplied is not of the valid type.");

			VaultGroup item1 = (VaultGroup)x;
			VaultGroup item2 = (VaultGroup)y;

			return item1.Name.CompareTo(item2.Name);
		}

	}

    /// <summary>
    /// 
    /// </summary>
	public class ReverseHistoryItemComparer : IComparer
	{
        /// <summary>
        /// 
        /// </summary>
		public ReverseHistoryItemComparer()
		{
		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
		int IComparer.Compare( Object x, Object y )  
		{
			if ( ! (x is VaultHistoryItem) || ! (y is VaultHistoryItem) )
				throw new InvalidCastException("One of objects supplied is not of the valid type.");

			VaultHistoryItem item1 = (VaultHistoryItem)x;
			VaultHistoryItem item2 = (VaultHistoryItem)y;

			return  item2.TxDate.CompareTo(item1.TxDate);
		}

	}
    /// <summary>
    /// 
    /// </summary>
	public class UserItemComparer : IComparer
	{
        /// <summary>
        /// 
        /// </summary>
		public UserItemComparer()
		{
		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
		int IComparer.Compare( Object x, Object y )  
		{
			if ( ! (x is VaultUser) || ! (y is VaultUser) )
				throw new InvalidCastException("One of objects supplied is not of the valid type.");

			VaultUser item1 = (VaultUser)x;
			VaultUser item2 = (VaultUser)y;

			return item1.Name.CompareTo(item2.Name);
		}

	}


}
