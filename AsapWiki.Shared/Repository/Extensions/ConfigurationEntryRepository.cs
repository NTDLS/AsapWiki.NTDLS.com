using AsapWiki.Shared.ADO;
using AsapWiki.Shared.Models;
using Dapper;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace AsapWiki.Shared.Repository
{
	public static partial class ConfigurationEntryRepository
	{
		public static List<ConfigurationEntry> GetConfigurationEntryValuesByGroupName(string groupName)
		{
			using (var handler = new SqlConnectionHandler())
			{
				var param = new
				{
					GroupName = groupName
				};

				return handler.Connection.Query<ConfigurationEntry>("GetConfigurationEntryValuesByGroupName",
					param, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).ToList();
			}
		}

		public static string GetConfigurationEntryValuesByGroupNameAndEntryName(string groupName, string entryName)
		{
			using (var handler = new SqlConnectionHandler())
			{
				var param = new
				{
					GroupName = groupName,
					EntryName = entryName
				};

				return handler.Connection.Query<ConfigurationEntry>("GetConfigurationEntryValuesByGroupNameAndEntryName",
					param, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).FirstOrDefault()?.Value;
			}
		}
	}
}