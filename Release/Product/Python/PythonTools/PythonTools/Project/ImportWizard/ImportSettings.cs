﻿/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation. 
 *
 * This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
 * copy of the license can be found in the License.html file at the root of this distribution. If 
 * you cannot locate the Apache License, Version 2.0, please send an email to 
 * vspython@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
 * by the terms of the Apache License, Version 2.0.
 *
 * You must not remove this notice, or any other, from this software.
 *
 * ***************************************************************************/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using Microsoft.PythonTools.Interpreter;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Language.Intellisense;

namespace Microsoft.PythonTools.Project.ImportWizard {
    internal class ImportSettings : DependencyObject {
        public ImportSettings() {
            var componentService = PythonToolsPackage.GetGlobalService(typeof(SComponentModel)) as IComponentModel;
            if (componentService != null) {
                AvailableInterpreters = new ObservableCollection<PythonInterpreterView>(
                    Enumerable.Repeat(new PythonInterpreterView("(Use my default)", Guid.Empty, new Version(), null), 1)
                    .Concat(
                        componentService.GetAllPythonInterpreterFactories()
                            .Select(fact => new PythonInterpreterView(fact))
                    )
                );
            } else {
                AvailableInterpreters = new ObservableCollection<PythonInterpreterView>();
                AvailableInterpreters.Add(new PythonInterpreterView("(Use my default)", Guid.Empty, new Version(), null));
            }

            SelectedInterpreter = AvailableInterpreters[0];
            TopLevelPythonFiles = new BulkObservableCollection<string>();

            Filters = "*.pyw;*.txt;*.htm;*.html;*.css;*.djt;*.js;*.png;*.jpg;*.gif;*.bmp;*.ico;*.svg";
        }

        public string ProjectPath {
            get { return (string)GetValue(ProjectPathProperty); }
            set { SetValue(ProjectPathProperty, value); }
        }

        public string SourcePath {
            get { return (string)GetValue(SourcePathProperty); }
            set { SetValue(SourcePathProperty, value); }
        }

        public string Filters {
            get { return (string)GetValue(FiltersProperty); }
            set { SetValue(FiltersProperty, value); }
        }

        public string SearchPaths {
            get { return (string)GetValue(SearchPathsProperty); }
            set { SetValue(SearchPathsProperty, value); }
        }

        public ObservableCollection<PythonInterpreterView> AvailableInterpreters {
            get { return (ObservableCollection<PythonInterpreterView>)GetValue(AvailableInterpretersProperty); }
            set { SetValue(AvailableInterpretersPropertyKey, value); }
        }

        public PythonInterpreterView SelectedInterpreter {
            get { return (PythonInterpreterView)GetValue(SelectedInterpreterProperty); }
            set { SetValue(SelectedInterpreterProperty, value); }
        }

        public ObservableCollection<string> TopLevelPythonFiles {
            get { return (ObservableCollection<string>)GetValue(TopLevelPythonFilesProperty); }
            private set { SetValue(TopLevelPythonFilesPropertyKey, value); }
        }

        public string StartupFile {
            get { return (string)GetValue(StartupFileProperty); }
            set { SetValue(StartupFileProperty, value); }
        }

        public bool SupportDjango {
            get { return (bool)GetValue(SupportDjangoProperty); }
            set { SetValue(SupportDjangoProperty, value); }
        }

