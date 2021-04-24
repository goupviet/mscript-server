using System;
using System.Threading.Tasks;
using System.Collections.Generic;

using metastrings;

namespace metascript
{
    public class User
    {
        public long Id;
        public string Email;
        public string Name;
        public string LoginToken;
        public bool Blocked;
        public DateTime Created;

        public static async Task<User> CreateUserAsync(HttpState state, string email, string name)
        {
            await WebUtils.LogInfoAsync(state, $"CreateUser: {email} - {name}");

            if (await GetUserIdFromEmailAsync(state.MsCtxt, email) >= 0 || await IsNameTakenAsync(state, email))
                throw new UserException("Another user is registered with this email address: " + email);
            
            if (await GetUserIdFromNameAsync(state.MsCtxt, name) >= 0 || await IsNameTakenAsync(state, name))
                throw new UserException("Another user is registered with this name: " + name);

            string userKey = Guid.NewGuid().ToString();
            var define = new Define("users", userKey);
            define.Set("email", email);
            define.Set("name", name);
            define.Set("logintoken", "");
            define.Set("blocked", 0.0);
            await state.MsCtxt.Cmd.DefineAsync(define);

            await RecordNameTakenAsync(state, email);
            await RecordNameTakenAsync(state, name);

            long newUserId = await state.MsCtxt.GetRowIdAsync("users", userKey);
            var newUser =
                new User()
                {
                    Id = newUserId,
                    Email = email,
                    Name = name,
                    LoginToken = "",
                    Blocked = false,
                    Created = DateTime.UtcNow
                };
            return newUser;
        }

        public static async Task<User> GetUserAsync(Context ctxt, long id)
        {
            if (id < 0)
                throw new MException($"User not found: {id}");

            var select = Sql.Parse($"SELECT email, name, logintoken, blocked, created FROM users WHERE id = @userid");
            select.AddParam("@userid", id);
            using (var reader = await ctxt.ExecSelectAsync(select))
            {
                if (!await reader.ReadAsync())
                    throw new MException($"User not found: {id}");

                var newUser =
                    new User()
                    {
                        Id = id,
                        Email = reader.GetString(0),
                        Name = reader.GetString(1),
                        LoginToken = reader.GetString(2),
                        Blocked = reader.GetDouble(3) != 0.0,
                        Created = reader.GetDateTime(4)
                    };
                return newUser;
            }
        }

        public static async Task<string> GetUserEmailAsync(Context ctxt, long id)
        {
            if (id < 0)
                return $"unknown ({id})";

            var select = Sql.Parse("SELECT email FROM users WHERE id = @id");
            select.AddParam("@id", id);
            using (var reader = await ctxt.ExecSelectAsync(select))
            {
                if (!await reader.ReadAsync())
                    return $"unknown ({id})";

                string email = reader.GetString(0);
                return email;
            }
        }

        public static async Task<string> GetUserNameAsync(Context ctxt, long id)
        {
            if (id < 0)
                return $"unknown ({id})";

            var select = Sql.Parse("SELECT name FROM users WHERE id = @id");
            select.AddParam("@id", id);
            using (var reader = await ctxt.ExecSelectAsync(select))
            {
                if (!await reader.ReadAsync())
                    return $"unknown ({id})";

                string name = reader.GetString(0);
                return name;
            }
        }

        public static async Task<long> GetUserIdFromEmailAsync(Context ctxt, string email)
        {
            var query = Sql.Parse("SELECT id FROM users WHERE email = @email");
            query.AddParam("@email", email);
            return await ctxt.ExecScalar64Async(query);
        }

        public static async Task<long> GetUserIdFromNameAsync(Context ctxt, string name)
        {
            var query = Sql.Parse("SELECT id FROM users WHERE name = @name");
            query.AddParam("@name", name);
            return await ctxt.ExecScalar64Async(query);
        }

        public static async Task SetLoginTokenAsync(HttpState state, User user)
        {
            string loginToken = MUtils.CreateToken(6).ToUpper();
            await WebUtils.LogTraceAsync(state, "SetLoginToken: {0}: {1}", user.Email, loginToken);
            string userKey = (await state.MsCtxt.GetRowValueAsync("users", user.Id)).ToString();
            var define = new Define("users", userKey);
            define.Set("logintoken", loginToken);
            await state.MsCtxt.Cmd.DefineAsync(define);
            user.LoginToken = loginToken;
        }

