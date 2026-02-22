using Dreamlands.Map;
using Dreamlands.Rules;

namespace Dreamlands.Flavor;

/// <summary>Static biome-aware food names and descriptions, keyed by (FoodType, Terrain, foraged).</summary>
public static class FoodNames
{
    public static (string Name, string Description) Pick(FoodType type, Terrain biome, bool foraged, Random? rng = null)
    {
        var key = (type, biome, foraged);
        if (!Data.TryGetValue(key, out var entries))
            key = (type, Terrain.Plains, foraged); // fallback to plains
        if (!Data.TryGetValue(key, out entries))
            return ("trail rations", "Bland but serviceable road food.");

        var idx = rng?.Next(entries.Length) ?? (Counter++ % entries.Length);
        return entries[idx];
    }

    static int Counter;

    static readonly Dictionary<(FoodType, Terrain, bool), (string Name, string Desc)[]> Data = new()
    {
        // ══════════════════════════════════════════
        // PROTEIN
        // ══════════════════════════════════════════

        // ── Plains ──

        [(FoodType.Protein, Terrain.Plains, false)] = [
            ("salt beef", "Thick slabs of beef packed in coarse salt and wrapped in waxed cloth."),
            ("smoked fish", "Pale fillets smoked over applewood until firm and golden at the edges."),
            ("cured sausage", "Dense links of spiced pork sealed in casings and hung to cure in a cool cellar."),
        ],
        [(FoodType.Protein, Terrain.Plains, true)] = [
            ("snared hare", "A lean hare caught in a wire snare, gutted and roasted over an open fire."),
            ("field mice", "Tiny morsels skewered on a green stick and charred over coals."),
            ("grasshoppers", "A handful of roasted grasshoppers, crunchy and faintly nutty."),
        ],

        // ── Scrub ──

        [(FoodType.Protein, Terrain.Scrub, false)] = [
            ("goat jerky", "Strips of lean goat, salted and wind-dried on high racks above the market stalls."),
            ("smoked lizard", "A whole desert monitor split and smoked until the meat turns dark and chewy."),
            ("spiced locusts", "Fat locusts fried crisp in oil and dusted with cumin and chili powder."),
        ],
        [(FoodType.Protein, Terrain.Scrub, true)] = [
            ("scorpion", "A large desert scorpion with the stinger removed, roasted tail-first over embers."),
            ("sun-dried gecko", "A small gecko split open and left on a flat stone until the desert sun does its work."),
            ("beetle grubs", "Plump white grubs dug from beneath a dead acacia, best eaten raw."),
        ],

        // ── Swamp ──

        [(FoodType.Protein, Terrain.Swamp, false)] = [
            ("smoked eel", "A fat eel coiled and pinned inside a clay smoker until the flesh turns amber."),
            ("pickled crayfish", "Whole crayfish packed in brine and vinegar in a stoppered clay jar."),
            ("salted frog legs", "Plump frog legs rubbed with salt and dried on reed frames above the water."),
        ],
        [(FoodType.Protein, Terrain.Swamp, true)] = [
            ("raw crayfish", "A handful of muddy crayfish cracked open and eaten cold from the shell."),
            ("frog", "A fat marsh frog speared on a sharpened reed and held over a smoky fire."),
            ("fat grubs", "Pale, thumb-sized grubs pulled from rotting logs, soft and surprisingly rich."),
        ],

        // ── Forest ──

        [(FoodType.Protein, Terrain.Forest, false)] = [
            ("venison jerky", "Dark strips of deer meat dried with juniper berries and cracked pepper."),
            ("smoked trout", "A whole brook trout smoked golden over green alder branches."),
            ("dried rabbit", "Boned rabbit pressed flat and dried until it snaps cleanly in the hand."),
        ],
        [(FoodType.Protein, Terrain.Forest, true)] = [
            ("snared squirrel", "A grey squirrel caught at dawn, skinned and spit-roasted over a small fire."),
            ("brook minnows", "A dozen tiny minnows scooped from a forest stream and fried in their own oil."),
            ("wood grubs", "Fat white grubs prised from beneath loose bark, mild and faintly sweet."),
        ],

        // ── Mountains ──

        [(FoodType.Protein, Terrain.Mountains, false)] = [
            ("yak jerky", "Tough strips of yak meat cured in mountain air until nearly black."),
            ("salted pork", "Dense slabs of pork belly crusted in rock salt and tightly wrapped."),
            ("wind-dried goat", "Lean goat hung on high lines where the cold wind dries it in days."),
        ],
        [(FoodType.Protein, Terrain.Mountains, true)] = [
            ("ptarmigan", "A plump mountain bird plucked and roasted whole in a pit of hot stones."),
            ("rock pigeon", "A wild pigeon brought down with a stone, cooked quickly over a ridge fire."),
            ("slugs", "Large mountain slugs skewered and roasted until they stop squirming."),
        ],

        // ══════════════════════════════════════════
        // GRAIN
        // ══════════════════════════════════════════

        // ── Plains ──

        [(FoodType.Grain, Terrain.Plains, false)] = [
            ("travel bread", "A dense wheat loaf baked twice for the road, heavy and slow to go stale."),
            ("barley cake", "A flat round of pressed barley sweetened with a little honey."),
            ("oat biscuit", "A dry, crumbly biscuit that keeps for weeks in a belt pouch."),
        ],
        [(FoodType.Grain, Terrain.Plains, true)] = [
            ("wild oats", "A pouch of hand-threshed wild oats, gritty but filling when boiled."),
            ("grass seeds", "Tiny seeds gathered from tall prairie grass and ground between stones."),
            ("field tubers", "Knobby tubers dug from soft earth, roasted in the coals until tender."),
        ],

        // ── Scrub ──

        [(FoodType.Grain, Terrain.Scrub, false)] = [
            ("flatbread", "Thin rounds of unleavened bread baked on a hot stone and folded for travel."),
            ("millet cake", "A compact cake of pressed millet and sesame, dry but sustaining."),
            ("dust crackers", "Paper-thin crackers baked until they shatter at a touch."),
        ],
        [(FoodType.Grain, Terrain.Scrub, true)] = [
            ("prickly pear flesh", "The scooped-out heart of a cactus fruit, wet and faintly tart."),
            ("dry roots", "Pale, fibrous roots pulled from cracked earth and chewed raw for their starch."),
            ("dust yam", "A tough desert tuber baked in sand until the skin blackens and splits."),
        ],

        // ── Swamp ──

        [(FoodType.Grain, Terrain.Swamp, false)] = [
            ("rice ball", "A fist-sized ball of sticky rice packed tight and wrapped in a broad leaf."),
            ("cattail flour bread", "A dark, heavy loaf made from cattail pollen flour and swamp water."),
            ("sago cake", "A pale, chewy cake pounded from sago palm pith and sun-dried on a rack."),
        ],
        [(FoodType.Grain, Terrain.Swamp, true)] = [
            ("cattail root", "A starchy root pulled from the shallows, peeled and eaten raw or boiled."),
            ("wild rice", "Dark grains of wild rice harvested from standing water and parched over flame."),
            ("lotus stem", "A crisp, hollow stem snapped from a marsh lotus and sliced into rounds."),
        ],

        // ── Forest ──

        [(FoodType.Grain, Terrain.Forest, false)] = [
            ("acorn bread", "A nutty brown loaf made from leached acorn flour and forest honey."),
            ("nut loaf", "A dense brick of ground hazelnuts and oats pressed into a travelling loaf."),
            ("rye hardtack", "A square of dark rye cracker, virtually indestructible."),
        ],
        [(FoodType.Grain, Terrain.Forest, true)] = [
            ("acorns", "A pouch of shelled acorns soaked and roasted until the bitterness fades."),
            ("wild onion", "Small pungent bulbs dug from the forest floor, best roasted in ash."),
            ("burdock root", "A long, earthy root scrubbed clean and sliced thin for chewing."),
        ],

        // ── Mountains ──

        [(FoodType.Grain, Terrain.Mountains, false)] = [
            ("buckwheat cake", "A heavy, dark cake of buckwheat groats baked with rendered fat."),
            ("dense hardtack", "A thick biscuit that could stop a knife, softened only by strong tea."),
            ("stone bread", "A round loaf baked in a stone oven until the crust rings when knocked."),
        ],
        [(FoodType.Grain, Terrain.Mountains, true)] = [
            ("lichen", "Pale rock lichen scraped from high boulders and boiled into a tasteless paste."),
            ("pine bark", "The soft inner bark of a mountain pine, stripped and chewed for its starch."),
            ("mountain sorrel", "Tart green leaves gathered from alpine meadows, eaten fresh by the handful."),
        ],

        // ══════════════════════════════════════════
        // SWEETS
        // ══════════════════════════════════════════

        // ── Plains ──

        [(FoodType.Sweets, Terrain.Plains, false)] = [
            ("honeycomb", "A dripping chunk of wild honeycomb wrapped in cloth, golden and fragrant."),
            ("dried apricots", "Soft, wrinkled apricots dried in the summer sun until deeply sweet."),
            ("molasses candy", "Dark, sticky blocks of boiled molasses rolled in coarse sugar."),
        ],
        [(FoodType.Sweets, Terrain.Plains, true)] = [
            ("wild clover", "A bunch of sweet clover blossoms chewed for their mild honey flavor."),
            ("hedge berries", "Small purple berries picked from a hedgerow, sweet with a sharp finish."),
            ("crab apples", "Tart little apples best eaten after the first frost softens them."),
        ],

        // ── Scrub ──

        [(FoodType.Sweets, Terrain.Scrub, false)] = [
            ("date paste", "A thick, dark paste of mashed dates pressed into a clay pot."),
            ("fig leather", "Sheets of dried fig pulp rolled thin and cut into strips for the road."),
            ("tamarind chew", "Sticky nuggets of tamarind pulp dusted with sugar and salt."),
        ],
        [(FoodType.Sweets, Terrain.Scrub, true)] = [
            ("dried cactus fruit", "Leathery slices of sun-dried cactus fruit, intensely sweet and chewy."),
            ("bitter figs", "Small, dark figs from a wild tree, sweet beneath their bitter skin."),
            ("ant honey", "A waxy lump of honeypot ant nectar dug from a desert nest."),
        ],

        // ── Swamp ──

        [(FoodType.Sweets, Terrain.Swamp, false)] = [
            ("cane sugar block", "A rough-cut block of unrefined cane sugar, brown and crumbly."),
            ("candied ginger", "Knobs of swamp ginger cooked in sugar syrup until translucent."),
            ("berry jam", "A small clay pot of thick dark jam made from marsh berries and cane sugar."),
        ],
        [(FoodType.Sweets, Terrain.Swamp, true)] = [
            ("swamp berries", "Clusters of tart red berries gathered from bushes at the water's edge."),
            ("wild ginger", "A knobby root pulled from wet soil, fiery and sweet when chewed raw."),
            ("sugar reed", "A length of marsh cane peeled and chewed for its sweet, watery juice."),
        ],

        // ── Forest ──

        [(FoodType.Sweets, Terrain.Forest, false)] = [
            ("wild berry preserve", "A small jar of forest berries cooked down with sugar into a thick preserve."),
            ("maple candy", "Hard amber drops of boiled maple sap that dissolve slowly on the tongue."),
            ("hazelnut paste", "Ground hazelnuts and honey worked into a rich, grainy paste."),
        ],
        [(FoodType.Sweets, Terrain.Forest, true)] = [
            ("blackberries", "Fat, dark berries picked from thorny brambles deep in the wood."),
            ("hazelnuts", "A pouch of wild hazelnuts cracked open and eaten by the handful."),
            ("maple sap", "A stoppered flask of raw maple sap, thin and faintly sweet."),
        ],

        // ── Mountains ──

        [(FoodType.Sweets, Terrain.Mountains, false)] = [
            ("pine nut brittle", "A slab of caramelized sugar studded with toasted pine nuts."),
            ("dried cherry", "Wizened mountain cherries dried until they are dark and intensely sweet."),
            ("heather honey", "A pot of thick, floral honey gathered from high alpine heather fields."),
        ],
        [(FoodType.Sweets, Terrain.Mountains, true)] = [
            ("juniper berries", "Dusty blue berries plucked from a mountain juniper, bitter and aromatic."),
            ("wild thyme", "A fragrant bundle of flowering thyme chewed more for comfort than nourishment."),
            ("pine nuts", "Tiny, rich nuts shaken from mountain pine cones and eaten raw."),
        ],
    };
}
