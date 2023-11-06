namespace UntrackedTorrents;

public static class StringHelper
{
    public static string PromptString(this string question, string? description = null, string? defaultValue = null)
    {
        string? response;
        do
        {
            Console.Write($"{question}{(string.IsNullOrEmpty(description) ? "" : $" ({description})")}{(defaultValue == null ? "" : $" [default {defaultValue}]")}: ");
            response = InputOrDefault();
        } while (string.IsNullOrWhiteSpace(response) && response != defaultValue);

        return response;

        string? InputOrDefault()
        {
            var input = Console.ReadLine();
            return string.IsNullOrEmpty(input) && defaultValue != null ? defaultValue : input;
        }
    }
}
