using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Instrumentation;
using Prometheus;
using Metrics = Prometheus.Metrics;

namespace ModelSaber.API.GraphQL
{
    public class MetricsFieldMiddleware : IFieldMiddleware
    {
        public static readonly Summary OperationSummary = Metrics.CreateSummary("graphql_operation_summary", "Operation execution summary in milliseconds", new SummaryConfiguration
        {
            LabelNames = new []{"name"},
            Objectives = new[]
            {
                new QuantileEpsilonPair(0.5, 0.05),
                new QuantileEpsilonPair(0.9, 0.01),
                new QuantileEpsilonPair(0.99, 0.005),
                new QuantileEpsilonPair(0.999, 0.001)
            }
        });

        public static readonly Summary DocumentSummary = Metrics.CreateSummary("graphql_document_summary", "Document execution summary in milliseconds", new SummaryConfiguration
        {
            LabelNames = new []{"name"},
            Objectives = new[]
            {
                new QuantileEpsilonPair(0.5, 0.05),
                new QuantileEpsilonPair(0.9, 0.01),
                new QuantileEpsilonPair(0.99, 0.005),
                new QuantileEpsilonPair(0.999, 0.001)
            }
        });

        public static readonly Gauge OperationCounter = Metrics.CreateGauge("graphql_operation_counter", "Operation execution time in milliseconds", new GaugeConfiguration
        {
            LabelNames = new[] { "name" }
        });

        public static readonly Gauge DocumentCounter = Metrics.CreateGauge("graphql_document_counter", "Document execution time in milliseconds", new GaugeConfiguration
        {
            LabelNames = new[] { "name" }
        });

        private static readonly object _lock = new();

        public static readonly List<PerfRecord[]> PerfRecords = new();

        public MetricsFieldMiddleware()
        {
            Task.Run(CheckList);
        }

        public async Task CheckList()
        {
            lock (_lock)
            {
                var subjects = new List<string>();
                foreach (var perfRecords in PerfRecords)
                {
                    var perfRecord = perfRecords.First(t => t.Category == "operation");
                    var subject = perfRecord.Subject!;
                    if (subjects.Contains(subject)) continue;
                    subjects.Add(subject);
                    OperationSummary.WithLabels(subject).Observe(perfRecord.Duration);
                    var docDur = perfRecords!.Where(t => t.Category == "document").Sum(t => t.Duration);
                    DocumentSummary.WithLabels(subject).Observe(docDur);
                }
                PerfRecords.Clear();
            }
            await Task.Delay(TimeSpan.FromMilliseconds(500));
            _ = Task.Run(CheckList);
        }

        public async Task<object?> Resolve(IResolveFieldContext context, FieldMiddlewareDelegate next)
        {
            var ret = await next(context).ConfigureAwait(false);
            lock (_lock)
            {
                var rec = context.Metrics.Finish();
                var perfRecord = rec!.First(t => t.Category == "operation");
                var subject = perfRecord.Subject!;
                var docDur = rec!.Where(t => t.Category == "document").Sum(t => t.Duration);
                DocumentCounter.WithLabels(subject).Set(docDur);
                OperationCounter.WithLabels(subject).Set(perfRecord.Duration);
                PerfRecords.Add(rec!);
            }

            return ret;
        }
    }
}
