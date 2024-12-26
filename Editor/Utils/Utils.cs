using System.IO;
using System.Text;

namespace UnityAIHelper.Editor
{
    public static class Utils
    {

        public static (string,string) GetScriptPathAndContent(string scriptName, string scriptPath)
        {
            var fullPath = GetFullPath(scriptName, scriptPath);
            if(File.Exists(fullPath))
                return (fullPath,File.ReadAllText(fullPath,Encoding.UTF8));
            return (null,null);
        }
        
        public static string GetScript(string scriptName, string scriptPath)
        {
            var (_, script) = GetScriptPathAndContent(scriptName, scriptPath);
            return script;
        }

        public static string CreateScript(string scriptName,string scriptPath,string scriptContent)
        {
            var fullPath = GetFullPath(scriptName, scriptPath);

            // Write script file
            File.WriteAllText(fullPath, scriptContent,Encoding.UTF8);
            return fullPath;
        }

        static string GetFullPath(string scriptName,string scriptPath)
        {
            scriptName=scriptName.Trim();
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
            return fullPath;
        }
    }
}