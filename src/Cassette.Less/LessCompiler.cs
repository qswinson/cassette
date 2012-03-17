﻿using System;
using System.Collections.Generic;
using Cassette.IO;
using Cassette.Utilities;
using dotless.Core;
using dotless.Core.Importers;
using dotless.Core.Input;
using dotless.Core.Loggers;
using dotless.Core.Parser;

namespace Cassette.Stylesheets
{
    public class LessCompiler : ICompiler
    {
        List<string> importedFilePaths;

        public CompileResult Compile(string source, CompileContext context)
        {
            var sourceFile = context.RootDirectory.GetFile(context.SourceFilePath);
            var parser = new Parser
            {
                Importer = new Importer(new CassetteLessFileReader(sourceFile.Directory))
            };
            var errorLogger = new ErrorLogger();
            var engine = new LessEngine(parser, errorLogger, false);
            
            importedFilePaths = new List<string>();
            // TODO: Ensure `@import`ed .less files get add to importedFilePaths

            var css = engine.TransformToCss(source, sourceFile.FullPath);

            if (errorLogger.HasErrors)
            {
                throw new LessCompileException(errorLogger.ErrorMessage);
            }
            else
            {
                return new CompileResult(css, importedFilePaths);
            }
        }

        class ErrorLogger : ILogger
        {
            readonly List<string> errors;

            public ErrorLogger()
            {
                errors = new List<string>();
            }

            public bool HasErrors
            {
                get { return errors.Count > 0; }
            }

            public string ErrorMessage
            {
                get { return string.Join(Environment.NewLine, errors.ToArray()).Trim(); }
            }

            public void Log(LogLevel level, string message)
            {
                switch (level)
                {
                    case LogLevel.Info: Info(message); break;
                    case LogLevel.Debug: Debug(message); break;
                    case LogLevel.Warn: Warn(message); break;
                    case LogLevel.Error: Error(message); break;
                }
            }

            public void Error(string message)
            {
                errors.Add(message);
                Trace.Source.TraceInformation(message);
            }

            public void Info(string message)
            {
                Trace.Source.TraceInformation(message);
            }

            public void Debug(string message)
            {
                Trace.Source.TraceInformation(message);
            }

            public void Warn(string message)
            {
                Trace.Source.TraceInformation(message);
            }
        }

        class CassetteLessFileReader : IFileReader
        {
            readonly IDirectory directory;

            public CassetteLessFileReader(IDirectory directory)
            {
                this.directory = directory;
            }

            public string GetFileContents(string fileName)
            {
                return directory.GetFile(fileName).OpenRead().ReadToEnd();
            }

            public bool DoesFileExist(string fileName)
            {
                return directory.GetFile(fileName).Exists;
            }
        }
    }
}