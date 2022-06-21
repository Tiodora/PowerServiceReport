using System;
using System.Linq;
using System.Xml;
using System.Timers;
using Timer = System.Timers.Timer;
using System.Text;
using static Axpo.PowerService;
using Axpo;
using Serilog.Core;
using Serilog;
using Serilog.Events;

namespace PowerServiceReports
{
    public class PowerServiceReports
    {
        private static string path = Path.Combine(Environment.CurrentDirectory, "Config.xml");
        private static PowerServiceReportsConfig config;
        private static Timer timer;
        private static Logger Logger;

        public static void Main(string[] args)
        {
            try
            {
                Logger = new LoggerConfiguration()
                  .MinimumLevel.Debug()
                  .WriteTo.File(Environment.CurrentDirectory + "\\Logs\\log.log", LogEventLevel.Verbose, "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level}] {Message}{NewLine}{Exception}")  // log file.
                  .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Error)
                  .CreateLogger();

                Console.WriteLine("1. Load configuration file.");
                Console.WriteLine("2. Input folder path for storing the CSV file and/or scheduled time interval in munites.");
                Console.WriteLine("3. Exit.");

                int tries = 0;
                while (tries < 3)
                {
                    if (int.TryParse(Console.ReadLine(), out int option))   // Check if the selected option is an integer
                    {
                        if (option == 3) return;
                        tries = GetConfigValues(option, tries);
                    }
                    else
                    {
                        Console.WriteLine("Invalid option");
                        tries++;
                    }
                }

                if (tries == 3) //3 opportunities to select the desired option
                {
                    Console.WriteLine("Too many falied attempts. Interrupting execution.");
                    return;
                }

                Console.WriteLine("Configuration values:");
                Console.WriteLine($"Folder path: {config.folderPath}");
                Console.WriteLine($"Time interval: {config.interval.Value}");
                Logger.Information($"Configuration values: Folder path - [{config.folderPath}] Time interval - [{config.interval.Value}]");

                GetPowerTradeVolume();  //First execution

                SetInterval();  //Set timer for periodic execution
                Console.WriteLine("Press 'Enter' to stop execution...");
                Console.ReadLine();

                timer.Stop();
                timer.Dispose();
                Logger.Information("Terminating the application...");
                Console.WriteLine("Terminating the application...");
            }
            catch (Exception e)
            {
                Logger.Error($"Error during the execution: {e}");
                Console.WriteLine($"Error during the execution: {e}");
            }
        }

        //Call to the PowerTrade method
        private static void GetPowerTradeVolume()
        {
            try
            {
                IPowerService powerSerice = new Axpo.PowerService();
                DateTime dateTime = DateTime.Now;
                config.trades = powerSerice.GetTrades(dateTime).ToList();

                foreach (PowerTrade trade in config.trades)    //loop through all the trades provided
                {
                    int period = 23;
                    foreach (PowerPeriod powerPeriod in trade.Periods)  //loop through each period
                    {
                        TimeSpan hour = TimeSpan.FromHours(period);
                        string localTime = hour.ToString("hh':'mm");
                        double volume = powerPeriod.Volume;
                        if (!config.tradesAggregate.ContainsKey(localTime))    //add to dictionary if period doesn't exist
                            config.tradesAggregate[localTime] = 0;
                        config.tradesAggregate[localTime] += volume;
                        period++;
                    }
                }

                string name = $"PowerPosition_{dateTime.ToString("yyyyMMdd")}_{dateTime.ToString("HHmm")}.csv";
                string file = Path.Combine(config.folderPath, name);

                if (!File.Exists(file))
                    File.Delete(file);

                CreateCsv(file);
            }
            catch (Exception e)
            {
                Logger.Error($"Error processing power trade values. {e}");
                Console.WriteLine($"Error processing power trade values. {e}");
            }
        }

        private static void CreateCsv(string file)
        {
            try
            {
                StringBuilder csv = new StringBuilder();

                string header = "Local Time;Volume";
                csv.AppendLine(header);

                foreach (KeyValuePair<string, double> line in config.tradesAggregate)
                    csv.AppendLine($"{line.Key};{line.Value}");

                File.WriteAllText(file, csv.ToString());
                Logger.Information($"File {Path.GetFileName(file)} creted!");
                Console.WriteLine($"File {Path.GetFileName(file)} creted!");
            }
            catch (Exception e)
            {
                Logger.Error($"Error creating csv file. {e}");
                Console.WriteLine($"Error creating csv file. {e}");
            }
        }

        private static void SetInterval()
        {
            double intervalms = config.interval.Value * 60000; //Convert interval to milliseconds
            timer = new Timer(intervalms);
            timer.Elapsed += OnTimedEvent;
            timer.AutoReset = true;
            timer.Enabled = true;
        }

