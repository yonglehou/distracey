﻿using System.Collections.Concurrent;
using System.Diagnostics;
using Distracey.Agent.SystemWeb.WebApi;
using Distracey.Common;

namespace Distracey.PerformanceCounter.ApiFilterCounter
{
    public class ApiFilterCounterAverageTimeCounter : IApiFilterCounter
    {
        private readonly string _applicationName;
        private readonly string _instanceName;

        private const string AverageTimeTakenMsCounter = "ApiFilterCounterAverageTimeCounter";
        private const string AverageTimeTakenMsBaseCounter = "ApiFilterCounterAverageTimeBaseCounter";

        private readonly ConcurrentDictionary<string, System.Diagnostics.PerformanceCounter> Counters = new ConcurrentDictionary<string, System.Diagnostics.PerformanceCounter>();
        private readonly ConcurrentDictionary<string, System.Diagnostics.PerformanceCounter> BaseCounters = new ConcurrentDictionary<string, System.Diagnostics.PerformanceCounter>();

        public ApiFilterCounterAverageTimeCounter(string applicationName, string instanceName)
        {
            _applicationName = applicationName;
            _instanceName = instanceName;
        }

        public void Start(IApmContext apmContext, ApmWebApiStartedMessage apmWebApiStartedMessage)
        {
            var key = string.Empty;
            
            object counterProperty;

            if (!apmContext.TryGetValue(AverageTimeTakenMsCounter, out counterProperty))
            {
                var categoryName = PerformanceCounterApmEventLogger.GetApiFilterCategoryName(_applicationName);
                var counterName = GetCounterName(apmWebApiStartedMessage.MethodIdentifier);

                var counter = Counters.GetOrAdd(key, s => GetCounter(categoryName, _instanceName, counterName));
                apmContext.Add(AverageTimeTakenMsCounter, counter);
            }

            object baseCounterProperty;

            if (!apmContext.TryGetValue(AverageTimeTakenMsBaseCounter, out baseCounterProperty))
            {
                var categoryName = PerformanceCounterApmEventLogger.GetApiFilterCategoryName(_applicationName);
                var counterName = GetBaseCounterName(apmWebApiStartedMessage.MethodIdentifier);
                var baseCounter = BaseCounters.GetOrAdd(key, s => GetBaseCounter(categoryName, _instanceName, counterName));
                apmContext.Add(AverageTimeTakenMsBaseCounter, baseCounter);
            }
        }

        public void Finish(IApmContext apmContext, ApmWebApiFinishedMessage apmWebApiFinishedMessage)
        {
            object counterProperty;

            if (apmContext.TryGetValue(AverageTimeTakenMsCounter, out counterProperty))
            {
                var counter = (System.Diagnostics.PerformanceCounter)counterProperty;
                counter.IncrementBy(apmWebApiFinishedMessage.Duration.Milliseconds);
            }

            object baseCounterProperty;

            if (apmContext.TryGetValue(AverageTimeTakenMsBaseCounter, out baseCounterProperty))
            {
                var baseCounter = (System.Diagnostics.PerformanceCounter)baseCounterProperty;
                baseCounter.Increment();
            }
        }

        private string GetCounterName(string methodIdentifier)
        {
            return string.Format("{0} - Average seconds taken to execute", methodIdentifier);
        }

        private string GetBaseCounterName(string methodIdentifier)
        {
            return string.Format("{0} - Average seconds taken to execute (base)", methodIdentifier);
        }

        public CounterCreationData[] GetCreationData(string methodIdentifier)
        {
            var counterCreationDatas = new CounterCreationData[2];
            counterCreationDatas[0] = new CounterCreationData
                                          {
                                              CounterType = PerformanceCounterType.AverageTimer32,
                                              CounterName = GetCounterName(methodIdentifier),
                                              CounterHelp = "Average seconds taken to execute"
                                          };
            counterCreationDatas[1] = new CounterCreationData
                                          {
                                              CounterType = PerformanceCounterType.AverageBase,
                                              CounterName = GetBaseCounterName(methodIdentifier),
                                              CounterHelp = "Average seconds taken to execute"
                                          };
            return counterCreationDatas;
        }

        private System.Diagnostics.PerformanceCounter GetCounter(string categoryName, string instanceName, string counterName)
        {
            var counter = new System.Diagnostics.PerformanceCounter {
                CategoryName = categoryName, 
                CounterName = counterName,
                InstanceName = instanceName,
                ReadOnly = false,
                InstanceLifetime = PerformanceCounterInstanceLifetime.Process,
            };
            counter.RawValue = 0;

            return counter;
        }

        private System.Diagnostics.PerformanceCounter GetBaseCounter(string categoryName, string instanceName, string counterName)
        {
            var counter = new System.Diagnostics.PerformanceCounter
            {
                CategoryName = categoryName,
                CounterName = counterName,
                InstanceName = instanceName,
                ReadOnly = false,
                InstanceLifetime = PerformanceCounterInstanceLifetime.Process,
            };
            counter.RawValue = 0;

            return counter;
        }

        public void Dispose()
        {
            foreach (var counter in Counters)
            {
                counter.Value.RemoveInstance();
                counter.Value.Dispose();
            }
            Counters.Clear();

            foreach (var counter in BaseCounters)
            {
                counter.Value.RemoveInstance();
                counter.Value.Dispose();
            }
            BaseCounters.Clear();
        }
    }
}