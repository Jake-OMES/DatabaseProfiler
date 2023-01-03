using CommandLineParser;
using CommandLineParser.Arguments;
using CommandLineParser.Exceptions;
using DatabaseProfiler;

if (args.Length == 0)
{
    Console.WriteLine("Please run with required arguments!");
    return;
}
else
{
    CommandLineParser.CommandLineParser Parser = new CommandLineParser.CommandLineParser();
    ValueArgument<string> database_name = new ValueArgument<string>('d', "database", "database name");
    ValueArgument<string> output_path = new ValueArgument<string>('o', "output", "output path");
    ValueArgument<string> connection_string = new ValueArgument<string>('c', "connection_string", "the SQL server connection string");

    Parser.Arguments.Add(database_name);
    Parser.Arguments.Add(output_path);
    Parser.Arguments.Add(connection_string);

    try
    {
        ImageMap mapper = new ImageMap();

        Parser.ParseCommandLine(args);
        if (database_name.Parsed)
        {
            if (database_name.Value.ToUpper() == "ALL")
            {
                mapper.ProfileServer = true;
            }
            mapper.Database_Name = database_name.Value;
        }
        if (output_path.Parsed)
        {
            mapper.OutputFolder = output_path.Value;
        }
        if (connection_string.Parsed)
        {
            mapper.SQLConnectionString = connection_string.Value;
        }
        Console.WriteLine("Database to Profile: {0}", database_name.Value);
        Console.WriteLine("Writing images to: {0}", output_path.Value);
        Console.WriteLine("Profiling...");

        mapper.CreateNew();
        Console.WriteLine("Done");
    }
    catch (CommandLineException e)
    {
        Console.WriteLine(e.Message);
    }
}
//ArgParse ArgParse = new ArgParse();



//ImageMap mapper = new ImageMap();
//mapper.CreateNew();