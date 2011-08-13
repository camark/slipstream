﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;

using log4net;
using log4net.Appender;
using log4net.Layout;
using log4net.Core;

namespace ObjectServer
{
    public sealed class LoggerProvider
    {
        public const string PlatformLoggerName = "platform";
        public const string BizLoggerName = "biz";
        public const string GatewayLoggerName = "gateway";

        public static readonly Dictionary<string, Level> LogLevelMapping =
            new Dictionary<string, Level>()
            {
                { "all", Level.All },
                { "debug", Level.Debug },
                { "info", Level.Info },
                { "warn", Level.Warn },
                { "error", Level.Error },
                { "fatal", Level.Fatal },
            };

        private readonly static LoggerProvider s_instance = new LoggerProvider();
        private readonly Log4netLogger platformLogger;
        private readonly Log4netLogger bizLogger;
        private readonly Log4netLogger gatewayLogger;

        public LoggerProvider()
        {
            var platformLog = LogManager.GetLogger(PlatformLoggerName);
            this.platformLogger = new Log4netLogger(platformLog);

            var bizLog = LogManager.GetLogger(BizLoggerName);
            this.bizLogger = new Log4netLogger(bizLog);

            var gatewayLog = LogManager.GetLogger(GatewayLoggerName);
            this.gatewayLogger = new Log4netLogger(gatewayLog);
        }

        public static ObjectServer.ILogger PlatformLogger
        {
            get
            {
                return s_instance.platformLogger;
            }
        }

        public static ObjectServer.ILogger BizLogger
        {
            get
            {
                return s_instance.bizLogger;
            }
        }

        public static ObjectServer.ILogger GatewayLogger
        {
            get
            {
                return s_instance.gatewayLogger;
            }
        }

        public static void Configurate(Config cfg)
        {
            if (cfg == null)
            {
                throw new ArgumentNullException("cfg");
            }

            var layout = new PatternLayout(StaticSettings.LogPattern);

            CreateFileAppenders(cfg, layout);

            var hierarchy = (log4net.Repository.Hierarchy.Hierarchy)LogManager.GetRepository();

            if (cfg.Debug)
            {
                var traceAppender = CreateTraceAppender(layout);
                hierarchy.Root.AddAppender(traceAppender);
            }

            var rootLevel = LogLevelMapping[cfg.LogLevel.ToLowerInvariant()];
            hierarchy.Root.Level = rootLevel;

            hierarchy.Configured = true;
        }

        private static void CreateFileAppenders(Config cfg, PatternLayout layout)
        {
            //创建文件 Appenders
            if (!string.IsNullOrEmpty(cfg.LogPath))
            {
                var logDir = Environment.ExpandEnvironmentVariables(cfg.LogPath);

                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }

                //创建平台 appender
                var platformLogFilePath = Path.Combine(logDir, StaticSettings.PlatformLogFileName);
                var platformFileAppender = CreateRollingFileAppender(layout, platformLogFilePath);
                AddAppender(PlatformLoggerName, platformFileAppender);

                //创建网关 appender
                var gatewayLogFilePath = Path.Combine(logDir, StaticSettings.GatewayLogFileName);
                var gatewayFileAppender = CreateRollingFileAppender(layout, gatewayLogFilePath);
                AddAppender(GatewayLoggerName, gatewayFileAppender);

                //创建业务 appender
                var bizLogFilePath = Path.Combine(logDir, StaticSettings.BizLogFileName);
                var bizFileAppender = CreateRollingFileAppender(layout, bizLogFilePath);
                AddAppender(BizLoggerName, bizFileAppender);
            }
        }

        private static void AddAppender(string loggerName, IAppender appender)
        {
            System.Diagnostics.Debug.Assert(!string.IsNullOrEmpty(loggerName));
            System.Diagnostics.Debug.Assert(appender != null);

            var log = LogManager.GetLogger(loggerName);
            var l = (log4net.Repository.Hierarchy.Logger)log.Logger;
            l.AddAppender(appender);
        }

        private static void SetLevel(string loggerName, string levelName)
        {
            System.Diagnostics.Debug.Assert(!string.IsNullOrEmpty(loggerName));
            System.Diagnostics.Debug.Assert(!string.IsNullOrEmpty(levelName));

            //TODO 检查 levelName

            var log = LogManager.GetLogger(loggerName);
            var l = (log4net.Repository.Hierarchy.Logger)log.Logger;

            l.Level = l.Hierarchy.LevelMap[levelName];
        }

        private static IAppender CreateRollingFileAppender(PatternLayout layout, string logPath)
        {
            var fileAppender = new log4net.Appender.RollingFileAppender()
            {
                File = logPath,
                AppendToFile = true,
                RollingStyle = log4net.Appender.RollingFileAppender.RollingMode.Size,
                Layout = layout,
                Encoding = Encoding.UTF8,
                StaticLogFileName = true,
            };
            fileAppender.ActivateOptions();
            return fileAppender;
        }

        private static IAppender CreateTraceAppender(PatternLayout layout)
        {
            var ta = new TraceAppender()
            {
                Layout = layout,
            };
            ta.ActivateOptions();
            return ta;
        }

        private static IAppender CreateColoredConsoleAppender(PatternLayout layout)
        {
            var cca = new ColoredConsoleAppender()
            {
                Layout = layout,
            };

            var errorColorMapping = new ColoredConsoleAppender.LevelColors()
            {
                Level = Level.Error,
                ForeColor = ColoredConsoleAppender.Colors.Red,
            };

            var debugColorMapping = new ColoredConsoleAppender.LevelColors()
            {
                Level = Level.Debug,
                ForeColor = ColoredConsoleAppender.Colors.White,
            };

            var fatalColorMapping = new ColoredConsoleAppender.LevelColors()
            {
                Level = Level.Fatal,
                ForeColor = ColoredConsoleAppender.Colors.Red,
            };

            var warnColorMapping = new ColoredConsoleAppender.LevelColors()
            {
                Level = Level.Fatal,
                ForeColor = ColoredConsoleAppender.Colors.Yellow,
            };

            cca.AddMapping(errorColorMapping);
            cca.AddMapping(fatalColorMapping);
            cca.AddMapping(debugColorMapping);
            cca.AddMapping(warnColorMapping);

            cca.ActivateOptions();
            return cca;
        }
    }
}
