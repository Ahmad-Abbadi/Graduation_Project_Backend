namespace Graduation_Project_Backend.Extensions
{
    public static class DotEnv
    {
        public static void Load(string? path = null)
        {
            path ??= Path.Combine(Directory.GetCurrentDirectory(), ".env");
            if (!File.Exists(path))
                return;

            foreach (string rawLine in File.ReadLines(path))
            {
                string line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith('#'))
                    continue;

                int separatorIndex = line.IndexOf('=');
                if (separatorIndex <= 0)
                    continue;

                string key = line[..separatorIndex].Trim();
                string value = line[(separatorIndex + 1)..].Trim();

                if (key.Length == 0)
                    continue;

                Environment.SetEnvironmentVariable(key, Unquote(value));
            }
        }

        private static string Unquote(string value)
        {
            if (value.Length >= 2 &&
                ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
            {
                return value[1..^1];
            }

            return value;
        }
    }
}
