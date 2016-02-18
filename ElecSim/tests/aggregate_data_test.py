import csv, sys
from datetime import datetime
import main

#goal: print averages per month for three households for total,lights and each device
def sanity_test():
    #generate year of date
    dateformat = '%Y-%m-%d %H:%M:%S'
    from_date = datetime.strptime("2016-01-01 12:00:00", dateformat)
    to_date = datetime.strptime("2016-12-31 12:00:00", dateformat)
    no_house_holds = 3
    config_file = "MY_CONFIGURE.json"
    main.make_config_file(no_house_holds, config_file)
    files = []
    for i in range(no_house_holds):
        output_file = "MY_FILE_2016_" + str(i+1) + ".csv"
        main.generate_sensor_data(config_file,i+1,from_date,to_date,output_file)
        files.append(output_file)
    #group by month, sum(total), sum(ligths), sum(tv) ...
    all_data = []
    for i in range(len(files)):
        file = files[i]
        f = open(file, 'r')
        reader = csv.DictReader(f,delimiter=';')
        sums_month = [{} for i in range(12)]
        for row in reader:
            #print(row)
            timestamp = datetime.strptime(row["Timestamp"], dateformat)
            for key, value in row.items():
                if key == "Timestamp" or key =='':
                    continue
                if not key in sums_month[timestamp.month-1]:
                    sums_month[timestamp.month-1][key] = 0;
                sums_month[timestamp.month-1][key] += float(value)
        f.close()
        print("household: %d" % (i+1))
        all_data.append(sums_month)
    #output like:
    save_file = open('group_by_month.csv','w')
    #Household; Month; Total; Lights; Frigde ...
    all_keys = set()
    for household in all_data:
        for month in household:
             for key in sorted(month):
                 all_keys.add(key)
    save_file.write("Household;Month;");
    for key in all_keys:
        save_file.write(key)
        save_file.write(";")
    save_file.write("\n")
    household_idx = 0
    for household in all_data:
        household_idx+=1
        month_idx = 0
        for month in household:
            month_idx+=1
            save_file.write(str(household_idx))
            save_file.write(";")
            save_file.write(str(month_idx))
            save_file.write(";")
            for key in all_keys:
                if key in month:
                    save_file.write(str(month[key]))
                    save_file.write(";")
                else:
                    save_file.write(";")
            save_file.write("\n")
    save_file.close()
    print("saved %s" % 'group_by_month.csv')

sanity_test()