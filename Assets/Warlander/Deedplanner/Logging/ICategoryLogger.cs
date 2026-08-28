using System;
using UnityEngine;

namespace Warlander.Deedplanner.Logging
{
    public interface ICategoryLogger
    {
        void Message(string message);

        void Warning(string message);

        void Error(string message);

        void Exception(Exception exception);

        void Write(LogType type, string message);
    }
}