        public static readonly DependencyProperty ProjectPathProperty = DependencyProperty.Register("ProjectPath", typeof(string), typeof(ImportSettings), new PropertyMetadata(RecalculateIsValid));
        public static readonly DependencyProperty SourcePathProperty = DependencyProperty.Register("SourcePath", typeof(string), typeof(ImportSettings), new PropertyMetadata(SourcePath_Updated));
        public static readonly DependencyProperty FiltersProperty = DependencyProperty.Register("Filters", typeof(string), typeof(ImportSettings), new PropertyMetadata());
        public static readonly DependencyProperty SearchPathsProperty = DependencyProperty.Register("SearchPaths", typeof(string), typeof(ImportSettings), new PropertyMetadata(RecalculateIsValid));
        private static readonly DependencyPropertyKey AvailableInterpretersPropertyKey = DependencyProperty.RegisterReadOnly("AvailableInterpreters", typeof(ObservableCollection<PythonInterpreterView>), typeof(ImportSettings), new PropertyMetadata());
        public static readonly DependencyProperty AvailableInterpretersProperty = AvailableInterpretersPropertyKey.DependencyProperty;
        public static readonly DependencyProperty SelectedInterpreterProperty = DependencyProperty.Register("SelectedInterpreter", typeof(PythonInterpreterView), typeof(ImportSettings), new PropertyMetadata(RecalculateIsValid));
        private static readonly DependencyPropertyKey TopLevelPythonFilesPropertyKey = DependencyProperty.RegisterReadOnly("TopLevelPythonFiles", typeof(ObservableCollection<string>), typeof(ImportSettings), new PropertyMetadata());
        public static readonly DependencyProperty TopLevelPythonFilesProperty = TopLevelPythonFilesPropertyKey.DependencyProperty;
        public static readonly DependencyProperty StartupFileProperty = DependencyProperty.Register("StartupFile", typeof(string), typeof(ImportSettings), new PropertyMetadata());
        public static readonly DependencyProperty SupportDjangoProperty = DependencyProperty.Register("SupportDjango", typeof(bool), typeof(ImportSettings), new PropertyMetadata(false));

        public bool IsValid {
            get { return (bool)GetValue(IsValidProperty); }
            private set { SetValue(IsValidPropertyKey, value); }
        }

        private static bool IsSafePath(string name) {
            return !string.IsNullOrEmpty(name) &&
                name.IndexOfAny(Path.GetInvalidPathChars()) == -1;
        }

        private static void RecalculateIsValid(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (!d.Dispatcher.CheckAccess()) {
                d.Dispatcher.Invoke((Action)(() => RecalculateIsValid(d, e)));
                return;
            }

            var s = d as ImportSettings;
            if (s == null) {
                d.SetValue(IsValidPropertyKey, false);
                return;
            }
            d.SetValue(IsValidPropertyKey,
                IsSafePath(s.SourcePath) &&
                IsSafePath(s.ProjectPath) &&
                Directory.Exists(s.SourcePath) &&
                s.SelectedInterpreter != null &&
                s.AvailableInterpreters.Contains(s.SelectedInterpreter)
            );
        }

        private static void SourcePath_Updated(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (!d.Dispatcher.CheckAccess()) {
                d.Dispatcher.BeginInvoke((Action)(() => SourcePath_Updated(d, e)));
                return;
            }

            RecalculateIsValid(d, e);

            var s = d as ImportSettings;
            if (s == null) {
                return;
            }

            if (string.IsNullOrEmpty(s.ProjectPath)) {
                s.ProjectPath = Path.Combine(s.SourcePath, Path.GetFileName(s.SourcePath) + ".pyproj");
            }

            var sourcePath = s.SourcePath;
            if (IsSafePath(sourcePath) && Directory.Exists(sourcePath)) {
                var filters = s.Filters;
                var dispatcher = s.Dispatcher;
                
                // Nice async machinery does not work correctly in unit-tests,
                // so using Dispatcher directly.
                Task.Factory.StartNew(() => {
                    var files = Directory.EnumerateFiles(sourcePath, "*.py", SearchOption.TopDirectoryOnly);
                    // Also include *.pyw files if they were in the filter list
                    foreach (var pywFilters in filters.Split(';').Where(filter => filter.TrimEnd().EndsWith(".pyw", StringComparison.OrdinalIgnoreCase))) {
                        files = files.Concat(Directory.EnumerateFiles(sourcePath, pywFilters, SearchOption.TopDirectoryOnly));
                    }
                    var fileList = files.Select(f => Path.GetFileName(f)).ToList();
                    dispatcher.BeginInvoke((Action)(() => {
                        var tlpf = s.TopLevelPythonFiles as BulkObservableCollection<string>;
                        if (tlpf != null) {
                            tlpf.Clear();
                            tlpf.AddRange(fileList);
                        } else {
                            s.TopLevelPythonFiles.Clear();
                            foreach (var file in fileList) {
                                s.TopLevelPythonFiles.Add(file);
                            }
                        }
                    }));
                });
            } else {
                s.TopLevelPythonFiles.Clear();
            }
        }

