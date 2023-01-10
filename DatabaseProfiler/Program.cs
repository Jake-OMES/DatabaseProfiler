using CommandLineParser;
using CommandLineParser.Arguments;
using CommandLineParser.Exceptions;
using DatabaseProfiler;
using ImageMagick;
using System.Diagnostics;

if (args.Length == 0)
{
    Console.WriteLine("Please supply at least a valid SQL connection string!");
    Console.WriteLine("(Ex: C:\\Documents\\DatabaseProfiler.exe -c \"Data Source=127.0.0.1;User Id=sa;Password=Password12;Connection Timeout=10\")");
    return;
}
else
{
    CommandLineParser.CommandLineParser Parser = new CommandLineParser.CommandLineParser();
    ValueArgument<string> database_name = new ValueArgument<string>('d', "database", "The name of the database you wish to profile, or 'All' for the entire server. (Default: All)");
    ValueArgument<string> output_path = new ValueArgument<string>('o', "output", "The folder where the images will be written to. (Default: Current Directory)");
    ValueArgument<string> connection_string = new ValueArgument<string>('c', "connection_string", "The SQL server connection string.");
    ValueArgument<string> fore_color = new ValueArgument<string>('f', "foreground", "The desired foreground color in hex format (Default: #161B22)");
    ValueArgument<string> back_color = new ValueArgument<string>('b', "background", "The desired background color in hex format (Default: #0D1117)");
    ValueArgument<string> border_color = new ValueArgument<string>('r', "border", "The desired color of box borders in hex format (Default: #30363D)");
    ValueArgument<string> font_color = new ValueArgument<string>('t', "text", "The desired color of the text in hex format (Default: #C9D1D9)");
    ValueArgument<string> font_name = new ValueArgument<string>('n', "font_name", "The desired font to use. (Default: Arial)");
    SwitchArgument transparent = new SwitchArgument('a', "alpha", "Set whether image base background is transparent or not.", false);

    Parser.Arguments.Add(database_name);
    Parser.Arguments.Add(output_path);
    Parser.Arguments.Add(connection_string);
    Parser.Arguments.Add(fore_color);
    Parser.Arguments.Add(back_color);
    Parser.Arguments.Add(border_color);
    Parser.Arguments.Add(font_color);
    Parser.Arguments.Add(font_name);
    Parser.Arguments.Add(transparent);

    try
    {
        Parser.ParseCommandLine(args);
        if (connection_string.Value == null)
        {
            Console.WriteLine("No connection string supplied!");
            Console.WriteLine("Please use arg '-c' to specify the SQL connection string.");
            Console.WriteLine("(Ex: -c \"Data Source=127.0.0.1;User Id=sa;Password=Password12;Connection Timeout=10\")");
            Console.WriteLine("Exiting...");
        }
        else
        {

            string db_name = database_name.Value == null ? "All" : database_name.Value;
            string out_path = output_path.Value == null ? Directory.GetCurrentDirectory() : output_path.Value;
            string cnx_string = connection_string.Value == null ? "" : connection_string.Value;
            string fore = fore_color.Value == null ? "#161B22" : fore_color.Value;
            string back = back_color.Value == null ? "#0D1117" : back_color.Value;
            string border = border_color.Value == null ? "#30363D" : border_color.Value;
            string font_c = font_color.Value == null ? "#C9D1D9" : font_color.Value;
            string font_n = font_name.Value == null ? "Arial" : font_name.Value;

            Console.WriteLine("-----|Run settings|-----");
            Console.WriteLine("Database to Profile: {0}", db_name, database_name.Value);
            Console.WriteLine("Saving images to: {0}", out_path, output_path.Value);
            Console.WriteLine("Connection string: {0}", cnx_string, connection_string.Value);
            Console.WriteLine("Foreground Color: {0}", fore);
            Console.WriteLine("Background Color: {0}", back);
            Console.WriteLine("Border Color: {0}", border);
            Console.WriteLine("Font Color: {0}", font_c);
            Console.WriteLine("Font Name: {0}", font_n);
            Console.WriteLine("Transparent: {0}", transparent.Value);
            Console.WriteLine("------------------------");
            Console.Write("Is this correct [Y/n]? ");
            ConsoleKeyInfo key = Console.ReadKey();
            if (key.Key == ConsoleKey.Y || key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                Console.WriteLine("Profiling...");
                ImageMap mapper = new ImageMap();
                mapper.OutputFolder = out_path;
                mapper.SQLConnectionString = cnx_string;
                mapper.Foreground_Color = new MagickColor(fore);
                mapper.Background_Color = new MagickColor(back);
                mapper.Font_Color = new MagickColor(font_c);
                mapper.Font_Name = font_n;
                mapper.Border_Color = new MagickColor(border);
                mapper.Transparent_Background = transparent.Value;
                mapper.Create(db_name);

                Console.Write(string.Format("Would you like to open {0} [y/N]? ", out_path));
                key = Console.ReadKey();
                if (key.Key == ConsoleKey.Y)
                {
                    Process.Start("explorer.exe", out_path);
                }
                Console.WriteLine();
                Console.WriteLine("Done");
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine("Exiting...");
            }
        }
    }
    catch (CommandLineException e)
    {
        Console.WriteLine(e.Message);
    }
}