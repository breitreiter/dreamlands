namespace Dreamlands.Rules;

public sealed partial class HaulDef
{
    static IEnumerable<HaulDef> Swamp() => new HaulDef[]
    {
        new()
        {
            Id = "bundled_river_reeds",
            Name = @"Bundled River Reeds",
            OriginBiome = "swamp",
            DestBiome = "plains",
            OriginFlavor = @"A basket-weaver's sister is getting married. He cut the reeds himself and bundled them as a gift — she'll weave them into her own wedding basket, the way their mother taught them.",
            DeliveryFlavor = @"You hand the bundle to the bride and she unties the twine to inspect them. She holds one reed up to the light, bends it slightly to test the flex, then runs her fingers along its length checking for splits. ""He cut these at the right time,"" she says. ""Tell him I'll bring the basket when I visit."" She pays you and sets the reeds in a basin of water to keep them supple.",
        },
        new()
        {
            Id = "peat_fuel_brick",
            Name = @"Peat Fuel Brick",
            OriginBiome = "swamp",
            DestBiome = "scrub",
            OriginFlavor = @"Someone has prepaid for a single brick of peat to be delivered to a specific address in the scrublands. The sender left no name and specified the delivery must happen within the week.",
            DeliveryFlavor = @"An old woman opens the door and takes the brick from your hands without a word. She turns it over once, pressing her thumb into the compressed moss, then holds it to her face and breathes in deeply. Her eyes close. When she opens them again, she sets the brick on a small shelf beside three others just like it, pays you exactly what the contract specified, and shuts the door.",
        },
        new()
        {
            Id = "leech_jar",
            Name = @"Leech Jar",
            OriginBiome = "swamp",
            DestBiome = "mountains",
            OriginFlavor = @"Request from one of the scholars up in the mountains. Most likely some sort of strange research project. Not our business to ask questions.",
            DeliveryFlavor = @"The woman who answers the door takes the jar and holds it up to the afternoon light. She unscrews the lid and peers inside, then dips two fingers into the water and lifts out a leech, watching it curl against her knuckle. ""Good,"" she says, though she doesn't look at you when she says it. She sets the jar on a shelf already crowded with similar containers—dozens of them, all writhing. She pays and closes the door before you can ask.",
        },
        new()
        {
            Id = "resin_sealed_waterproof_satchel",
            Name = @"Resin-Sealed Waterproof Satchel",
            OriginBiome = "swamp",
            DestBiome = "plains",
            OriginFlavor = @"A barrister up in the mountains lost a week of notes in a rainstorm. He's demanding someting absolutely waterproof.",
            DeliveryFlavor = @"You hand over the satchel and he submerges it completely in a water trough, holding it under for a full minute. He pulls it out, turns it over slowly, checking every seam and corner for beading or seepage. He opens it, runs his fingers along the dry interior, then nods once. ""Good work."" He counts out your payment and buckles the satchel to his pack without another word.",
        },
        new()
        {
            Id = "embalming_salts",
            Name = @"Embalming Salts",
            OriginBiome = "swamp",
            DestBiome = "mountains",
            OriginFlavor = @"Someone sold us a parcel of blessed embalming salts. Usually the Alwati are pretty uptight about that stuff leaving their borders. Not our problem, but maybe yours if they catch you.",
            DeliveryFlavor = @"You deliver them to a mortuary in one of the company valleys. The undertaker breaks the wax seal on the first jar and rubs a pinch between his fingers, then touches it to his tongue. ""Myrrh base, camphor-cut, proper temple grade."" He checks the rest of the jars by weight alone, nodding as he counts. He pays in coin, not scrip.",
        },
        new()
        {
            Id = "blackwater_dye_vial",
            Name = @"Blackwater Dye Vial",
            OriginBiome = "swamp",
            DestBiome = "plains",
            OriginFlavor = @"The dye-maker's cooperative has a surplus of blackwater extract this season. A plainsman merchant placed a standing order months ago.",
            DeliveryFlavor = @"The cloth merchant unstoppers the vial and tilts a single dark drop onto a scrap of undyed linen. It spreads like ink in water, staining deep purple-black. She nods once, already reaching for her main bolt of fabric, and sets your payment on the counter without looking away from the stain.",
        },
        new()
        {
            Id = "venom_ampoules",
            Name = @"Venom Ampoules",
            OriginBiome = "swamp",
            DestBiome = "mountains",
            OriginFlavor = @"A swamp hunter sold his stock to an Alwati herbalist who doesn't advertise what she does with it. She arranged the sale through a third party.",
            DeliveryFlavor = @"You hand over the wrapped ampoules in a back room behind the assayer's office. The woman unwraps one carefully, holds it to the lamplight, checks the seal. She sets coins on the table and wraps the package again without looking at you. You take the money and leave through the door you came in.",
        },
        new()
        {
            Id = "bog_iron_ingot",
            Name = @"Bog-Iron Ingot",
            OriginBiome = "swamp",
            DestBiome = "mountains",
            OriginFlavor = @"The local smiths gave up bog-iron extraction years ago—too labor-intensive, too unpredictable—but a buyer up in the mountains specifically requested swamp stock, and coin is coin.",
            DeliveryFlavor = @"You present the ingot at the assay office window. The clerk weighs it on a balance scale, scratches a notation in his ledger, then taps the surface twice with a small hammer to check for air pockets. He slides a receipt across the counter along with your payment, already turning to call the next number in line.",
        },
        new()
        {
            Id = "preserved_lotus_resin",
            Name = @"Preserved Lotus Resin",
            OriginBiome = "swamp",
            DestBiome = "plains",
            OriginFlavor = @"The resin merchant has a steady contract with a plains herbalist. Supply runs have been consistent for years.",
            DeliveryFlavor = @"You find the herbalist's house easily enough — the only building with every window shuttered at midday. She cracks the door, takes the wrapped resin without looking at you, and holds it to her nose. Her eyes are completely black, pupils swallowed by dilation. ""Good,"" she says, and slides your payment through the gap before closing the door. You hear three locks slide into place.",
        },
        new()
        {
            Id = "witchfire_lamp",
            Name = @"Witchfire Lamp",
            OriginBiome = "swamp",
            DestBiome = "mountains",
            OriginFlavor = @"The seller didn't say where he got the lamp, and we didn't ask. Says to keep it upright at all times. Says you'll be sorry if you don't.",
            DeliveryFlavor = @"The scholar unwraps the lamp and holds it up to the window light, turning it slowly to examine the glass. She uncaps it and sniffs the reservoir. ""Genuine witchfire oil. Difficult to obtain legally. How did..."" She looks ready to continue, then stops. She sets it on her desk and counts out your payment in silence.",
        },
        new()
        {
            Id = "nightbloom_poison_resin",
            Name = @"Nightbloom Poison Resin",
            OriginBiome = "swamp",
            DestBiome = "scrub",
            OriginFlavor = @"We don't usually deal in poisons, but our buyer is making it worth our while.",
            DeliveryFlavor = @"You meet the buyer in an alley. ""Did any Kesherat dogs follow you, outlander?"" You shake your head; you know discretion. She spits, then counts out your payment in small coins. ""The alley was empty. You took a wrong turn. You saw nothing here.""",
        },
        new()
        {
            Id = "marsh_pearl",
            Name = @"Marsh Pearl",
            OriginBiome = "swamp",
            DestBiome = "plains",
            OriginFlavor = @"A pearl merchant in the deeper swamp had this one rejected by an imperial buyer — insufficient luster. We found a buyer on the plains willing to take it at cost.",
            DeliveryFlavor = @"The woman opens the small cloth packet and goes completely still. She holds the pearl up to the window light, turning it slowly, and her mouth tightens. ""My mother wore one like this,"" she says quietly. ""Before the fever."" She closes her hand around it, then straightens and counts out your payment with careful precision.",
        },
        new()
        {
            Id = "root_head",
            Name = @"Root Head",
            OriginBiome = "swamp",
            DestBiome = "plains",
            OriginFlavor = @"Someone wrapped this carefully and paid extra for discreet transport. The roots are fresh-cut, still damp, and you've been told not to ask what they're for.",
            DeliveryFlavor = @"The woman meets you outside the granary at dusk. She unwraps the root head, turns it over once in her hands, and studies the cut marks where it was severed. Her eyes flick to yours, then away. She tucks it into her apron and counts out your payment in silence.",
        },
        new()
        {
            Id = "jar_of_rendered_fish_oil",
            Name = @"Jar of Rendered Fish Oil",
            OriginBiome = "swamp",
            DestBiome = "plains",
            OriginFlavor = @"The rendering went faster than expected this year, and the buyer's caravan leaves in two days. Better to send it overland now than wait for the next barge and miss the contract window entirely.",
            DeliveryFlavor = @"You hand the jar to the quartermaster. She pries open the jar, sniffs, winkles her face, and seals it again. ""The physician ordered it. Good for coughs he says. You ever heard of such a thing?"" She hands you the payment without waiting for a reply.",
        },
        new()
        {
            Id = "alwati_basket",
            Name = @"Alwati Basket",
            OriginBiome = "swamp",
            DestBiome = "scrub",
            OriginFlavor = @"A Kesharat archivist purchased the basket as a study piece. I hear they're starting some sort of museum.",
            DeliveryFlavor = @"The Kesharat archivist accepts the basket and immediately begins measuring the weave angles with a brass compass, muttering numbers under his breath. He sketches diagrams in a logbook without looking up, his pen moving in quick mechanical strokes. Your payment appears on the desk a moment later, counted out in exact denominations.",
        },
        new()
        {
            Id = "dried_water_lily_roots",
            Name = @"Dried Water Lily Roots",
            OriginBiome = "swamp",
            DestBiome = "scrub",
            OriginFlavor = @"Someone paid good money for water lily roots pulled from a particular stretch of bog. The Alwati who filled the order didn't ask what they're for, but they wrapped the roots in silk before sealing the crate.",
            DeliveryFlavor = @"You find the buyer in a courtyard behind a shuttered house. She unwraps the roots carefully, checking each one against something written on her palm. She counts out your payment in silence, her eyes never quite meeting yours. The coins are still warm from her pocket.",
        },
        new()
        {
            Id = "unsettling_alwati_carving",
            Name = @"Unsettling Alwati Carving",
            OriginBiome = "swamp",
            DestBiome = "scrub",
            OriginFlavor = @"The carving was pulled from a collapsed stilt-house foundation — older than anyone remembers, with marks that don't match any pattern the living use. The seller wouldn't say why he wanted it gone, only that someone in the scrub had asked for exactly this kind of thing.",
            DeliveryFlavor = @"The Kesharat administrator unwraps the carving and sets it on her desk, then pauses. She turns it over twice, checking the base, then pulls out a different wooden figure from her drawer — nearly identical in style. ""This isn't what was ordered. The iconography is inverted."" She studies both pieces for a long moment before paying you, her fingers lingering on the swamp carving as you leave.",
        },
        new()
        {
            Id = "jar_of_wriggling_spawn",
            Name = @"Jar of Wriggling Spawn",
            OriginBiome = "swamp",
            DestBiome = "scrub",
            OriginFlavor = @"A local alchemist is offloading surplus... whatever these are. He was quite insistant that they're safe to transport.",
            DeliveryFlavor = @"You hand the jar to a Kesharat clerk at a waystation checkpoint. She doesn't open it, just signs for it and sets it on a shelf behind her desk. ""Someone will collect it,"" she says, already turning back to her ledger. Through the window you see a clansman watching from across the dusty square, but he doesn't approach.",
        },
        new()
        {
            Id = "certified_tile_specimen",
            Name = @"Certified Tile Specimen",
            OriginBiome = "swamp",
            DestBiome = "plains",
            OriginFlavor = @"The village council voted to sell one of their oldest tiles. Not everyone agreed, but grain stores are low and the offer was high.",
            DeliveryFlavor = @"The man who accepts delivery wears fine clothes but stands in an empty room with bare floorboards. He opens the certificate first, reads it twice, then unwraps the tile and sets it face-down on the floor without looking at the glazed surface. ""And the settlement marks are intact?"" he asks, tracing the back with one finger. You confirm. He pays and immediately wraps it again, still face-down.",
        },
        new()
        {
            Id = "treated_crocodile_hide",
            Name = @"Treated Crocodile Hide",
            OriginBiome = "swamp",
            DestBiome = "mountains",
            OriginFlavor = @"A leatherworker in the mountains put in an order for swamp crocodile — the tanning makes it supple enough to work but keeps the scale texture intact.",
            DeliveryFlavor = @"The woman sets it flat on her workbench and bends a corner between her fingers, testing the give. She holds it up to the light from the window, checking for thin spots or tears in the grain. ""Good,"" she says, and folds it once before setting it with her other stock. She counts out your payment from a tin on the shelf.",
        },
        new()
        {
            Id = "water_levels_report",
            Name = @"Water Levels Report",
            OriginBiome = "swamp",
            DestBiome = "mountains",
            OriginFlavor = @"A scholar of hydrology is compiling seasonal runoff data from lowland watersheds. The Alwati who measure the swamp levels agreed to sell their records.",
            DeliveryFlavor = @"The hydrologist spreads the report flat on her desk, tracing the margin notes with one finger. She looks up at you, her expression softening. ""This is meticulous work. Please, sit — I'll make tea while I prepare your payment."" She returns with both, the coins wrapped in paper inscribed with her thanks.",
        },
        new()
        {
            Id = "swamp_willow_bark_strips",
            Name = @"Swamp Willow Bark Strips",
            OriginBiome = "swamp",
            DestBiome = "mountains",
            OriginFlavor = @"Someone peeled these fresh and packed them carefully in oiled cloth. The sender paid in coin and didn't give a name.",
            DeliveryFlavor = @"The herbalist unwraps the cloth and holds a strip up to the light, checking the color of the inner bark. She scrapes a thumbnail across it, sniffs, then touches it briefly to her tongue. ""Good harvest. Early spring cut."" She counts out the strips one by one, noting the number in a ledger before handing over your payment.",
        },
        new()
        {
            Id = "jar_of_sacred_mud",
            Name = @"Jar of Sacred Mud",
            OriginBiome = "swamp",
            DestBiome = "scrub",
            OriginFlavor = @"The jar passed through three owners before ending up in a merchant's storage. Temple mud from one of the old Alwati sanctuaries, sealed with wax that's cracked and been resealed twice.",
            DeliveryFlavor = @"The Kesharat clerk sets the jar on a ledger and breaks the wax seal to inspect the contents. She takes a sample on a wooden spatula, rubs it between her fingers, and notes the texture in a column you can't read. ""Consistency acceptable. Origin verified."" She stamps the receipt twice, and counts out your payment while the ink is still wet.",
        },
        new()
        {
            Id = "mangrove_root_wood",
            Name = @"Mangrove Root Wood",
            OriginBiome = "swamp",
            DestBiome = "scrub",
            OriginFlavor = @"A clan woodworker is working on some sort of statement piece. Wants to pair local spicewood with swamp wood.",
            DeliveryFlavor = @"The carver sets the pieces on her workbench and runs a finger across the grain, then picks up the darkest piece and scores it lightly with her thumbnail. ""Dense. Old growth, probably fifty years in the bog before harvest."" She flexes it between her hands, testing for weak points that aren't there. She counts out your payment and sets the wood in a locked chest with her other stock.",
        },
        new()
        {
            Id = "bundle_of_dried_cattails",
            Name = @"Bundle of Dried Cattails",
            OriginBiome = "swamp",
            DestBiome = "forest",
            OriginFlavor = @"Someone in the forest placed an order for cattails—a full bundle, not a handful. They specified they needed them dried, not fresh, and paid in advance.",
            DeliveryFlavor = @"The woman who receives them lays three stalks flat on her workbench and splits them lengthwise with a thin blade. She examines the pith, then bends a section until it nearly breaks. She nods once, satisfied with the fiber structure. ""Good,"" she says, and counts your payment from a pouch at her belt.",
        },
        new()
        {
            Id = "jar_of_peat_ash",
            Name = @"Jar of Peat Ash",
            OriginBiome = "swamp",
            DestBiome = "forest",
            OriginFlavor = @"A medicine man packed a jar of ash from the temple hearths. He says it's for wound poultices, but the buyer's letter mentioned other uses.",
            DeliveryFlavor = @"The herbalist uncaps the jar before you've set your pack down and presses a pinch between her fingers. She holds it to her nose, then touches it to her tongue. ""Good concentration. Dark enough."" She's already measuring it into smaller vessels when she gestures toward the coin purse on her table.",
        },
        new()
        {
            Id = "spool_of_reed_cordage",
            Name = @"Spool of Reed Cordage",
            OriginBiome = "swamp",
            DestBiome = "forest",
            OriginFlavor = @"An old weaver says the cordage is payment for shelter given years ago, when she fell ill while travelling through the forest.",
            DeliveryFlavor = @"The woodsman accepts the spool and unwinds a length between his hands, testing the tension. He nods once, sets two coins on the stump between you, and turns back toward his cabin.",
        },
        new()
        {
            Id = "packet_of_water_lily_seeds",
            Name = @"Packet of Water Lily Seeds",
            OriginBiome = "swamp",
            DestBiome = "forest",
            OriginFlavor = @"The seeds came from a nearly extinct lily strain. The grower agreed to sell some of his stock, but only after the village elder spoke on the guild's behalf.",
            DeliveryFlavor = @"You hand over the packet to a woman who opens it immediately and tips the seeds into her palm. She counts them three times, her lips moving silently, then closes her fist around them and stands perfectly still for what feels like a full minute. When she finally looks up, her eyes are wet, though her voice is steady when she pays you.",
        },
        new()
        {
            Id = "flask_of_bog_myrtle_oil",
            Name = @"Flask of Bog Myrtle Oil",
            OriginBiome = "swamp",
            DestBiome = "forest",
            OriginFlavor = @"A healer's apprentice wants the oil sent to her mentor. The girl says her teacher asked for it in a letter.",
            DeliveryFlavor = @"You find the recipient standing outside her cabin with two other forest folk, all three of them examining dried herbs spread on a plank table. She uncorks the flask and holds it under her nose, then passes it to the others. One wrinkles his face. The other nods slowly. ""That's the real thing,"" she says, recorking it carefully. She pays you while the others argue about whether bog myrtle even works.",
        },
        new()
        {
            Id = "alwati_fish_trap",
            Name = @"Alwati Fish Trap",
            OriginBiome = "swamp",
            DestBiome = "forest",
            OriginFlavor = @"A local Alwati craftsman has been making these traps for years. He has a few extra and we've found a buyer.",
            DeliveryFlavor = @"A group of fisherman cluster around you, eager to examine the trap. They flex and twist it and debate how it will work with the local fish. Remembering your presence, one of them thanks and pays you.",
        },
        new()
        {
            Id = "crooked_walking_stick",
            Name = @"Crooked Walking Stick",
            OriginBiome = "swamp",
            DestBiome = "forest",
            OriginFlavor = @"The stick came from deeper than most Alwati go; old growth bog-oak, twisted just so.",
            DeliveryFlavor = @"The exile takes it from you and immediately leans his weight into it, testing the curve against his hip. He walks a tight circle, adjusting his grip twice, then nods. ""Better than the last one."" He's already halfway down the path when he tosses the coins back over his shoulder.",
        },
        new()
        {
            Id = "woven_rush_mat",
            Name = @"Woven Rush Mat",
            OriginBiome = "swamp",
            DestBiome = "forest",
            OriginFlavor = @"An elder is sending the mat to her sister in the forest. They haven't spoken in years, but the weave pattern is one their mother taught them both.",
            DeliveryFlavor = @"The woman who answers the door takes the mat and unrolls it halfway across the threshold. Her fingers find the edge binding, then trace inward to where the pattern shifts from plain to complex. She stands there a long moment, then refolds it carefully and pays you without meeting your eyes.",
        },
        new()
        {
            Id = "vaguely_threatening_carving",
            Name = @"Vaguely Threatening Carving",
            OriginBiome = "swamp",
            DestBiome = "swamp",
            OriginFlavor = @"A fisher in the village pulled this thing from the peat, but his children won't stay in the house with it. He needs someone else to take it off his hands.",
            DeliveryFlavor = @"You hand over the carving to an old woman sitting outside her stilt-house. She unwraps it slowly, traces one finger along the carved lines, then sets it on a cloth beside three others like it. ""I thought these were all gone,"" she says quietly. She brings you tea before she pays you, and doesn't say anything else.",
        },
        new()
        {
            Id = "firefly_lantern",
            Name = @"Firefly lantern",
            OriginBiome = "swamp",
            DestBiome = "swamp",
            OriginFlavor = @"The temple guide needed the lantern two days ago for a night ceremony in the deep complex. The fireflies only glow at full strength for a week after harvest.",
            DeliveryFlavor = @"You hand over the lantern and she lifts it to eye level in the dim light of the stilt-house. The fireflies pulse weakly, their glow already fading to amber. She sets it down without comment and counts out your payment.",
        },
        new()
        {
            Id = "antique_alwati_sword",
            Name = @"Antique Alwati Sword",
            OriginBiome = "swamp",
            DestBiome = "swamp",
            OriginFlavor = @"The owner grievously offended the folk in a nearby village. He's been searching for a gift precious enough to mend the rift, and believes this will do.",
            DeliveryFlavor = @"You hand over the sword and three elders gather to examine it. One tests the balance, another traces the pattern on the crossguard with her fingertip. ""This was carried at the water festivals,"" the third says quietly. They argue in low voices about which family it belonged to, then pay you and wrap it in undyed cloth.",
        },
        new()
        {
            Id = "sack_of_dried_mushrooms",
            Name = @"Sack of Dried Mushrooms",
            OriginBiome = "swamp",
            DestBiome = "swamp",
            OriginFlavor = @"The forager says these are the last from a good patch near an old temple. Blessed. The location is her secret.",
            DeliveryFlavor = @"You hand over the sack to a woman standing knee-deep in the water beneath her stilt-house. She opens it, sniffs once, then upends the entire contents into the murk. The mushrooms float briefly before sinking. She watches them descend, nods to herself, and pays you from a pouch tied to her belt.",
        },
        new()
        {
            Id = "box_of_salvaged_tile_shards",
            Name = @"Box of Salvaged Tile Shards",
            OriginBiome = "swamp",
            DestBiome = "swamp",
            OriginFlavor = @"An old ranger collected these fragments from the swamp. He says someone else should have them now.",
            DeliveryFlavor = @"You hand the box to a woman sitting cross-legged on a platform above the water. She opens it and begins arranging the shards in patterns on the boards, not looking at them, her fingers moving like she's reading something written in the glazework. After several minutes of silence she stops, leaves the fragments scattered, and pays you without explaining what she found.",
        },
        new()
        {
            Id = "coil_of_waxed_fishing_line",
            Name = @"Coil of Waxed Fishing Line",
            OriginBiome = "swamp",
            DestBiome = "swamp",
            OriginFlavor = @"The village's line-maker is sick with fever and no one else knows the work. They're buying from another settlement until the woman recovers.",
            DeliveryFlavor = @"You hand over the coil and the fisherman unwinds an arm's length, holding it taut between his hands. He bends it sharply, watches how it holds the crease, then pulls it across the calloused edge of his palm to test the wax coating. ""This'll do. Ours peels after two days in the water."" He counts out your payment in small coin.",
        },
        new()
        {
            Id = "bag_of_crayfish_seasoning",
            Name = @"Bag of Crayfish Seasoning",
            OriginBiome = "swamp",
            DestBiome = "swamp",
            OriginFlavor = @"A batch came out stronger than expected—enough heat to strip paint. The cook who blended it has no use for three bags' worth and arranged a sale upriver.",
            DeliveryFlavor = @"You find the buyer already standing over a pot of boiling crawfish, and she opens the bag before you've set it down. She pinches some between her fingers, sniffs it, then tosses a handful into the water. The smell hits immediately—sharp ginger and something darker. She nods once and counts out your payment while stirring.",
        },
        new()
        {
            Id = "pot_of_marsh_glue",
            Name = @"Pot of Marsh Glue",
            OriginBiome = "swamp",
            DestBiome = "swamp",
            OriginFlavor = @"A batch came out stronger than expected—enough heat to strip paint. The cook who blended it has no use for three bags' worth and arranged a sale upriver.",
            DeliveryFlavor = @"You hand over the pot to a reed-worker who pries the lid off and dips a thin stick into the amber paste. She smears it between two scraps of bark and presses them flat, counting silently. After twenty breaths she tries to pry them apart, fails, then nods. ""Good batch,"" she says, and pays you from a leather pouch at her hip.",
        },
    };
}