        public static async Task<User> HandleLoginTokenAsync(HttpState state, string token, string email)
        {
            await WebUtils.LogTraceAsync(state, "HandleLoginToken: {0} - {1}", token, email);

            token = token.ToUpper();

            if (string.IsNullOrWhiteSpace(token))
                throw new UserException("The login token is missing");
            WebUtils.ValidateEmail(email);

            var query = Sql.Parse("SELECT id FROM users WHERE logintoken = @token AND email = @email");
            query.AddParam("@token", token).AddParam("@email", email);
            long userId = await state.MsCtxt.ExecScalar64Async(query);
            if (userId < 0)
            {
                userId = await GetUserIdFromEmailAsync(state.MsCtxt, email);
                if (userId >= 0)
                {
                    string userKey = (await state.MsCtxt.GetRowValueAsync("users", userId)).ToString();
                    var define = new Define("users", userKey);
                    define.Set("logintoken", "");
                    await state.MsCtxt.Cmd.DefineAsync(define);
                }
                throw new UserException($"No drummer found with token {token}"); // caller can clear the cookie
            }

            {
                string userKey = (await state.MsCtxt.GetRowValueAsync("users", userId)).ToString();
                var define = new Define("users", userKey);
                define.Set("logintoken", "");
                await state.MsCtxt.Cmd.DefineAsync(define);
            }

            await WebUtils.LogTraceAsync(state, "HandleLoginToken: {0} -> {1}", token, userId);
            return await GetUserAsync(state.MsCtxt, userId); // caller's convenience
        }

        public static async Task DeleteUserAsync(HttpState state, string email)
        {
            await WebUtils.LogInfoAsync(state, $"DeleteUserAsync: {email}");
            bool isFirst = true;
            while (true)
            {
                long userId = await GetUserIdFromEmailAsync(state.MsCtxt, email);
                if (userId < 0)
                {
                    if (isFirst)
                        throw new UserException($"User not found: {email}");
                    else
                        return;
                }
                isFirst = false;

                {
                    var select = Sql.Parse("SELECT value FROM scripts WHERE userid = @userid");
                    select.AddParam("@userid", userId);
                    var scriptValues = await state.MsCtxt.ExecListAsync<object>(select);
                    await state.MsCtxt.Cmd.DeleteAsync("scripts", scriptValues);
                }

                await state.MsCtxt.Cmd.DeleteAsync("users", await state.MsCtxt.GetRowValueAsync("users", userId));
                await Session.ForceUserOutAsync(state, userId);
            }
        }

        public static async Task<List<User>> GetUsersAsync(Context ctxt, string nameQuery, string emailQuery, string tokenQuery, bool onlyBlocked)
        {
            nameQuery = "%" + nameQuery + "%";
            emailQuery = "%" + emailQuery + "%";
            tokenQuery = "%" + tokenQuery + "%";

            List<User> users = new List<User>();
            var select = 
                Sql.Parse
                (
                    "SELECT id, email, name, logintoken, blocked, created\n" +
                    "FROM users\n" +
                    "WHERE name LIKE @nameQuery " +
                    "AND email LIKE @emailQuery " +
                    "AND logintoken LIKE @tokenQuery" +
                    (onlyBlocked ? " AND blocked <> @notBlocked" : "") +
                    "\n" +
                    "ORDER BY id ASC\n" +
                    "LIMIT 100"
                );
            select.AddParam("@nameQuery", nameQuery);
            select.AddParam("@emailQuery", emailQuery);
            select.AddParam("@tokenQuery", tokenQuery);
            if (onlyBlocked)
                select.AddParam("@notBlocked", 0.0);
            using (var reader = await ctxt.ExecSelectAsync(select))
            {
                while (await reader.ReadAsync())
                {
                    User user =
                        new User()
                        {
                            Id = reader.GetInt64(0),
                            Email = reader.GetString(1),
                            Name = reader.GetString(2),
                            LoginToken = reader.GetString(3),
                            Blocked = reader.GetBoolean(4),
                            Created = reader.GetDateTime(5)
                        };
                    users.Add(user);
                }
            }
            return users;
        }

        public static async Task BlockUserAsync(HttpState state, string email, int blockSet = 1)
        {
            long userId = await GetUserIdFromEmailAsync(state.MsCtxt, email);
            if (userId < 0)
                throw new UserException("User to block not found: " + email);

            if (blockSet > 0)
                await Session.ForceUserOutAsync(state, userId);

            string userKey = (await state.MsCtxt.GetRowValueAsync("users", userId)).ToString();
            var define = new Define("users", userKey);
            define.Set("blocked", blockSet);
            await state.MsCtxt.Cmd.DefineAsync(define);
        }

        public static async Task UnblockUserAsync(HttpState state, string email)
        {
            await BlockUserAsync(state, email, 0);
        }

        public static async Task<bool> IsNameTakenAsync(HttpState state, string name)
        {
            var select = Sql.Parse("SELECT id FROM usednames WHERE value = @name");
            select.cmdParams = new Dictionary<string, object>() { { "@name", name.ToLower() } };
            return await state.MsCtxt.ExecScalar64Async(select) >= 0;
        }

        public static async Task RecordNameTakenAsync(HttpState state, string name)
        {
            var define = new Define("usednames", name.ToLower());
            await state.MsCtxt.Cmd.DefineAsync(define);
        }

        public static async Task UnrecordNameTakenAsync(HttpState state, string name)
        {
            await state.MsCtxt.Cmd.DeleteAsync("usednames", name.ToLower());
        }
    }
}
