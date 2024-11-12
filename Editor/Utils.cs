using System.IO;

namespace UnityAIHelper.Editor
{
    public static class Utils
    {
        public static string CreateScript(string scriptName,string scriptPath,string scriptContent)
        {
            // Ensure script name ends with .cs
            if (!scriptName.EndsWith(".cs"))
            {
                scriptName += ".cs";
            }

            scriptPath = scriptPath.Trim('.', '/', ' ', '\\', '\'', '\"');

            string fullPath=scriptPath;
            if (!scriptPath.StartsWith("Assets/"))
            {
                fullPath = Path.Combine("Assets", scriptPath.TrimStart('/'));
            }

            if (!scriptPath.EndsWith(scriptName))
            {
                fullPath = Path.Combine(fullPath, scriptName);
            }

            string directory = Path.GetDirectoryName(fullPath);

            // Create directory if it doesn't exist
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Write script file
            File.WriteAllText(fullPath, scriptContent);

            return fullPath;
        }
    }
}