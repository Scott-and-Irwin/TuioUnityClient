using System;
using System.Collections.Generic;
using System.Configuration;
using Microsoft.Extensions.Logging;

namespace TuioUnity.Utils
{
    public class UnityLogger : ILogger
    {
        public static bool enabled = true;

        // The websocket clients retry every second while the table is offline, alternating their messages:
        // each distinct message is logged once until a connection is established
        static readonly object loggedLock = new object();
        static readonly HashSet<string> logged = new HashSet<string>();

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!enabled) return;
            if (IsRepeated(exception != null ? exception.GetType().FullName + exception.Message : formatter.Invoke(state, exception))) return;
            switch (logLevel)
            {
                case LogLevel.Trace:
                case LogLevel.Debug:
                case LogLevel.Information:

                    UnityEngine.Debug.Log(FormatMessage(state, exception, formatter));
                    break;

                case LogLevel.Warning:
                case LogLevel.Critical:
                    UnityEngine.Debug.LogWarning(FormatMessage(state, exception, formatter));
                    break;

                case LogLevel.Error:
                    if (exception != null)
                    {
                        UnityEngine.Debug.LogException(exception);
                    }
                    else
                    {
                        UnityEngine.Debug.LogError(FormatMessage(state, exception, formatter));
                    }
                    break;

                case LogLevel.None:
                    break;

                default:
                    break;
            }
        }

        // Without domain reload the static survives between play sessions
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetLogged()
        {
            lock (loggedLock) logged.Clear();
        }

        // Called from the client threads too
        static bool IsRepeated(string message)
        {
            lock (loggedLock)
            {
                // once connected, a later disconnection is reported again
                if (message.Contains("] Connected to"))
                {
                    logged.Clear();
                    return false;
                }
                return !logged.Add(message);
            }
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None;
        }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }

        private object FormatMessage<TState>(TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var message = formatter.Invoke(state, exception);
            return $"{message}";
        }
    }
}