namespace Dreamlands.Rules;

/// <summary>Flavor text for a condition's lifecycle events.</summary>
public sealed class ConditionFlavor
{
    public string Ongoing { get; init; } = "";
    public string Resist { get; init; } = "";
    public string Succumb { get; init; } = "";
    public string HealProgress { get; init; } = "";
    public string HealFailure { get; init; } = "";
    public string HealComplete { get; init; } = "";
    public string Death { get; init; } = "";

    internal static IReadOnlyDictionary<string, ConditionFlavor> All { get; } = BuildAll();

    static Dictionary<string, ConditionFlavor> BuildAll() => new()
    {
        ["lattice_sickness"] = new()
        {
            Ongoing = "The colors bleed at the edges of your vision. Something in the lattice has taken root in you.",
            Resist = "The scrubland shimmers with a frequency that sets your teeth on edge, but your blood stays clean.",
            Succumb = "The world splits along lines that shouldn't exist. Color drains from your left eye first, then the pain begins.",
            HealProgress = "The colors stabilize. The lattice's grip loosens, though it leaves marks you can feel but not see.",
            HealFailure = "The medicine burns through you and the lattice drinks it down. The sickness has learned your remedies.",
            HealComplete = "The world snaps back to a single image. The lattice releases you, leaving only the memory of geometries that should not be.",
            Death = "The colors separate completely. You see the lattice for what it is -- beautiful, perfect, and utterly indifferent to the small thing dying inside it.",
        },
        ["irradiated"] = new()
        {
            Ongoing = "Something has changed in the quality of your blood. You can feel it, if not name it. The Glowing Curse persists.",
            Resist = "By fortune or instinct, your camp sits on clean ground. The land\u2019s slow poison finds no purchase tonight.",
            Succumb = "Your teeth sit loose in their moorings. Blisters rise where nothing touched you. The plains have put their mark on you.",
            HealProgress = "Something in the medicine fights something it was never made to fight. Ground is yielded.",
            HealFailure = "The medicine does what it can. But you are still here, and the land\u2019s poison is patient.",
            HealComplete = "The curse burns itself out at last, leaving you scarred and still standing. The marks will stay.",
            Death = "Your lungs have forgotten their purpose. The light that has no business being inside you fills every dark space. Then nothing.",
        },
        ["poisoned"] = new()
        {
            Ongoing = "The wound has gone the wrong color. A sick heat spreads from it, slow and deliberate.",
            Resist = "The venom smells acrid, but none broke the skin. You carefully wipe the vile liquid away.",
            Succumb = "The fire spreads from the wound faster than it should. The venom is in you now.",
            HealProgress = "The antidote is buying you ground. The fire in your blood has cooled a little.",
            HealFailure = "The antidote slows it. No more than that. The venom clings to what it has claimed.",
            HealComplete = "The wound closes clean at last. Whatever was in your blood has been driven out.",
            Death = "The fever takes your thoughts first, then your limbs, then everything. The venom finishes what the bite began.",
        },
        ["injured"] = new()
        {
            Ongoing = "Your injuries worsen. You\u2019ll need healing soon.",
            Resist = "You steady yourself. Cuts, bruises. Nothing serious. You\u2019re shaken but you\u2019ll walk it off.",
            Succumb = "A dark patch of blood. Yours. You bind it as best you can, but you\u2019ll need healing to mend.",
            HealProgress = "You check your wound. A little better today.",
            HealFailure = "New injuries piled atop old. Your body cannot withstand much more of this.",
            HealComplete = "It will leave a scar, but your wound is mended.",
            Death = "The smell is awful. You cannot bear to look beneath the bandages. You press on, but stumble after a few steps.",
        },
    };
}
