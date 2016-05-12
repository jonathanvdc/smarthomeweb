
GraphHelpers = new function()
{
    // Formats the given date-time instance as an ISO date-time string.
    this.timeString = function(time) {
        var seconds = time.getSeconds() < 10 ? "0" + time.getSeconds().toString() : time.getSeconds().toString();
        var minutes = time.getMinutes() < 10 ? "0" + time.getMinutes().toString() : time.getMinutes().toString();
        var hours = time.getHours() < 10 ? "0" + time.getHours().toString() : time.getHours().toString();
        var day =  time.getDate().toString();
        var month = (time.getMonth() + 1).toString();
        var year =	time.getFullYear().toString();
        var datestring = year + "-" + month + "-" + day + "T" + hours + ":" + minutes + ":" + seconds;
        return datestring;
    };

    // Determines the type of interval that the given endTime
    // and start-time represent.
    this.getIntervalType = function(startTime, endTime, measurementCount) {
        var diff = Math.abs(endTime.getTime() - startTime.getTime())
        if (diff / 60000 <= measurementCount) {
            return "minutes";
        }
        else if (diff / (60000 * 60) <= measurementCount) {
            return "hours";
        }
        else if (diff / (60000 * 60 * 24) <= measurementCount) {
            return "days";
        }
        else if (diff / (60000 * 60 * 24 * 365) <= measurementCount) {
            return "months";
        }
        else {
            return "years";
        }
    };

    // Prints the given date, according to the given interval type.
    this.printTime = function(date, intervalType) {
        if (intervalType == "minutes") {
            return date.toISOString().substring(0, 16);
        }
        else if (intervalType == "hours") {
            return date.toISOString().substring(0, 13);
        }
        else if (intervalType == "days") {
            return date.toISOString().substring(0, 10);
        }
        else if (intervalType == "months") {
            return date.toISOString().substring(0, 7);
        }
        else {
            return date.toISOString().substring(0, 4);
        }
    };

    // Replaces measurement data that consists of 'null'
    // measurements only, by a simple empty array. This
    // allows us to display an accurate error message if
    // we have no measurements to show.
    this.processData = function(json) {
        for (var i = 0; i < json.length; i++) {
            if (json[i].measurement !== null) {
                return json;
            }
        }
        return json;
    };

    // Performs a GET REST call to our API,
    // and parses the response as JSON.
    this.requestData = function(url, callback) {
        var xmlhttp = new XMLHttpRequest();
        xmlhttp.open("GET", url, true);
        xmlhttp.onload = function (e) {
            callback(JSON.parse(xmlhttp.responseText));
        };
        xmlhttp.send(null);
    };

    // Performs a GET REST call to our API,
    // and parses the response as JSON.
    // A jQuery deferred task is returned that
    // accomplishes this behavior.
    this.requestDataAsync = function(url) {
        return $.get(url).then(JSON.parse);
    };

    // Gets the location that is associated with the given
    // location identifier.
    // The given callback handles the resulting value.
    this.getLocation = function(locationId, callback) {
        var url = "/api/locations/" + locationId.toString();
        return this.requestData(url, callback);
    };

    // Gets the location belonging to the given sensor.
    // The given callback handles the resulting value.
    this.getSensorLocation = function(sensorId, callback) {
        var url = "/api/sensors/" + sensorId.toString();
        return this.requestData(url, function(sensor) {
            getLocation(sensor.data.locationId, callback);
        });
    };

    // Computes and displays the total electricity price
    // for the given array of measurements.
    this.computePrice = function(sensorId, measurements, callback) {
        this.getSensorLocation(sensorId, function(loc) {
            // A location stores its price per unit of electricity as currency/kWh.
            // Since we want the total electricity price, we will multiply
            // the location's price per unit by the total time, and the average
            // electricity usage of the currently selected sensor.
            var elecPrice = loc.data.electricityPrice;
            if (elecPrice === null)
                // This is kind of lame, really.
                return;

            var hourDiff = Math.abs(endTime.getTime() - startTime.getTime()) / 1000 / 3600;
            var totalUsage = 0.0;
            var n = 0;
            for (var i = 0; i < measurements.length; i++) {
                var sensorData = measurements[i];
                if (sensorData.measurement !== null) {
                    totalUsage += sensorData.measurement;
                    n++;
                }
            }
            var totalPrice = elecPrice * hourDiff * totalUsage / n;
            callback(totalPrice);
        });
    };

    // Executes the given array of tasks in parallel.
    // Once they have completed, the given callback is
    // called on an array that holds their results.
    this.whenAll = function(tasks, callback) {
        // Wait for all tasks to complete...
        return $.when.apply($, tasks).then(function() {
            // ... then call the callback on the results.
            // (use the 'arguments' pseudo-array to get all arguments)
            callback(arguments);
        });
    };
};

