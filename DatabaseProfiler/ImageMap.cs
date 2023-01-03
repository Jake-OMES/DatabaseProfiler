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
        public bool ProfileServer { get; set; } = false;
        public SQLConnector Connector { get; set; } = new SQLConnector();
        public ImageMap()
        {

        }
        public void CreateNew()
        {
            Connector.SQLConnectionString = SQLConnectionString;
            if (ProfileServer)
            {
                Connector.GetDatabaseList();
                foreach (DatabaseInfo database in Connector.DatabaseList)
                {
                    database.Info.Database_Name = database.Name;
                    database.Info.Host = "muthur.junkgineering.com";
                    database.Info.Date = DateTime.Now;
                    Connector.GetDatabaseInfo(database, true);
                    InfoObject diagram = new InfoObject(1024, 768, false);
                    diagram.Font_Size = 14;
                    diagram.Create(database);
                }
            }
            else
            {
                DatabaseInfo dbinfo = new DatabaseInfo();
                dbinfo.Name = Database_Name;
                dbinfo.Info.Database_Name = Database_Name;
                dbinfo.Info.Date = DateTime.Now;
                dbinfo = Connector.GetDatabaseInfo(dbinfo, true);
                dbinfo.Info.Host = Connector.DataSource;

                InfoObject diagram = new InfoObject(1024, 768, false);
                diagram.Font_Size = 14;
                diagram.Create(dbinfo);
            }
        }
    }
}