        private static void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            GetPowerTradeVolume();
        }

        #region Config
        private static int GetConfigValues(int option, int tries)
        {
            try
            {
                switch (option)
                {
                    case 1:
                        LoadConigFile();
                        tries = 4;
                        break;
                    case 2:
                        Console.WriteLine("Input the folder path for storing the CSV file and scheduled time interval in munites. " +
                                          "Values left blanc will be loaded from configration file.");

                        ReadConfigInput();
                        tries = 4;
                        break;
                    case 0:
                    default:
                        Logger.Warning($"Invalid input option: {option}");
                        Console.WriteLine("Invalid option");
                        tries++;
                        break;
                }

                return tries;
            }
            catch (Exception e)
            {
                Logger.Error($"Error processing selected option. {e}");
                Console.WriteLine($"Error processing selected option. {e}");
                return 3;
            }
        }

        private static void ReadConfigInput()
        {
            try
            {
                PowerServiceReportsConfig auxconfig = new PowerServiceReportsConfig();
                Console.WriteLine("Folder path: ");
                auxconfig.folderPath = Console.ReadLine();

                int inputTries = 0;
                while (!File.Exists(path) && inputTries < 3)
                {
                    if (string.IsNullOrEmpty(auxconfig.folderPath) || string.IsNullOrWhiteSpace(auxconfig.folderPath))
                    {
                        Logger.Warning("Configuration file missing. Value can't be null.");
                        Console.WriteLine("Configuration file missing. Value can't be null.");
                        Console.WriteLine("Folder path: ");
                        auxconfig.folderPath = Console.ReadLine();

                        inputTries++;
                    }
                    else
                        inputTries = 4;
                }

                if (inputTries == 3) //3 opportunities to select the desired option
                {
                    Logger.Error("Too many falied attempts. Interrupting execution.");
                    Console.WriteLine("Too many falied attempts. Interrupting execution.");
                    return;
                }

                Console.WriteLine("Time interval: ");
                auxconfig.interval = null;
                if (int.TryParse(Console.ReadLine(), out int intValue))
                    auxconfig.interval = intValue;

                inputTries = 0;
                while (!File.Exists(path) && inputTries < 3)
                {
                    if (!auxconfig.interval.HasValue)
                    {
                        Logger.Error("Configuration file missing. Value can't be null.");
                        Console.WriteLine("Configuration file missing. Value can't be null.");
                        Console.WriteLine("Time interval: ");

                        if (!int.TryParse(Console.ReadLine(), out intValue))
                            auxconfig.interval = intValue;

                        inputTries++;
                    }
                    else
                        inputTries = 4;
                }

                if (inputTries == 3) //3 opportunities to select the desired option
                {
                    Logger.Error("Too many falied attempts. Interrupting execution.");
                    Console.WriteLine("Too many falied attempts. Interrupting execution.");
                    return;
                }

                if (!File.Exists(path))
                {
                    config.folderPath = auxconfig.folderPath;
                    config.interval = auxconfig.interval.Value;
                    XmlParser.CreateConfigFile(Logger, path, config);
                }
                else if (auxconfig.interval.HasValue || !string.IsNullOrEmpty(auxconfig.folderPath) || !string.IsNullOrWhiteSpace(auxconfig.folderPath))
                {
                    Console.WriteLine("New configuration values:");
                    Console.WriteLine(!string.IsNullOrEmpty(auxconfig.folderPath) || !string.IsNullOrWhiteSpace(auxconfig.folderPath) ? $"Folder path: {auxconfig.folderPath}" : string.Empty);
                    Console.WriteLine(auxconfig.interval.HasValue ? $"Time interval: {auxconfig.interval.Value}" : string.Empty);
                    Console.WriteLine("Would you like to update the configuration file with the new values? Yes (Y) / No (N)");

                    config = XmlParser.LoadConfigFile(Logger, path);
                    if (!string.IsNullOrEmpty(auxconfig.folderPath) || !string.IsNullOrWhiteSpace(auxconfig.folderPath))
                        config.folderPath = auxconfig.folderPath;
                    if (config.interval.HasValue)
                        config.interval = auxconfig.interval;

                    string answer = Console.ReadLine().ToUpper();
                    if (answer == "Y" || answer == "YES")
                        XmlParser.UpdateConfigFile(Logger, config, path);
                }
            }
            catch (Exception e)
            {
                Logger.Error($"An error ocurred reading configuration values from command line. {e}");
            }
        }

        private static void LoadConigFile()
        {
            try
            {
                config = XmlParser.LoadConfigFile(Logger, path);
                if (config == null)
                {
                    config = new PowerServiceReportsConfig();
                    ReadConfigInput();
                }
            }
            catch (Exception e)
            {
                Logger.Error($"An error ocurred while loading the configuration. {e}");
            }
        }
        #endregion
    }
}

