using System;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer.Unity;
using Warlander.Deedplanner.Features;
using Warlander.Deedplanner.Gui;
using Warlander.Deedplanner.Gui.Windows;
using Warlander.UI.Windows;
using Warlogic.Features;

namespace Warlander.Deedplanner.Logic
{
    /// <summary>
    /// Shows a modal report window for the first exception/error logged in the session.
    /// Latch state is static because one presenter instance exists per scene scope,
    /// while "first in session" spans scene transitions.
    /// </summary>
    public class ErrorReportPresenter : IInitializable, IDisposable
    {
        private static ErrorReport _firstReport;
        private static bool _windowOpened;

        private readonly WindowCoordinator _windowCoordinator;
        private readonly IFeatureStateRetriever<Feature> _featureStateRetriever;
        private SynchronizationContext _mainThreadContext;

        public ErrorReportPresenter(WindowCoordinator windowCoordinator, IFeatureStateRetriever<Feature> featureStateRetriever)
        {
            _windowCoordinator = windowCoordinator;
            _featureStateRetriever = featureStateRetriever;
        }

        public void Initialize()
        {
            if (!_featureStateRetriever.IsFeatureEnabled(Feature.ErrorWindow))
            {
                return;
            }

            _mainThreadContext = SynchronizationContext.Current;
            Application.logMessageReceivedThreaded += OnLogMessageReceived;
        }

        public void Dispose()
        {
            Application.logMessageReceivedThreaded -= OnLogMessageReceived;
        }

        // Invoked on the originating thread, potentially off-main and in parallel - no Unity API access here.
        private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Exception && type != LogType.Error)
            {
                return;
            }

            ErrorReport report = new ErrorReport(condition, stackTrace, DateTime.UtcNow);
            if (Interlocked.CompareExchange(ref _firstReport, report, null) != null)
            {
                return;
            }

            _mainThreadContext.Post(_ => ShowFirstReport(), null);
        }

        private void ShowFirstReport()
        {
            if (_windowOpened)
            {
                return;
            }
            _windowOpened = true;

            ErrorReportWindow window = _windowCoordinator.CreateWindowExclusive<ErrorReportWindow>(WindowNames.ErrorReportWindow);
            window?.ShowReport(BuildReportText(_firstReport), GetPlayerLogFolder());
        }

        private static string BuildReportText(ErrorReport report)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("DeedPlanner 3 error report");
            builder.AppendLine($"Version: {Application.version}");
            builder.AppendLine($"Platform: {Application.platform} ({SystemInfo.operatingSystem})");
            builder.AppendLine($"Time: {report.UtcTime:yyyy-MM-dd HH:mm:ss} UTC");
            builder.AppendLine($"Scene: {SceneManager.GetActiveScene().name}");
            builder.AppendLine();
            builder.AppendLine(report.Condition);
            builder.Append(report.StackTrace);
            return builder.ToString();
        }

        private static string GetPlayerLogFolder()
        {
#if UNITY_WEBGL
            return null;
#elif UNITY_STANDALONE_OSX
            string home = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            return System.IO.Path.Combine(home, "Library", "Logs", Application.companyName, Application.productName);
#else
            return Application.persistentDataPath;
#endif
        }

        private sealed class ErrorReport
        {
            public readonly string Condition;
            public readonly string StackTrace;
            public readonly DateTime UtcTime;

            public ErrorReport(string condition, string stackTrace, DateTime utcTime)
            {
                Condition = condition;
                StackTrace = stackTrace;
                UtcTime = utcTime;
            }
        }
    }
}
