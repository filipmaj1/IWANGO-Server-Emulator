using MySql.Data.MySqlClient;
using System.Collections.Generic;

namespace IWANGOEmulator.GateServer
{
    class Database
    {
        const string HOST = "127.0.0.1";
        const string PORT = "3306";
        const string DB_NAME = "iwango";
        const string USERNAME = "root";
        const string PASSWORD = "";

        const string DP_HOST = "";
        const string DP_PORT = "";
        const string DP_DB_NAME = "";
        const string DP_USERNAME = "";
        const string DP_PASSWORD = "";

        public static string IwangoGetVerification(string daytonaHash)
        {
            MySqlCommand cmd;

            using MySqlConnection conn = new MySqlConnection($"Server={HOST}; Port={PORT}; Database={DB_NAME}; UID={USERNAME}; Password={PASSWORD}");
            try
            {
                conn.Open();
                string query = @"
                    SELECT username FROM accounts WHERE daytonaHash = @daytonaHash;
                ";
                cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@daytonaHash", daytonaHash);
                using MySqlDataReader Reader = cmd.ExecuteReader();
                while (Reader.Read())
                {
                    return Reader.GetString("username");
                }
            }
            catch (MySqlException e)
            {
                Program.Log.Error(e.ToString());
            }
            finally
            {
                conn.Dispose();
            }

            return null;
        }

        public static string DreamPipeGetVerification(string daytonaHash)
        {
            MySqlCommand cmd;

            using MySqlConnection conn = new MySqlConnection($"Server={DP_HOST}; Port={DP_PORT}; Database={DP_DB_NAME}; UID={DP_USERNAME}; Password={DP_PASSWORD}");
            try
            {
                conn.Open();
                string query = @"
                    SELECT username FROM users WHERE daytonaHash = @daytonaHash;
                ";
                cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@daytonaHash", daytonaHash);
                using MySqlDataReader Reader = cmd.ExecuteReader();
                while (Reader.Read())
                {
                    return Reader.GetString("username");
                }
            }
            catch (MySqlException e)
            {
                Program.Log.Error(e.ToString());
            }
            finally
            {
                conn.Dispose();
            }

            return null;
        }

        public static bool AddUserIfMissing(string username, string daytonaHash)
        {
            username += "@daytonakey";

            MySqlCommand cmd;
            using MySqlConnection conn = new MySqlConnection($"Server={HOST}; Port={PORT}; Database={DB_NAME}; UID={USERNAME}; Password={PASSWORD}");
            try
            {
                conn.Open();

                string queryInsert = @"
                    INSERT IGNORE INTO accounts SET username = @username, password = '', daytonaHash = @daytonaHash
                    ";

                cmd = new MySqlCommand(queryInsert, conn);
                cmd.Parameters.AddWithValue("@username", username);
                cmd.Parameters.AddWithValue("@daytonaHash", daytonaHash);

                cmd.ExecuteNonQuery();
            }
            catch (MySqlException e)
            {
                return false;
            }
            finally
            {
                conn.Dispose();
            }

            return true;
        }

        public static List<LobbyServer> GetLobbyServers(string commodityId)
        {
            MySqlCommand cmd;
            List<LobbyServer> lobbyList = new List<LobbyServer>();

            using MySqlConnection conn = new MySqlConnection($"Server={HOST}; Port={PORT}; Database={DB_NAME}; UID={USERNAME}; Password={PASSWORD}");
            try
            {
                conn.Open();
                string query = "SELECT name, ip, port FROM lobby_servers WHERE commodityId = @commodityId";
                cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@commodityId", commodityId);
                using (MySqlDataReader Reader = cmd.ExecuteReader())
                {
                    while (Reader.Read())
                    {
                        string name = Reader.GetString("name");
                        string ip = Reader.GetString("ip");
                        ushort port = Reader.GetUInt16("port");
                        lobbyList.Add(new LobbyServer(name, ip, port));
                    }
                }
            }
            catch (MySqlException e)
            {
                Program.Log.Error(e.ToString());
            }
            finally
            {
                conn.Dispose();
            }

            return lobbyList;
        }