        private static readonly DependencyPropertyKey IsValidPropertyKey = DependencyProperty.RegisterReadOnly("IsValid", typeof(bool), typeof(ImportSettings), new PropertyMetadata(false));
        public static readonly DependencyProperty IsValidProperty = IsValidPropertyKey.DependencyProperty;


        private static XmlWriter GetDefaultWriter(string projectPath) {
            var settings = new XmlWriterSettings {
                CloseOutput = true,
                Encoding = Encoding.UTF8,
                Indent = true,
                IndentChars = "    ",
                NewLineChars = Environment.NewLine,
                NewLineOnAttributes = false
            };

            var dir = Path.GetDirectoryName(projectPath);
            if (!Directory.Exists(dir)) {
                Directory.CreateDirectory(dir);
            }

            return XmlWriter.Create(projectPath, settings);
        }

        public string CreateRequestedProject() {
            var task = CreateRequestedProjectAsync();
            task.Wait();
            return task.Result;
        }

        public Task<string> CreateRequestedProjectAsync() {
            string projectPath = ProjectPath;
            string sourcePath = SourcePath;
            string filters = Filters;
            string searchPaths = string.Join(";", (SearchPaths ?? "").Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(p => CommonUtils.GetRelativeDirectoryPath(SourcePath, p)));
            string startupFile = StartupFile;
            PythonInterpreterView selectedInterpreter = SelectedInterpreter;
            bool supportDjango = SupportDjango;
            return Task.Factory.StartNew<string>(() => {
                bool success = false;
                try {
                    using (var writer = GetDefaultWriter(projectPath)) {
                        WriteProjectXml(writer, projectPath, sourcePath, filters, searchPaths, startupFile, selectedInterpreter, supportDjango);
                    }
                    success = true;
                    return projectPath;
                } finally {
                    if (!success) {
                        try {
                            File.Delete(projectPath);
                        } catch {
                            // Try and avoid leaving stray files, but it does
                            // not matter much if we do.
                        }
                    }
                }
            });
        }

