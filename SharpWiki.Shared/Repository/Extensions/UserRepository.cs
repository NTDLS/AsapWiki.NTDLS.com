using Dapper;
using SharpWiki.Shared.ADO;
using SharpWiki.Shared.Models.Data;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace SharpWiki.Shared.Repository
{
    public static partial class UserRepository
	{
		public static List<Role> GetUserRolesByUserId(int userID)
		{
			using (var handler = new SqlConnectionHandler())
			{
				return handler.Connection.Query<Role>("GetUserRolesByUserId",
					new { UserID = userID }, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).ToList();
			}
		}

		public static User GetUserById(int id)
		{
			using (var handler = new SqlConnectionHandler())
			{
				return handler.Connection.Query<User>("GetUserById",
					new { Id = id }, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).FirstOrDefault();
			}
		}

		public static User GetUserByNavigation(string navigation)
		{
			using (var handler = new SqlConnectionHandler())
			{
				return handler.Connection.Query<User>("GetUserByNavigation",
					new { Navigation = navigation }, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).FirstOrDefault();
			}
		}

		public static User GetUserByEmail(string emailAddress)
		{
			using (var handler = new SqlConnectionHandler())
			{
				var param = new
				{
					EmailAddress = emailAddress
				};

				return handler.Connection.Query<User>("GetUserByEmail",
					param, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).FirstOrDefault();
			}
		}

		public static User GetUserByEmailAndPassword(string emailAddress, string password)
		{
			string passwordHash = Library.Security.Sha256(password);
			using (var handler = new SqlConnectionHandler())
			{
				var param = new
				{
					EmailAddress = emailAddress,
					PasswordHash = passwordHash
				};

				return handler.Connection.Query<User>("GetUserByEmailAndPasswordHash",
					param, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).FirstOrDefault();
			}
		}

		public static void UpdateUserLastLoginDateByUserId(int userId)
		{
			using (var handler = new SqlConnectionHandler())
			{
				var param = new
				{
					UserId = userId
				};

				handler.Connection.Execute("UpdateUserLastLoginDateByUserId",
					param, null, Singletons.CommandTimeout, CommandType.StoredProcedure);
			}
		}

		public static void UpdateUserAvatar(int userId, byte[] imageData)
		{
			using (var handler = new SqlConnectionHandler())
			{
				var param = new
				{
					UserId = userId,
					Avatar = imageData,
				};

				handler.Connection.Execute("UpdateUserAvatar",
					param, null, Singletons.CommandTimeout, CommandType.StoredProcedure);
			}
		}

		public static byte[] GetUserAvatarBynavigation(string navigation)
		{
			using (var handler = new SqlConnectionHandler())
			{
				return handler.Connection.Query<byte[]>("GetUserAvatarBynavigation",
					new
					{
						Navigation = navigation
					}, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).FirstOrDefault();
			}
		}

		public static void UpdateUser(User item)
		{
			using (var handler = new SqlConnectionHandler())
			{
				var param = new
				{
					Id = item.Id,
					EmailAddress = item.EmailAddress,
					AccountName = item.AccountName,
					Navigation = item.Navigation,
					PasswordHash = item.PasswordHash,
					FirstName = item.FirstName,
					LastName = item.LastName,
					TimeZone = item.TimeZone,
					Country = item.Country,
					AboutMe = item.AboutMe,
					ModifiedDate = item.ModifiedDate
				};

				handler.Connection.Execute("UpdateUser",
					param, null, Singletons.CommandTimeout, CommandType.StoredProcedure);
			}
		}
	}
}