        public static List<string> GetHandles(string username)
        {
            MySqlCommand cmd;
            List<string> handleList = new List<string>();

            using MySqlConnection conn = new MySqlConnection($"Server={HOST}; Port={PORT}; Database={DB_NAME}; UID={USERNAME}; Password={PASSWORD}");
            try
            {
                conn.Open();
                string query = @"
                    SELECT handles.name FROM handles
                    INNER JOIN accounts ON accounts.id = handles.accountId
                    WHERE daytonaHash = @username
                    ORDER BY handles.creationDate ASC";
                cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@username", username);
                using (MySqlDataReader Reader = cmd.ExecuteReader())
                {
                    while (Reader.Read())
                    {
                        handleList.Add(Reader.GetString("name"));
                    }
                }
            }
            catch (MySqlException e)
            {
                Program.Log.Error(e.ToString());
            }
            finally
            {
                conn.Dispose();
            }

            return handleList;
        }

        public static int CreateHandle(string username, string handleName)
        {
            MySqlCommand cmd;

            using MySqlConnection conn = new MySqlConnection($"Server={HOST}; Port={PORT}; Database={DB_NAME}; UID={USERNAME}; Password={PASSWORD}");
            try
            {
                conn.Open();

                string queryInsert = @"
                    INSERT INTO handles (accountId, name)
                    VALUES ((SELECT id FROM accounts WHERE daytonaHash=@username), @name);
                    ";

                cmd = new MySqlCommand(queryInsert, conn);
                cmd.Parameters.AddWithValue("@name", handleName);
                cmd.Parameters.AddWithValue("@username", username);

                cmd.ExecuteNonQuery();
            }
            catch (MySqlException e)
            {
                if (e.Number == 1062)
                    return -1;
                else
                    return 0;
            }
            finally
            {
                conn.Dispose();
            }

            return 1;
        }

        public static int ReplaceHandle(string username, int handleIndex, string newHandleName)
        {
            string query;
            MySqlCommand cmd;

            using MySqlConnection conn = new MySqlConnection($"Server={HOST}; Port={PORT}; Database={DB_NAME}; UID={USERNAME}; Password={PASSWORD}");
            try
            {
                conn.Open();

                string getNameQuery = @"
                    SELECT name FROM handles 
                    INNER JOIN accounts on accounts.id = handles.accountId 
                    WHERE daytonaHash = @username 
                    ORDER BY creationDate DESC LIMIT @handleIndex, 1";

                string oldHandleName;
                cmd = new MySqlCommand(getNameQuery, conn);
                cmd.Parameters.AddWithValue("@username", username);
                cmd.Parameters.AddWithValue("@handleIndex", handleIndex);
                var hasEntry = cmd.ExecuteScalar();
                if (hasEntry != null)
                    oldHandleName = hasEntry.ToString();
                else
                    return 0;

                query = @"
                    UPDATE handles 
                    SET name = @newHandleName
                    WHERE name = @oldHandleName
                    ";

                cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@username", username);
                cmd.Parameters.AddWithValue("@oldHandleName", oldHandleName);
                cmd.Parameters.AddWithValue("@newHandleName", newHandleName);
                cmd.ExecuteNonQuery();
            }
            catch (MySqlException e)
            {
                if (e.Number == 1062)
                    return -1;
                else
                    return 0;
            }
            finally
            {
                conn.Dispose();
            }

            return 1;
        }

        public static bool DeleteHandle(string username, int handleIndex)
        {
            MySqlCommand cmd;

            using MySqlConnection conn = new MySqlConnection($"Server={HOST}; Port={PORT}; Database={DB_NAME}; UID={USERNAME}; Password={PASSWORD}");
            try
            {
                conn.Open();

                string handleName;

                string getNameQuery = @"
                    SELECT name FROM handles 
                    INNER JOIN accounts on accounts.id = handles.accountId 
                    WHERE daytonaHash = @username 
                    ORDER BY creationDate DESC LIMIT @handleIndex, 1";


                cmd = new MySqlCommand(getNameQuery, conn);
                cmd.Parameters.AddWithValue("@username", username);
                cmd.Parameters.AddWithValue("@handleIndex", handleIndex);
                var hasEntry = cmd.ExecuteScalar();
                if (hasEntry != null)
                    handleName = hasEntry.ToString();
                else
                    return false;

                string deleteQuery = @"
                    DELETE FROM handles 
                    WHERE name = @handleName
                    ";

                cmd = new MySqlCommand(deleteQuery, conn);
                cmd.Parameters.AddWithValue("@handleName", handleName);
                cmd.ExecuteNonQuery();
            }
            catch (MySqlException e)
            {
                Program.Log.Error(e.ToString());
                return false;
            }
            finally
            {
                conn.Dispose();
            }

            return true;
        }

    }
}
