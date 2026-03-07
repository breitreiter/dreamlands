namespace Dreamlands.Rules;

public sealed partial class HaulDef
{
    static IEnumerable<HaulDef> Plains() => new HaulDef[]
    {
        new()
        {
            Id = "blank_ledger_book",
            Name = @"Blank Ledger Book",
            OriginBiome = "plains",
            DestBiome = "mountains",
            OriginFlavor = @"The county ordered forty ledgers for the tax assessors but someone wrote ""four hundred"" on the requisition, and now they're selling off the surplus at cost rather than pay for warehouse space.",
            DeliveryFlavor = @"The scholar takes the ledger from you and opens it flat on her desk, running her palm across the binding to test the spine. She nods once, satisfied, then reaches for her purse. ""We always need more of these. The archives go through them faster than anyone plans for."" She counts out your payment and offers you tea before you go, gesturing to a pot already warming by the stove.",
        },
        new()
        {
            Id = "bolt_of_undyed_wool_cloth",
            Name = @"Bolt of Undyed Wool Cloth",
            OriginBiome = "plains",
            DestBiome = "forest",
            OriginFlavor = @"The weaver's guild ran out of stock during the disputes last month, and none of the local merchants will extend credit anymore. They had to source directly from a farm cooperative at twice the usual rate.",
            DeliveryFlavor = @"A girl of maybe fourteen waits at the edge of the settlement, not the woman whose name is on the contract. ""My mother's laid up with fever. I'll take it."" She unfolds a corner of the bolt and checks the weave against the light filtering through the canopy, then nods and produces a purse from her belt. ""She said to pay you and get a receipt.""",
        },
        new()
        {
            Id = "stamped_grain_weights",
            Name = @"Stamped Grain Weights",
            OriginBiome = "plains",
            DestBiome = "plains",
            OriginFlavor = @"The weaver's guild ran out of stock during the disputes last month, and none of the local merchants will extend credit anymore. They had to source directly from a farm cooperative at twice the usual rate.",
            DeliveryFlavor = @"The buyer sets them on his scale one by one, comparing each against his own certified weights. He marks the differences in a ledger with careful notation. ""Off by a quarter-ounce on three of them,"" he says, tapping the page. ""Still useful for rough work."" He pays without comment and slides the weights into a drawer beneath his counter.",
        },
        new()
        {
            Id = "silver_trade_bar",
            Name = @"Silver Trade Bar",
            OriginBiome = "plains",
            DestBiome = "mountains",
            OriginFlavor = @"A merchant's estate is being quietly liquidated before the creditors finish their audit. The bars moved fast, no questions, and you're carrying one toward a buyer who doesn't keep books.",
            DeliveryFlavor = @"You hand the bar to a woman in a wool coat who unwraps it, weighs it in her palm, and nods once. ""My brother sent word you were coming."" She tucks it into her satchel without ceremony, counts out your payment in small coins, and walks back toward the ridge without another word.",
        },
        new()
        {
            Id = "army_surplus_spearheads",
            Name = @"Army Surplus Spearheads",
            OriginBiome = "plains",
            DestBiome = "scrub",
            OriginFlavor = @"The garrison withdrew three months ago and left a cache behind. No one else will buy obsolete military stock, so the scavengers are selling what they can.",
            DeliveryFlavor = @"You hand over the bundle and a dozen men gather around as the headman unwraps the oilcloth. He tests an edge with his thumb, holds one up to the light, checks the tang. ""Socket's wrong for our shafts,"" someone mutters. ""We'll make it work,"" another says. The headman nods and counts out your payment while two younger men are already arguing about the reforging.",
        },
        new()
        {
            Id = "salvaged_signal_lantern",
            Name = @"Salvaged Signal Lantern",
            OriginBiome = "plains",
            DestBiome = "mountains",
            OriginFlavor = @"The garrison commander finally admitted the old signal network isn't coming back online. He's selling off the lanterns piece by piece rather than watch them rust in a warehouse.",
            DeliveryFlavor = @"You arrive at the ridgeline watchtower to find three trappers already there, arguing about whether the thing still works. The watch captain takes the lantern, oils the shutters, tests the mechanism. ""Imperial issue. Good lens."" One of the trappers leans in. ""Needs new oil, new wick—"" The captain cuts him off. ""I know what it needs."" He counts out your payment while the trappers continue their debate.",
        },
        new()
        {
            Id = "cracked_surveyor_s_transit",
            Name = @"Cracked Surveyor's Transit",
            OriginBiome = "plains",
            DestBiome = "mountains",
            OriginFlavor = @"The old transit hasn't worked properly in years, but with the new surveyor dead and his replacement weeks away, someone remembered seeing it gathering dust in a barn loft.",
            DeliveryFlavor = @"The engineer takes the transit from you and sets it on her workbench without a word. She runs her thumb along the cracked arc, then opens the case fully and stares at the inscription inside the lid. Her hand stays there a moment. ""My father's,"" she says quietly. ""I thought it was lost in the valley floods."" She pays you and turns back to the bench.",
        },
        new()
        {
            Id = "vitrified_stone_shards",
            Name = @"Vitrified Stone Shards",
            OriginBiome = "plains",
            DestBiome = "mountains",
            OriginFlavor = @"Someone found these fused to the ground near an old wildlands garrison — lightning strike, maybe, or something from the failed campaign. They've been sitting in a scavenger's collection for years.",
            DeliveryFlavor = @"You hand over the box at the assay office. The clerk opens it, examines one shard briefly, then fills out a requisition form in triplicate. He stamps each copy with practiced efficiency, hands you one along with your payment, and files the rest without looking at the shards again.",
        },
        new()
        {
            Id = "depot_iron_mess_kit",
            Name = @"Depot Iron Mess Kit",
            OriginBiome = "plains",
            DestBiome = "forest",
            OriginFlavor = @"A scavenger turned up a depot mess kit in good condition, still stacked and wrapped. The buyer wanted it immediately and paid double the asking price without haggling.",
            DeliveryFlavor = @"You hand over the kit to a forest woman outside a timber cabin. She opens it carefully, runs her thumb along the spoon's edge, then holds up the canteen and shakes it near her ear. A small crowd gathers—two men and a boy—murmuring about the markings stamped into the metal. One of them says something you don't catch. The woman closes the kit, nods once, and counts out your payment without looking at you again.",
        },
        new()
        {
            Id = "collapsed_tower_bell_fragment",
            Name = @"Collapsed Tower Bell Fragment",
            OriginBiome = "plains",
            DestBiome = "plains",
            OriginFlavor = @"The bell was cast two centuries ago for a tower that fell in the last war. The town council voted to sell it rather than recast it, but the vote was narrow.",
            DeliveryFlavor = @"You hand over the fragment. The buyer turns it in his hands, checking the maker's mark stamped into the bronze. He sets it on his desk with the others—five pieces, all different bells. He counts your payment without comment.",
        },
        new()
        {
            Id = "imperial_road_marker_plaque",
            Name = @"Imperial Road Marker Plaque",
            OriginBiome = "plains",
            DestBiome = "scrub",
            OriginFlavor = @"The old marker from the main road cracked through the center during last season's floods. The replacement was commissioned from a plains foundry and sat in a warehouse until someone remembered to arrange transport.",
            DeliveryFlavor = @"You hand over the plaque to the Kesharat road surveyor, who unfolds a diagram and places the marker against it for comparison. He measures twice with calipers, then shakes his head. ""The bore holes are offset by three finger-widths. This was cast to the old imperial standard, not the revised specification."" He fills out a voucher for your payment anyway, muttering about coordinating with antiquated suppliers.",
        },
        new()
        {
            Id = "grid_regulator_plate",
            Name = @"Grid Regulator Plate",
            OriginBiome = "plains",
            DestBiome = "scrub",
            OriginFlavor = @"The salvagers recovered a crate of grid regulators from an old garrison depot, but only one buyer responded to the listing — some Kesharat clerk who needs it for rail maintenance.",
            DeliveryFlavor = @"You hand over the plate at the requisitions depot. The clerk compares the stamped serial number against his ledger, runs his finger down three columns of text, and marks a box with red ink. He slides a receipt across the counter. ""Sign here. And here."" He files the plate in a numbered drawer without looking at it again.",
        },
        new()
        {
            Id = "untouched_market_coin_chest",
            Name = @"Untouched Market Coin Chest",
            OriginBiome = "plains",
            DestBiome = "mountains",
            OriginFlavor = @"The market master's been holding an emergency reserve chest since the grain shortage scare last spring. Now that the harvest came through strong, he's sending it up to the peaks before winter closes the passes.",
            DeliveryFlavor = @"The clerk who accepts the chest doesn't open it. She sets it on her desk, runs her finger along the seal, then slides it under her workspace without counting the contents. ""We don't actually need this yet,"" she says, ""but we will in eleven weeks when the Goravic petition enters monetary phase."" She pays you and returns to her ledgers.",
        },
        new()
        {
            Id = "golem_memory_cylinder",
            Name = @"Golem Memory Cylinder",
            OriginBiome = "plains",
            DestBiome = "mountains",
            OriginFlavor = @"A scavenger claims he pulled the cylinder from a deactivated golem in the frontier zone, though he won't say which garrison or what became of the rest of it.",
            DeliveryFlavor = @"You hand the cylinder to a woman in scholar's robes, but she barely glances at it. ""For the magistrate,"" she says, tucking it into a leather satchel already packed for the ascent. She counts out your payment and leaves before you can ask anything. Through the window, you watch her join a small party heading up the mountain trail.",
        },
        new()
        {
            Id = "surveyor_s_chain",
            Name = @"Surveyor's Chain",
            OriginBiome = "plains",
            DestBiome = "mountains",
            OriginFlavor = @"The surveyor retired after decades of measuring property lines across the plains, and his son would rather have the coin than keep the chain rusting in a shed.",
            DeliveryFlavor = @"You hand over the chain and the foreman uncoils it across his work table, running his thumb along each link to check for wear. He nods once, counts out your payment in scrip, and turns back to his maps without another word.",
        },
        new()
        {
            Id = "finest_imported_whetstones",
            Name = @"Finest Imported Whetstones",
            OriginBiome = "plains",
            DestBiome = "forest",
            OriginFlavor = @"A merchant bought the stones from a traveling buyer who didn't say where they came from. The quality is exceptional, but the previous owner's mark has been filed off.",
            DeliveryFlavor = @"You hand over the wrapped bundle. The woodsman unwraps it slowly, selects a stone, and tests it against his hatchet blade with three careful strokes. He holds the edge to the light, studying the burr. ""Garnet grit. Deep quarry stone."" He wraps them back up without looking at you. ""I know what these are worth. Here's your coin.""",
        },
        new()
        {
            Id = "roll_of_canvas_sailcloth",
            Name = @"Roll of Canvas Sailcloth",
            OriginBiome = "plains",
            DestBiome = "forest",
            OriginFlavor = @"The sailcloth belonged to a trader's father, who never made it to the coast but kept it folded in his chest anyway. His son finally let it go to someone who might use it.",
            DeliveryFlavor = @"The woodworker already has the canvas spread across his workbench before you've closed the door, running his hands along the weave to check for rot. ""Good. Tight enough."" He's measuring it against a half-built frame in the corner—some kind of pavilion or shade structure. He pays you without looking up, already marking cut lines with charcoal.",
        },
        new()
        {
            Id = "crate_of_tallow_candles",
            Name = @"Crate of Tallow Candles",
            OriginBiome = "plains",
            DestBiome = "forest",
            OriginFlavor = @"The festival starts in three days and someone miscounted the candles for the vigil lanterns. The plains chandler packed them still warm from the molds.",
            DeliveryFlavor = @"You find the vigil keeper standing beside rows of unlit lanterns, each waiting for its candle. She opens the crate and her shoulders drop with relief. She lifts one candle, tests its weight, then begins placing them one by one into the tin frames. ""My daughter's name will be spoken tonight after all,"" she says quietly, and pays you without looking up from her work.",
        },
        new()
        {
            Id = "bundle_of_quill_pens",
            Name = @"Bundle of Quill Pens",
            OriginBiome = "plains",
            DestBiome = "forest",
            OriginFlavor = @"The festival starts in three days and someone miscounted the candles for the vigil lanterns. The plains chandler packed them still warm from the molds.",
            DeliveryFlavor = @"You hand over the bundle to the forester's clerk, who unties the cord and fans the quills across his desk. He picks one up, tests the nib against his thumbnail, and frowns. ""These are goose, not crow. Wrong width for the ledger columns."" He calls over another clerk, and they argue in low voices about whether the contract specified the bird. Eventually he pays you, but keeps two quills back as a penalty.",
        },
        new()
        {
            Id = "bolt_of_dyed_linen",
            Name = @"Bolt of Dyed Linen",
            OriginBiome = "plains",
            DestBiome = "forest",
            OriginFlavor = @"A merchant commissioned the fabric for his daughter's wedding gift, but she called off the engagement and left for the coast. He wants it gone rather than keeping it as a reminder.",
            DeliveryFlavor = @"The recipient unfolds the bolt and holds it up to the light filtering through the canopy. Her face tightens. ""This is rose madder, not sorrel root. The dye's all wrong."" She argues that you've brought the wrong cloth entirely, but the manifest matches what you carry. She pays, but makes it clear she'll be writing a complaint.",
        },
        new()
        {
            Id = "glazed_terracotta_tiles",
            Name = @"Glazed Terracotta Tiles",
            OriginBiome = "plains",
            DestBiome = "forest",
            OriginFlavor = @"The tiles came from a manor house teardown — owner died without heirs, debts settled, everything sold off. The buyer specified these particular tiles, from the cellar room.",
            DeliveryFlavor = @"The builder sets each tile face-down on his workbench and taps the back with a wooden mallet, listening. He flips them over, checks the glaze for hairline cracks with his thumbnail, holds one up to catch the light through the cabin door. ""Good fired clay. No spalting."" He stacks them carefully and counts out your payment without looking up.",
        },
        new()
        {
            Id = "tin_of_rendered_tallow",
            Name = @"Tin of Rendered Tallow",
            OriginBiome = "plains",
            DestBiome = "plains",
            OriginFlavor = @"The renderer filled an order for six tins, but the buyer only wanted five. The math was checked twice, the contract was clear, but somewhere the count went wrong.",
            DeliveryFlavor = @"You hand over the tin and the man pries it open immediately, sniffing once before dipping two fingers into the pale fat. He rubs it between his fingertips for longer than seems necessary, eyes half-closed, then wipes his hand on his trousers. ""Good consistency,"" he says, and pays without looking at you again.",
        },
        new()
        {
            Id = "wheelwright_s_iron_tire",
            Name = @"Wheelwright's Iron Tire",
            OriginBiome = "plains",
            DestBiome = "plains",
            OriginFlavor = @"The wheelwright forged a tire to specification, but the merchant who ordered it sent measurements for a donkey cart instead of his grain hauler. Now he's stuck with expensive ironwork gathering dust.",
            DeliveryFlavor = @"You hand over the tire to a woman at the inn who checks it against a scrap of paper, nods once, and sets it behind the bar. ""My brother will collect it when he passes through next week."" She counts out your payment without comment, already turning back to her ledgers.",
        },
        new()
        {
            Id = "exhaustive_land_deed_research",
            Name = @"Exhaustive Land Deed Research",
            OriginBiome = "plains",
            DestBiome = "plains",
            OriginFlavor = @"Someone at the land registry ordered the full deed history on a property — boundary disputes, every transfer, every annotation going back a century. When it arrived, they realized they'd requested the wrong parcel number entirely.",
            DeliveryFlavor = @"You hand the research to a woman sitting alone at a table in the back of the grain exchange. She doesn't open it. She runs her finger along the sealed edge, then sets it flat on the table and rests both palms on top of it. She sits like that for a long moment. ""Good,"" she says finally, and pays you without looking up.",
        },
        new()
        {
            Id = "notarized_land_survey",
            Name = @"Notarized Land Survey",
            OriginBiome = "plains",
            DestBiome = "plains",
            OriginFlavor = @"A landowner's selling off parcels and hired a surveyor to mark the boundaries properly. The buyer's in another district and wants the notarized copy before funds clear.",
            DeliveryFlavor = @"You hand over the document and she unfolds it on her kitchen table, smoothing the creases with her palm. She traces the boundary lines with one finger, checking the measurements against a letter she pulls from her pocket. ""Good. That matches what we agreed."" She pours you a cup of small beer while she counts out your payment, and you drink it standing.",
        },
        new()
        {
            Id = "barrel_stave_bundle",
            Name = @"Barrel Stave Bundle",
            OriginBiome = "plains",
            DestBiome = "plains",
            OriginFlavor = @"The cooperage needs white oak staves by week's end or the grain buyers will take their casks elsewhere. Harvest season doesn't wait.",
            DeliveryFlavor = @"You arrive to find the cooper's yard already crowded with apprentices sorting lumber. The master pulls three staves from your bundle and checks them for grain, running his thumb along the edge. ""Too green,"" someone mutters from the stack of rejected wood. The cooper ignores him, nods once, and waves you toward the counting house for payment.",
        },
        new()
        {
            Id = "bundle_of_imperial_dispatches",
            Name = @"Bundle of Imperial Dispatches",
            OriginBiome = "plains",
            DestBiome = "plains",
            OriginFlavor = @"The dispatches were pulled from a courier's satchel after he disappeared near the frontier. No one opened them, but someone paid well to have them delivered anyway.",
            DeliveryFlavor = @"You hand over the bundle and the clerk cuts the binding cord with a small knife. He checks each seal against a register, running his thumb along the wax to feel for tampering. One dispatch makes him pause — he holds it up to the window light, studying the paper grain. He sets them all in a locked drawer and counts out your payment without comment.",
        },
        new()
        {
            Id = "brass_compass_housing",
            Name = @"Brass Compass Housing",
            OriginBiome = "plains",
            DestBiome = "scrub",
            OriginFlavor = @"The housing arrived with no return address and a scrawled note reading ""better someone uses it."" The compass mechanism itself was smashed beyond repair, but the brass case is pristine.",
            DeliveryFlavor = @"The Kesharat surveyor turns the housing over in his hands, checking the interior threads with a fingernail. He produces a brass compass mechanism from a drawer and tests the fit three times, rotating it each time to confirm the seal. ""Acceptable,"" he says, setting both pieces on his desk. He counts out your payment in small coins without looking up.",
        },
        new()
        {
            Id = "unusual_medical_kit",
            Name = @"Unusual Medical Kit",
            OriginBiome = "plains",
            DestBiome = "scrub",
            OriginFlavor = @"A garrison physician left the territory without filing transfer papers. His equipment sat in the quartermaster's lockbox until someone decided to sell it rather than let it gather dust.",
            DeliveryFlavor = @"The clan healer unwraps the kit and examines each instrument in turn — probe, bone saw, extraction forceps. She tests the spring tension on a pair of clamps, holds a scalpel to the light to check the edge. ""Military issue. Good steel."" She sets aside three pieces that duplicate what she already owns, but keeps the rest. She pays without haggling.",
        },
        new()
        {
            Id = "copper_still_parts",
            Name = @"Copper Still Parts",
            OriginBiome = "plains",
            DestBiome = "scrub",
            OriginFlavor = @"An old brewer passed without heirs, and his nephews sold the equipment off piecemeal rather than learn the trade themselves.",
            DeliveryFlavor = @"A thin man in dusty robes accepts the parts and immediately hands them to a younger woman waiting behind him. She turns the coil over in her hands, checking the seams. ""Good enough,"" she says. ""My cousin's been waiting two months."" The man counts out your payment while she wraps the parts back up.",
        },
        new()
        {
            Id = "history_of_the_empire_volume_vi",
            Name = @"History of the Empire Volume VI",
            OriginBiome = "plains",
            DestBiome = "scrub",
            OriginFlavor = @"A local scribe purchased the volume from an estate sale, then changed his mind about keeping it—said he didn't like the way certain passages were annotated in the margins.",
            DeliveryFlavor = @"The old woman takes the book with both hands and opens it to a page near the middle. Her finger traces a line of text, then stops. She closes her eyes for a long moment. ""My father's unit,"" she says quietly. ""They're listed here."" She wraps the book in cloth and pays you without another word.",
        },
        new()
        {
            Id = "iron_bucket_set",
            Name = @"Iron Bucket Set",
            OriginBiome = "plains",
            DestBiome = "scrub",
            OriginFlavor = @"Someone offloaded a set of buckets with no maker's mark and rust already showing at the seams. The price was good enough that no one asked where they came from.",
            DeliveryFlavor = @"You arrive during market day and a dozen plateau traders immediately gather to inspect your delivery. They pass the buckets around, tapping the metal, testing the handles, arguing in their own tongue about whether the iron will last a season or crack in the heat. The buyer finally waves them off and counts out your payment while two others are still debating the quality of the rivets.",
        },
        new()
        {
            Id = "sealed_jar_of_acid",
            Name = @"Sealed Jar of Acid",
            OriginBiome = "plains",
            DestBiome = "swamp",
            OriginFlavor = @"An alchemist's apprentice says the master refused to work with the acid anymore after an accident left scars on both hands. The guild won't buy it back, but someone in the swamp might need it.",
            DeliveryFlavor = @"The Revathi woman sets the jar on a wood plank and tilts it slowly, watching how the liquid moves inside the glass. She opens a leather pouch and counts out coins one at a time, each one placed deliberately on the plank between you. You take the payment and leave her crouched beside the jar, still watching it.",
        },
        new()
        {
            Id = "an_exquisite_comb",
            Name = @"An Exquisite Comb",
            OriginBiome = "plains",
            DestBiome = "swamp",
            OriginFlavor = @"A trader came through with the comb wrapped in plain linen, no questions about provenance. We found a buyer in the swamp who's willing to pay more than it's probably worth.",
            DeliveryFlavor = @"The woman unwraps it slowly, turning it over in her hands. Her fingers trace the carved pattern — flowers that haven't grown in the lowlands for two generations. ""My grandmother had one like this,"" she says quietly. She sets it carefully on the table and pours you tea before she counts out your payment, insisting you finish the cup before you go.",
        },
        new()
        {
            Id = "spool_of_hemp_twine",
            Name = @"Spool of Hemp Twine",
            OriginBiome = "plains",
            DestBiome = "swamp",
            OriginFlavor = @"A fisherman's order — he needs line that won't rot in swamp water. The hemp stock from the plains lasts twice as long as anything local.",
            DeliveryFlavor = @"You hand over the spool and the man pulls several arm-lengths free, testing the lay of the twist between his fingers. He wets a short section with his tongue, watching how the fibers take moisture, then coils it back with practiced speed. ""Good,"" he says, and counts out your payment in small copper.",
        },
        new()
        {
            Id = "tin_of_preservative_paste",
            Name = @"Tin of Preservative Paste",
            OriginBiome = "plains",
            DestBiome = "swamp",
            OriginFlavor = @"The fur buyer's trying to finish a batch before the weather turns. The pelts will rot if they're not treated soon.",
            DeliveryFlavor = @"You hand over the tin and the woman pries it open with her knife, not bothering to look up from the stretched pelt on her workbench. She scoops paste onto two fingers and works it into the hide in long, practiced strokes. ""Good,"" she says, still working. She wipes her hand on her apron and counts out your payment.",
        },
        new()
        {
            Id = "bundle_of_iron_fishhooks",
            Name = @"Bundle of Iron Fishhooks",
            OriginBiome = "plains",
            DestBiome = "swamp",
            OriginFlavor = @"A local smith secured a bulk order from the swamps. The hooks are bundled in oiled cloth to keep them from rusting before delivery.",
            DeliveryFlavor = @"The Revathi woman opens the bundle and picks through the hooks one by one, testing points against her thumbnail and checking the eyes for burrs. She sets aside three with flaws, counts the rest twice, and places your payment on the table without meeting your eyes.",
        },
        new()
        {
            Id = "an_atwali_family_crest",
            Name = @"An Atwali Family Crest",
            OriginBiome = "plains",
            DestBiome = "swamp",
            OriginFlavor = @"The crest was pulled from a collapsed house in the old river valley—someone's grandfather wore it on formal occasions. His grandson thought it should go back to the people who made it, not sit in a box.",
            DeliveryFlavor = @"The elder takes the crest and turns it over slowly, tracing the inscription with one finger. She calls to someone in the next room, and a younger woman appears, already holding a wooden frame half-assembled. They begin fitting the crest into place without looking at you. The elder counts out your payment while the daughter adjusts the backing.",
        },
        new()
        {
            Id = "roll_of_oilcloth",
            Name = @"Roll of Oilcloth",
            OriginBiome = "plains",
            DestBiome = "swamp",
            OriginFlavor = @"A freighter's widow is selling off her husband's trade stock piece by piece. The oilcloth has been in storage for two years now, still good, but she has no use for it.",
            DeliveryFlavor = @"You arrive during a boat repair, and half the village is standing in the shallows arguing about whether the hull needs replacing or just patching. The builder takes the oilcloth, unrolls a section across the damaged planking, and presses it flat with both hands to test the coverage. ""This'll do,"" she says. Three people immediately start debating whether it should be laid in strips or as a single sheet. She pays you while they're still talking.",
        },
        new()
        {
            Id = "imported_woodworking_tools",
            Name = @"Imported Woodworking Tools",
            OriginBiome = "plains",
            DestBiome = "swamp",
            OriginFlavor = @"The tools arrived at the grain cooperative with no sender's mark and a bill of sale signed with initials only. The cooperative head decided it was easier to sell them on than ask questions.",
            DeliveryFlavor = @"The Revathi woodworker opens the wrapped bundle and immediately selects a narrow gouge, testing its edge against a piece of bog-oak he's been shaping into what might be a door panel. He works in silence for several minutes, carving a geometric pattern that matches the fragments of tilework set into his wall. When he finally looks up, he counts out your payment without meeting your eyes.",
        },
    };
}
