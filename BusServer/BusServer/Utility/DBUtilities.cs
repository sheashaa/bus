using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusServer.Utility
{
    class DBUtilities
    {
        public static SqlConnection BeginDBConnection(string connectionString)
        {
            SqlConnection connection = new SqlConnection(connectionString);

            if (connection.State != ConnectionState.Open) connection.Open();

            return connection;
        }

        public static DataTable ExecuteQuery(string query, string connectionString)
        {
            SqlConnection connection = BeginDBConnection(connectionString);

            DataTable table = new DataTable();
            SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
            adapter.Fill(table);

            return table;
        }

        public static void ExecuteNonQuery(string query, string connectionString)
        {
            SqlConnection connection = BeginDBConnection(connectionString);

            SqlCommand command = new SqlCommand(query, connection);

            command.ExecuteNonQuery();
        }
    }
}
