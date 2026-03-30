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
        ["freezing"] = new()
        {
            Ongoing = "The chill has settled into you and you cannot seem to shake it.",
            Resist = "The mountain\u2019s teeth find your cloak before they find your skin. Tonight, that is enough.",
            Succumb = "The cold slips past every layer you brought. It settles in as if it means to stay.",
            HealProgress = "The cold retreats a little. Not vanquished \u2014 only pushed back.",
            HealFailure = "You feed the fire until dawn. The cold does not care.",
            HealComplete = "Your warmth is your own again. The cold retreats to the peaks where it belongs.",
            Death = "You drift rather than wake; numb past cold, past pain, past everything. The mountain takes what it is owed.",
        },
        ["thirsty"] = new()
        {
            Ongoing = "Your tongue is a strip of leather. The waste watches, and waits.",
            Resist = "You count every drop. The scrubland circles, patient \u2014 but tonight your throat stays wet.",
            Succumb = "The waste has taken your water. All that passes through this land pays that toll.",
            HealProgress = "A mouthful of water, precious as gold out here. Not enough \u2014 but something.",
            HealFailure = "You lick the inside of your canteen and taste dust.",
            HealComplete = "Water. You had forgotten that water could taste like anything but want.",
            Death = "Something silver gleams between the dunes. Your legs carry you toward it before your mind can object. It is always sand. In the end, it is only sand.",
        },
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
        ["exhausted"] = new()
        {
            Ongoing = "Boots, boots, boots. Another dawn, another road. Your body has nothing left to spend.",
            Resist = "The road has had its way with you, but the boots held, and the camp was warm enough. You\u2019ve paid in sweat, not blood.",
            Succumb = "The blisters split. The cold finds every gap in your bedroll. The road tallies what you owe, and tonight it collects.",
            HealProgress = "The rest does something. Not everything, but the weight lifts a little.",
            HealFailure = "You sleep, and the road undoes it. You rise as tired as you lay down.",
            HealComplete = "Your legs remember what they are for. The road ahead looks possible again.",
            Death = "You stop to rest for just a moment. The moment never ends.",
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
        ["lost"] = new()
        {
            Ongoing = "Every track looks like the last one. Every hill the same hill. You are still lost.",
            Resist = "You scratch landmarks into your notes before they blur together. The way holds, for now.",
            Succumb = "You look back the way you came and see only wilderness. The path that brought you here is gone.",
            HealProgress = "A landmark clicks into place. The wilderness is not quite so featureless as it was.",
            HealFailure = "Your map is a lie. The landmarks refuse to answer to what you\u2019ve drawn. You are still turned around.",
            HealComplete = "The country makes sense again. You know where you\u2019ve been, and where you\u2019re going.",
            Death = "The wilderness does not yield your body to searchers.",
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
