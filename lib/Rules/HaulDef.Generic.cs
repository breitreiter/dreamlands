namespace Dreamlands.Rules;

public sealed partial class HaulDef
{
    public static readonly string[] GenericDeliveryFlavors =
    {
        "Signed and accepted without comment.",
        "The factor nods to a stack of packages and returns to his work. You add your delivery to the pile.",
        "The factor's young assistant accepts the package with genuine enthusiasm.",
        "The factor looks the package over with an appraising eye, and, presumably satisfied, signs the ledger.",
        "The factor processes the package with remarkable efficiency, then dispatches you on your way.",
        "You wait an eternity while the trader in front of you argues about a pricing issue, then finally deliver the package without ceremony.",
        "The factor is nowhere to be found. With some trepidation, you stash the package below their desk and sign the ledger yourself.",
        "The factor is eating a hearty meal and invites you to join. You decline, but leave the package in his greasy hands.",
        "The factor has fallen ill, but insists on performing her duties. She pauses occasionally to cough violently before returning to the work. You're glad to be rid of the package.",
        "The factor insists the package was due two days ago. You explain you delivered it as quickly as the roads allowed. He says he'll file a complaint.",
        "The factor is asleep at his desk. You nudge him awake and, after a moment's disorientation, he starts to explain that he was entirely awake. You don't argue the point and deliver the package.",
        "The factor seems fidgety and furtive, but she has a guild ring. You drop off the package and hope for the best.",
        "The factor is having a loud conversation with a local. You are not invited to join. You're directed where to drop the package off with hand signals. You do so and move on.",
    };

    static IEnumerable<HaulDef> Generic() => new HaulDef[]
    {
        new()
        {
            Id = "generic_guild_reports",
            Name = "Guild Reports",
            OriginBiome = "",
            DestBiome = "",
            IsGeneric = true,
            OriginFlavor = "Normally a courier handles these, but if you're heading that direction, the work pays a little coin.",
        },
        new()
        {
            Id = "generic_sealed_crate",
            Name = "Sealed Crate",
            OriginBiome = "",
            DestBiome = "",
            IsGeneric = true,
            OriginFlavor = "Nondescript wooden crate with a wax seal. Nobody mentions what's inside.",
        },
        new()
        {
            Id = "generic_bonded_cargo",
            Name = "Bonded Cargo",
            OriginBiome = "",
            DestBiome = "",
            IsGeneric = true,
            OriginFlavor = "A parcel under guild bond. You signed for it, so don't lose it.",
        },
        new()
        {
            Id = "generic_unmarked_parcel",
            Name = "Unmarked Parcel",
            OriginBiome = "",
            DestBiome = "",
            IsGeneric = true,
            OriginFlavor = "Brown paper, twine, no label. Someone at the other end is expecting it.",
        },
        new()
        {
            Id = "generic_dusty_ledger",
            Name = "Dusty Ledger",
            OriginBiome = "",
            DestBiome = "",
            IsGeneric = true,
            OriginFlavor = "Looks like someone's to be audited. Or was audited. Anyway, the book needs to move.",
        },
        new()
        {
            Id = "generic_tax_remittance",
            Name = "Tax Remittance",
            OriginBiome = "",
            DestBiome = "",
            IsGeneric = true,
            OriginFlavor = "A locked strongbox of collected duties. Heavy for its size.",
        },
        new()
        {
            Id = "generic_requisition_forms",
            Name = "Requisition Forms",
            OriginBiome = "",
            DestBiome = "",
            IsGeneric = true,
            OriginFlavor = "A stack of forms requesting supplies. Technically urgent, practically ignored.",
        },
        new()
        {
            Id = "generic_merchants_samples",
            Name = "Merchant's Samples",
            OriginBiome = "",
            DestBiome = "",
            IsGeneric = true,
            OriginFlavor = "Fabric swatches and spice vials. A trader wants them moved to the next market.",
        },
        new()
        {
            Id = "generic_mail_bundle",
            Name = "Mail Bundle",
            OriginBiome = "",
            DestBiome = "",
            IsGeneric = true,
            OriginFlavor = "A stack of letters tied with twine. Courier work but coin is coin.",
        },
        new()
        {
            Id = "generic_blank_guild_ledger",
            Name = "Blank Guild Ledger",
            OriginBiome = "",
            DestBiome = "",
            IsGeneric = true,
            OriginFlavor = "The factor ended up with an extra and needs it moved to the next office in line.",
        },
        new()
        {
            Id = "generic_box_of_ink_jars",
            Name = "Box of Ink Jars",
            OriginBiome = "",
            DestBiome = "",
            IsGeneric = true,
            OriginFlavor = "They say the guild runs on ink and blood, but we just need you to deliver the former.",
        },
    };
}
