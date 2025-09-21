namespace SharedMessaging.BuildingBlocks.Messaging.RabbitMQ;

internal static class RoutingKeyConventions
{
    public static string For<T>() => ToKebab(typeof(T).Name).Replace('-', '.');

    private static string ToKebab(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        var chars = new List<char>(s.Length * 2);
        for (int i = 0; i < s.Length; i++)
        {
            var ch = s[i];
            if (char.IsUpper(ch) && i > 0) chars.Add('-');
            chars.Add(char.ToLowerInvariant(ch));
        }
        return new string(chars.ToArray());
    }
}
