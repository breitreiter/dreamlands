namespace Dreamlands.Rules;

public sealed partial class HaulDef
{
    static IEnumerable<HaulDef> Forest() => new HaulDef[]
    {
        new()
        {
            Id = "stacked_firewood_bundle",
            Name = @"Stacked Firewood Bundle",
            OriginBiome = "forest",
            DestBiome = "scrub",
            OriginFlavor = @"The forester says the wood came from a tree marked for the sawmill, but it fell the wrong way and landed in a gully. Easier to sell it quiet than explain the loss to the tallyman.",
            DeliveryFlavor = @"The clan matriarch crouches beside the bundle and pulls a single log free. She turns it in her hands, checking the grain, then scrapes at the bark with her thumbnail and sniffs the pale wood beneath. ""Green cut. Maybe two weeks."" She nods to her nephew, who counts out your payment. ""We'll dry it properly.""",
        },
        new()
        {
            Id = "tanned_deerhide",
            Name = @"Tanned Deerhide",
            OriginBiome = "forest",
            DestBiome = "mountains",
            OriginFlavor = @"The tanner's been sitting on this hide for two seasons, unwilling to sell it — says the grain and color are too fine for most buyers. A mountain scholar finally offered enough to make him reconsider.",
            DeliveryFlavor = @"You hand over the hide and the scholar unfolds it across his workbench. He examines it for a moment, then shakes his head. ""This isn't deer. Elk, maybe. The grain's too coarse."" He calls over two colleagues and they debate the matter in low voices, pointing at different sections of the leather. Eventually he sighs and counts out your payment. ""It'll do for what we need.""",
        },
        new()
        {
            Id = "charcoal_sack",
            Name = @"Charcoal Sack",
            OriginBiome = "forest",
            DestBiome = "plains",
            OriginFlavor = @"The charcoal burner's nephew ran a double batch through the kiln without telling anyone — good quality, but no buyer lined up and questions about where the timber came from.",
            DeliveryFlavor = @"The baker's already untying the sack before you've set it down. He scoops a handful, crushes it between his fingers, nods once. ""Fine enough."" He's pouring it into his bread oven while counting out your payment with his free hand, ash already dusting his knuckles.",
        },
        new()
        {
            Id = "pitch_resin_pot",
            Name = @"Pitch Resin Pot",
            OriginBiome = "forest",
            DestBiome = "swamp",
            OriginFlavor = @"The cooper's stock of pitch resin cracked and dried out after his storage shed flooded last spring. He's been buying small batches wherever he can find them until a proper shipment arrives from the coast.",
            DeliveryFlavor = @"You hand the pot to a woman who's sitting cross-legged on a stilt platform, surrounded by rows of identical clay vessels. She opens it, sniffs once, then dips two fingers in and rubs the resin between them slowly, watching how it moves. She wipes her hand on a rag that's already black with the stuff and pays you without explaining what any of it's for.",
        },
        new()
        {
            Id = "ghostcap_extract_vials",
            Name = @"Ghostcap Extract Vials",
            OriginBiome = "forest",
            DestBiome = "mountains",
            OriginFlavor = @"An herbalist's widow had no use for the vials her husband prepared before he died. She sold them rather than let them sit.",
            DeliveryFlavor = @"You hand the vials to a pale scholar who works in a windowless room lit by a single candle. She holds each one up to the flame, studying the way the extract refracts the light, and makes small satisfied sounds. She sets them in a wooden rack beside two dozen identical vials, already arranged. ""For my work on luminescence and decay,"" she says, counting out your payment. Her fingernails are stained black.",
        },
        new()
        {
            Id = "figured_heartwood_planks",
            Name = @"Figured Heartwood Planks",
            OriginBiome = "forest",
            DestBiome = "mountains",
            OriginFlavor = @"A woodwright's daughter is marrying into a mountain family, and her father cut planks from his best stock for her wedding gift — something that will last generations in her new home.",
            DeliveryFlavor = @"The bride's father-in-law meets you at the workshop door. He lifts each plank, checking the grain in the afternoon light, running his fingertips across the figured patterns. He sets them down carefully, one by one, and nods once. He counts out your payment and turns back to his bench without another word.",
        },
        new()
        {
            Id = "luck_charms",
            Name = @"Luck Charms",
            OriginBiome = "forest",
            DestBiome = "plains",
            OriginFlavor = @"A well-known trapper died last winter, and his daughter is selling off the luck charms he kept strung above his door. She says they didn't work for him.",
            DeliveryFlavor = @"You hand over the bundle and the farmhand unwraps it on the fence post. He picks through them—carved wood, knotted cord, a few painted stones—then selects two and loops them over a nail by the barn door. ""For the lambing season,"" he says. He counts out your payment and pockets the rest.",
        },
        new()
        {
            Id = "dryad_s_knot_fungus",
            Name = @"Dryad's Knot Fungus",
            OriginBiome = "forest",
            DestBiome = "swamp",
            OriginFlavor = @"A forager brought in a pouch of the fungus and wouldn't say where he got it. The herbalist who usually buys declined, so it's being sold south instead.",
            DeliveryFlavor = @"You hand over the pouch to a clerk behind a narrow desk. She opens it, pulls out a specimen, and checks it against a chart pinned to the wall. She makes a notation in a ledger, stamps a receipt twice, and slides you payment across the desk. ""We'll log it with the rest,"" she says, already looking past you to the next courier.",
        },
        new()
        {
            Id = "trail_kit_roll",
            Name = @"Trail Kit Roll",
            OriginBiome = "forest",
            DestBiome = "plains",
            OriginFlavor = @"A deep wood exile stitched the kit together from surplus without asking where it all came from. Better sold on the plains where no one checks the provenance.",
            DeliveryFlavor = @"The merchant unfolds the canvas roll and spreads it flat. His finger stops on a faded regimental mark still visible on the inner pocket. ""Where did you get this?"" You show him the contract. He folds it back up slowly, glances toward the garrison watchtower visible from his shop, and pays you in silence.",
        },
        new()
        {
            Id = "unusual_local_writings",
            Name = @"Unusual Local Writings",
            OriginBiome = "forest",
            DestBiome = "mountains",
            OriginFlavor = @"An exile scribe kept notebooks in a cipher only he understood. His widow thinks scholars might make something of them where she cannot.",
            DeliveryFlavor = @"The archivist pages through the notebooks slowly, pausing at certain marks. ""Coastal script mixed with forest cant. And this—"" She taps a margin note. ""militia movement records, I think. Twenty years old at least."" She sets them carefully in a cloth-lined box, then pours you tea without asking and pays you while it cools.",
        },
        new()
        {
            Id = "caged_songbird",
            Name = @"Caged Songbird",
            OriginBiome = "forest",
            DestBiome = "mountains",
            OriginFlavor = @"A woman from the exile camps is sending the bird to her sister — they haven't spoken in years, but the sister always loved its song.",
            DeliveryFlavor = @"The woman opens the cage door and lets the bird hop onto her finger. She watches how it moves, tilts her head to listen when it chirps twice. ""Still healthy,"" she says, almost to herself. She closes her eyes when it begins to sing, just for a moment. She pays you and carries the cage inside without another word.",
        },
        new()
        {
            Id = "unsettling_wooden_doll",
            Name = @"Unsettling Wooden Doll",
            OriginBiome = "forest",
            DestBiome = "plains",
            OriginFlavor = @"The doll was found in an abandoned exile camp deeper in than most people go. No one wanted to keep it in their home, but a collector on the plains sent word he'd pay for oddities.",
            DeliveryFlavor = @"You hand over the doll and the collector turns it slowly in his hands, examining the joints, the carved face, the way the wood grain runs against the expression. He holds it at arm's length, tilts his head. ""The proportions are wrong on purpose,"" he says, more to himself than to you. He wraps it in cloth without looking at it again and counts out your payment.",
        },
        new()
        {
            Id = "legion_standard_banner",
            Name = @"Legion Standard Banner",
            OriginBiome = "forest",
            DestBiome = "plains",
            OriginFlavor = @"A deserter's daughter found the standard rolled in a hollow log near her father's old camp. She says he would have wanted it returned, though she never met him.",
            DeliveryFlavor = @"You present the banner at the garrison's records office. The clerk unfurls it halfway, checks the unit marking against a ledger, then makes two notations in separate books. ""Fourth company, disbanded eighteen years back. I'll need you to sign here, and here."" He slides a receipt across the desk and counts out your payment while the ink dries.",
        },
        new()
        {
            Id = "intricate_snare_mechanism",
            Name = @"Intricate Snare Mechanism",
            OriginBiome = "forest",
            DestBiome = "scrub",
            OriginFlavor = @"A trapper's grandfather built the mechanism decades ago—delicate work, bronze and spring steel, precise as a clockmaker's hand. The old man is gone now, and the trapper says he'd rather it went to someone who'd use it than let it rust in a drawer.",
            DeliveryFlavor = @"The clan hunter turns it over in her hands, testing each spring with her fingernail, watching how the trigger plate responds. ""My uncle made traps like this,"" she says quietly. ""Before the fever took him."" She sets it down carefully and gestures for you to sit. She pours tea from a worn copper pot and counts out your payment while it steeps.",
        },
        new()
        {
            Id = "bundle_of_willow_withies",
            Name = @"Bundle of Willow Withies",
            OriginBiome = "forest",
            DestBiome = "plains",
            OriginFlavor = @"The basket maker's usual supplier died last winter, and no one else in the settlement knows where the good willows grow.",
            DeliveryFlavor = @"A young apprentice meets you at the workshop door. ""For the master,"" she says, taking the bundle without unwrapping it. She tests the weight, nods, and carries it inside. You hear an older voice call from the back room asking if they're flexible. The girl returns with your payment.",
        },
        new()
        {
            Id = "one_queen_bee",
            Name = @"One Queen Bee",
            OriginBiome = "forest",
            DestBiome = "plains",
            OriginFlavor = @"The local beekeeper's prize colony swarmed two weeks ago, and a buyer in the plains needs a replacement queen before her hives collapse entirely.",
            DeliveryFlavor = @"She lifts the small wooden cage to her ear and listens, then slides the cork partway out to inspect the attendants clustered around the queen. Without a word, she walks directly to the nearest hive, pries the top off, and begins the introduction while you're still standing there. She comes back wiping propolis from her fingers and counts out your payment.",
        },
        new()
        {
            Id = "imperial_army_uniform",
            Name = @"Imperial Army Uniform",
            OriginBiome = "forest",
            DestBiome = "plains",
            OriginFlavor = @"The local beekeeper's prize colony swarmed two weeks ago, and a buyer in the plains needs a replacement queen before her hives collapse entirely.",
            DeliveryFlavor = @"The old quartermaster unrolls the bundle and appraises it carefully. ""Shemati wool. Fine stitching. Mountain Corps. Maybe 40 years old? Rough campaign, hard winters."" He frowns slightly, ""I'll see to the uniform, take your pay and be off.""",
        },
        new()
        {
            Id = "carved_antler_buttons",
            Name = @"Carved Antler Buttons",
            OriginBiome = "forest",
            DestBiome = "mountains",
            OriginFlavor = @"A tailor needs the buttons for a scholar's coat. The client has an important case coming up and wants to look sharp.",
            DeliveryFlavor = @"She spreads them on a piece of black velvet and compares them to the coat already laid out on her table. She picks four that match in tone. She holds each to the buttonhole. ""These,"" she says, setting them aside. She pays for all of them and takes the remainder for stock.",
        },
        new()
        {
            Id = "beeswax_block",
            Name = @"Beeswax Block",
            OriginBiome = "forest",
            DestBiome = "mountains",
            OriginFlavor = @"The apiary keeper says the season's turning and we need to move the surplus before the next extraction. There's a standing order with a candlemaker up in the mountains who's run low.",
            DeliveryFlavor = @"An older woman answers the door, takes the wax block, and weighs it in both hands. ""For my husband. He's at the workshop."" She calls over her shoulder in a language you don't speak, and a boy appears to carry it inside. She counts out your payment from a tin on the mantle.",
        },
        new()
        {
            Id = "packet_of_hallucinogenic_mushrooms",
            Name = @"Packet of Hallucinogenic Mushrooms",
            OriginBiome = "forest",
            DestBiome = "mountains",
            OriginFlavor = @"A forager has been gathering the right kind all season and finally has enough surplus to sell. The scholars always pay well for research material.",
            DeliveryFlavor = @"The woman who accepts the packet wears the robes of an academic, but her eyes are unfocused and her fingernails are stained dark. She opens the packet immediately and brings one cap to her nose, inhaling deeply. ""Yes. Good. These will do."" She counts out your payment twice, as if she's forgotten she already did it once.",
        },
        new()
        {
            Id = "boar_tusk_necklace",
            Name = @"Boar Tusk Necklace",
            OriginBiome = "forest",
            DestBiome = "scrub",
            OriginFlavor = @"A woodsman's daughter inherited the necklace but refuses to wear it — her father took the boar in his last hunt, and she'd rather have coin than memories.",
            DeliveryFlavor = @"You hand over the necklace at the Kesharat import station. The clerk holds it against a printed diagram, counts the tusks twice, and marks something on a form in small, precise script. He slides the necklace into a numbered bin with three others like it. ""Quota fulfilled,"" he says, and stamps your receipt without looking up from his ledger.",
        },
        new()
        {
            Id = "bundle_of_arrow_shafts",
            Name = @"Bundle of Arrow Shafts",
            OriginBiome = "forest",
            DestBiome = "scrub",
            OriginFlavor = @"A fletcher broke his lathe and has been waiting on metal parts from the plains. He's selling off what stock he has while the workshop sits idle.",
            DeliveryFlavor = @"The woman who meets you outside the compound doesn't look like an archer. She's dressed in loose desert robes, but her hands are wrong — too smooth, fingers too long. She counts the shafts twice, then snaps one in half to check the grain. ""Good,"" she says, and pays you from a purse that seems heavier than it should be. As you leave, you see her carrying the bundle toward a structure half-buried in the hillside.",
        },
        new()
        {
            Id = "fox_pelt",
            Name = @"Fox Pelt",
            OriginBiome = "forest",
            DestBiome = "scrub",
            OriginFlavor = @"A trapper sold the forest folk a dozen pelts, but local demand is light. The scrub dealers pay better for quality fox.",
            DeliveryFlavor = @"You hand over the pelt at the customs hall. The clerk unfolds it across his desk, measures the length with a marked stick, and notes the figure in his ledger. He inspects the fur for damage, finds none, and stamps three separate forms before counting out your payment in clipped silver.",
        },
        new()
        {
            Id = "cedar_oil_flask",
            Name = @"Cedar Oil Flask",
            OriginBiome = "forest",
            DestBiome = "scrub",
            OriginFlavor = @"A woman in the exile camp sold us the flask. She said it came from a locked storehouse, and the less you knew about how she got in, the better for both of you.",
            DeliveryFlavor = @"The merchant unscrews the cap and sniffs cautiously, then tips a drop onto his palm and rubs it between his fingers. His face darkens. ""This is pine resin, not cedar. Whoever sold you this didn't know the difference or didn't care."" He argues for half the agreed price, citing the error. You argue back—a contract is a contract. He finally pays in full, muttering about forest thieves.",
        },
        new()
        {
            Id = "birch_bark_scroll_case",
            Name = @"Birch Bark Scroll Case",
            OriginBiome = "forest",
            DestBiome = "scrub",
            OriginFlavor = @"A clerk at the exile camp says the case belonged to someone who left in a hurry. He won't say more, but he's eager to sell it and asks that you deliver it quickly.",
            DeliveryFlavor = @"The Kesharat official who receives it turns it over slowly, examining the birch bark with uncommon attention. He opens it, peers inside, closes it again. ""Empty,"" he says quietly, as though confirming something. He sets it on his desk among identical scroll cases — seven, maybe eight of them, all birch bark. He pays precisely and dismisses you without looking up again.",
        },
        new()
        {
            Id = "forester_s_marking_axe",
            Name = @"Forester's Marking Axe",
            OriginBiome = "forest",
            DestBiome = "scrub",
            OriginFlavor = @"A forester's workshop needs to clear out redundant stock after a gear consolidation. The local crews all use the same style now.",
            DeliveryFlavor = @"You hand the axe to a rail supervisor outside the station. He turns it over once, then walks it to a flatbed cart where a young Kesharat surveyor is loading equipment. ""For the boundary work,"" he says. The surveyor nods, straps it to his pack without comment. The supervisor counts out your payment from a lockbox on the cart.",
        },
        new()
        {
            Id = "dried_herb_bundle",
            Name = @"Dried Herb Bundle",
            OriginBiome = "forest",
            DestBiome = "swamp",
            OriginFlavor = @"A forester's widow bundled these herbs herself before she died — meadowsweet, yarrow, and something darker underneath. Her son sold them cheap and wouldn't meet your eyes when you asked what the third plant was.",
            DeliveryFlavor = @"The herbalist takes the bundle and unwinds the cord slowly, spreading the dried stems across her workbench. She picks through them with practiced fingers, then pauses over something deep in the center. ""This is kind work,"" she says quietly. ""My grandmother suffered the same way at the end."" She wraps a portion back up and presses it into your hands along with the payment. ""Brew this if you can't sleep.""",
        },
        new()
        {
            Id = "woven_bark_mat",
            Name = @"Woven Bark Mat",
            OriginBiome = "forest",
            DestBiome = "swamp",
            OriginFlavor = @"The mat was promised to a swamp merchant two weeks ago, but the weaver's hands locked up with joint-sickness and she couldn't finish until yesterday.",
            DeliveryFlavor = @"You hand the mat to a clerk who unrolls it across his desk and measures the dimensions with a notched stick. He marks figures in a ledger, compares them to a previous entry, then stamps the page twice. He slides your payment across without comment and re-rolls the mat with practiced efficiency.",
        },
        new()
        {
            Id = "acorn_flour_sack",
            Name = @"Acorn Flour Sack",
            OriginBiome = "forest",
            DestBiome = "swamp",
            OriginFlavor = @"The miller's grandmother used to grind acorns before the mill had grain contracts. The equipment still works, and someone down in the wetlands is paying.",
            DeliveryFlavor = @"The cook opens the sack and pinches a measure between her fingers, rubbing it slowly to test the grind. She wets her fingertip, dabs it in the flour, and tastes it carefully. ""Good leaching,"" she says. ""No bitterness."" She measures out payment in small coins and sets the sack beside two others already waiting.",
        },
        new()
        {
            Id = "impressive_deer_antlers",
            Name = @"Impressive Deer Antlers",
            OriginBiome = "forest",
            DestBiome = "swamp",
            OriginFlavor = @"A woodsman's trophy rack finally came down after his widow remarried. The antlers are wide and symmetrical — worth more as material than memory.",
            DeliveryFlavor = @"The knifemaker turns them over in his hands, checking the tines for cracks and the base for rot. He sets them on his workbench next to a stack of salvaged spearhead fragments. ""Good stock. I can get eight handles from these."" He counts out your payment and returns to his grinding wheel.",
        },
        new()
        {
            Id = "wooden_bowl_set",
            Name = @"Wooden Bowl Set",
            OriginBiome = "forest",
            DestBiome = "swamp",
            OriginFlavor = @"A woodworker promised a matched set to a swamp merchant three weeks ago, but the final bowl split in the oil cure and had to be replaced.",
            DeliveryFlavor = @"You hand over the wrapped bundle to a clerk in a stilted customs house. She unwraps each bowl, checks the count against a form on her desk, initials three separate lines, then stamps the receipt twice with practiced efficiency. She slides your payment across without looking up, already reaching for the next item in her queue.",
        },
        new()
        {
            Id = "root_dye_concentrate",
            Name = @"Root Dye Concentrate",
            OriginBiome = "forest",
            DestBiome = "swamp",
            OriginFlavor = @"The dye-maker's family has been using this concentrate for three generations of weddings and funerals. Her daughter convinced her to sell half the stock because they need grain more than tradition.",
            DeliveryFlavor = @"You arrive during a dye bath. A circle of Revathi women are gathered around a vat of steaming fabric, arguing in low voices about whether the color has set. The buyer uncorks your bottle, sniffs it, and tilts it so the others can see the thickness. One woman dips a reed into the concentrate and touches it to wet cloth — the stain blooms dark and fast. ""Good enough,"" the buyer says, and counts out your payment while the others return to their work.",
        },
        new()
        {
            Id = "decorated_hat",
            Name = @"Decorated Hat",
            OriginBiome = "forest",
            DestBiome = "forest",
            OriginFlavor = @"The hat was promised for a ceremony three days ago, but the dyer only just finished the featherwork this morning.",
            DeliveryFlavor = @"You hand the hat to a man sitting alone outside a cabin at the edge of the settlement. He holds it in both hands without putting it on, studying the decoration with an expression you can't read. After a long silence, he sets it on the stump beside him and counts out your payment in small coins, one at a time.",
        },
        new()
        {
            Id = "jar_of_pine_tar",
            Name = @"Jar of Pine Tar",
            OriginBiome = "forest",
            DestBiome = "forest",
            OriginFlavor = @"The rope works ran through their last batch of tar before they finished cording the winter stores. They need enough to seal two hundred fathoms by week's end.",
            DeliveryFlavor = @"You find the rope maker working in the yard, his hands already black with pitch. He uncaps the jar, dips two fingers in, and works the tar between them to test the consistency. ""Good. This'll do."" He sets the jar on a stump beside three others and counts out your payment while his apprentice hauls the next coil into position.",
        },
        new()
        {
            Id = "sanja_s_favorite_rope",
            Name = @"Sanja's Favorite Rope",
            OriginBiome = "forest",
            DestBiome = "forest",
            OriginFlavor = @"Sanja died three months back and his daughter wants the rope sent to his old partner. They worked a timber claim together before the falling-out.",
            DeliveryFlavor = @"You hand the rope to a younger man at the cabin door. He looks at it, then calls back inside. An older woodsman comes out, takes the coil without a word, and turns it over in his hands. He nods once and sets your payment on the step.",
        },
        new()
        {
            Id = "allegedly_magical_staff",
            Name = @"Allegedly Magical Staff",
            OriginBiome = "forest",
            DestBiome = "forest",
            OriginFlavor = @"A woman from the exile camps says the staff belonged to someone she cared about. She wants it in the hands of someone who still believes, even if she can't anymore.",
            DeliveryFlavor = @"The man who receives it wears no shoes and keeps his hair tied with grass. He holds the staff vertically between his palms and closes his eyes for a long moment. When he opens them, he says only, ""Such. Power."" He sets down your payment in a cloth pouch that smells faintly of smoke and turned earth.",
        },
        new()
        {
            Id = "box_of_shrill_whistles",
            Name = @"Box of Shrill Whistles",
            OriginBiome = "forest",
            DestBiome = "forest",
            OriginFlavor = @"A woodcarver made these as toys for his children, but the locals demanded he sell them. Lucky for him, we found a buyer.",
            DeliveryFlavor = @"You hand over the box and the hunter lifts the lid, counts quickly, then pulls one out and blows. The sound is wrong—too high, almost painful. ""These aren't marsh loon calls,"" she says flatly. She closes the box and stares at you for a long moment before paying.",
        },
        new()
        {
            Id = "bundle_of_tanned_rabbit_pelts",
            Name = @"Bundle of Tanned Rabbit Pelts",
            OriginBiome = "forest",
            DestBiome = "forest",
            OriginFlavor = @"A naturalist is cataloging the pelting patterns of regional rabbit populations. He requested six specimens with intact winter fur.",
            DeliveryFlavor = @"You hand over the bundle at the forest warden's station. The clerk unfolds each pelt across her desk, checks the tanning marks against a reference sheet, and copies a series of numbers into a ledger. She signs the receipt twice, stamps it with pine pitch, and slides your payment across without comment.",
        },
        new()
        {
            Id = "creepy_bark_mask",
            Name = @"Creepy Bark Mask",
            OriginBiome = "forest",
            DestBiome = "forest",
            OriginFlavor = @"Someone wants this returned to the person who carved it years ago — a debt settled, or an apology they can't make in person.",
            DeliveryFlavor = @"The woman takes the mask and turns it over in her hands. She holds it up to her face briefly, then lowers it and looks at you. She sets coins on the table and wraps the mask in oilcloth without another word.",
        },
        new()
        {
            Id = "eighty_page_list_of_grievances",
            Name = @"Eighty Page List of Grievances",
            OriginBiome = "forest",
            DestBiome = "forest",
            OriginFlavor = @"One of the exiles drafted an extensive list of personal grievances and wants it delivered by hand. Not our normal wheelhouse, but the job is here if you want it.",
            DeliveryFlavor = @"The old man flips through the pages slowly, lips moving as he counts. He stops after a few pages, then closes the bundle. ""You understand I'll need time to draft a response."" He pays you and sets the list on a stump weighted down with a rock.",
        },
    };
}
