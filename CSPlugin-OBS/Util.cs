using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace CSPluginOBS
{
    public static class Util
    {
        /// <summary>
        /// Checks if a file with the path, name, and extension exists. If it does, adds an index to the name
        /// so no naming conflict exists. Returns the indexed name without the folder or extension.
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="filename"></param>
        /// <param name="fileExt"></param>
        /// <returns></returns>
        public static string SafeFileName(string folder, string filename, string fileExt)
        {
            
            if (filename == "")
                return filename;
            string result = filename;
            filename = MakeValidFilename(filename);
            var fileString = filename;
            var file = new FileInfo(JoinPaths(folder, fileString) + fileExt);
            Logger.Trace($"Checking if {file} already exists");
            int index = 2;
            while (file.Exists)
            {
                Logger.Trace($"{file} exists, changing file name");
                fileString = MakeValidFilename(filename + $" ({index})");
                string newString = $"{JoinPaths(folder, fileString)}";
                file = new FileInfo(newString + fileExt);
                index++;
            }
            result = fileString;
            Logger.Trace($"Using filename {result}");
            return result;
        }

        /// <summary>
        /// Replaces any characters in the provided string that aren't valid for a file name.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string MakeValidFilename(string text)
        {
            text = text.Replace('"', '”'); // U+201D right double quotation mark
            text = text.Replace('/', '⁄');  // U+2044 fraction slash
            text = text.Replace('\'', '’'); // U+2019 right single quotation mark 
            text = text.Replace('\\', '_');
            text = text.Replace('<', '‹');
            text = text.Replace('>', '›');
            text = text.Replace('|', '‖');
            text = text.Replace(':', '⁚');
            text = text.Replace('*', '⁎');
            text = text.Replace('?', '？');
            text = text.Replace(' ', '_');

            foreach (char c in System.IO.Path.GetInvalidFileNameChars())
            {
                text = text.Replace(c, '_');
            }
            return text;
        }

        /// <summary>
        /// Joins two parts of a path together to correct for whether or not the parts end/start with "\"
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <returns>A string path from the combined parts</returns>
        public static string JoinPaths(string first, string second)
        {
            if (first == "" | second == "")
                return first + second;
            string newPath = first;
            var endsWithSlash = newPath.EndsWith(@"\");
            if (endsWithSlash)
            {
                // First part ends with "\"
                if (second.StartsWith(@"\"))
                    newPath = newPath + second.Substring(1);
                else
                    newPath = newPath + second;
            }
            else
                // First part doesn't end with "\"
                if (second.StartsWith(@"\"))
                newPath = newPath + second;
            else
                newPath = newPath + @"\" + second;
            return newPath;
        }
    }
}