        internal static void WriteProjectXml(
            XmlWriter writer,
            string projectPath,
            string sourcePath,
            string filters,
            string searchPaths,
            string startupFile,
            PythonInterpreterView selectedInterpreter,
            bool supportDjango) {

            var projectHome = CommonUtils.GetRelativeDirectoryPath(Path.GetDirectoryName(projectPath), sourcePath);

            writer.WriteStartDocument();
            writer.WriteStartElement("Project", "http://schemas.microsoft.com/developer/msbuild/2003");
            writer.WriteAttributeString("DefaultTargets", "Build");

            writer.WriteStartElement("PropertyGroup");

            writer.WriteStartElement("Configuration");
            writer.WriteAttributeString("Condition", " '$(Configuration)' == '' ");
            writer.WriteString("Debug");
            writer.WriteEndElement();

            writer.WriteElementString("SchemaVersion", "2.0");
            writer.WriteElementString("ProjectGuid", Guid.NewGuid().ToString("B"));
            writer.WriteElementString("ProjectHome", projectHome);
            if (IsSafePath(startupFile)) {
                writer.WriteElementString("StartupFile", startupFile);
            } else if (supportDjango) {
                writer.WriteElementString("StartupFile", "manage.py");
            } else {
                writer.WriteElementString("StartupFile", "");
            }
            writer.WriteElementString("SearchPath", searchPaths);
            writer.WriteElementString("WorkingDirectory", ".");
            writer.WriteElementString("OutputPath", ".");
            if (supportDjango) {
                writer.WriteElementString("ProjectTypeGuids", "{5F0BE9CA-D677-4A4D-8806-6076C0FAAD37};{349c5851-65df-11da-9384-00065b846f21};{888888a0-9f3d-457c-b088-3a5042f75d52}");
                writer.WriteElementString("LaunchProvider", "Django launcher");
            }
            if (selectedInterpreter != null && selectedInterpreter.Id != Guid.Empty) {
                writer.WriteElementString("InterpreterId", selectedInterpreter.Id.ToString("B"));
                writer.WriteElementString("InterpreterVersion", selectedInterpreter.Version.ToString());
            }

            writer.WriteStartElement("VisualStudioVersion");
            writer.WriteAttributeString("Condition", "'$(VisualStudioVersion)' == ''");
            writer.WriteString("10.0");
            writer.WriteEndElement();

            writer.WriteStartElement("VSToolsPath");
            writer.WriteAttributeString("Condition", "'$(VSToolsPath)' == ''");
            writer.WriteString(@"$(MSBuildExtensionsPath32)\Microsoft\VisualStudio\v$(VisualStudioVersion)");
            writer.WriteEndElement();

            writer.WriteEndElement(); // </PropertyGroup>

            // VS requires property groups with conditions for Debug
            // and Release configurations or many COMExceptions are
            // thrown.
            writer.WriteStartElement("PropertyGroup");
            writer.WriteAttributeString("Condition", "'$(Configuration)' == 'Debug'");
            writer.WriteEndElement();
            writer.WriteStartElement("PropertyGroup");
            writer.WriteAttributeString("Condition", "'$(Configuration)' == 'Release'");
            writer.WriteEndElement();

            var folders = new HashSet<string>();
            writer.WriteStartElement("ItemGroup");
            foreach (var file in EnumerateAllFiles(sourcePath, filters)) {
                var ext = Path.GetExtension(file);
                if (PythonConstants.FileExtension.Equals(ext, StringComparison.OrdinalIgnoreCase) ||
                    PythonConstants.WindowsFileExtension.Equals(ext, StringComparison.OrdinalIgnoreCase)) {
                    writer.WriteStartElement("Compile");
                } else {
                    writer.WriteStartElement("Content");
                }
                folders.Add(Path.GetDirectoryName(file));
                writer.WriteAttributeString("Include", file);
                writer.WriteEndElement();
            }
            writer.WriteEndElement();

            writer.WriteStartElement("ItemGroup");
            foreach (var folder in folders.Where(s => !string.IsNullOrWhiteSpace(s)).OrderBy(s => s)) {
                writer.WriteStartElement("Folder");
                writer.WriteAttributeString("Include", folder);
                writer.WriteEndElement();
            }
            writer.WriteEndElement();

            if (supportDjango) {
                writer.WriteStartElement("ItemGroup");

                writer.WriteStartElement("WebPiReference");
                writer.WriteAttributeString("Include", "https://www.microsoft.com/web/webpi/3.0/toolsproductlist.xml%3fDjango");

                writer.WriteElementString("Feed", "https://www.microsoft.com/web/webpi/3.0/toolsproductlist.xml");
                writer.WriteElementString("ProductId", "Django");
                writer.WriteElementString("FriendlyName", "Django 1.4");

                writer.WriteEndElement(); // </WebPiReference>

                writer.WriteEndElement(); // </ItemGroup>
            }

            writer.WriteStartElement("Import");
            if (supportDjango) {
                writer.WriteAttributeString("Project", @"$(VSToolsPath)\Python Tools\Microsoft.PythonTools.Django.targets");
            } else {
                writer.WriteAttributeString("Project", @"$(MSBuildToolsPath)\Microsoft.Common.targets");
            }
            writer.WriteEndElement();

            writer.WriteEndElement(); // </Project>

            writer.WriteEndDocument();
        }

        private static IEnumerable<string> EnumerateAllFiles(string source, string filters) {
            var files = Directory.EnumerateFiles(source, "*.py", SearchOption.AllDirectories);
            foreach (var pattern in filters.Split(';')) {
                try {
                    var theseFiles = Directory.EnumerateFiles(source, pattern.Trim(), SearchOption.AllDirectories);
                    files = files.Concat(theseFiles);
                } catch (ArgumentException) {
                    // Probably an invalid pattern.
                }
            }
            return files
                .Where(path => path.StartsWith(source))
                .Select(path => path.Substring(source.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
                .Distinct(StringComparer.OrdinalIgnoreCase);
        }

    }
}
