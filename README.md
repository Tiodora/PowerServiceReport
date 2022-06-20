# PowerServiceReport
The power traders require an intra-day report to give them their day ahead power position. This application generates a report with the aggregated volume per hour to a CSV file based upon a configurable schedule.
## Installation
The program is self-contained so there is no need to install anything, just decompress Release.7z and run MartianRobots.exe 

## Options
1. Load configuration file.
2. Input folder path for storing the CSV file and/or scheduled time interval in munites.
3. Exit

### Load configuration file.
 The first option allows the user to load the configuration file containing the folder path for storing the CSV file and the time interval. If the file doesn't exist, the user will be requested to input both values manually. These values will be used to create a new configuration file.
```sh
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <path>C:\Users\Tiodora\Desktop\PowerServiceReports\Export\</path>
  <interval>3</interval>
</configuration>
```

### Input folder path for storing the CSV file and/or scheduled time interval in munites.
 The second option allows the user to enter the values manually. The user can enter one, none or both configuration valus. If an input is left blanc, the value defined in the configuration file will be used.

### Exit
End the program execution.
 
## License
**Free Software**