// Defines an autofitted range "class".
//
// Instantiate with:
//
//     new AutofitRange(...)
//
AutofitRange = function(sensorId, startTime, endTime, maxMeasurements) {
    this.sensorId = sensorId;
    this.startTime = startTime;
    this.endTime = endTime;
    this.maxMeasurements = maxMeasurements;

    // Determines the type of interval described by this
    // autofitted range.
    // This function is part of the public API.
    this.getIntervalType = function() {
        return GraphHelpers.getIntervalType(this.startTime, this.endTime, this.maxMeasurements);
    };

    // Creates a JSON object literal that describes this range.
    // This function is part of the public API.
    this.toJSON = function() {
        return JSON.stringify({
            'sensorId' : this.sensorId,
            'startTime' : GraphHelpers.TimeString(this.startTime),
            'endTime' : GraphHelpers.TimeString(this.endTime),
            'maxMeasurements' : this.maxMeasurements
        });
    };

    // Encodes this autofitted range as a relative path.
    // This function is "private".
    var urlPathEncode = function() {
        return this.sensorId.toString()
            + "/" + timeString(this.startTime) + "/"
            + timeString(this.endTime) + "/"
            + this.maxMeasurements.toString();
    };

    // Converts this autofitted range to an autofit URL request.
    // This function is "private".
    var toUrl = function() {
        return "/api/autofit/" + urlPathEncode();
    };

    // Retrieves this sensor's measurements, as
    // a deferred task that provides an array
    // of JSON measurement objects.
    // This function is part of the public API.
    this.getMeasurementsAsync = function() {
        return GraphHelpers.requestDataAsync(toUrl()).then(GraphHelpers.processData);
    };

    // Retrieves this sensor's total usage, as
    // a deferred task that produces a floating-point
    // number.
    // This function is part of the public API.
    this.getTotalUsageAsync = function() {
        var url = "/api/autofit/total/" + urlPathEncode();
        return $.get(url).then(parseFloat);
    };

    // Retrieves this sensor's measurements.
    // This function is part of the public API.
    this.getMeasurements = function(callback) {
        GraphHelpers.requestData(toUrl(), function(results) { callback(GraphHelpers.processData(results)); });
    };

    // Gets this sensor's location object.
    // This function is part of the public API.
    this.getLocation = function(callback) {
        GraphHelpers.getSensorLocation(this.sensorId, callback);
    };

    // Computes and displays the total electricity price
    // for the given array of measurements.
    // This function is part of the public API.
    this.computePrice = function(measurements, callback) {
        GraphHelpers.computePrice(this.sensorId, measurements, callback);
    };
};

// A key-value pair that has a "key" object, and a lazily
// computed "value."
// Note: the computeValue function must take exactly
// one argument, which is the key.
LazyKeyValuePair = function(key, computeValue) {
    var cachedVal = null;

    // Gets this key-value pair's key.
    this.getKey = function() {
        return key;
    };

    // Gets this key-value pair's value.
    // If this value does not exist yet,
    // then it is computed.
    this.getValue = function() {
        if (cachedVal === null) {
            cachedVal = computeValue(key);
        }
        return cachedVal;
    };
};

