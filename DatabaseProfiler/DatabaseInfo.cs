using DatabaseProfiler;
using ImageMagick;
using System;
using System.Collections.Generic;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace DatabaseProfiler
{
    public class InfoObject
    {
        public MagickImage Image { get; set; } = new MagickImage();
        public string Description { get; set; } = "";
        private int _padding { get; set; } = 10;
        public int Margin { get; set; } = 10;
        public int Line_Spacing { get; set; } = 5;
        public int Canvas_Width { get; set; } = 1024;
        public int Canvas_Height { get; set; } = 768;
        public int Width { get; set; } = 200;
        public int Height { get; set; } = 100;
        public string Font_Name { get; set; } = "Coda"; //Coda //Frank //Aldritch
        public int Font_Size { get; set; } = 18;
        public bool Transparent_Background { get; set; } = false;
        public MagickColor Font_Color { get; set; } = new MagickColor("#C9D1D9");
        public MagickColor Background_Color { get; set; } = new MagickColor("#0D1117");
        public MagickColor Foreground_Color { get; set; } = new MagickColor("#161B22");
        public MagickColor Border_Color { get; set; } = new MagickColor("#30363D");
        // dark 0D1117 //light 161B22 // font C9D1D9 // border 30363D // buttons 21262D
        public int Border_Width { get; set; } = 5;
        public int Corner_Radius { get; set; } = 5;
        public int X { get; set; } = 10;
        public int Y { get; set; } = 10;
        public string OutputFolder { get; set; } = "D:\\Pictures\\DatabaseMaps";
        public string TablesOutputFolder { get; set; } = "D:\\Pictures\\DatabaseMaps\\Tables";
        internal InfoObject(int canvas_width = 1024, int canvas_height = 768, bool transparent = false)
        {
            Canvas_Width = canvas_width;
            Canvas_Height = canvas_height;
            Transparent_Background = transparent;
        }
        private void _init()
        {
            if (Transparent_Background)
            {
                Image = new MagickImage(MagickColors.Transparent, Canvas_Width, Canvas_Height);
            }
            else
            {
                Image = new MagickImage(Background_Color, Canvas_Width, Canvas_Height);
            }
            Image.Format = MagickFormat.Png;
        }
        public void Create(DatabaseInfo dbinfo)
        {

            foreach (SchemaInfo sch in dbinfo.Schemas)
            {
                // First, calculate the height and width the image will need to be.
                int total_max_width = 0;
                int total_max_height = 0;
                ///// calculate the width of the info panel box
                Font_Size = Font_Size + 2;
                List<int> lengths = new List<int>() { dbinfo.Info.Host.Length + "DataSource: ".Length, dbinfo.Info.Database_Name.Length + "Database: ".Length, dbinfo.Info.Schema_Name.Length + "Schema: ".Length, dbinfo.Info.Date.ToShortDateString().Length + "As of: ".Length };
                int info_panel_width = (lengths.Max() * (Font_Size / 2)) + (_padding * 2) + (Border_Width * 2) + (Margin * 2);
                int info_panel_height = (Font_Size * 3) + (Line_Spacing * 3) + (_padding * 2) + (Border_Width * 2) + (Margin * 2);
                dbinfo.Info.Width = info_panel_width;
                dbinfo.Info.Height = info_panel_height;
                //total_max_height = info_panel_height;
                foreach (TableInfo tbl in sch.Tables)
                {
                    // Make sure the table width is at least wide enough to fit the name of the table.
                    int minimum_spacing = 10; // spacing between column name and column type based on size of a single letter.
                    int table_max_width = ((tbl.Name.Length) * ((Font_Size + 2) / 2)) + (Font_Size / 2) + minimum_spacing + (tbl.RecordCount.ToString().Length * (Font_Size / 2));

                    foreach (ColumnInfo col in tbl.Columns)
                    {
                        //// Calculate the width of a column based on the text of the name of the column and the data type, padding, margins, borders, etc.
                        int column_width = ((col.Name.Length + col.DataType.Length) * (Font_Size / 2)) + (minimum_spacing * (Font_Size / 2));
                        // Make it the max width if it's longer than anything that came before it.
                        if (table_max_width < column_width)
                        {
                            table_max_width = column_width;
                        }
                    }
                    total_max_width += table_max_width + (_padding * 2) + (Margin * 4);

                    // Check the max height: height of row in the column list + top and bottom padding + the space between the lines.
                    int title_panel_height = (Font_Size + 2) + (_padding * 2);
                    //int table_height = ((tbl.Columns.Count) * Font_Size) + _padding + (tbl.Columns.Count * Line_Spacing) + title_panel_height;
                    int table_height = tbl.Columns.Count * (Font_Size + (Line_Spacing / 2)) + (_padding * 2) + title_panel_height;
                    // y += Font_Size + (Line_Spacing / 2);

                    ///// Make sure total max width is at least as wide as the info panel.
                    if (total_max_width < info_panel_width)
                    {
                        total_max_width = info_panel_width;
                    }
                    ///// Set the width and height of the table to draw later so we don't have to recalculate it.
                    tbl.Width = table_max_width;
                    tbl.Height = table_height;

                    //// Set the overall image height to the the table height if it's too small.
                    if (table_height > total_max_height)
                    {
                        total_max_height = table_height;
                    }
                }
                //Console.WriteLine(string.Format("Final Max Width: {0}\n\n================", cur_max_width));
                Canvas_Width = total_max_width + _padding + (Margin * 2);
                Canvas_Height = total_max_height + info_panel_height + _padding + Margin;
                _init();

                // Now we do the drawing.
                int x = 10;
                int y = 10;
                //cur_max_height = y;
                dbinfo.Info.Schema_Name = sch.Name;
                CreateInfoPanelObject(dbinfo.Info, x, y);
                dbinfo.Info.Object_Image.Draw(Image);
                y += dbinfo.Info.Height + Margin;

                Font_Size = Font_Size - 2;
                foreach (TableInfo tbl in sch.Tables)
                {
                    CreateTableObject(tbl, x, y);
                    tbl.Object_Image.Draw(Image);
                    x += tbl.Width + (Margin);
                }
                Image.Write(Path.Combine(OutputFolder, string.Format("{0}_{1}.png", dbinfo.Info.Database_Name, dbinfo.Info.Schema_Name)));
            }
        }
        public InfoPanel CreateInfoPanelObject(InfoPanel info, int x, int y)
        {
            int y_orig = y;
            int x_orig = x;

            int orig_font_size = Font_Size;
            Font_Size += 3;
            //List<int> lengths = new List<int>() { info.Host.Length, info.Database_Name.Length, info.Schema_Name.Length, info.Date.ToShortDateString().Length };
            //int width = (lengths.Max() * (Font_Size / 2)) + (_padding * 2) + (Border_Width * 2);
            //int height = (Font_Size * (lengths.Count - 1)) + (Line_Spacing * (lengths.Count - 1)) + (_padding * (lengths.Count - 1));
            Drawables inf = new Drawables();
            //inf.FillColor(Background_Color);
            //inf.StrokeColor(Border_Color);
            //inf.RoundRectangle(x, y, x + width + (_padding * 3), y + height, Corner_Radius, Corner_Radius); //Main box

            inf.FontPointSize(Font_Size);
            inf.Font(Font_Name);

            inf.FillColor(Font_Color);
            inf.StrokeColor(Border_Color);
            inf.RoundRectangle(x, y + _padding, x + info.Width + (_padding * 4), y + Font_Size + (_padding * 1.5), Corner_Radius, Corner_Radius);

            x += _padding;
            y += Font_Size + _padding;

            inf.FillColor(Background_Color);
            inf.StrokeColor(MagickColors.Transparent);
            inf.TextAlignment(TextAlignment.Left);
            inf.Text(x, y, "Database: ");

            inf.FillColor(Font_Color);
            y += Font_Size + Line_Spacing;
            inf.Text(x, y, "Schema: ");
            y += Font_Size + Line_Spacing;
            inf.Text(x, y, "DataSource: ");
            y += Font_Size + Line_Spacing;
            inf.Text(x, y, "As of: ");

            y = y_orig;
            x = x_orig;

            x += _padding + ("DataSource: ".Length * (Font_Size / 2));

            //inf.FillColor(Font_Color);
            //inf.StrokeColor(Border_Color);
            //inf.RoundRectangle(x, y + _padding, x + width + (_padding * 3), y + Font_Size + (_padding * 1.5), Corner_Radius, Corner_Radius);


            inf.FillColor(Background_Color);
            inf.StrokeColor(MagickColors.Transparent);
            inf.TextAlignment(TextAlignment.Left);
            x += _padding;
            y += Font_Size + _padding;
            inf.Text(x, y, info.Database_Name);

            inf.FillColor(Font_Color);
            y += Font_Size + Line_Spacing;
            inf.Text(x, y, info.Schema_Name);
            y += Font_Size + Line_Spacing;
            inf.Text(x, y, info.Host);
            y += Font_Size + Line_Spacing;
            inf.Text(x, y, info.Date.ToShortDateString());
            Font_Size = orig_font_size;
            info.Object_Image = inf;
            //info.Width = width;
            //info.Height = height;
            return info;
        }
        public Drawables CreateDatabaseObject(string name, int x, int y, int width = 100, int height = 50, int font_size = 16)
        {
            Drawables db = new Drawables();
            db.FillColor(MagickColors.Snow);
            db.StrokeColor(MagickColors.Black);
            db.RoundRectangle(x, y, x + width, x + height, Corner_Radius, Corner_Radius);
            db.FontPointSize(font_size);
            db.Font(Font_Name);
            db.FillColor(MagickColors.Black);
            db.TextAlignment(TextAlignment.Center);
            x = x + (width / 2);
            y = y + (height / 2) - (font_size / 2) - (_padding / 2);
            db.Text(x, y, name);
            db.Draw(Image);
            return db;
        }
        public Drawables CreateSchemaObject(string name, int x, int y, int width = 100, int height = 50, int font_size = 16)
        {
            //Console.WriteLine(string.Format("x: {0} | y: {1} | width: {2} | height: {3}", x, y, width, height));
            Drawables schema = new Drawables();
            schema.FillColor(MagickColors.Snow);
            schema.StrokeColor(MagickColors.Black);
            schema.RoundRectangle(x, y, x + width, y + height, Corner_Radius, Corner_Radius);
            schema.FontPointSize(font_size);
            schema.Font(Font_Name);
            schema.FillColor(MagickColors.Black);
            schema.TextAlignment(TextAlignment.Center);
            x = x + (width / 2);
            y = y + (height / 2) + (font_size / 2) - (_padding / 2);
            schema.Text(x, y, name);
            schema.Draw(Image);
            return schema;
        }
        public TableInfo CreateTableObject(TableInfo table, int x, int y)
        {
            //--Not Implemented
            //MagickImage table_icon = new MagickImage(".\\images\\Database_Table_Light.png");
            //table_icon.Resize(32, 32);

            Drawables tbl = new Drawables();
            tbl.FillColor(Background_Color);
            tbl.StrokeColor(Border_Color);
            tbl.RoundRectangle(x, y, x + table.Width + (_padding * 3), y + table.Height, Corner_Radius, Corner_Radius); //Main box

            tbl.FillColor(Foreground_Color);
            tbl.RoundRectangle(x, y, x + table.Width + (_padding * 3), y + Font_Size + (Line_Spacing * 2) + _padding, Corner_Radius, Corner_Radius); //Title box

            tbl.FontPointSize(Font_Size + 2);
            tbl.Font(Font_Name);
            tbl.FillColor(Font_Color);
            tbl.StrokeColor(MagickColors.Transparent);
            tbl.TextAlignment(TextAlignment.Left);
            x = x + Border_Width + _padding;
            y = y + Border_Width + Font_Size + (_padding / 2);
            tbl.Text(x, y, table.Name);
            tbl.TextAlignment(TextAlignment.Right);
            tbl.Text(x + table.Width, y, string.Format("({0})", table.RecordCount.ToString()));

            tbl.FontPointSize(Font_Size);
            tbl.FillColor(Font_Color);
            tbl.StrokeColor(MagickColors.Transparent);
            y += Font_Size + Line_Spacing + _padding;
            for (int c = 0; c < table.Columns.Count; c++)
            {
                tbl.TextAlignment(TextAlignment.Left);
                tbl.Text(x, y, table.Columns[c].Name);
                tbl.TextAlignment(TextAlignment.Right);
                tbl.Text(x + table.Width, y, table.Columns[c].DataType);
                y += Font_Size + (Line_Spacing / 2);
            }
            table.Object_Image = tbl;
            table.Width = table.Width + (_padding * 3);
            table.Height = table.Height;
            return table;
        }
    }
    public class DatabaseInfo : InfoObject
    {
        public string Name { get; set; } = "";
        public int DatabaseID { get; set; }
        public string Schema { get; set; } = "";
        public int FileSize { get; set; }
        public string StateDescription { get; set; } = "";
        public string UserAccessDescription { get; set; } = "";
        public DateTime CreateDate { get; set; }
        public InfoPanel Info = new InfoPanel();
        public List<SchemaInfo> Schemas = new List<SchemaInfo>();
        public Drawables Object_Image = new Drawables();
        public DatabaseInfo()
        {

        }
    }
    public class SchemaInfo : InfoObject
    {
        public string Name = "";
        public List<TableInfo> Tables = new List<TableInfo>();
        public Drawables Object_Image = new Drawables();
    }
    public class TableInfo : InfoObject
    {
        public string Name { get; set; }
        public int RecordCount { get; set; }
        public string Schema { get; set; }
        public string Catalog { get; set; }
        public string TableType { get; set; }

        public List<ColumnInfo> Columns = new List<ColumnInfo>();
        public Drawables Object_Image = new Drawables();
        public TableInfo()
        {

        }
    }
    public class ColumnInfo : InfoObject
    {
        public string Name { get; set; }
        public string DataType { get; set; }
        public string CharacterMaxLength { get; set; }
        public string isNullable { get; set; }
        public ColumnInfo()
        {

        }
    }
    public class InfoPanel
    {
        public string Database_Name = "";
        public string Schema_Name = "dbo";
        public string Host = "";
        public DateTime Date = DateTime.Now;
        public int Width;
        public int Height;
        public Drawables Object_Image = new Drawables();
    }
}
