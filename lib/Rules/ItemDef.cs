namespace Dreamlands.Rules;

/// <summary>One entry in an item's RPS combat moveset. <see cref="Encoding"/> is
/// the mechanical move (parsed by <c>Move.Parse</c>); <see cref="DisplayName"/>
/// is the player-facing label rendered on the action button. The encoding alone
/// does not tell the player what family a move belongs to (the UI buttons carry
/// no family icon), so the display name is expected to include the base verb —
/// e.g. encoding <c>"Stunning Power Attack"</c>, display <c>"Stunning Pommel Attack"</c>.
/// Plain bases display as the bare verb: <c>"Attack"</c>, <c>"Defend"</c>,
/// <c>"Recover"</c>, <c>"Read"</c>.</summary>
public sealed record RpsMove(string Encoding, string DisplayName);

/// <summary>Type of equipment item.</summary>
public enum ItemType { Tool, Consumable, Weapon, Armor, Haul }

/// <summary>Weapon class for weapon-type items.</summary>
public enum WeaponClass { Dagger, Axe, Sword }

/// <summary>Armor class for armor-type items.</summary>
public enum ArmorClass { Light, Medium, Heavy }

/// <summary>Definition of an equipment item.</summary>
public sealed class ItemDef
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string? Description { get; init; }
    public ItemType Type { get; init; }
    public int Slots { get; init; } = 1;
    public IReadOnlySet<string> Cures { get; init; } = new HashSet<string>();

    /// <summary>Conditions this item passively prevents while present in the pack
    /// (never consumed, no market role). Used by EndOfDay immunity check.</summary>
    public IReadOnlySet<string> PassiveImmunities { get; init; } = new HashSet<string>();

    public WeaponClass? WeaponClass { get; init; }
    public ArmorClass? ArmorClass { get; init; }
    public int? Cost { get; init; }
    public string? Biome { get; init; }
    public int? ShopTier { get; init; }

    /// <summary>RPS combat moves contributed when equipped, in encoded form (e.g.
    /// "Riposte Attack", "Heavy Power Defend"). Parsed lazily by the combat profile
    /// builder. Prefix any move with the <c>Better</c> adjective (e.g.
    /// <c>"Better Wary Read"</c>) to mark it as a strict improvement over its
    /// base verb — the unmutated base is then dropped from the player's pool.
    /// See super_rps.md § Item Movesets.
    ///
    /// AUTHORING GOTCHAS — read CombatPlayerProfile.From before changing gear:
    ///
    ///   Weapons: this list is the COMPLETE set of Attack-family moves the
    ///     player gets. There is no implicit basic Attack added under the hood.
    ///     A weapon with RpsMoves = ["Riposte Attack"] means the player can
    ///     only Riposte Attack — they cannot throw a plain Attack. T-4 weapons
    ///     (The Old Tooth, Revathi Labrys) intentionally omit "Attack" to
    ///     enforce a specialist identity. T-1/T-2 typically declare "Attack"
    ///     first plus any upgrades.
    ///
    ///   Armor: must declare at least one Defend-base move (plain or mutated).
    ///     The basic-Defend fallback in CombatPlayerProfile only fires when no
    ///     armor is equipped at all. Equipping armor whose RpsMoves contains
    ///     no Defend variant leaves the player with ZERO Defend in the pool —
    ///     strictly worse than fighting unarmored. Cooldown-gated Defends
    ///     (Power/Slow) count, but mean the player has no Defend after
    ///     the cooldown burns.
    ///
    ///   No-weapon = no Attack at all is the design (T-0 = "disable Attack").
    ///   No-armor = basic Defend is the design (T-0 = "basic defense").
    ///   The asymmetry is intentional but easy to forget.</summary>
    public IReadOnlyList<RpsMove> RpsMoves { get; init; } = Array.Empty<RpsMove>();

    /// <summary>Minimum Combat skill required to wield/wear this item meaningfully.
    /// 0 = daggers/light, 2 = axes/medium, 4 = swords/heavy. Not enforced today —
    /// players can equip above their level (TODO: gate at equip-time).</summary>
    public int RequiredCombat { get; init; } = 0;

    /// <summary>True for items that go in Pack.</summary>
    public bool IsPackItem => Type is ItemType.Weapon or ItemType.Armor or ItemType.Tool or ItemType.Haul;

    public static IReadOnlyDictionary<string, ItemDef> All { get; } = BuildAll();

    public static bool IsValidId(string id) => All.ContainsKey(id);

    static Dictionary<string, ItemDef> BuildAll() => new()
    {
        // ── Weapons: Daggers (Untrained Combat, cancel-focused) ──
        // Pool: momentum_to_progress, momentum_to_cancel, free_momentum, spirits_to_cancel, free_cancel

        ["hunting_knife"] = new()
        {
            Id = "hunting_knife", Name = "Hunting Knife", Type = ItemType.Weapon,
            Description = "A short blade with a bone handle, worn from use. Good for most things that need a blade.",
            WeaponClass = Rules.WeaponClass.Dagger,
            RequiredCombat = 0,
            RpsMoves = [new("Attack", "Attack")],
            Biome = "plains", ShopTier = 1, Cost = 15,
        },
        ["kukri"] = new()
        {
            Id = "kukri", Name = "Kukri", Type = ItemType.Weapon,
            Description = "A forward-curved dagger from the eastern scrublands. The heavy tip makes a pommel strike land like a hammer.",
            WeaponClass = Rules.WeaponClass.Dagger,
            RequiredCombat = 0,
            RpsMoves = [new("Attack", "Attack"), new("Stun Power Attack", "Pommel Stun ✦")],
            Biome = "scrub", ShopTier = 2, Cost = 40,
        },
        ["seax"] = new()
        {
            Id = "seax", Name = "Fine Seax", Type = ItemType.Weapon,
            Description = "A narrow single-edge blade, weighted for the counter. Not a first-strike weapon — a last-resort one.",
            WeaponClass = Rules.WeaponClass.Dagger,
            RequiredCombat = 0,
            RpsMoves = [new("Riposte Attack", "Riposte")],
            Biome = "mountains", ShopTier = 2, Cost = 80,
        },
        ["the_old_tooth"] = new()
        {
            Id = "the_old_tooth", Name = "The Old Tooth", Type = ItemType.Weapon,
            Description = "A dagger so old the steel has gone gray. It has no plain attack — only the riposte and the provoke, for those who know how to use fear.",
            WeaponClass = Rules.WeaponClass.Dagger,
            RequiredCombat = 0,
            RpsMoves = [new("Riposte Attack", "Riposte"), new("Heavy Power Provoking Attack", "Provoke ✦")],
        },

        // ── Weapons: Axes (Trained Combat, aggro-focused, zero cancels) ──
        // Pool: momentum_to_progress, free_momentum, momentum_to_progress_large, threat_to_progress_large, momentum_to_progress_huge

        ["hatchet"] = new()
        {
            Id = "hatchet", Name = "Hatchet", Type = ItemType.Weapon,
            Description = "A wood-camp tool pressed into service. Axe, Trained Combat. Hits hard.",
            WeaponClass = Rules.WeaponClass.Axe,
            RequiredCombat = 2,
            RpsMoves = [new("Attack", "Attack"), new("Slow Power Attack", "Wild Chop ✦")],
            Biome = "forest", ShopTier = 1, Cost = 15,
        },
        ["war_axe"] = new()
        {
            Id = "war_axe", Name = "War Axe", Type = ItemType.Weapon,
            Description = "A proper fighting axe, balanced for the follow-through. Axe, Trained Combat.",
            WeaponClass = Rules.WeaponClass.Axe,
            RequiredCombat = 2,
            RpsMoves = [new("Attack", "Attack"), new("Heavy Power Attack", "Heavy Chop ✦")],
            Biome = "forest", ShopTier = 2, Cost = 40,
        },
        ["broadaxe"] = new()
        {
            Id = "broadaxe", Name = "Broadaxe", Type = ItemType.Weapon,
            Description = "A two-handed mountain campaign weapon. Axe, Trained Combat. Staggers anything it touches.",
            WeaponClass = Rules.WeaponClass.Axe,
            RequiredCombat = 2,
            RpsMoves = [new("Attack", "Attack"), new("Heavy Stunning Power Attack", "Brutal Stun ✦")],
            Biome = "mountains", ShopTier = 2, Cost = 80,
        },
        ["revathi_labrys"] = new()
        {
            Id = "revathi_labrys", Name = "Revathi Labrys", Type = ItemType.Weapon,
            Description = "A Revathi ceremonial axe that has seen real use. Axe, Trained Combat. No plain attack.",
            WeaponClass = Rules.WeaponClass.Axe,
            RequiredCombat = 2,
            RpsMoves = [new("Heavy Attack", "Arcing Chop"), new("Slow Terrifying Attack", "Psychic Warp ✦")],
        },

        // ── Weapons: Swords (Expert Combat, hybrid) ──
        // Pool: momentum_to_progress, free_momentum, momentum_to_cancel, momentum_to_progress_large, free_cancel

        ["falchion"] = new()
        {
            Id = "falchion", Name = "Falchion", Type = ItemType.Weapon,
            Description = "A heavy single-edge blade for cavalry or desperate work. Sword, Expert Combat.",
            WeaponClass = Rules.WeaponClass.Sword,
            RequiredCombat = 4,
            RpsMoves = [new("Attack", "Attack"), new("Exhausting Heavy Attack", "Wild Lunge ✦")],
            Biome = "plains", ShopTier = 1, Cost = 15,
        },
        ["short_sword"] = new()
        {
            Id = "short_sword", Name = "Short Sword", Type = ItemType.Weapon,
            Description = "Versatile and fast. Sword, Expert Combat.",
            WeaponClass = Rules.WeaponClass.Sword,
            RequiredCombat = 4,
            RpsMoves = [new("Riposte Attack", "Riposte"), new("Stun Power Attack", "Pommel Stun ✦")],
            Biome = "plains", ShopTier = 1, Cost = 15,
        },
        ["scimitar"] = new()
        {
            Id = "scimitar", Name = "Scimitar", Type = ItemType.Weapon,
            Description = "A curved blade from the scrub trade routes. Sword, Expert Combat.",
            WeaponClass = Rules.WeaponClass.Sword,
            RequiredCombat = 4,
            RpsMoves = [new("Attack", "Attack"), new("Heavy Power Defend", "Whirling Blade ✦")],
            Biome = "scrub", ShopTier = 2, Cost = 80,
        },
        ["shimmering_blade"] = new()
        {
            Id = "shimmering_blade", Name = "Shimmering Blade", Type = ItemType.Weapon,
            Description = "A Lattice-touched sword that mends as it cuts. Sword, Expert Combat.",
            WeaponClass = Rules.WeaponClass.Sword,
            RequiredCombat = 4,
            RpsMoves = [new("Riposte Attack", "Riposte"), new("Heavy Wary Recover", "Lattice Mending")],
        },

        // ── Armor: Light (Untrained Combat) ──

        ["tunic"] = new()
        {
            Id = "tunic", Name = "Tunic", Type = ItemType.Armor,
            Description = "Padded cloth. Light armor, no Combat requirement.",
            ArmorClass = Rules.ArmorClass.Light,
            RequiredCombat = 0,
            RpsMoves = [new("Defend", "Defend")],
            Biome = "plains", ShopTier = 1,
        },
        ["silks"] = new()
        {
            Id = "silks", Name = "Silks", Type = ItemType.Armor,
            Description = "Layered silk from the scrub trade. Light armor, no Combat requirement.",
            ArmorClass = Rules.ArmorClass.Light,
            RequiredCombat = 0,
            RpsMoves = [new("Defend", "Defend"), new("Wary Read", "Cautious Read")],
            Biome = "scrub", ShopTier = 1, Cost = 15,
        },
        ["cartographers_cloak"] = new()
        {
            Id = "cartographers_cloak", Name = "Cartographer's Cloak", Type = ItemType.Armor,
            Description = "A traveler's outer garment stitched with hidden compartments. Light armor, no Combat requirement.",
            ArmorClass = Rules.ArmorClass.Light,
            RequiredCombat = 0,
            RpsMoves = [new("Defend", "Defend"), new("Power Wary Recover", "Cartographer's Guile ✦")],
            Biome = "mountains", ShopTier = 2, Cost = 40,
        },
        ["robe_of_twilight"] = new()
        {
            Id = "robe_of_twilight", Name = "Robe of Twilight", Type = ItemType.Armor,
            Description = "A deep-dyed robe that blurs in low light. Light armor, no Combat requirement.",
            ArmorClass = Rules.ArmorClass.Light,
            RequiredCombat = 0,
            RpsMoves = [new("Shielding Defend", "Shadow Cloak"), new("Heavy Power Wary Recover", "Shadow Step ✦")],
        },

        // ── Armor: Medium (Trained Combat) ──

        ["hide_armor"] = new()
        {
            Id = "hide_armor", Name = "Hide Armor", Type = ItemType.Armor,
            Description = "Cured leather over a quilted undergarment. Medium armor, Trained Combat.",
            ArmorClass = Rules.ArmorClass.Medium,
            RequiredCombat = 2,
            RpsMoves = [new("Defend", "Defend")],
            Biome = "mountains", ShopTier = 1, Cost = 15,
        },
        ["lamellar"] = new()
        {
            Id = "lamellar", Name = "Lamellar", Type = ItemType.Armor,
            Description = "Riveted plates on a leather backing, the standard of mountain garrison troops. Medium armor, Trained Combat.",
            ArmorClass = Rules.ArmorClass.Medium,
            RequiredCombat = 2,
            RpsMoves = [new("Defend", "Defend"), new("Shielding Power Defend", "Evade ✦")],
            Biome = "mountains", ShopTier = 2, Cost = 80,
        },
        ["mountain_regiment_armor"] = new()
        {
            Id = "mountain_regiment_armor", Name = "17th Mountain Regiment Armor", Type = ItemType.Armor,
            Description = "Imperial campaign armor from the 17th Regiment, built for the long siege. Medium armor, Trained Combat.",
            ArmorClass = Rules.ArmorClass.Medium,
            RequiredCombat = 2,
            RpsMoves = [new("Perfect Power Defend", "Perfect Block ✦"), new("Power Wary Recover", "Cautious ✦")],
        },

        // ── Armor: Heavy (Expert Combat) ──

        ["gambeson"] = new()
        {
            Id = "gambeson", Name = "Gambeson", Type = ItemType.Armor,
            Description = "Thick quilted cloth, the workhorse of heavy infantry. Heavy armor, Expert Combat.",
            ArmorClass = Rules.ArmorClass.Heavy,
            RequiredCombat = 4,
            RpsMoves = [new("Defend", "Defend")],
            Biome = "mountains", ShopTier = 1, Cost = 15,
        },
        ["scale_armor"] = new()
        {
            Id = "scale_armor", Name = "Scale Armor", Type = ItemType.Armor,
            Description = "Overlapping iron scales on a leather backing. Heavy armor, Expert Combat.",
            ArmorClass = Rules.ArmorClass.Heavy,
            RequiredCombat = 4,
            RpsMoves = [new("Heavy Defend", "Armored")],
            Biome = "scrub", ShopTier = 2, Cost = 40,
        },
        ["brigandine"] = new()
        {
            Id = "brigandine", Name = "Brigandine", Type = ItemType.Armor,
            Description = "Small steel plates riveted inside a cloth shell. Heavy armor, Expert Combat.",
            ArmorClass = Rules.ArmorClass.Heavy,
            RequiredCombat = 4,
            RpsMoves = [new("Defend", "Defend"), new("Heavy Power Shielding Defend", "Unstoppable ✦")],
            Biome = "plains", ShopTier = 2, Cost = 80,
        },
        ["golem_armor"] = new()
        {
            Id = "golem_armor", Name = "Golem Armor", Type = ItemType.Armor,
            Description = "Lattice-forged plates that move with eerie precision. Heavy armor, Expert Combat.",
            ArmorClass = Rules.ArmorClass.Heavy,
            RequiredCombat = 4,
            RpsMoves = [new("Heavy Defend", "Defend: Armored"), new("Perfect Power Defend", "Perfect Block ✦")],
        },

        // ── Scarecrow Boots (passive Tool — exhaustion immunity while carried) ──

        ["scarecrow_boots"] = new()
        {
            Id = "scarecrow_boots", Name = "Scarecrow Boots", Type = ItemType.Tool,
            Description = "Patchwork boots stitched together from a hundred salt-stained leathers. The wearer never tires.",
            PassiveImmunities = new HashSet<string> { "exhausted" },
        },

        // ── Tools: Shopable ──

        ["canteen"] = new()
        {
            Id = "canteen", Name = "Canteen", Type = ItemType.Tool,
            Description = "A sealed tin flask that keeps water cool.",
            Biome = "forest", ShopTier = 1, Cost = 15,
        },
        ["waterskin"] = new()
        {
            Id = "waterskin", Name = "Waterskin", Type = ItemType.Tool,
            Description = "A treated hide bag holding two days of water.",
            Biome = "scrub", ShopTier = 2, Cost = 40,
        },
        ["letters_of_introduction"] = new()
        {
            Id = "letters_of_introduction", Name = "Letters of Introduction", Type = ItemType.Tool,
            Description = "Guild correspondence bearing official seals. Carried as credential; produces no mechanical bonus under the approach system.",
            Biome = "scrub", ShopTier = 1, Cost = 40,
        },
        ["peoples_borderlands"] = new()
        {
            Id = "peoples_borderlands", Name = "A Guide to the Borderlands", Type = ItemType.Tool,
            Description = "A dense compendium of Borderlands history and precedent. Carried as reference; produces no mechanical bonus under the approach system.",
            Biome = "mountains", ShopTier = 2, Cost = 80,
        },
        ["cartographers_diary"] = new()
        {
            Id = "cartographers_diary", Name = "Cartographer's Diary", Type = ItemType.Tool,
            Description = "A field notebook dense with routes, landmarks, and marginal notes. Carried as reference; produces no mechanical bonus under the approach system.",
            Biome = "mountain", ShopTier = 1, Cost = 40,
        },
        ["ornate_spyglass"] = new()
        {
            Id = "ornate_spyglass", Name = "Ornate Spyglass", Type = ItemType.Tool,
            Description = "A brass-fitted glass with exceptional optics. Carried for scouting; produces no mechanical bonus under the approach system.",
            Biome = "scrub", ShopTier = 2, Cost = 80,
        },
        ["cartographers_kit"] = new()
        {
            Id = "cartographers_kit", Name = "Cartographer's Kit", Type = ItemType.Tool,
            Description = "Compass, sighting rod, and folded survey sheets.",
            Biome = "plains", ShopTier = 1, Cost = 80,
        },
        ["sleeping_kit"] = new()
        {
            Id = "sleeping_kit", Name = "Sleeping Kit", Type = ItemType.Tool,
            Description = "A wool blanket, oilskin ground sheet, and cordage.",
            Biome = "forest", ShopTier = 2, Cost = 80,
        },

        // ── Tools: Dungeon-only ──

        ["lattice_ward"] = new()
        {
            Id = "lattice_ward", Name = "Lattice Ward", Type = ItemType.Tool,
            Description = "A ceramic disc etched with suppression glyphs.",
        },
        ["sakharov_mask"] = new()
        {
            Id = "sakharov_mask", Name = "Sakharov's Mask", Type = ItemType.Tool,
            Description = "A fitted respirator from the old irradiation surveys.",
        },
        ["antivenom_kit"] = new()
        {
            Id = "antivenom_kit", Name = "Antivenom Kit", Type = ItemType.Tool,
            Description = "Vials of broad-spectrum antivenom packed in a padded case.",
        },

        // ── Food ──
        // Single ration item; display name set via FlavorText.RationName(biome) at refill time.
        // 1 ration = 1 day of food = 1 haversack slot. Free at any settlement.

        ["food_ration"] = new()
        {
            Id = "food_ration", Name = "Rations", Type = ItemType.Consumable,
            Cost = 3,
        },

        // ── Medicines ──

        ["medical_kit"] = new()
        {
            Id = "medical_kit", Name = "Medical Kit", Type = ItemType.Tool,
            Description = "A canvas roll of splints, linen bandages, and pine-resin salve. Cures injured without being consumed.",
            Cures = new HashSet<string> { "injured" },
            Cost = 25,
        },
        ["siphon_glass"] = new()
        {
            Id = "siphon_glass", Name = "Siphon Glass", Type = ItemType.Consumable,
            Description = "Strange crystalline fragments. It's said they draw out and capture unnatural colors.",
            Cures = new HashSet<string> { "lattice_sickness" },
            Biome = "scrub", ShopTier = 2, Cost = 40,
        },
        // Removed: pale_knot_berry cured exhausted, but exhaustion is now ClearedOnSettlement.
        // Minor conditions no longer have item cures (haversack_refactor.md).
        ["shustov_tonic"] = new()
        {
            Id = "shustov_tonic", Name = "Shustov Tonic", Type = ItemType.Consumable,
            Description = "A smoky distillation of charcoal and salt-marsh herbs. Flushes radiation sickness over several nights.",
            Cures = new HashSet<string> { "irradiated" },
            Biome = "plains", ShopTier = 2, Cost = 40,
        },
        ["mudcap_fungus"] = new()
        {
            Id = "mudcap_fungus", Name = "Mudcap Fungus", Type = ItemType.Consumable,
            Description = "A squat brown mushroom with a gritty cap. Eaten raw, it draws poison from the blood.",
            Cures = new HashSet<string> { "poisoned" },
            Biome = "swamp", ShopTier = 2, Cost = 15,
        },

        ["control_shaft"] = new()
        {
            Id = "control_shaft", Name = "Control Shaft", Type = ItemType.Tool,
            Description = "A metal rod wrapped in waxed linen and bound with cording. An imperial armory seal holds the bindings in place.",
        },
        ["brass_lantern"] = new()
        {
            Id = "brass_lantern", Name = "Old Brass Lantern", Type = ItemType.Tool,
            Description = "A dented lantern of tarnished brass. The glass is cracked but it still holds a flame. Provides light in dark places.",
            Biome = "plains", ShopTier = 1, Cost = 15,
        },

        // ── Haul (generic def — per-haul identity comes from HaulDefId on ItemInstance) ──

        ["haul"] = new()
        {
            Id = "haul", Name = "Haul", Type = ItemType.Haul,
            Description = "A delivery bound for a specific settlement.",
        },
    };
}
