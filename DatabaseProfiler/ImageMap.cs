using ImageMagick;
using ImageMagick.Formats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseProfiler
{
    internal class ImageMap
    {
        public string OutputFolder { get; set; } = "";
        public string Database_Name { get; set; } = "";
        public string SQLConnectionString { get; set; } = "";
        public string Host { get; set; } = "";
        public bool ProfileServer { get; set; } = false;
        public SQLConnector Connector { get; set; } = new SQLConnector();
        public ImageMap()
        {

        }
        public void CreateNew()
        {
            Connector.SQLConnectionString = SQLConnectionString;
            List<string> connection_info = SQLConnectionString.Split(';').ToList<string>();
            foreach (string item in connection_info)
            {
                if (item.Contains("Host") || item.Contains("Data Source"))
                {
                    if (item.Split('=').Length > 1)
                    {
                        Host = item.Split('=')[1];
                        break;
                    }
                }
            }
            if (ProfileServer)
            {
                Console.WriteLine("Retrieving list of databases on the server...");
                Connector.GetDatabaseList();
                foreach (DatabaseInfo database in Connector.DatabaseList)
                {
                    Console.Write(string.Format("Retrieving info for [{0}]...", database.Name));
                    database.Info.Database_Name = database.Name;
                    database.Info.Host = Host;
                    database.Info.Date = DateTime.Now;
                    Connector.GetDatabaseInfo(database, true);
                    InfoObject diagram = new InfoObject(1024, 768, false);

                    diagram.Font_Size = 14;
                    Console.Write(string.Format("Drawing images...", database.Name));
                    diagram.Create(database);
                    Console.WriteLine("Complete");
                }
            }
            else
            {
                Console.Write(string.Format("Retrieving info for [{0}]...", Database_Name));
                DatabaseInfo dbinfo = new DatabaseInfo();
                dbinfo.Name = Database_Name;
                dbinfo.Info.Database_Name = Database_Name;
                dbinfo.Info.Host = Host;
                dbinfo.Info.Date = DateTime.Now;
                dbinfo = Connector.GetDatabaseInfo(dbinfo, true);

                InfoObject diagram = new InfoObject(1024, 768, false);
                diagram.Font_Size = 14;
                Console.Write(string.Format("Drawing images...", dbinfo.Name));
                diagram.Create(dbinfo);
                Console.WriteLine("Complete");
            }
        }
    }
}
