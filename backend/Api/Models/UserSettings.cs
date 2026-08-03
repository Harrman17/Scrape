namespace AmazonScraper.Api.Models;

/// <summary>
/// Stores user-specific scraping and selling preferences.
/// These are default settings applied when a user imports/scrapes products.
/// </summary>
public class UserSettings
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public int Qty { get; set; } = 1;
    public decimal ProfitMarkup { get; set; } = 0m; // In percentage (e.g., 20 = 20%)
    public decimal? BlockProductsUnder { get; set; } // Block products under this Amazon price
    public string? ItemLocationPostcode { get; set; }
    public string? ItemLocationCity { get; set; }
    public bool AutoRemoveBrand { get; set; } = false;
    public string? Blocklist { get; set; } // Comma-separated keywords to block products
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>
    /// Default blocklist keywords that apply to all users.
    /// These keywords will block products if found in title, description, or features.
    /// </summary>
    public static readonly List<string> DefaultBlocklist = new()
    {
        "WHAT DO YOU MEME?", "Danielwellington", "Dressthatman.Com", "Mightymaxbattery", "Peachpopsclipart",
        "my everyday home", "bigsmokesupplies", "Revolutionultra", "Blossomsugarart", "Growing Success",
        "Gummy Bear Mold", "Hewlett-Packard", "J L Brands Y722", "Jackets 4 Bikes", "Jeannine Holper",
        "Jet Performance", "King Technology", "Kiniart Studios", "Law Enforcement", "Linda Mcsweeney",
        "Magnetic Poetry", "Makaton Charity", "Meal Prep Haven", "Mountain Khakis", "Moviecraft Inc.",
        "Mystudygroup101", "Naturo Sciences", "Nordic Naturals", "Open University", "Oreckcommercial",
        "Originalbedband", "Pantry Elements", "Patu Cable Ties", "Peak Coffee Ltd", "Philips Norelco",
        "Philosophy Inc.", "Primrose London", "Punk And Pissed", "Ridgerock Tools", "Schlyer Designs",
        "Sensation Press", "Solid Strategic", "Solowork Studio", "Spin Master Ltd", "Survival Shovel",
        "Teeter Hang Ups", "Telescopic Rake", "The Gro Company", "The Joy Factory", "The Red Society",
        "Tornado Spinner", "Tshirt Bordello", "Y&T - Meniketti", "bostik 30812809", "Ear Wax Remover",
        "Armani Exchange", "makeup bag eBuy", "Easter Charades", "Toy Storage Bag", "Igluu Meal Prep",
        "lazy drawstring", "Detoxpeople Ltd", "The Executioner", "Chic & raw root", "Petersham Pipes",
        "Buydefinition29", "Psychic Sisters", "Nikita by nikki", "Hermann Sachse", "Maison & White",
        "Perfect Plants", "The Beach Boys", "Kyoketsu Shoge", "Throwing Stars", "Addicoreadeept",
        "Admincosmetics", "Alfred Dunhill", "American Scope", "American Weigh", "Amybug'S Attic",
        "Audio-Technica", "Bang & Olufsen", "Bar@Drinkstuff", "Beauty Junkees", "Blender Bottle",
        "Bloomberg L.P.", "Body Sculpture", "Break Ventures", "Bronze Gallery", "Cablewholesale",
        "Capital Brands", "Charles Jacobs", "Choon'S Design", "Cooking Savior", "David Delamare",
        "Deik And Dunes", "Demograss Plus", "Direct Designs", "Eco Zoom Stove", "Edesia Espress",
        "Etienne Aigner", "Fashion Polish", "Ferret Company", "Filemaker Inc.", "Frito-Lay Inc.",
        "G. Loomis Inc.", "Game Room Guys", "General Motors", "Gq Electronics", "Guess Marciano",
        "Hair Flair Ltd", "Heirloom Finds", "Hks Europe Ltd", "Home Brew Ohio", "Homeplanetgear",
        "Howard Elliott", "Hs Ann Limited", "Hugs Not Drugs", "Ill Rock Merch"
    };
}
