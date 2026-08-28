using System;
using UnityEngine;

namespace Warlander.Deedplanner.Logging
{
    public class CategoryLogger : ICategoryLogger
    {
        private readonly LogCategory _category;
        private readonly LogLevelFilter _filter;

        public CategoryLogger(LogCategory category, LogLevelFilter filter)
        {
            _category = category;
            _filter = filter;
        }

        public void Message(string message)
        {
            Write(LogType.Log, message);
        }

        public void Warning(string message)
        {
            Write(LogType.Warning, message);
        }

        public void Error(string message)
        {
            Write(LogType.Error, message);
        }

        public void Exception(Exception exception)
        {
            Debug.LogException(exception);
        }

        public void Write(LogType type, string message)
        {
            if (!_filter.IsAllowed(_category, type))
            {
                return;
            }

            string formatted = "[" + _category.Name + "] " + message;
            switch (type)
            {
                case LogType.Warning:
                    Debug.LogWarning(formatted);
                    break;
                case LogType.Error:
                case LogType.Assert:
                    Debug.LogError(formatted);
                    break;
                default:
                    Debug.Log(formatted);
                    break;
            }
        }
    }
}
