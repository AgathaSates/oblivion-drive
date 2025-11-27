using System.Globalization;

namespace OblivionDrive.Application.Shared;
public static class NameFormatter
{
    private static readonly CultureInfo DefaultCulture = new("pt-BR");

    private static readonly HashSet<string> LowercaseParticles =
    [
        "da", "de", "do", "das", "dos", "e"
    ];

    public static string FormatName(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
            return string.Empty;

        string[] words = rawName
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(word => word.ToLower(DefaultCulture))
            .ToArray();

        for (int index = 0; index < words.Length; index++)
        {
            string currentWord = words[index];

            if (index > 0 && LowercaseParticles.Contains(currentWord))
            {
                words[index] = currentWord;
                continue;
            }

            char firstChar = char.ToUpper(currentWord[0], DefaultCulture);
            string remainingText = currentWord.Length > 1
                ? currentWord[1..]
                : string.Empty;

            words[index] = firstChar + remainingText;
        }

        return string.Join(' ', words);
    }
}