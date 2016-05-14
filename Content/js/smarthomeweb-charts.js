
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
    // A jQuery deferred task is returned that
    // accomplishes this behavior.
    this.requestDataAsync = function(url) {
        return $.get(url);
    };
    var requestDataAsync = this.requestDataAsync;

    // Performs a GET REST call to our API,
    // and parses the response as JSON.
    this.requestData = function(url, callback) {
        requestDataAsync(url).then(callback);
    };
    var requestData = this.requestData;

    // Gets the location that is associated with the given
    // location identifier.
    // The given callback handles the resulting value.
    this.getLocationAsync = function(locationId) {
        var url = "/api/locations/" + locationId.toString();
        return requestDataAsync(url);
    };
    var getLocationAsync = this.getLocationAsync;

    // Gets the location that is associated with the given
    // location identifier.
    // The given callback handles the resulting value.
    this.getLocation = function(locationId, callback) {
        return getLocationAsync(locationId).then(callback);
    };
    var getLocation = this.getLocation;

    // Gets the sensor that is associated with the given
    // sensor identifier.
    // The given callback handles the resulting value.
    this.getSensorAsync = function(sensorId) {
        var url = "/api/sensors/" + sensorId.toString();
        return requestDataAsync(url);
    };
    var getSensorAsync = this.getSensorAsync;

    // Gets the sensor that is associated with the given
    // sensor identifier.
    // The given callback handles the resulting value.
    this.getSensor = function(sensorId, callback) {
        getSensorAsync(sensorId).then(callback);
    };
    var getSensor = this.getSensor;

    // Executes the given array of tasks in parallel.
    // Once they have completed, the given callback is
    // called on an array that holds their results.
    this.whenAll = function(tasks, callback) {
        if (tasks.length === 0)
            return callback([]);
        else if (tasks.length === 1)
            return tasks[0].then(function() { callback(arguments); });

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

    var cachedSensorData = null;
    var cachedLocationData = null;

    // Determines the type of interval described by this
    // autofitted range.
    // This function is part of the public API.
    this.getIntervalType = function() {
        return GraphHelpers.getIntervalType(startTime, endTime, maxMeasurements);
    };

    // Creates a JSON object literal that describes this range.
    // This function is part of the public API.
    this.toJSON = function() {
        return JSON.stringify({
            'sensorId' : sensorId,
            'startTime' : GraphHelpers.timeString(startTime),
            'endTime' : GraphHelpers.timeString(endTime),
            'maxMeasurements' : maxMeasurements
        });
    };

    // Encodes this autofitted range as a relative path.
    // This function is "private".
    var urlPathEncode = function() {
        return sensorId.toString()
            + "/" + GraphHelpers.timeString(startTime) + "/"
            + GraphHelpers.timeString(endTime) + "/"
            + maxMeasurements.toString();
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
        return $.get(url);
    };

    // Retrieves this sensor's measurements.
    // This function is part of the public API.
    this.getMeasurements = function(callback) {
        GraphHelpers.requestData(toUrl(), function(results) { callback(GraphHelpers.processData(results)); });
    };

    // Gets this sensor's additional data, such as its name.
    // The requested data is returned as a promise.
    // This function is part of the public API.
    this.getSensorAsync = function() {
        if (cachedSensorData === null) {
            cachedSensorData = GraphHelpers.getSensorAsync(sensorId);
        }
        return cachedSensorData;
    };
    var getSensorAsync = this.getSensorAsync;

    // Gets this sensor's location.
    // The requested data is returned as a promise.
    // This function is part of the public API.
    this.getLocationAsync = function() {
        if (cachedLocationData === null) {
            cachedLocationData = getSensorAsync().pipe(function(sensor) {
                return GraphHelpers.getLocationAsync(sensor.data.locationId);
            });
        }
        return cachedLocationData;
    };
    var getLocationAsync = this.getLocationAsync;

    // Gets this sensor's location object.
    // This function is part of the public API.
    this.getLocation = function(callback) {
        getLocationAsync().then(callback);
    };
    var getLocation = this.getLocation;

    // Computes the total electricity price
    // for the given array of measurements.
    // The requested data is returned as a promise.
    // This function is part of the public API.
    this.computePriceAsync = function(measurements) {
        return getLocationAsync().then(function(loc) {
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
            for (var i = 0; i < measurements.length; i++) {
                var sensorData = measurements[i];
                if (sensorData.measurement !== null) {
                    totalUsage += sensorData.measurement;
                }
            }
            return elecPrice * hourDiff * totalUsage / measurements.length;
        });
    };

    // Clears all non-essential data that was cached
    // by this autofitted range.
    // This function is part of the public API.
    this.invalidateCache = function() {
        cachedSensorData = null;
        cachedLocationData = null;
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
    // This function is part of the public API.
    this.changed = function() {
        for (var i = 0; i < onChangedHandlers.length; i++) {
            onChangedHandlers[i]();
        }
    };
    // Define this as a private alias.
    var changed = this.changed;

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

    // Gets the number of ranges in this chart.
    // This function is part of the public API.
    this.getRangeCount = function() {
        return ranges.length;
    };

    // Gets a promise that returns a list of
    // range-measurements key-value pair lists.
    // This function is part of the public API.
    this.getRangesWithMeasurements = function(callback) {
        var results = [];
        $.each(ranges, function(index, kvPair) {
            results.push(kvPair.getValue().then(function(val) {
                return [kvPair.getKey(), val];
            }));
        });
        return GraphHelpers.whenAll(results, callback);
    };

    var createRangeKvPair = function(range) {
        return new LazyKeyValuePair(range, function(key) {
            return key.getMeasurementsAsync();
        });
    };

    // Adds the given range to this chart description.
    // This function is part of the public API.
    this.addRange = function(value) {
        ranges.push(createRangeKvPair(value));
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
    // remain. A boolean is returned that is 'true'
    // if at least one range has been removed.
    // This function is part of the public API.
    this.filterRanges = function(predicate) {
        var oldLength = ranges.length;
        ranges = $.grep(ranges, function(kvPair) {
            return predicate(kvPair.getKey());
        });
        if (ranges.length === oldLength) {
            return false;
        }
        else {
            changed();
            return true;
        }
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

    // Invalidates this chart description's
    // data cache. An optional predicate function
    // controls which ranges get ejected from the
    // cache.
    this.invalidateCache = function(predicate) {
        ranges = $.map(ranges, function(kvPair) {
            if (predicate === undefined || predicate(kvPair.getKey()))
                return createRangeKvPair(kvPair.getKey());
            else
                return kvPair;
        });
    };
};
