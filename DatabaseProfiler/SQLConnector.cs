using ImageMagick.Formats;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;

namespace DatabaseProfiler
{
    public class SQLConnector
    {
        public string SQLConnectionString { get; set; } = "";
        public string DataSource { get; set; } = "";
        public List<DatabaseInfo> DatabaseList { get; set; } = new List<DatabaseInfo>();
        public SQLConnector()
        {

        }
        public SQLConnector(string connection_string)
        {
            SQLConnectionString = connection_string;
        }
        public List<DatabaseInfo> GetDatabaseList()
        {
            using (SqlConnection con = new SqlConnection(SQLConnectionString))
            {
                string qry = @"
                    SELECT [name], [database_id], [state_desc], [user_access_desc], [create_date] FROM sys.databases
                    WHERE [name] NOT IN ('master', 'tempdb', 'model', 'msdb')";

                using (SqlCommand sqlcommand = new SqlCommand(qry, con))
                {
                    con.Open();
                    SqlDataReader reader = sqlcommand.ExecuteReader();
                    //int ctr = 2;
                    while (reader.Read())
                    {
                        DatabaseInfo database = new DatabaseInfo();
                        database.DatabaseID = int.Parse(reader["database_id"].ToString())!;
                        database.Name = reader["name"].ToString()!;
                        database.CreateDate = DateTime.Parse(reader["create_date"].ToString());
                        database.StateDescription = reader["state_desc"].ToString()!;
                        database.UserAccessDescription = reader["user_access_desc"].ToString()!;
                        DatabaseList.Add(database);
                        //Console.WriteLine(string.Format("Database: {0}", database.Name));
                    }

                    con.Close();
                }
            }
            return DatabaseList;
        }
        public DatabaseInfo GetDatabaseInfo(DatabaseInfo database, bool include_row_counts = false)
        {
            database = GetSchemaAndTableList(database);

            for (int s = 0; s < database.Schemas.Count; s++)
            {
                SchemaInfo schema = database.Schemas[s];
                for (int t = 0; t < schema.Tables.Count; t++)
                {
                    TableInfo table = schema.Tables[t];
                    table = GetColumnData(table);
                    if (include_row_counts)
                    {
                        table = GetRowCount(table);
                    }
                    schema.Tables[t] = table;
                }
            }
            return database;
        }
        public DatabaseInfo GetSchemaAndTableList(DatabaseInfo database)
        {
            Dictionary<string, SchemaInfo> schema_list = new Dictionary<string, SchemaInfo>();
            using (SqlConnection con = new SqlConnection(SQLConnectionString))
            {
                DataSource = con.DataSource;
                string use = string.Format("USE [{0}]", database.Name);
                string qry = @"
                    SELECT * FROM INFORMATION_SCHEMA.TABLES ORDER BY [TABLE_SCHEMA] ASC";
                qry = string.Format("{0} {1}", use, qry);
                using (SqlCommand sqlcommand = new SqlCommand(qry, con))
                {
                    con.Open();

                    try
                    {
                        SqlDataReader reader = sqlcommand.ExecuteReader();
                        while (reader.Read())
                        {
                            string schem = reader["TABLE_SCHEMA"].ToString()!;
                            if (!schema_list.Keys.Contains(schem))
                            {
                                schema_list.Add(schem, new SchemaInfo());
                                schema_list[schem].Name = schem;
                            }
                            TableInfo table = new TableInfo();
                            table.Name = reader["TABLE_NAME"].ToString()!;
                            table.Schema = reader["TABLE_SCHEMA"].ToString()!;
                            table.Catalog = reader["TABLE_CATALOG"].ToString()!;
                            schema_list[schem].Tables.Add(table);
                        }
                        con.Close();
                        foreach (SchemaInfo schema in schema_list.Values)
                        {
                            database.Schemas.Add(schema);
                        }
                    }
                    catch (SqlException ex)
                    {
                        Console.WriteLine(string.Format("SQL Error!\n{0}", ex.Message));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(string.Format("Error!\n{0}", ex.Message));
                    }
                }
            }
            return database;
        }
        public DatabaseInfo GetTableList(DatabaseInfo database)
        {
            using (SqlConnection con = new SqlConnection(SQLConnectionString))
            {
                string use = string.Format("USE [{0}]", database.Name);
                string qry = @"
                    SELECT * FROM INFORMATION_SCHEMA.TABLES ORDER BY [TABLE_SCHEMA] ASC";
                qry = string.Format("{0} {1}", use, qry);
                using (SqlCommand sqlcommand = new SqlCommand(qry, con))
                {
                    con.Open();
                    SqlDataReader reader = sqlcommand.ExecuteReader();
                    TableInfo table = new TableInfo();
                    while (reader.Read())
                    {
                        table = new TableInfo();
                        table.Catalog = reader["TABLE_CATALOG"].ToString()!;
                        table.Schema = reader["TABLE_SCHEMA"].ToString()!;
                        table.Name = reader["TABLE_NAME"].ToString()!;
                        //database.Tables.Add(table);
                    }
                    con.Close();
                }
            }
            return database;
        }
        public TableInfo GetColumnData(TableInfo table)
        {
            using (SqlConnection con = new SqlConnection(SQLConnectionString))
            {
                string use = string.Format("USE [{0}]", table.Catalog);
                string qry = @"
                    SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE [TABLE_SCHEMA] = @schema AND [TABLE_NAME] = @name ORDER BY [TABLE_NAME] ASC";
                qry = string.Format("{0} {1}", use, qry);
                using (SqlCommand sqlcommand = new SqlCommand(qry, con))
                {
                    con.Open();
                    sqlcommand.Parameters.Add(new SqlParameter("schema", table.Schema));
                    sqlcommand.Parameters.Add(new SqlParameter("name", table.Name));
                    SqlDataReader reader = sqlcommand.ExecuteReader();
                    while (reader.Read())
                    {
                        ColumnInfo column = new ColumnInfo();
                        column.Name = reader["COLUMN_NAME"].ToString()!;
                        column.DataType = reader["DATA_TYPE"].ToString()!;
                        column.CharacterMaxLength = reader["CHARACTER_MAXIMUM_LENGTH"] == DBNull.Value ? "N/A" : reader["CHARACTER_MAXIMUM_LENGTH"].ToString();
                        column.isNullable = reader["IS_NULLABLE"].ToString()!;
                        table.Columns.Add(column);
                    }
                    con.Close();
                }
            }
            return table;
        }
        public TableInfo GetRowCount(TableInfo table)
        {
            using (SqlConnection con = new SqlConnection(SQLConnectionString))
            {
                string qry = string.Format("SELECT COUNT(*) FROM [{0}].[{1}].[{2}]", table.Catalog, table.Schema, table.Name);
                using (SqlCommand sqlcommand = new SqlCommand(qry, con))
                {
                    con.Open();
                    table.RecordCount = (int)sqlcommand.ExecuteScalar();
                    con.Close();
                }
            }
            return table;
        }

