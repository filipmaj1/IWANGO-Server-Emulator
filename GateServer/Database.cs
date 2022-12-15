using MySql.Data.MySqlClient;
using System.Collections.Generic;

namespace IWANGOEmulator.GateServer
{
    class Database
    {
        const string HOST = "127.0.0.1";
        const string PORT = "3306";
        const string DB_NAME = "playonline";
        const string USERNAME = "root";
        const string PASSWORD = "";

        public static List<string> GetLobbyServers(string commodityId)
        {
            MySqlCommand cmd;
            List<string> serverString = new List<string>();

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
                        string port = Reader.GetUInt16("port").ToString();
                        serverString.Add($"{name} {ip} {port}");
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

            return serverString;
        }

        public static List<string> GetHandles(string username)
        {
            MySqlCommand cmd;
            List<string> handleList = new List<string>();

            using MySqlConnection conn = new MySqlConnection($"Server={HOST}; Port={PORT}; Database={DB_NAME}; UID={USERNAME}; Password={PASSWORD}");
            try
            {
                conn.Open();
                string query = "SELECT name FROM handles WHERE username = @username";
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

        public static void CreateHandle(string username, string handleName)
        {
            string query;
            MySqlCommand cmd;

            using MySqlConnection conn = new MySqlConnection($"Server={HOST}; Port={PORT}; Database={DB_NAME}; UID={USERNAME}; Password={PASSWORD}");
            try
            {
                conn.Open();

                query = @"
                    INSERT INTO handles (name, username) 
                    VALUES (@name, @username)
                    ";

                cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@name", handleName);
                cmd.Parameters.AddWithValue("@username", username);

                cmd.ExecuteNonQuery();
            }
            catch (MySqlException e)
            {
                Program.Log.Error(e.ToString());
            }
            finally
            {
                conn.Dispose();
            }
        }

        public static void ReplaceHandle(string username, int handleIndex, string newHandleName)
        {
            string query;
            MySqlCommand cmd;

            using MySqlConnection conn = new MySqlConnection($"Server={HOST}; Port={PORT}; Database={DB_NAME}; UID={USERNAME}; Password={PASSWORD}");
            try
            {
                conn.Open();

                query = @"
                    UPDATE handles 
                    SET name = @newHandleName
                    WHERE name IN (SELECT name FROM handles WHERE username = @username ORDER BY creationDate DESC LIMIT @handleIndex, 1)
                    ";

                cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@username", username);
                cmd.Parameters.AddWithValue("@handleIndex", handleIndex);
                cmd.Parameters.AddWithValue("@newHandleName", newHandleName);
                cmd.ExecuteNonQuery();
            }
            catch (MySqlException e)
            {
                Program.Log.Error(e.ToString());
            }
            finally
            {
                conn.Dispose();
            }
        }

        public static void DeleteHandle(string username, int handleIndex)
        {
            string query;
            MySqlCommand cmd;

            using MySqlConnection conn = new MySqlConnection($"Server={HOST}; Port={PORT}; Database={DB_NAME}; UID={USERNAME}; Password={PASSWORD}");
            try
            {
                conn.Open();

                query = @"
                    DELETE FROM handles 
                    WHERE name IN (SELECT name FROM handles WHERE username = @username ORDER BY creationDate DESC LIMIT @handleIndex, 1)
                    ";

                cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@username", username);
                cmd.Parameters.AddWithValue("@handleIndex", handleIndex);
                cmd.ExecuteNonQuery();
            }
            catch (MySqlException e)
            {
                Program.Log.Error(e.ToString());
            }
            finally
            {
                conn.Dispose();
            }
        }

    }
}
