using UnityEngine;

/// <summary>
/// ScriptableObject that generates random athlete identities (name + nationality).
/// Assign one instance to RaceManager. Expand the name/nationality lists here to
/// grow the pool without touching any other script.
///
/// Future extensions:
///   - Career mode: draw from a saved athlete pool instead of random generation.
///   - Seeded competitions: call Random.InitState(seed) before generating.
///   - Larger databases: replace the arrays with TextAsset CSV/JSON files.
/// </summary>
[CreateAssetMenu(fileName = "AthleteIdentityGenerator", menuName = "Athletics/Athlete Identity Generator")]
public class AthleteIdentityGenerator : ScriptableObject
{
    [Header("Name Pool")]
    [SerializeField]
    private string[] firstNames = new string[]
    {
        "Marcus", "James", "Andre", "Yohan", "Usain", "Noah", "Christian", "Adam",
        "Trayvon", "Ferdinand", "Akani", "Ronnie", "Justin", "Asafa", "Tyson",
        "Oblique", "Jereem", "Michael", "Steve", "Ato", "Daniel", "Marcell",
        "Frederick", "Letsile", "Erriyon", "Kenny", "Zharnel", "Reece"
    };

    [SerializeField]
    private string[] lastNames = new string[]
    {
        "Thompson", "Blake", "De Grasse", "Blake", "Bolt", "Lyles", "Coleman",
        "Gemili", "Bromell", "Omanyala", "Simbine", "Baker", "Gatlin", "Powell",
        "Gay", "Seville", "Richards", "Johnson", "Lewis", "Boldon", "Rudisha",
        "Jacobs", "Kerley", "Tebogo", "Knighton", "Bednarek", "Hughes", "Walsh"
    };

    [Header("Nationality Pool")]
    [SerializeField]
    private string[] nationalities = new string[]
    {
        "USA", "JAM", "CAN", "GBR", "RSA", "NGR", "TTO", "FRA",
        "GER", "ITA", "BRA", "AUS", "JPN", "CHN", "BOT", "GHA",
        "BAH", "CUB", "PAN", "DOM", "UGA", "KEN", "ETH", "ZIM"
    };

    /// <summary>
    /// Generates a unique identity from the pool.
    /// Each call independently randomizes name and nationality.
    /// </summary>
    public AthleteIdentity Generate()
    {
        string first = firstNames.Length > 0
            ? firstNames[Random.Range(0, firstNames.Length)]
            : "Athlete";

        string last = lastNames.Length > 0
            ? lastNames[Random.Range(0, lastNames.Length)]
            : string.Empty;

        string nation = nationalities.Length > 0
            ? nationalities[Random.Range(0, nationalities.Length)]
            : "UNK";

        return new AthleteIdentity($"{first} {last}", nation);
    }
}

/// <summary>
/// Plain data container for a generated athlete identity.
/// </summary>
public readonly struct AthleteIdentity
{
    public readonly string Name;
    public readonly string Nationality;

    public AthleteIdentity(string name, string nationality)
    {
        Name = name;
        Nationality = nationality;
    }
}
