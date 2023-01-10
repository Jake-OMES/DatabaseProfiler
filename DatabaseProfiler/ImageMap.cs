using DatabaseProfiler;
using ImageMagick;
using ImageMagick.Formats;
using System;
using System.Collections.Generic;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace DatabaseProfiler
{
    public class ImageMap
    {
        public string SQLConnectionString { get; set; } = "";
        public SQLConnector Connector { get; set; } = new SQLConnector();
        public string Host { get; set; } = "";
        public string Description { get; set; } = "";
        public double Line_Spacing { get; set; } = 5;
        public double Canvas_Width { get; set; } = 1024;
        public double Canvas_Height { get; set; } = 768;
        public string Font_Name { get; set; } = "Arial"; //Coda //Frank //Aldritch
        public double Font_Size { get; set; } = 18;
        public bool Transparent_Background { get; set; } = false;
        public MagickColor Font_Color { get; set; } = new MagickColor("#C9D1D9");
        public MagickColor Background_Color { get; set; } = new MagickColor("#0D1117");
        public MagickColor Foreground_Color { get; set; } = new MagickColor("#161B22");
        public MagickColor Border_Color { get; set; } = new MagickColor("#30363D");
        // dark 0D1117 //light 161B22 // font C9D1D9 // border 30363D // buttons 21262D
        public double Border_Width { get; set; } = 5;
        public double Corner_Radius { get; set; } = 5;
        public double X { get; set; } = 10;
        public double Y { get; set; } = 10;
        public string OutputFolder { get; set; } = "D:\\Pictures\\DatabaseMaps";
        internal ImageMap(double canvas_width = 1024, double canvas_height = 768, bool transparent = false)
        {
            Canvas_Width = canvas_width;
            Canvas_Height = canvas_height;
            Transparent_Background = transparent;
            Connector.SQLConnectionString = SQLConnectionString;
        }
        public void Create(string database_name)
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
            if (database_name.ToUpper() == "ALL")
            {
                Console.WriteLine("Retrieving list of databases on the server...");
                Connector.GetDatabaseList();
                Console.WriteLine(string.Format("Processing ({0}) databases...", Connector.DatabaseList.Count));
                foreach (DatabaseInfo database in Connector.DatabaseList)
                {
                    Console.WriteLine(string.Format("Retrieving info for [{0}]...", database.Name));
                    database.InfoBox.Host = Host;
                    database.InfoBox.Date = DateTime.Now;
                    database.Image.Settings.Font = Font_Name;
                    Connector.GetDatabaseInfo(database, true);
                    _createDatabaseImages(database);

                    Console.WriteLine(string.Format("[{0}] complete", database.Name));
                }
            }
            else
            {
                Console.WriteLine(string.Format("Retrieving info for [{0}]...", database_name));
                DatabaseInfo database = new DatabaseInfo();
                database.Name = database_name;
                database.InfoBox.Host = Host;
                database.InfoBox.Date = DateTime.Now;
                database.Image.Settings.Font = Font_Name;
                database.Transparent_Background = Transparent_Background;
                database = Connector.GetDatabaseInfo(database, true);
                if (database.Schemas.Count > 0)
                {
                    _createDatabaseImages(database);

                    Console.WriteLine(string.Format("[{0}] complete", database.Name));
                }
                else
                {
                    Console.WriteLine("There's nothing to profile. Exiting...");
                }
            }
        }

        private void _createDatabaseImages(DatabaseInfo dbinfo)
        {
            dbinfo.Font_Name = Font_Name;
            dbinfo.Font_Size = 18;
            foreach (SchemaInfo sch in dbinfo.Schemas)
            {
                // First, calculate the height and width the image will need to be.
                double total_max_width = 0;
                double total_max_height = 0;
                ///// calculate the width of the info panel box
                sch.Font_Name = Font_Name;
                sch.Font_Size = 18;
                sch.Foreground_Color = dbinfo.Foreground_Color;
                sch.Background_Color = dbinfo.Background_Color;
                sch.Font_Color = dbinfo.Font_Color;
                sch.Border_Color = dbinfo.Border_Color;

                List<double> lengths = new List<double>() {
                    sch.Image.FontTypeMetrics(string.Format("{0}", "DataSource: " + dbinfo.InfoBox.Host)).TextWidth,
                    sch.Image.FontTypeMetrics(string.Format("{0}", "Database: " + dbinfo.InfoBox.Database_Name)).TextWidth,
                    sch.Image.FontTypeMetrics(string.Format("{0}", "Schema: " + dbinfo.InfoBox.Schema_Name)).TextWidth,
                    sch.Image.FontTypeMetrics(string.Format("{0}", "As of: " + dbinfo.InfoBox.Date.ToShortDateString())).TextWidth
                };
                dbinfo.InfoBox.Width = lengths.Max() + (dbinfo.Padding * 2) + (Border_Width * 2) + (dbinfo.Margin * 2);
                dbinfo.InfoBox.Height = dbinfo.Image.FontTypeMetrics(dbinfo.InfoBox.Host).TextHeight * 3 + (Line_Spacing * 3) + (dbinfo.Padding * 2) + (Border_Width * 2) + (dbinfo.Margin * 2);

                sch.CalculateDimensions();
                if (sch.Width < dbinfo.InfoBox.Width)
                {
                    total_max_width = dbinfo.InfoBox.Width;
                }
                else
                {
                    total_max_width = sch.Width;
                }

                total_max_height = dbinfo.InfoBox.Height + sch.Height + (dbinfo.Padding * 2) + (dbinfo.Margin * 2); ;
                total_max_width += dbinfo.Padding;
                dbinfo.SetImage((int)total_max_width, (int)total_max_height, Background_Color, Transparent_Background);

                // Now we do the drawing.
                double x = 10;
                double y = 10;
                dbinfo.InfoBox.Schema_Name = sch.Name;
                CreateInfoPanelObject(dbinfo.InfoBox, x, y);

                dbinfo.InfoBox.Object_Image.Draw(dbinfo.Image);
                y += dbinfo.InfoBox.Height + (dbinfo.Margin * 2);
                Console.WriteLine(string.Format("Drawing ({0}) tables...", sch.Tables.Count));
                foreach (TableInfo tbl in sch.Tables)
                {
                    CreateTableObject(tbl, x, y);
                    tbl.Object_Image.Draw(dbinfo.Image);
                    x += tbl.Width + (tbl.Margin);
                }
                string file_name = Path.Combine(OutputFolder, string.Format("{0}_{1}.png", dbinfo.InfoBox.Database_Name, dbinfo.InfoBox.Schema_Name));
                Console.Write(string.Format("Writing image to {0}...", file_name));
                dbinfo.Image.Write(file_name);
                Console.WriteLine("success");
            }
        }
        public InfoPanel CreateInfoPanelObject(InfoPanel info, double x, double y)
        {
            double y_orig = y;
            double x_orig = x;

            Drawables inf = new Drawables();

            inf.FontPointSize(info.Font_Size);
            inf.Font(Font_Name);

            inf.FillColor(Font_Color);
            inf.StrokeColor(Border_Color);
            inf.RoundRectangle(x, y + info.Padding, x + info.Width, y + info.Font_Size + (info.Padding * 1.5), Corner_Radius, Corner_Radius);

            x += info.Padding;
            y += info.Font_Size + info.Padding;

            inf.FillColor(Background_Color);
            inf.StrokeColor(MagickColors.Transparent);
            inf.TextAlignment(TextAlignment.Left);
            inf.Text(x, y, "Database: ");

            inf.FillColor(Font_Color);
            y += info.Font_Size + Line_Spacing;
            inf.Text(x, y, "Schema: ");
            y += info.Font_Size + Line_Spacing;
            inf.Text(x, y, "DataSource: ");
            y += info.Font_Size + Line_Spacing;
            inf.Text(x, y, "As of: ");

            y = y_orig;
            x = x_orig;

            x += info.Padding + ("DataSource: ".Length * (info.Font_Size / 2));

            inf.FillColor(Background_Color);
            inf.StrokeColor(MagickColors.Transparent);
            inf.TextAlignment(TextAlignment.Left);
            x += info.Padding;
            y += Font_Size + info.Padding;
            inf.Text(x, y, info.Database_Name);

            inf.FillColor(Font_Color);
            y += info.Font_Size + Line_Spacing;
            inf.Text(x, y, info.Schema_Name);
            y += info.Font_Size + Line_Spacing;
            inf.Text(x, y, info.Host);
            y += info.Font_Size + Line_Spacing;
            inf.Text(x, y, info.Date.ToShortDateString());
            info.Object_Image = inf;
            return info;
        }
        public TableInfo CreateTableObject(TableInfo table, double x, double y)
        {
            table.Foreground_Color = Foreground_Color;
            table.Background_Color = Background_Color;
            table.Font_Color = Font_Color;
            table.Border_Color = Border_Color;

            Drawables tbl = new Drawables();
            tbl.FillColor(table.Background_Color);
            tbl.StrokeColor(table.Border_Color);
            tbl.RoundRectangle(x, y, x + table.Width + (table.Padding * 2), y + table.Height, Corner_Radius, Corner_Radius); //Main box
            tbl.FillColor(table.Foreground_Color);
            tbl.RoundRectangle(x, y, x + table.Width + (table.Padding * 2), y + table.Font_Size + (Line_Spacing * 2) + table.Padding, Corner_Radius, Corner_Radius); //Title box

            tbl.FontPointSize(table.Font_Size + 2);
            tbl.Font(table.Font_Name);
            tbl.FillColor(table.Font_Color);
            tbl.StrokeColor(MagickColors.Transparent);
            tbl.TextAlignment(TextAlignment.Left);
            x = x + Border_Width + table.Padding;
            y = y + Border_Width + table.Font_Size + (table.Padding / 2);
            tbl.Text(x, y, table.Name);
            tbl.TextAlignment(TextAlignment.Right);
            tbl.Text(x + table.Width - table.Padding, y, string.Format("({0})", table.RecordCount.ToString()));

            tbl.FontPointSize(table.Font_Size);
            tbl.FillColor(table.Font_Color);
            tbl.StrokeColor(MagickColors.Transparent);
            y += table.Font_Size + Line_Spacing + table.Padding;
            for (int c = 0; c < table.Columns.Count; c++)
            {
                tbl.TextAlignment(TextAlignment.Left);
                tbl.Text(x, y, table.Columns[c].Name);
                tbl.TextAlignment(TextAlignment.Right);
                if (table.Columns[c].CharacterMaxLength == "N/A")
                {
                    tbl.Text(x + table.Width - table.Padding, y, table.Columns[c].DataType);
                }
                else if (table.Columns[c].CharacterMaxLength == "-1")
                {
                    tbl.Text(x + table.Width - table.Padding, y, string.Format("{0}(MAX)", table.Columns[c].DataType));
                }
                else
                {
                    tbl.Text(x + table.Width - table.Padding, y, string.Format("{0}({1})", table.Columns[c].DataType, table.Columns[c].CharacterMaxLength));
                }
                y += table.Font_Size + (Line_Spacing / 2);
            }
            table.Object_Image = tbl;
            table.Width = table.Width + (table.Padding * 2);
            table.Height = table.Height;
            return table;
        }
    }
    public class ImageData
    {
        public double Width { get; set; } = 200;
        public double Height { get; set; } = 100;
        public MagickColor Font_Color { get; set; } = new MagickColor("#C9D1D9");
        public MagickColor Background_Color { get; set; } = new MagickColor("#0D1117");
        public MagickColor Foreground_Color { get; set; } = new MagickColor("#161B22");
        public MagickColor Border_Color { get; set; } = new MagickColor("#30363D");
        public string Font_Name { get { return _getFont(); } set { _setFont(value); } }
        public double Font_Size { get { return _getFontSize(); } set { _setFontSize(value); } }
        public double Margin { get; set; } = 10;
        public double Padding { get; set; } = 10;
        public string OutputFolder { get; set; } = "D:\\Pictures\\DatabaseMaps";
        public MagickImage Image { get; set; } = new MagickImage();
        private double _getFontSize()
        {
            if (Image.Settings.FontPointsize == 0)
            {
                Image.Settings.FontPointsize = 18;
            }
            return Image.Settings.FontPointsize;
        }
        private string _getFont()
        {
            if (Image.Settings.Font is null)
            {
                Image.Settings.Font = "Arial";
            }
            return Image.Settings.Font;
        }
        private void _setFontSize(double font_size)
        {
            Image.Settings.FontPointsize = font_size;
        }
        private void _setFont(string font_name)
        {
            Image.Settings.Font = font_name;
        }
    }
    public class DatabaseInfo : ImageData
    {
        private string _name { get; set; }
        public string Name { get { return _name; } set { InfoBox.Database_Name = value; _name = value; } }
        public double DatabaseID { get; set; }
        private string _schema { get; set; }
        public string Schema { get { return _schema; } set { InfoBox.Schema_Name = value; _schema = value; } }
        public double FileSize { get; set; }
        public string StateDescription { get; set; } = "";
        public string UserAccessDescription { get; set; } = "";
        public DateTime CreateDate { get; set; }
        public bool Transparent_Background { get; set; } = false;
        public InfoPanel InfoBox = new InfoPanel();
        public List<SchemaInfo> Schemas = new List<SchemaInfo>();
        public Drawables Object_Image = new Drawables();

        public DatabaseInfo()
        {

        }
        public void SetImage(int width, int height, MagickColor background_color, bool is_transparent = false)
        {
            Image.Settings.FontPointsize = Font_Size;
            if (is_transparent)
            {
                Image = new MagickImage(MagickColors.Transparent, width, height);
            }
            else
            {
                Image = new MagickImage(background_color, width, height);
            }
            Image.Format = MagickFormat.Png;
        }
    }
    public class SchemaInfo : ImageData
    {
        public string Name = "";
        public List<TableInfo> Tables = new List<TableInfo>();
        public Drawables Object_Image = new Drawables();

        public SchemaInfo()
        {
            Font_Size = 16;
        }
        public void CalculateDimensions()
        {
            double total_max_width = 0;
            double total_max_height = 0;
            foreach (TableInfo tbl in Tables)
            {
                tbl.Font_Name = Font_Name;
                tbl.CalulcateDimensions();
                total_max_width += tbl.Width + tbl.Margin + 20;
                if (tbl.Height > total_max_height)
                {
                    total_max_height = tbl.Height;
                }
            }
            Width = total_max_width;
            Height = total_max_height;
        }
    }
    public class TableInfo : ImageData
    {
        public string Name { get; set; } = "";
        public int RecordCount { get; set; }
        public string Schema { get; set; } = "";
        public string Catalog { get; set; } = "";
        public string TableType { get; set; } = "";
        public List<ColumnInfo> Columns = new List<ColumnInfo>();
        public Drawables Object_Image = new Drawables();
        public TableInfo()
        {
            Font_Size = 12;
        }
        public void CalulcateDimensions()
        {
            Font_Size = 12;
            double minimum_spacing = Image.FontTypeMetrics("XXX").TextWidth;
            double table_max_width = Image.FontTypeMetrics(Name).TextWidth + minimum_spacing + Image.FontTypeMetrics(string.Format("({0})", RecordCount.ToString())).TextWidth;
            foreach (ColumnInfo col in Columns)
            {
                //// Calculate the width of a column based on the text of the name of the column and the data type, padding, margins, borders, etc.
                double column_width = Image.FontTypeMetrics(col.Name).TextWidth + minimum_spacing + Image.FontTypeMetrics(col.DataType).TextWidth + Image.FontTypeMetrics(col.CharacterMaxLength).TextWidth;
                if (table_max_width < column_width)
                {
                    table_max_width = column_width;
                }
            }
            table_max_width += (Padding * 2) + (Margin);
            double title_panel_height = Image.FontTypeMetrics(Name).TextHeight + (Padding * 2);
            double table_height = Columns.Count * (Image.FontTypeMetrics(Name).TextHeight) + (Padding * 2) + title_panel_height;

            ///// Set the width and height of the table to draw later so we don't have to recalculate it.
            Width = table_max_width;
            Height = table_height;
        }
    }
    public class ColumnInfo : ImageData
    {
        public string Name { get; set; }
        public string DataType { get; set; }
        public string CharacterMaxLength { get; set; }
        public string isNullable { get; set; }
        public ColumnInfo()
        {

        }
    }
    public class InfoPanel : ImageData
    {
        public string Database_Name = "";
        public string Schema_Name = "dbo";
        public string Host = "None";
        public DateTime Date = DateTime.Now;
        public Drawables Object_Image = new Drawables();
    }
}