// Describes a measurements chart. This type does two things:
//
//     * Store autofitted ranges of measurements
//     * Raise events when anything changes.
//
ChartDescription = function() {

    // Holds this chart description's autofitted ranges.
    var ranges = [];

    // Holds on-changed handlers for this chart.
    var onChangedHandlers = [];

    // Invokes all on-changed handlers.
    var changed = function() {
        for (var i = 0; i < onChangedHandlers.length; i++) {
            onChangedHandlers[i]();
        }
    };

    // Registers a (parameterless) handler function
    // with this chart description that is fired
    // whenever the chart changes.
    // This function is part of the public API.
    this.onChanged = function(handler) {
        onChangedHandlers.push(handler);
    };

    // Performs an action. Any changes to this chart description are
    // only reported when the entire action has completed.
    // This function is part of the public API.
    this.batchChanges = function(action) {
        // Replace the on-changed handlers by
        // a single function that remembers whether
        // any changes have occurred or not.
        var hasChanged = false;
        var oldHandlers = onChangedHandlers;
        onChangedHandlers = [function() { hasChanged = true; }];

        // Perform the given action.
        action();

        // Restore the old handlers.
        onChangedHandlers = oldHandlers;
        // Call them if the chart has changed.
        if (hasChanged)
            changed();
    };

    // Gets this chart's ranges.
    // This function is part of the public API.
    this.getRanges = function() {
        // Copy the ranges, so external users don't
        // corrupt our painstakingly constructed
        // event system.
        var results = [];
        for (var i = 0; i < ranges.length; i++) {
            results.push(ranges[i].getKey());
        }
        return results;
    };

    // Gets a dictionary that maps ranges to
    // measurement promises for this chart.
    // This function is part of the public API.
    this.getRangesWithMeasurements = function() {
        var results = {};
        for (var i = 0; i < ranges.length; i++) {
            results[ranges[i].getKey()] = ranges[i].getValue();
        }
        return results;
    };

    // Adds the given range to this chart description.
    // This function is part of the public API.
    this.addRange = function(value) {
        ranges.push(new LazyKeyValuePair(value, function(key) {
            return key.getMeasurementsAsync();
        }));
        changed();
    };

    // Add the given ranges to this chart description.
    // This function is part of the public API.
    this.addRanges = function(values) {
        batchChanges(function() {
            for (var i = 0; i < values.length; i++) {
                this.addRange(values[i]);
            }
        });
    };

    // Filters ranges based on the given predicate.
    // Any ranges for which the predicate returns
    // 'false' are discarded. All other ranges
    // remain.
    // This function is part of the public API.
    this.filterRanges = function(predicate) {
        ranges = $.grep(ranges, function(kvPair) {
            return predicate(kvPair.getKey());
        });
        changed();
    };

    // Checks if this graph contains at least one range
    // for which the given predicate returns 'true'.
    // This function is part of the public API.
    this.containsRange = function(predicate) {
        return $.grep(ranges, function(kvPair) {
            return predicate(kvPair.getKey());
        }).length > 0;
    };

    // Removes all ranges from this chart description.
    // This function is part of the public API.
    this.clearRanges = function() {
        ranges = [];
        changed();
    };

    // Asynchronously gets the given ranges' total usages.
    // The given callback is invoked on an array that contains
    // `{ 'range' : AutofitRange, 'usage' : float }` tuples.
    // This function is part of the public API.
    this.getTotalUsages = function(callback) {
        // Create an array of GET tasks.
        var tasks = [];
        for (var i = 0; i < ranges.length; i++) {
            var ran = ranges[i].getKey();
            tasks.push(ran.getTotalUsageAsync().then(function(result) {
                return { 'range' : ran, 'usage' : result };
            }));
        }
        // Run them all in parallel.
        return GraphHelpers.whenAll(tasks, callback);
    };
};
