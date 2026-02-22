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
            return ("Trail Rations", "Bland but serviceable road food.");

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
            ("Salt Beef", "Thick slabs of beef packed in coarse salt and wrapped in waxed cloth."),
            ("Smoked Fish", "Pale fillets smoked over applewood until firm and golden at the edges."),
            ("Cured Sausage", "Dense links of spiced pork sealed in casings and hung to cure in a cool cellar."),
        ],
        [(FoodType.Protein, Terrain.Plains, true)] = [
            ("Snared Hare", "A lean hare caught in a wire snare, gutted and roasted over an open fire."),
            ("Field Mice", "Tiny morsels skewered on a green stick and charred over coals."),
            ("Grasshoppers", "A handful of roasted grasshoppers, crunchy and faintly nutty."),
        ],

        // ── Scrub ──

        [(FoodType.Protein, Terrain.Scrub, false)] = [
            ("Goat Jerky", "Strips of lean goat, salted and wind-dried on high racks above the market stalls."),
            ("Smoked Lizard", "A whole desert monitor split and smoked until the meat turns dark and chewy."),
            ("Spiced Locusts", "Fat locusts fried crisp in oil and dusted with cumin and chili powder."),
        ],
        [(FoodType.Protein, Terrain.Scrub, true)] = [
            ("Scorpion", "A large desert scorpion with the stinger removed, roasted tail-first over embers."),
            ("Sun-Dried Gecko", "A small gecko split open and left on a flat stone until the desert sun does its work."),
            ("Beetle Grubs", "Plump white grubs dug from beneath a dead acacia, best eaten raw."),
        ],

        // ── Swamp ──

        [(FoodType.Protein, Terrain.Swamp, false)] = [
            ("Smoked Eel", "A fat eel coiled and pinned inside a clay smoker until the flesh turns amber."),
            ("Pickled Crayfish", "Whole crayfish packed in brine and vinegar in a stoppered clay jar."),
            ("Salted Frog Legs", "Plump frog legs rubbed with salt and dried on reed frames above the water."),
        ],
        [(FoodType.Protein, Terrain.Swamp, true)] = [
            ("Raw Crayfish", "A handful of muddy crayfish cracked open and eaten cold from the shell."),
            ("Frog", "A fat marsh frog speared on a sharpened reed and held over a smoky fire."),
            ("Fat Grubs", "Pale, thumb-sized grubs pulled from rotting logs, soft and surprisingly rich."),
        ],

        // ── Forest ──

        [(FoodType.Protein, Terrain.Forest, false)] = [
            ("Venison Jerky", "Dark strips of deer meat dried with juniper berries and cracked pepper."),
            ("Smoked Trout", "A whole brook trout smoked golden over green alder branches."),
            ("Dried Rabbit", "Boned rabbit pressed flat and dried until it snaps cleanly in the hand."),
        ],
        [(FoodType.Protein, Terrain.Forest, true)] = [
            ("Snared Squirrel", "A grey squirrel caught at dawn, skinned and spit-roasted over a small fire."),
            ("Brook Minnows", "A dozen tiny minnows scooped from a forest stream and fried in their own oil."),
            ("Wood Grubs", "Fat white grubs prised from beneath loose bark, mild and faintly sweet."),
        ],

        // ── Mountains ──

        [(FoodType.Protein, Terrain.Mountains, false)] = [
            ("Yak Jerky", "Tough strips of yak meat cured in mountain air until nearly black."),
            ("Salted Pork", "Dense slabs of pork belly crusted in rock salt and tightly wrapped."),
            ("Wind-Dried Goat", "Lean goat hung on high lines where the cold wind dries it in days."),
        ],
        [(FoodType.Protein, Terrain.Mountains, true)] = [
            ("Ptarmigan", "A plump mountain bird plucked and roasted whole in a pit of hot stones."),
            ("Rock Pigeon", "A wild pigeon brought down with a stone, cooked quickly over a ridge fire."),
            ("Slugs", "Large mountain slugs skewered and roasted until they stop squirming."),
        ],

        // ══════════════════════════════════════════
        // GRAIN
        // ══════════════════════════════════════════

        // ── Plains ──

        [(FoodType.Grain, Terrain.Plains, false)] = [
            ("Travel Bread", "A dense wheat loaf baked twice for the road, heavy and slow to go stale."),
            ("Barley Cake", "A flat round of pressed barley sweetened with a little honey."),
            ("Oat Biscuit", "A dry, crumbly biscuit that keeps for weeks in a belt pouch."),
        ],
        [(FoodType.Grain, Terrain.Plains, true)] = [
            ("Wild Oats", "A pouch of hand-threshed wild oats, gritty but filling when boiled."),
            ("Grass Seeds", "Tiny seeds gathered from tall prairie grass and ground between stones."),
            ("Field Tubers", "Knobby tubers dug from soft earth, roasted in the coals until tender."),
        ],

        // ── Scrub ──

        [(FoodType.Grain, Terrain.Scrub, false)] = [
            ("Flatbread", "Thin rounds of unleavened bread baked on a hot stone and folded for travel."),
            ("Millet Cake", "A compact cake of pressed millet and sesame, dry but sustaining."),
            ("Dust Crackers", "Paper-thin crackers baked until they shatter at a touch."),
        ],
        [(FoodType.Grain, Terrain.Scrub, true)] = [
            ("Prickly Pear Flesh", "The scooped-out heart of a cactus fruit, wet and faintly tart."),
            ("Dry Roots", "Pale, fibrous roots pulled from cracked earth and chewed raw for their starch."),
            ("Dust Yam", "A tough desert tuber baked in sand until the skin blackens and splits."),
        ],

        // ── Swamp ──

        [(FoodType.Grain, Terrain.Swamp, false)] = [
            ("Rice Ball", "A fist-sized ball of sticky rice packed tight and wrapped in a broad leaf."),
            ("Cattail Flour Bread", "A dark, heavy loaf made from cattail pollen flour and swamp water."),
            ("Sago Cake", "A pale, chewy cake pounded from sago palm pith and sun-dried on a rack."),
        ],
        [(FoodType.Grain, Terrain.Swamp, true)] = [
            ("Cattail Root", "A starchy root pulled from the shallows, peeled and eaten raw or boiled."),
            ("Wild Rice", "Dark grains of wild rice harvested from standing water and parched over flame."),
            ("Lotus Stem", "A crisp, hollow stem snapped from a marsh lotus and sliced into rounds."),
        ],

        // ── Forest ──

        [(FoodType.Grain, Terrain.Forest, false)] = [
            ("Acorn Bread", "A nutty brown loaf made from leached acorn flour and forest honey."),
            ("Nut Loaf", "A dense brick of ground hazelnuts and oats pressed into a travelling loaf."),
            ("Rye Hardtack", "A square of dark rye cracker, virtually indestructible."),
        ],
        [(FoodType.Grain, Terrain.Forest, true)] = [
            ("Acorns", "A pouch of shelled acorns soaked and roasted until the bitterness fades."),
            ("Wild Onion", "Small pungent bulbs dug from the forest floor, best roasted in ash."),
            ("Burdock Root", "A long, earthy root scrubbed clean and sliced thin for chewing."),
        ],

        // ── Mountains ──

        [(FoodType.Grain, Terrain.Mountains, false)] = [
            ("Buckwheat Cake", "A heavy, dark cake of buckwheat groats baked with rendered fat."),
            ("Dense Hardtack", "A thick biscuit that could stop a knife, softened only by strong tea."),
            ("Stone Bread", "A round loaf baked in a stone oven until the crust rings when knocked."),
        ],
        [(FoodType.Grain, Terrain.Mountains, true)] = [
            ("Lichen", "Pale rock lichen scraped from high boulders and boiled into a tasteless paste."),
            ("Pine Bark", "The soft inner bark of a mountain pine, stripped and chewed for its starch."),
            ("Mountain Sorrel", "Tart green leaves gathered from alpine meadows, eaten fresh by the handful."),
        ],

        // ══════════════════════════════════════════
        // SWEETS
        // ══════════════════════════════════════════

        // ── Plains ──

        [(FoodType.Sweets, Terrain.Plains, false)] = [
            ("Honeycomb", "A dripping chunk of wild honeycomb wrapped in cloth, golden and fragrant."),
            ("Dried Apricots", "Soft, wrinkled apricots dried in the summer sun until deeply sweet."),
            ("Molasses Candy", "Dark, sticky blocks of boiled molasses rolled in coarse sugar."),
        ],
        [(FoodType.Sweets, Terrain.Plains, true)] = [
            ("Wild Clover", "A bunch of sweet clover blossoms chewed for their mild honey flavor."),
            ("Hedge Berries", "Small purple berries picked from a hedgerow, sweet with a sharp finish."),
            ("Crab Apples", "Tart little apples best eaten after the first frost softens them."),
        ],

        // ── Scrub ──

        [(FoodType.Sweets, Terrain.Scrub, false)] = [
            ("Date Paste", "A thick, dark paste of mashed dates pressed into a clay pot."),
            ("Fig Leather", "Sheets of dried fig pulp rolled thin and cut into strips for the road."),
            ("Tamarind Chew", "Sticky nuggets of tamarind pulp dusted with sugar and salt."),
        ],
        [(FoodType.Sweets, Terrain.Scrub, true)] = [
            ("Dried Cactus Fruit", "Leathery slices of sun-dried cactus fruit, intensely sweet and chewy."),
            ("Bitter Figs", "Small, dark figs from a wild tree, sweet beneath their bitter skin."),
            ("Ant Honey", "A waxy lump of honeypot ant nectar dug from a desert nest."),
        ],

        // ── Swamp ──

        [(FoodType.Sweets, Terrain.Swamp, false)] = [
            ("Cane Sugar Block", "A rough-cut block of unrefined cane sugar, brown and crumbly."),
            ("Candied Ginger", "Knobs of swamp ginger cooked in sugar syrup until translucent."),
            ("Berry Jam", "A small clay pot of thick dark jam made from marsh berries and cane sugar."),
        ],
        [(FoodType.Sweets, Terrain.Swamp, true)] = [
            ("Swamp Berries", "Clusters of tart red berries gathered from bushes at the water's edge."),
            ("Wild Ginger", "A knobby root pulled from wet soil, fiery and sweet when chewed raw."),
            ("Sugar Reed", "A length of marsh cane peeled and chewed for its sweet, watery juice."),
        ],

        // ── Forest ──

        [(FoodType.Sweets, Terrain.Forest, false)] = [
            ("Wild Berry Preserve", "A small jar of forest berries cooked down with sugar into a thick preserve."),
            ("Maple Candy", "Hard amber drops of boiled maple sap that dissolve slowly on the tongue."),
            ("Hazelnut Paste", "Ground hazelnuts and honey worked into a rich, grainy paste."),
        ],
        [(FoodType.Sweets, Terrain.Forest, true)] = [
            ("Blackberries", "Fat, dark berries picked from thorny brambles deep in the wood."),
            ("Hazelnuts", "A pouch of wild hazelnuts cracked open and eaten by the handful."),
            ("Maple Sap", "A stoppered flask of raw maple sap, thin and faintly sweet."),
        ],

        // ── Mountains ──

        [(FoodType.Sweets, Terrain.Mountains, false)] = [
            ("Pine Nut Brittle", "A slab of caramelized sugar studded with toasted pine nuts."),
            ("Dried Cherry", "Wizened mountain cherries dried until they are dark and intensely sweet."),
            ("Heather Honey", "A pot of thick, floral honey gathered from high alpine heather fields."),
        ],
        [(FoodType.Sweets, Terrain.Mountains, true)] = [
            ("Juniper Berries", "Dusty blue berries plucked from a mountain juniper, bitter and aromatic."),
            ("Wild Thyme", "A fragrant bundle of flowering thyme chewed more for comfort than nourishment."),
            ("Pine Nuts", "Tiny, rich nuts shaken from mountain pine cones and eaten raw."),
        ],
    };
}
