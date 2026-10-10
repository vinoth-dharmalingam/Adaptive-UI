namespace MoodTune;

public sealed record Song(
    string Id,
    string Title,
    string Artist,
    string[] Moods,
    string Genre,
    int Energy,
    string Color);

public sealed class SongCatalog
{
    public IReadOnlyList<Song> Songs { get; } =
    [
        new("golden-hour", "Golden Hour", "JVKE", ["joyful", "romantic", "hopeful"], "Pop", 7, "#f6ad55"),
        new("good-days", "Good Days", "SZA", ["blue", "hopeful", "reflective"], "R&B", 4, "#8b8fd8"),
        new("weightless", "Weightless", "Arlo Parks", ["anxious", "calm", "blue"], "Indie", 2, "#80bca8"),
        new("sunflower", "Sunflower", "Rex Orange County", ["joyful", "romantic", "hopeful"], "Indie", 6, "#f4a261"),
        new("experience", "Experience", "Ludovico Einaudi", ["focused", "reflective", "calm"], "Classical", 3, "#7399c6"),
        new("levitating", "Levitating", "Dua Lipa", ["joyful", "restless", "confident"], "Pop", 9, "#ea77a5"),
        new("holocene", "Holocene", "Bon Iver", ["blue", "reflective", "calm"], "Indie", 3, "#7296a5"),
        new("espresso", "Espresso", "Sabrina Carpenter", ["joyful", "confident", "restless"], "Pop", 8, "#ee9b68"),
        new("coffee", "Coffee", "beabadoobee", ["romantic", "blue", "calm"], "Indie", 3, "#bd907b"),
        new("intro", "Intro", "The xx", ["focused", "reflective", "calm"], "Electronic", 3, "#7d9aab"),
        new("dog-days", "Dog Days Are Over", "Florence + The Machine", ["hopeful", "restless", "joyful"], "Indie", 9, "#e58b69"),
        new("pink-blue", "Pink + White", "Frank Ocean", ["romantic", "reflective", "calm"], "R&B", 4, "#d48fa5"),
        new("clair-de-lune", "Clair de Lune", "Claude Debussy", ["calm", "focused", "reflective"], "Classical", 2, "#8499c6"),
        new("electric-feel", "Electric Feel", "MGMT", ["joyful", "restless", "confident"], "Electronic", 8, "#b07ccc"),
        new("rivers-and-roads", "Rivers and Roads", "The Head and the Heart", ["blue", "hopeful", "reflective"], "Folk", 5, "#83a98a"),
        new("telepatia", "telepatía", "Kali Uchis", ["romantic", "calm", "confident"], "R&B", 5, "#d990b3"),
        new("time", "Time", "Hans Zimmer", ["focused", "hopeful", "reflective"], "Soundtrack", 5, "#728da5"),
        new("dont-stop-me-now", "Don't Stop Me Now", "Queen", ["joyful", "confident", "restless"], "Rock", 10, "#e89b62"),
        new("bloom", "Bloom", "The Paper Kites", ["romantic", "calm", "hopeful"], "Folk", 3, "#89ad89"),
        new("night-changes", "Night Changes", "One Direction", ["blue", "romantic", "reflective"], "Pop", 4, "#8c91b7"),
        new("sunset-lover", "Sunset Lover", "Petit Biscuit", ["calm", "focused", "hopeful"], "Electronic", 5, "#ef9877"),
        new("vienna", "Vienna", "Billy Joel", ["anxious", "hopeful", "reflective"], "Rock", 4, "#d8ac72"),
        new("ocean-eyes", "Ocean Eyes", "Billie Eilish", ["blue", "calm", "romantic"], "Pop", 3, "#80a6bd"),
        new("innerbloom", "Innerbloom", "RÜFÜS DU SOL", ["restless", "focused", "hopeful"], "Electronic", 7, "#c480a4"),
        new("put-your-records-on", "Put Your Records On", "Corinne Bailey Rae", ["joyful", "calm", "hopeful"], "R&B", 5, "#dbad71"),
        new("dreams", "Dreams", "Fleetwood Mac", ["hopeful", "reflective", "romantic"], "Rock", 6, "#91a77d"),
        new("gymnopedie", "Gymnopédie No. 1", "Erik Satie", ["anxious", "calm", "focused"], "Classical", 1, "#8794ad"),
        new("mystery-of-love", "Mystery of Love", "Sufjan Stevens", ["romantic", "blue", "hopeful"], "Folk", 3, "#b08fa8"),
        new("unwritten", "Unwritten", "Natasha Bedingfield", ["hopeful", "joyful", "confident"], "Pop", 8, "#e9ae67"),
        new("the-less-i-know", "The Less I Know the Better", "Tame Impala", ["restless", "blue", "confident"], "Indie", 7, "#df826d")
    ];
}
