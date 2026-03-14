namespace Dreamlands.Rules;

public sealed partial class HaulDef
{
    static IEnumerable<HaulDef> Scrub() => new HaulDef[]
    {
        new()
        {
            Id = "caravan_spice_pouch",
            Name = @"Caravan Spice Pouch",
            OriginBiome = "scrub",
            DestBiome = "plains",
            OriginFlavor = @"A caravan outrider died in the pass last month. His brother is selling off the travel gear piece by piece rather than keep reminders.",
            DeliveryFlavor = @"The cook takes the pouch, loosens the drawstring, and tips a pinch of spice onto her palm. She tastes it, nods once, then turns and shakes half the contents into a pot already simmering over the fire. The kitchen smells like cumin and dried chilies. She counts out your payment between stirring.",
        },
        new()
        {
            Id = "hammered_copper_armbands",
            Name = @"Hammered Copper Armbands",
            OriginBiome = "scrub",
            DestBiome = "swamp",
            OriginFlavor = @"A researcher at the survey office acquired the armbands from a plateau burial site. She's arranged a sale to fund more digs, but wants them catalogued first by someone who might recognize the maker's style.",
            DeliveryFlavor = @"You hand over the armbands in the village square, where a circle of elders has already gathered. The oldest woman turns them over slowly, tracing the hammered patterns with her fingertip. ""Pre-displacement work,"" she says. ""The geometry is ours."" The others murmur agreement, disagreement, uncertainty—no one is certain, but everyone has a theory. The woman pays you and sets the armbands on a cloth while the debate continues.",
        },
        new()
        {
            Id = "perfumed_resin_cakes",
            Name = @"Perfumed Resin Cakes",
            OriginBiome = "scrub",
            DestBiome = "mountains",
            OriginFlavor = @"A clan elder is sending the cakes to a territorial magistrate up in the peaks—there's a border dispute brewing, and nothing oils the wheels of adjudication like a gift that arrives before the formal petition.",
            DeliveryFlavor = @"You hand over the wrapped cakes to the magistrate in his drafty chambers. He unwraps one carefully, breathes in the scent, and his shoulders drop. ""Haven't smelled this since I was posted in the lowlands twenty years back,"" he says quietly. He offers you tea, which you accept, and he pays while the water boils.",
        },
        new()
        {
            Id = "exquisite_kaftan",
            Name = @"Exquisite Kaftan",
            OriginBiome = "scrub",
            DestBiome = "mountains",
            OriginFlavor = @"The clan weaver who made this piece says it's cursed — her daughter died the week she finished it and no one local will touch it. A mountain merchant agreed to take it off her hands.",
            DeliveryFlavor = @"The tailor holds it up to the window light and examines the stitching at each seam, then flips it to study the interior joins. ""Plateau work. Single-thread buttonholes, see? And the dye — safflower, I'd say, with madder for depth."" She folds it carefully and sets coins on the table without comment.",
        },
        new()
        {
            Id = "rail_line_survey",
            Name = @"Rail Line Survey",
            OriginBiome = "scrub",
            DestBiome = "scrub",
            OriginFlavor = @"A Kesharat surveyor quietly sold his notes before the official filing — the clans want to know where the rails are heading before construction starts.",
            DeliveryFlavor = @"You hand over the survey and the clan elder spreads the folded pages across a low table, tracing the proposed route with one finger. He stops at a narrow pass, taps it twice, then looks up at his nephew. ""Send word to Zarik. His wells are in the path."" He folds the papers carefully and pays you from a leather pouch without another word.",
        },
        new()
        {
            Id = "jade_slab",
            Name = @"Jade Slab",
            OriginBiome = "scrub",
            DestBiome = "plains",
            OriginFlavor = @"A clan elder offloaded the slab quietly after a Kesharat surveyor asked too many questions about where it came from. Better to move it now than explain later.",
            DeliveryFlavor = @"You hand over the jade to a woman in a merchant's coat who immediately turns it over and studies the back longer than the front. She wets her thumb and rubs at something you can't see, then wraps it carefully in oilcloth without comment. She counts out your payment in small coins, each one placed deliberately on the table between you, her eyes never quite meeting yours.",
        },
        new()
        {
            Id = "signal_tower_clockwork_core",
            Name = @"Signal Tower Clockwork Core",
            OriginBiome = "scrub",
            DestBiome = "plains",
            OriginFlavor = @"The clockwork came out of a decommissioned signal tower on the old imperial rail line. A Kesharat maintenance clerk sold it as surplus after logging the tower's formal closure.",
            DeliveryFlavor = @"You arrive at the garrison outpost to find a dozen scavengers already crowded around the engineer's table, organizing a dozen or so mechanical oddities. The engineer takes the core from you and turns it over twice, checking the maker's stamp. ""Pre-occupation manufacture. Good."" She counts out your payment while two scavengers immediately start negotiating to buy it from her.",
        },
        new()
        {
            Id = "the_lattice_an_introduction",
            Name = @"The Lattice, An Introduction",
            OriginBiome = "scrub",
            DestBiome = "mountains",
            OriginFlavor = @"A Kesharat surveyor died in the field last month, and his effects are being distributed according to procedure. The textbook goes to a mountain scholar who requested it by catalog number.",
            DeliveryFlavor = @"You hand over the book at the acquisitions desk. The clerk opens to the title page, checks it against a requisition form, then stamps three different boxes on a yellow chit. She slides the book into a cloth sleeve, writes a call number on the spine label, and sets it in a cart with two dozen others. ""Countersign here,"" she says, turning the logbook toward you. She counts out your payment while you write.",
        },
        new()
        {
            Id = "stamped_rail_spikes",
            Name = @"Stamped Rail Spikes",
            OriginBiome = "scrub",
            DestBiome = "plains",
            OriginFlavor = @"The Kesharat depot has excess stock from their latest shipment. A plains rail contractor put in a standing order months ago.",
            DeliveryFlavor = @"The foreman opens the crate and pulls one spike free. He holds it up to the light, squinting at the stamp. ""These are narrow-gauge. We're laying standard."" He tosses it back in the crate and crosses his arms. You point to the requisition number on the manifest. He sighs, checks his ledger, and nods. ""Clerk error. We'll make it work."" He pays you and waves over two workers to haul the crate away.",
        },
        new()
        {
            Id = "kesharat_robe",
            Name = @"Kesharat Robe",
            OriginBiome = "scrub",
            DestBiome = "mountains",
            OriginFlavor = @"A Kesharat archive curator acquired the robe from an estate sale — she says it predates the Lattice influence and wants to document pre-colonial administrative dress.",
            DeliveryFlavor = @"The scholar takes it from you and immediately unfolds it across her worktable, comparing the weave pattern against an open manuscript. She mutters something about dye fastness and pulls a magnifying lens from her pocket. Without looking up, she slides your payment across the table with two fingers.",
        },
        new()
        {
            Id = "confiscated_clan_banner",
            Name = @"Confiscated Clan Banner",
            OriginBiome = "scrub",
            DestBiome = "scrub",
            OriginFlavor = @"A Kesharat clerk confiscated the banner during a dispute over well rights, but the paperwork never quite made it into official records. Selling it back through intermediaries keeps everyone's hands clean.",
            DeliveryFlavor = @"You unfurl the banner in the clan elder's courtyard and three generations crowd in to see. An older woman touches the embroidered corner and names the weaver who made it. The elder examines where the dye has faded along the fold lines, then nods once. ""It was taken in my father's time."" He counts out your payment while his grandson carefully refolds the silk.",
        },
        new()
        {
            Id = "unusual_coins",
            Name = @"Unusual Coins",
            OriginBiome = "scrub",
            DestBiome = "plains",
            OriginFlavor = @"A clan elder's granddaughter found the coins in a cedar chest after he passed. They meant something to him once, but no one remembers what, and she needs the silver more than the mystery.",
            DeliveryFlavor = @"The collector spreads them on his desk and tilts each one under the lamp. He scratches one with his thumbnail, weighs another in his palm. ""Pre-reform mintage. Probably Kesharat, before the rail."" He counts out your payment in modern coin and sweeps the old ones into a velvet pouch.",
        },
        new()
        {
            Id = "alignment_rod",
            Name = @"Alignment Rod",
            OriginBiome = "scrub",
            DestBiome = "plains",
            OriginFlavor = @"A Kesharat surveyor requisitioned a set of alignment rods for the new rail expansion, but only needs three of the four. The spare is being sold to a plains engineering office.",
            DeliveryFlavor = @"You hand over the rod at the municipal works office. The clerk measures it against a standard bar mounted to the wall, checks the graduations with a magnifying lens, then enters the specifications into a ledger. He stamps your receipt twice—once for delivery, once for inventory—and counts out your payment while his assistant files the rod in a narrow cabinet with dozens of identical slots.",
        },
        new()
        {
            Id = "colorless_crystal_node",
            Name = @"Colorless Crystal Node",
            OriginBiome = "scrub",
            DestBiome = "mountains",
            OriginFlavor = @"A Kesharat surveyor requisitioned the wrong kind of crystal for their instruments — the node arrived entirely clear instead of calibrated for spectral readings. They've resold it to a mountain institution that apparently has use for untuned specimens.",
            DeliveryFlavor = @"The scholar holds the crystal up to the window light, rotating it slowly while squinting at the internal structure. She taps it twice with a brass rod, listening to the tone, then sets it into a velvet-lined case alongside three others. ""Flawless lattice. No inclusions. Exactly what we need for the refraction series."" She counts out your payment without looking away from the crystal.",
        },
        new()
        {
            Id = "worker_identification_band",
            Name = @"Worker Identification Band",
            OriginBiome = "scrub",
            DestBiome = "mountains",
            OriginFlavor = @"A Kesharat clerk was caught selling worker bands to non-workers. The bands were confiscated and are being liquidated through proper channels, though the Administration would prefer they simply disappeared.",
            DeliveryFlavor = @"The foreman turns the band over in his hands, reading the stamped number twice. His jaw tightens. ""This was Daren's. He went down in the collapse two years back."" He closes his fist around it for a long moment, then tucks it into his coat pocket and counts out your payment in silence.",
        },
        new()
        {
            Id = "desert_glass_pendant",
            Name = @"Desert Glass Pendant",
            OriginBiome = "scrub",
            DestBiome = "mountains",
            OriginFlavor = @"The pendant belonged to a Kesharat surveyor who died mapping the flats. His effects were sold off by the local administrator to settle outstanding lodging debts.",
            DeliveryFlavor = @"The man who meets you wears scholar's robes but his hands are calloused like a miner's. He turns the pendant over twice, looking at something on the back, then closes his fist around it and breathes out slowly. ""She said she lost this in the desert."" He pays you and walks away without another word.",
        },
        new()
        {
            Id = "clan_genealogy_scroll",
            Name = @"Clan Genealogy Scroll",
            OriginBiome = "scrub",
            DestBiome = "scrub",
            OriginFlavor = @"A clan elder commissioned the scroll to settle a water rights dispute—three families claim descent from the well-keeper, and only documented lineage will hold in front of the council.",
            DeliveryFlavor = @"You hand over the scroll and the adjudicator unrolls it immediately, spreading it across the stone table with his palms. He traces one branch with his finger, mutters something to his clerk, then circles a name with charcoal. He pays you without looking up, already calling for the first claimant.",
        },
        new()
        {
            Id = "woven_saddlebag",
            Name = @"Woven Saddlebag",
            OriginBiome = "scrub",
            DestBiome = "scrub",
            OriginFlavor = @"The bag's weave pattern marks it as clan work, but it was sold quietly—kinship obligations make outright sales complicated, and the weaver would rather avoid questions.",
            DeliveryFlavor = @"The recipient turns the bag over in her hands, checking the reinforced corners and the double-stitched straps. She loops it over her shoulder to test the weight distribution, then nods. ""My cousin's work. I thought so."" She presses a cup of mint tea into your hands before she counts out your payment, insisting you drink before the dust takes you back on the road.",
        },
        new()
        {
            Id = "roll_of_tanned_goatskins",
            Name = @"Roll of Tanned Goatskins",
            OriginBiome = "scrub",
            DestBiome = "scrub",
            OriginFlavor = @"A Kesharat scholar requested samples of local tanning work to compare regional techniques. The processor wrapped the best examples from three different clans.",
            DeliveryFlavor = @"You hand the roll to a young clerk at the outpost, who signs for it without unrolling the skins. ""Scholar Qiren ordered this last month. He's at the northern survey station now."" She marks the ledger and slides your payment across the desk, then carries the bundle to a back room already stacked with crates and parcels waiting for collection.",
        },
        new()
        {
            Id = "copper_wire_coil",
            Name = @"Copper Wire Coil",
            OriginBiome = "scrub",
            DestBiome = "scrub",
            OriginFlavor = @"A Kesharat rail station needs wire to repair a section of their telegraph line. Supply requisitions take weeks, so the stationmaster arranged a private purchase.",
            DeliveryFlavor = @"You hand over the coil and the engineer immediately unspools a length across his workbench, bending it sharply twice to test for brittleness. He grunts, satisfied, and is already walking toward the telegraph poles with the wire over his shoulder when his clerk steps forward to count out your payment.",
        },
        new()
        {
            Id = "nomad_s_repair_kit",
            Name = @"Nomad's Repair Kit",
            OriginBiome = "scrub",
            DestBiome = "scrub",
            OriginFlavor = @"A father is sending the repair kit to his son who left the clan three years back after a feud. It's the sort of gift that doesn't require words.",
            DeliveryFlavor = @"You hand over the kit and the young man opens it slowly, checking each tool against the light—awl, needles, waxed thread, a small leather punch. He tests the tension on a pair of pliers, then sets everything back in its slots. He pays you and tucks the kit under his arm without looking up.",
        },
        new()
        {
            Id = "box_of_mesa_ochre",
            Name = @"Box of Mesa Ochre",
            OriginBiome = "scrub",
            DestBiome = "scrub",
            OriginFlavor = @"A clan elder passed last summer and left behind pigments he'd gathered from the old mesa shrines. His granddaughter wants them used to mark her brother's coming-of-age, the way their grandfather would have done.",
            DeliveryFlavor = @"The boy is already seated on a low stool when you arrive, bare-chested, while his sister unwraps the ochre. She wets a finger, tests the color on her wrist, then begins tracing the first line down his shoulder. The pattern is halfway complete before she glances up and tells you where to find your pay.",
        },
        new()
        {
            Id = "camel_hair_blanket",
            Name = @"Camel-Hair Blanket",
            OriginBiome = "scrub",
            DestBiome = "swamp",
            OriginFlavor = @"The seller says the blanket belonged to someone who left in the night, and she'd rather see it gone than keep it around. She didn't offer more and we didn't ask.",
            DeliveryFlavor = @"The woman who receives it unfolds it across her lap, then stops. ""This isn't camel hair. Feel the weave — it's goat, maybe sheep."" She looks at you like you've tried something. You pull out the contract and she reads it twice, lips tight. Finally she pays, but folds the blanket with the kind of care that says she'll be writing someone a letter.",
        },
        new()
        {
            Id = "bundle_of_dried_sage",
            Name = @"Bundle of Dried Sage",
            OriginBiome = "scrub",
            DestBiome = "swamp",
            OriginFlavor = @"An elder's funeral rites were completed, and his family is sending the remainder of his ceremonial sage to the lowlands. His daughter says he would have wanted it used, not buried.",
            DeliveryFlavor = @"You hand over the bundle and the woman unwraps a corner, bringing it close to her face. She breathes in once, nods, and reties the cord. She counts out your payment while her daughter carries the sage toward the back room where the drying racks are.",
        },
        new()
        {
            Id = "carved_meerschaum_pipes",
            Name = @"Carved Meerschaum Pipes",
            OriginBiome = "scrub",
            DestBiome = "swamp",
            OriginFlavor = @"A plateau merchant's stock of carved meerschaum pipes has been sitting too long in the dry air—the stone is starting to crack. He's arranged a sale to a swamp dealer who says the humidity will stabilize them.",
            DeliveryFlavor = @"The old Revathi opens the case and lifts one pipe with both hands, turning it slowly in the light. His thumb traces the carved pattern—geometric, intricate, nothing like the work here. He sets it down carefully and picks up another, then another, his breathing shallow. When he finally speaks, his voice is quiet: ""My father made pipes like these. Different stone, same designs."" He counts out your payment in small coins, never looking up from the box.",
        },
        new()
        {
            Id = "101_kesharat_recipes",
            Name = @"101 Kesharat Recipes",
            OriginBiome = "scrub",
            DestBiome = "plains",
            OriginFlavor = @"An imperial scholar living among the Kesharat compiled these recipes as a gift for her hosts. The locals have little interest in the volume, but we found a buyer in the heartlands.",
            DeliveryFlavor = @"You hand the book to the innkeeper, but three of his regulars immediately crowd the bar to look. One flips through and stops at a page. ""That's not how you make proper spice paste,"" he mutters. The innkeeper ignores him, counts out your payment, and tucks the book under the counter before the argument can properly start.",
        },
        new()
        {
            Id = "bolt_of_mesa_cotton",
            Name = @"Bolt of Mesa Cotton",
            OriginBiome = "scrub",
            DestBiome = "plains",
            OriginFlavor = @"The merchant who sold us the bolt wouldn't say where he got it, and the dye pattern doesn't match any local clan work. We chose discretion.",
            DeliveryFlavor = @"You hand over the bolt and the weaver unrolls a full arm's length across her table, holding the fabric up to the window light. She runs her fingers across the weave, checking the thread count, then folds a corner and tests the give. ""Mesa cotton,"" she says flatly. ""Hasn't been on the market in three years."" She rolls it back up and pays you in old silver.",
        },
        new()
        {
            Id = "brass_tea_service",
            Name = @"Brass Tea Service",
            OriginBiome = "scrub",
            DestBiome = "mountains",
            OriginFlavor = @"A clan elder's wife died last spring. The tea service was hers, brought from the lowlands when she married, and he can't quite bring himself to use it anymore.",
            DeliveryFlavor = @"The miner's daughter unwraps the cloth and sets each piece on the wooden table. She lifts the pot, turns it slowly, sets it down. Her father stands in the doorway watching. She looks at him. He nods once. She counts out the payment in silence.",
        },
        new()
        {
            Id = "clay_incense_burner",
            Name = @"Clay Incense Burner",
            OriginBiome = "scrub",
            DestBiome = "swamp",
            OriginFlavor = @"A plateau merchant is selling off temple goods with no explanation of how he acquired them. The incense burner still smells faintly of sandalwood.",
            DeliveryFlavor = @"You deliver the burner to a stilt-house where three Revathi elders are already waiting. The oldest turns it over in her hands, tracing the pattern with one fingertip, then shows it to the others. They murmur in a language you don't recognize. One nods. Another shakes his head. The eldest sets coins on the table without looking at you—they're still arguing when you leave.",
        },
        new()
        {
            Id = "sandstone_carving",
            Name = @"Sandstone Carving",
            OriginBiome = "scrub",
            DestBiome = "swamp",
            OriginFlavor = @"The carving sat in a clan elder's house for three generations before his grandson sold it to pay a debt. The sandstone shows fingerprints worn smooth in places someone used to grip it.",
            DeliveryFlavor = @"You hand over the wrapped carving and the old woman turns it slowly in her hands, tracing the worn grooves with her fingertips. Her daughter watches from the doorway as she sets it on the wooden shelf beside a cracked tile fragment. She pays you without counting the coins, her attention already elsewhere.",
        },
        new()
        {
            Id = "signal_mirrors",
            Name = @"Signal Mirrors",
            OriginBiome = "scrub",
            DestBiome = "swamp",
            OriginFlavor = @"A Kesharat functionary ordered the mirrors but won't say what they're for. The requisition form lists them as ""signaling equipment,"" but the destination makes no sense for standard relay work.",
            DeliveryFlavor = @"The woman waiting at the dock unwraps the first mirror and gazes into it, tilting her head this way and that. Satisfied, she hands you the payment. ""The young men will love these, they are so fussy with their appearance.""",
        },
        new()
        {
            Id = "sun_dried_brick_mold",
            Name = @"Sun-Dried Brick Mold",
            OriginBiome = "scrub",
            DestBiome = "swamp",
            OriginFlavor = @"A mason's daughter is moving to the swamp to marry an Revathi man. Her father commissioned the mold—a gift so she can build in the old way, even where clay dries slow and strange.",
            DeliveryFlavor = @"The man who takes the mold from you is young, maybe her age. He turns it over in his hands, testing the weight, tracing the beveled edges with his thumb. He glances toward a stilt-house where someone is watching from the doorway. He sets coins in your palm without counting them aloud.",
        },
        new()
        {
            Id = "pouch_of_mesa_saffron",
            Name = @"Pouch of Mesa Saffron",
            OriginBiome = "scrub",
            DestBiome = "forest",
            OriginFlavor = @"A local's daughter is fled to the exiles camps in the forest. He worries she misses home cooking, and wants to send her a pouch of saffron.",
            DeliveryFlavor = @"You find her in a shared cabin near the tree line. She opens the pouch and pinches a few threads between her fingers, holding them up to the light. ""He sent the autumn harvest."" She closes her eyes briefly, then pays you and turns back to her workbench without another word.",
        },
        new()
        {
            Id = "bolt_of_woven_goat_hair",
            Name = @"Bolt of Woven Goat Hair",
            OriginBiome = "scrub",
            DestBiome = "forest",
            OriginFlavor = @"A dye-house ran out of prepared fiber two weeks ago and their standing orders won't wait. The bolt is rough-woven, undyed, ready for immediate work.",
            DeliveryFlavor = @"You find the dyer outside her workshop, hands already stained blue to the wrists. She unrolls a length of the goat hair and nods approvingly at the weave. ""You've saved me three days of delay."" She brings you a cup of pine tea while she counts out your payment, still smiling.",
        },
        new()
        {
            Id = "tin_of_ground_cumin",
            Name = @"Tin of Ground Cumin",
            OriginBiome = "scrub",
            DestBiome = "forest",
            OriginFlavor = @"Someone bought this in bulk and now needs it gone quickly — no story about where it came from, just a price too good to ask questions about.",
            DeliveryFlavor = @"The exile takes the tin from your hand and twists the lid open. She tilts it toward her face, closes her eyes briefly, then seals it again. She counts out your payment in mixed coin and turns back to her fire without another word.",
        },
        new()
        {
            Id = "jar_of_rendered_tallow",
            Name = @"Jar of Rendered Tallow",
            OriginBiome = "scrub",
            DestBiome = "forest",
            OriginFlavor = @"A clan grandmother is settling her affairs and wants a jar of her family's tallow recipe sent to her nephew in the forest. She says he'll know what it means.",
            DeliveryFlavor = @"The woodsman cracks the seal and dips two fingers into the rendered fat. He works it between his thumb and fingertips, testing the texture, then rubs it into the blade of his hatchet in long, even strokes. The steel darkens with the coating. He sets three coins on the stump beside him and goes back to his work.",
        },
        new()
        {
            Id = "kesharat_abacus",
            Name = @"Kesharat Abacus",
            OriginBiome = "scrub",
            DestBiome = "forest",
            OriginFlavor = @"A Kesharat clerk retired after thirty years of ledger work. His counting frame still functions perfectly, though the lacquer has worn smooth where his fingers pressed the beads.",
            DeliveryFlavor = @"You meet a young woman at the edge of the settlement who accepts the package and unwraps it carefully. She tests each bead, sliding them back and forth, checking for binding. ""My uncle ordered this weeks ago,"" she says. ""He's upstream at the lumber camp."" She wraps it again and tucks it under her arm, then counts out your payment from a pouch at her belt.",
        },
        new()
        {
            Id = "date_tree_sapling",
            Name = @"Date Tree Sapling",
            OriginBiome = "scrub",
            DestBiome = "forest",
            OriginFlavor = @"A clan elder wants the sapling delivered to his exiled nephew. He says nothing about forgiveness, but the tree was always meant to be the boy's.",
            DeliveryFlavor = @"You find the recipient at the edge of a clearing, kneeling beside three other saplings already planted in a precise line. He doesn't look up when you arrive. He takes the sapling, unwraps the root ball, and sets it in the last hole without a word. When he finally stands to pay you, his hands are shaking.",
        },
        new()
        {
            Id = "packet_of_dye_powder",
            Name = @"Packet of Dye Powder",
            OriginBiome = "scrub",
            DestBiome = "forest",
            OriginFlavor = @"A dye merchant's stockpile is running low on a particular shade, and the local scrub lichen produces the exact pigment his clients want.",
            DeliveryFlavor = @"The dyer opens the packet and pinches a small amount between her fingers, rubbing it to test the texture. She drops a measure into a clay bowl of water and watches how it disperses, then holds it to the light filtering through the canopy. ""Good concentration. Clean harvest."" She wraps the packet carefully and counts out your payment.",
        },
        new()
        {
            Id = "clay_oil_lamp",
            Name = @"Clay Oil Lamp",
            OriginBiome = "scrub",
            DestBiome = "forest",
            OriginFlavor = @"An old clan matriarch passed last month, and her belongings are being dispersed according to her wishes. The lamp goes to a daughter who took exile in the forest years ago.",
            DeliveryFlavor = @"You hand the lamp to a woman sitting outside a half-sunken cabin. She turns it over twice, sniffs the clay, then sets it on a stump and stares at it without blinking for nearly a minute. ""My mother had one like this,"" she says finally, though her voice suggests she's talking to the lamp, not you. She pays and carries it inside without another word.",
        },
    };
}
