using System.Collections.Generic;

public static class PlayerNames
{
    private static readonly string[] Adjectives =
    {
        "Happy", "Brave", "Calm", "Lucky", "Swift", "Sunny", "Jolly", "Clever",
        "Bouncy", "Cozy", "Daring", "Fizzy", "Gentle", "Mighty", "Nimble", "Peppy",
        "Quick", "Rosy", "Sleepy", "Snappy", "Sparky", "Witty", "Zesty", "Breezy",
        "Cheery", "Fuzzy", "Plucky", "Zippy", "Chill", "Bold", "Merry", "Shiny"
    };

    private static readonly string[] Animals =
    {
        "Penguin", "Otter", "Koala", "Frog", "Panda", "Fox", "Owl", "Seal",
        "Bunny", "Tiger", "Kitten", "Puppy", "Hedgehog", "Badger", "Dolphin", "Duck",
        "Goose", "Lemur", "Llama", "Moose", "Narwhal", "Octopus", "Parrot", "Puffin",
        "Raccoon", "Sloth", "Turtle", "Walrus", "Whale", "Yak", "Zebra", "Hamster"
    };

    public static IReadOnlyList<string> AdjectiveList => Adjectives;
    public static IReadOnlyList<string> AnimalList => Animals;
    public static int Combinations => Adjectives.Length * Animals.Length;

    public static string ForId(string playerId)
    {
        var random = new MatchRandom(unchecked((int)Hash(playerId ?? string.Empty)));
        string adjective = Adjectives[random.NextUInt() % (uint)Adjectives.Length];
        string animal = Animals[random.NextUInt() % (uint)Animals.Length];
        return $"{adjective} {animal}";
    }

    public static string ProfileFor(string path)
    {
        return $"p{Hash(path ?? string.Empty):x8}";
    }

    public static uint Hash(string text)
    {
        unchecked
        {
            uint hash = 2166136261u;
            foreach (char c in text)
            {
                hash ^= c;
                hash *= 16777619u;
            }
            return hash;
        }
    }
}
