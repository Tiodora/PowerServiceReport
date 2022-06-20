using System;
using System.Linq;
using System.Xml;
using System.Timers;
using Timer = System.Timers.Timer;
using System.Text;
using static Axpo.PowerService;
using Axpo;

namespace PowerServiceReports
{
    public class PowerServiceReports
    {
        private static string path = AppDomain.CurrentDomain.BaseDirectory + @"/Config/Config.xml";
        private static bool existsFile = true;
        private static string folderPath;
        private static int? interval;
        private static Timer timer;

        public static void Main(string[] args)
        {
            try
            {
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

                GetPowerTradeVolume();  //First execution

                SetInterval();  //Set timer for periodic execution
                Console.WriteLine("Press 'Enter' to stop execution...");
                Console.ReadLine();

                timer.Stop();
                timer.Dispose();
                Console.WriteLine("Terminating the application...");
            }
            catch (Exception e)
            {
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
                List<PowerTrade> trades = powerSerice.GetTrades(dateTime).ToList();
                Dictionary<string, double> tradesAggregate = new Dictionary<string, double>();

                foreach (PowerTrade trade in trades)    //loop through all the trades provided
                {
                    int period = 23;
                    foreach (PowerPeriod powerPeriod in trade.Periods)  //loop through each period
                    {
                        TimeSpan hour = TimeSpan.FromHours(period);
                        string localTime = hour.ToString("hh':'mm");
                        double volume = powerPeriod.Volume;
                        if (!tradesAggregate.ContainsKey(localTime))    //add to dictionary if period doesn't exist
                            tradesAggregate[localTime] = 0;
                        tradesAggregate[localTime] += volume;
                        period++;
                    }
                }

                string name = $"PowerPosition_{dateTime.ToString("yyyyMMdd")}_{dateTime.ToString("HHmm")}.csv";
                string file = Path.Combine(folderPath, name);

                if (!File.Exists(file))
                    File.Delete(file);

                CreateCsv(file, tradesAggregate);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error processing power trade values. {e}");
            }
        }

        private static void CreateCsv(string file, Dictionary<string, double> tradesAggregate)
        {
            try
            {
                StringBuilder csv = new StringBuilder();

                string header = "Local Time;Volume";
                csv.AppendLine(header);

                foreach (KeyValuePair<string, double> line in tradesAggregate)
                    csv.AppendLine($"{line.Key};{line.Value}");

                File.WriteAllText(file, csv.ToString());
                Console.WriteLine($"File {Path.GetFileName(file)} creted!");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error creating csv file. {e}");
            }
        }

        private static void SetInterval()
        {
            double intervalms = interval.Value * 60000; //Convert interval to milliseconds
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
                        Console.WriteLine("Invalid option");
                        tries++;
                        break;
                }

                return tries;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error processing selected option. {e}");
                return 3;
            }
        }

        private static void ReadConfigInput()
        {
            try
            {
                Console.WriteLine("Folder path: ");
                folderPath = Console.ReadLine();

                int inputTries = 0;
                while (!existsFile && inputTries < 3)
                {
                    if (string.IsNullOrEmpty(folderPath) || string.IsNullOrWhiteSpace(folderPath))
                    {
                        Console.WriteLine("Value can't be null.");
                        Console.WriteLine("Folder path: ");
                        folderPath = Console.ReadLine();

                        inputTries++;
                    }
                    else
                        inputTries = 4;
                }

                Console.WriteLine("Time interval: ");
                interval = null;
                if (int.TryParse(Console.ReadLine(), out int intValue))
                    interval = intValue;

                inputTries = 0;
                while (!existsFile && inputTries < 3)
                {
                    if (!interval.HasValue)
                    {
                        Console.WriteLine("Value can't be null.");
                        Console.WriteLine("Time interval: ");

                        if (!int.TryParse(Console.ReadLine(), out intValue))
                            interval = intValue;

                        inputTries++;
                    }
                    else
                        inputTries = 4;
                }

                if (!existsFile)
                {
                    CreateXmlConfig(folderPath, interval);
                }
                else if (interval.HasValue || !string.IsNullOrEmpty(folderPath) || !string.IsNullOrWhiteSpace(folderPath))
                {
                    Console.WriteLine("New configuration values:");
                    Console.WriteLine(!string.IsNullOrEmpty(folderPath) || !string.IsNullOrWhiteSpace(folderPath) ? $"Folder path: {folderPath}" : string.Empty);
                    Console.WriteLine(interval.HasValue ? $"Time interval: {interval.Value}" : string.Empty);
                    Console.WriteLine("Would you like to update the configuration file with the new values? Yes (Y) / No (N)");

                    string answer = Console.ReadLine().ToUpper();
                    if (answer == "Y" || answer == "YES")
                        UpdateXmlConfig();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error reading configuration values from command line. {e}");
            }
        }

        private static void LoadConigFile()
        {
            try
            {
                if (!File.Exists(path))
                {
                    Console.WriteLine("The configuration file is missing. Please input the folder path for storing the CSV file and " +
                                      "scheduled time interval in munites. Configuration file will be created from your input.");

                    existsFile = false;

                    ReadConfigInput();
                    return;
                }

                LoadXmlConfig();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error loading configuration from config.xml file. {e}");
            }
        }
        #endregion

        #region XML Operations
        private static void LoadXmlConfig()
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(path);

                foreach (XmlNode node in doc["configuration"].ChildNodes)   //loop thhrough all the nodes in the config.xml file
                {
                    if (node.Name.ToUpper().Equals("PATH"))
                        folderPath = node.InnerText;
                    else if (node.Name.ToUpper().Equals("INTERVAL"))
                        interval = Convert.ToInt32(node.InnerText);
                }

                Console.WriteLine("Configuration values:");
                Console.WriteLine($"Folder path: {folderPath}");
                Console.WriteLine($"Time interval: {interval.Value}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error opening file config.xml. {e}");
            }
        }

        private static void UpdateXmlConfig()
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(path);

                foreach (XmlNode node in doc["configuration"].ChildNodes)
                {
                    if (node.Name.ToUpper().Equals("PATH") && (!string.IsNullOrEmpty(folderPath) || !string.IsNullOrWhiteSpace(folderPath)))
                        node.InnerText = folderPath;
                    else if (node.Name.ToUpper().Equals("INTERVAL") && interval.HasValue)
                        node.InnerText = interval.ToString();
                }

                doc.Save(path);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error updating the configuration file. {e}");
            }
        }

        private static void CreateXmlConfig(string? folderPath, int? interval)
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml($"<configuration>" +
                            $"<path>{folderPath}</path>" +
                            $"<interval>{interval}</interval>" +
                            $"</configuration>");


                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Indent = true;
                // Save the document to a file and auto-indent the output.
                XmlWriter writer = XmlWriter.Create(AppDomain.CurrentDomain.BaseDirectory + @"/Config.xml", settings);
                doc.Save(writer);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error creating the configuration file. {e}");
            }
        }
        #endregion
    }
}

