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
        ["hungry"] = new()
        {
            Ongoing = "The belly gnaws at itself. Another night of empty hands.",
            Resist = "Thin rations, thin comfort \u2014 but the belly does not yet turn on itself.",
            Succumb = "Your last scraps are gone. The road ahead looks longer than it did this morning.",
            HealProgress = "Real food. Your hands have stopped shaking. Your body still remembers the want.",
            HealFailure = "You scrape the bottom of your pack. Whatever was there, it was not enough.",
            HealComplete = "Your belly is full. You had forgotten what that felt like.",
            Death = "You wait for a friendly traveller, but no one comes to save you. The road does not mourn its dead.",
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
        ["swamp_fever"] = new()
        {
            Ongoing = "The fever burns on. The swamp does not give back what it takes.",
            Resist = "The insects find every inch of exposed skin. Whatever they carry, your body refuses it \u2014 this time.",
            Succumb = "Your joints are hot iron. The swamp has gotten into your blood.",
            HealProgress = "The fever breaks, then returns, lessened this time. You are winning, slowly, at great cost.",
            HealFailure = "The medicine fights. The insects undo its work. The swamp will not release you cheaply.",
            HealComplete = "The fever breaks for the last time. You are wrung out, hollow, but clean.",
            Death = "The world blurs and runs together. The swamp has many dead in it. It will have one more.",
        },
        ["gut_worms"] = new()
        {
            Ongoing = "Something writhes behind your navel. The forest's tiny passengers have made themselves at home.",
            Resist = "Something in the water looked wrong. You boiled it twice and ate nothing you couldn't identify. Caution, not luck.",
            Succumb = "It started with the berries, or the stream, or the meat that sat too long in the humid air. The forest feeds you its own way.",
            HealProgress = "The cramps ease. Whatever is living in you has lost ground, though it hasn't surrendered.",
            HealFailure = "The medicine passes through you. The worms remain, patient and well-fed.",
            HealComplete = "The last of them passes. You are hollow and scoured, but the food stays down again.",
            Death = "You cannot keep water down. The forest takes back everything it gave you, and then the rest.",
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