        // This is just a bonus cheat method for generating INSERT SELECT's to quickly move data from one db to another.
        // It doesn't necessarily belong in this app, but I needed it, so here it is.
        public void CreateTransferScripts(DatabaseInfo database, string destination_db_name)
        {
            foreach (SchemaInfo sch in database.Schemas)
            {

                foreach (TableInfo tbl in sch.Tables)
                {

                    string columns = "";
                    foreach (ColumnInfo col in tbl.Columns)
                    {
                        columns += string.Format("[{0}],\n ", col.Name);
                    }
                    columns = columns.Substring(0, columns.Length - 3);
                    string truncate_query = string.Format("\nTRUNCATE TABLE [{0}].[{1}].[{2}]\nGO", destination_db_name, sch.Name, tbl.Name);
                    string ident_insert_on = string.Format("SET IDENTITY_INSERT [{0}].[{1}].[{2}] ON\nGO", destination_db_name, sch.Name, tbl.Name);
                    string insertquery = string.Format("INSERT INTO [{0}].[{1}].[{2}] (\n {3}\n)", destination_db_name, sch.Name, tbl.Name, columns);
                    string selectquery = string.Format("SELECT\n {0} \nFROM [{1}].[{2}].[{3}]\nGO", columns, database.Name, sch.Name, tbl.Name);
                    string ident_insert_off = string.Format("SET IDENTITY_INSERT [{0}].[{1}].[{2}] OFF\nGO", destination_db_name, sch.Name, tbl.Name);
                    Console.WriteLine(truncate_query);
                    Console.WriteLine(ident_insert_on);
                    Console.WriteLine(insertquery);
                    Console.WriteLine(selectquery);
                    Console.WriteLine(ident_insert_off);
                    Console.WriteLine("------------");
                }
            }
        }
    }
}
