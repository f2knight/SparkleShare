//   SparkleShare, an instant update workflow to Git.
//   Copyright (C) 2010  Hylke Bons <hylkebons@gmail.com>
//
//   This program is free software: you can redistribute it and/or modify
//   it under the terms of the GNU General Public License as published by
//   the Free Software Foundation, either version 3 of the License, or
//   (at your option) any later version.
//
//   This program is distributed in the hope that it will be useful,
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//   GNU General Public License for more details.
//
//   You should have received a copy of the GNU General Public License
//   along with this program. If not, see <http://www.gnu.org/licenses/>.


using System;
using System.IO;
using System.Diagnostics;

namespace SparkleLib {

    // A helper class that fetches and configures
    // a remote repository
    public class SparkleFetcher {

        // TODO: remove 'cloning' prefix
        public delegate void CloningStartedEventHandler (object o, SparkleEventArgs args);
        public delegate void CloningFinishedEventHandler (object o, SparkleEventArgs args);
        public delegate void CloningFailedEventHandler (object o, SparkleEventArgs args);

        public event CloningStartedEventHandler CloningStarted;
        public event CloningFinishedEventHandler CloningFinished;
        public event CloningFailedEventHandler CloningFailed;

        private string TargetFolder;
        private string RemoteOriginUrl;


        public SparkleFetcher (string url, string folder)
        {
            TargetFolder = folder;
            RemoteOriginUrl = url;
        }


        // Clones the remote repository
        public void Start ()
        {
            SparkleHelpers.DebugInfo ("Git", "[" + TargetFolder + "] Cloning Repository");

            if (Directory.Exists (TargetFolder))
                Directory.Delete (TargetFolder, true);

            
            if (CloningStarted != null)
                CloningStarted (this, new SparkleEventArgs ("CloningStarted")); 

            SparkleGit git = new SparkleGit (SparklePaths.SparkleTmpPath,
                "clone \"" + RemoteOriginUrl + "\" " + "\"" + TargetFolder + "\"");

            git.Exited += delegate {
                SparkleHelpers.DebugInfo ("Git", "Exit code " + git.ExitCode.ToString ());

                if (git.ExitCode != 0) {
                    SparkleHelpers.DebugInfo ("Git", "[" + TargetFolder + "] Cloning failed");

                    if (CloningFailed != null)
                        CloningFailed (this, new SparkleEventArgs ("CloningFailed"));
                } else {
                    InstallConfiguration ();
                    InstallExcludeRules ();
                    
                    SparkleHelpers.DebugInfo ("Git", "[" + TargetFolder + "] Repository cloned");

                    if (CloningFinished != null)
                        CloningFinished (this, new SparkleEventArgs ("CloningFinished"));
                }
            };

            git.Start ();
        }


        // Install the user's name and email and some config into
        // the newly cloned repository
        private void InstallConfiguration ()
        {
            string global_config_file_path = Path.Combine (SparklePaths.SparkleConfigPath, "config");

            if (File.Exists (global_config_file_path)) {
                StreamReader reader = new StreamReader (global_config_file_path);
                string user_info = reader.ReadToEnd ();
                reader.Close ();

                string repo_config_file_path = SparkleHelpers.CombineMore (TargetFolder, ".git", "config");
                string config = String.Join ("\n", File.ReadAllLines (repo_config_file_path));

                // Be case sensitive explicitly to work on Mac
                config = config.Replace ("ignorecase = true", "ignorecase = false");

                // Ignore permission changes
                config = config.Replace ("filemode = true", "filemode = false");

                // Add user info
                config += Environment.NewLine + user_info;

                TextWriter writer = new StreamWriter (repo_config_file_path);
                writer.WriteLine (config);
                writer.Close ();

                SparkleHelpers.DebugInfo ("Config", "Added configuration to '" + repo_config_file_path + "'");
            }
        }


        // Add a .gitignore file to the repo
        private void InstallExcludeRules ()
        {
            string exlude_rules_file_path = SparkleHelpers.CombineMore (
                TargetFolder, ".git", "info", "exclude");

            TextWriter writer = new StreamWriter (exlude_rules_file_path);

                // gedit and emacs
                writer.WriteLine ("*~");

                // vi(m)
                writer.WriteLine (".*.sw[a-z]");
                writer.WriteLine ("*.un~");
                writer.WriteLine ("*.swp");
                writer.WriteLine ("*.swo");
                
                // KDE
                writer.WriteLine (".directory");
    
                // Mac OSX
                writer.WriteLine (".DS_Store");
                writer.WriteLine ("Icon?");
                writer.WriteLine ("._*");
                writer.WriteLine (".Spotlight-V100");
                writer.WriteLine (".Trashes");

                // Mac OSX
                writer.WriteLine ("*(Autosaved).graffle");
            
                // Windows
                writer.WriteLine ("Thumbs.db");
                writer.WriteLine ("Desktop.ini");

                // CVS
                writer.WriteLine ("*/CVS/*");
                writer.WriteLine (".cvsignore");
                writer.WriteLine ("*/.cvsignore");
                
                // Subversion
                writer.WriteLine ("/.svn/*");
                writer.WriteLine ("*/.svn/*");

            writer.Close ();
        }
    }
}
